using System.ComponentModel;
using CollectionLib.Models;
using CollectionLib.Models.Popup;
using GroupLib.Models;
using SortLib.Models;
using WhereLib.Models;
using CollectionLib.Resources.ExtensionResources;
using CollectionLib.Controls.Components;
using CollectionLib.Controls.Components.Picker;

namespace CollectionLib.Controls;

public abstract partial class CollectionListView<
    TItem,TFilterParameter,TSearchParameter,TGroupParameter,TSortParameter,TFilterRule,TSearchRule,TGroupRule,TSortRule>
    where TItem : class, INotifyPropertyChanged
    where TFilterParameter : struct, Enum
    where TSearchParameter : struct, Enum
    where TGroupParameter : struct, Enum
    where TSortParameter : struct, Enum
    where TFilterRule : FilterRule<TFilterParameter>, new()
    where TSearchRule : SearchRule<TSearchParameter>, new()
    where TGroupRule : GroupRule<TGroupParameter>, new()
    where TSortRule : SortRule<TSortParameter>, new()
{
    protected ImageButton? clearSortButton;
    protected ImageButton? clearFilterButton;
    protected ImageButton? clearGroupButton;
    protected View? itemColorPickerPanel;
    protected View? listColorPickerPanel;
    protected ListImagePickerView? listImagePickerPanel;
    protected Label? listImageStatusLabel;

    protected PopupKind activePopup;
    protected bool popupOpenedFromCentral;
    protected Point? centralContextPosition;

    protected void ShowPopup(PopupKind kind, bool fromCentral = false, Point? position = null)
    {
        activePopup = kind;
        popupOpenedFromCentral = fromCentral;
        isSearchSettingsOpen = kind == PopupKind.SearchOptions;

        PopupContent.Children.Clear();
        ResetPopupVisuals();

        clearSortButton = null;
        clearFilterButton = null;
        clearGroupButton = null;
        itemColorPickerPanel = null;
        listColorPickerPanel = null;
        listImagePickerPanel = null;
        listImageStatusLabel = null;
        PopupPanel.WidthRequest = kind is PopupKind.SearchOptions or PopupKind.View ? 380 : 320;

        if (kind == PopupKind.Central && position is not null) centralContextPosition = position;
        else if (kind == PopupKind.Central && position is null) position = centralContextPosition;

        PositionPopup(kind, position);
        AddPopupHeader(GetPopupTitle(kind), fromCentral && kind != PopupKind.Central);

        switch (kind)
        {
            case PopupKind.Central:
                OnBuildCentralContext();
                break;
            case PopupKind.Types:
                OnBuildTypesContext();
                break;
            case PopupKind.SearchOptions:
                OnBuildSearchOptionsContext();
                break;
            case PopupKind.Sort:
                OnBuildSortContext();
                break;
            case PopupKind.Filter:
                OnBuildFilterContext();
                break;
            case PopupKind.Group:
                OnBuildGroupContext();
                break;
            case PopupKind.View:
                OnBuildViewContext();
                break;
        }

        PopupLayer.IsVisible = true;
        PopupPanel.Opacity = 0;
        PopupPanel.Scale = 0.97;

        _ = Task.WhenAll(
            PopupPanel.FadeToAsync(1, 130, Easing.CubicOut),
            PopupPanel.ScaleToAsync(1, 150, Easing.CubicOut));
    }

    protected virtual string GetPopupTitle(PopupKind kind) => kind switch
    {
        PopupKind.Central => "Действия со списком",
        PopupKind.Types => "Типы элементов",
        PopupKind.SearchOptions => "Параметры поиска",
        PopupKind.Sort => "Сортировка",
        PopupKind.Filter => "Фильтрация",
        PopupKind.Group => "Группировка",
        PopupKind.View => "Вид и фон",
        _ => string.Empty
    };

    protected virtual void OnBuildTypesContext() { }

    private void PositionPopup(PopupKind kind, Point? anchorPosition)
    {
        PopupPanel.HorizontalOptions = LayoutOptions.End;
        PopupPanel.VerticalOptions = LayoutOptions.Start;
        PopupPanel.Margin = new Thickness(0, 38, 4, 4);
        PopupPanel.MaximumHeightRequest = 480;

        if (kind != PopupKind.Central
            || anchorPosition is not Point anchor
            || PopupLayer.Width <= 0
            || PopupLayer.Height <= 0) return;

        const double edge = 8;
        double width = kind is PopupKind.SearchOptions or PopupKind.View ? 380 : 320;
        double x = Math.Clamp(anchor.X + 4, edge, Math.Max(edge, PopupLayer.Width - width - edge));
        double y = Math.Clamp(anchor.Y + 4, edge, Math.Max(edge, PopupLayer.Height - 150 - edge));
        double availableHeight = Math.Max(120, PopupLayer.Height - y - edge);

        PopupPanel.HorizontalOptions = LayoutOptions.Start;
        PopupPanel.VerticalOptions = LayoutOptions.Start;
        PopupPanel.Margin = new Thickness(x, y, 0, 0);
        PopupPanel.MaximumHeightRequest = Math.Min(480, availableHeight);
    }

    private void AddPopupHeader(string title, bool showBack)
    {
        PopupHeaderView header = new();

        header.Configure(
            title,
            showBack,
            showBack ? GetIcon(CollectionListImageKeys.ListBack, "list_back.svg") : null,
            GetIcon(CollectionListImageKeys.ListClose, "list_close.svg"));

        header.BackButton.Clicked += (_, _) => ShowPopup(PopupKind.Central);
        header.CloseButton.Clicked += (_, _) => HidePopup();

        ToolTipProperties.SetText(header.BackButton, "Назад");
        ToolTipProperties.SetText(header.CloseButton, "Закрыть");
        PopupContent.Children.Add(header);
    }

    protected void UpdatePopupOptionAvailability()
    {
        RefreshPopupVisualAvailability();

        if (clearSortButton is not null) clearSortButton.IsVisible = HasSorting;
        if (clearFilterButton is not null) clearFilterButton.IsVisible = HasFilters;
        if (clearGroupButton is not null) clearGroupButton.IsVisible = HasGrouping;

        OnPopupOptionAvailabilityChanged();
    }

    protected void AddPopupText(string value)
    {
        PopupContent.Children.Add(new Label
        {
            Text = value,
            FontSize = 11,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = PopupMutedColor
        });
    }

    protected void AddPopupButton(string text, string resourceKey, string fileName, Action action, bool addDivider = true)
    {
        PopupContent.Children.Add(CreatePopupCommand(text, GetIcon(resourceKey, fileName), action));

        if (addDivider) AddPopupDivider(PopupContent, indent: false);
    }

    protected ImageButton AddPopupResetButton(string tooltip, Action action)
    {
        ImageButton button = CreateIconButton(CollectionListImageKeys.ListReset, "list_reset.svg", tooltip, 34);
        button.HorizontalOptions = LayoutOptions.End;
        button.Clicked += (_, _) => action();

        PopupContent.Children.Add(button);

        return button;
    }

    protected void AddCollapsibleSection(
        string title,
        Action<VerticalStackLayout> buildContent,
        bool initiallyExpanded = false,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
    {
        CollectionPopupSectionVisual section = CreatePopupSection(
            title, buildContent,
            expanded => GetIcon(
                expanded ? CollectionListImageKeys.ListChevronDown : CollectionListImageKeys.ListChevronRight,
                expanded ? "list_chevron_down.svg" : "list_chevron_right.svg"),
            initiallyExpanded, addDivider, isAvailable);

        PopupContent.Children.Add(section.Container);
        PopupSectionVisuals.Add(section);
    }

    protected void AddPopupDivider(VerticalStackLayout host, bool indent = true) => CreatePopupDivider(host, indent);

    protected void AddPopupOptionSection(
        string title,
        IEnumerable<CollectionListOptionDescriptor> options,
        bool addDivider = true,
        Func<bool>? isAvailable = null,
        Action<VerticalStackLayout>? appendContent = null) => AddPopupSection(
            PopupContent,
            new CollectionListSectionDescriptor(
                title, options.ToArray(), appendContent, InitiallyExpanded: false, AddDividerAfter: addDivider, IsAvailable: isAvailable),
            expanded => GetIcon(
                expanded ? CollectionListImageKeys.ListChevronDown : CollectionListImageKeys.ListChevronRight,
                expanded ? "list_chevron_down.svg" : "list_chevron_right.svg"));

    protected void AddPopupOptionRow(
        VerticalStackLayout host,
        string text,
        string iconResourceKey,
        string iconFileName,
        Action action,
        bool addDivider = true,
        Action<int, int>? reorderAction = null,
        string? stateKey = null,
        Func<bool>? isAvailable = null) => AddPopupOption(host,
            new CollectionListOptionDescriptor(
                stateKey, text, GetIcon(iconResourceKey, iconFileName), action, addDivider, reorderAction, isAvailable));

    protected void HidePopup()
    {
        if (!PopupLayer.IsVisible) return;

        _ = ClosePopupAsync();
    }

    private async ValueTask ClosePopupAsync()
    {
        bool wasSearchSettings = isSearchSettingsOpen;

        await Task.WhenAll(
            PopupPanel.FadeToAsync(0, 90, Easing.CubicIn),
            PopupPanel.ScaleToAsync(0.97, 100, Easing.CubicIn));

        PopupLayer.IsVisible = false;
        PopupPanel.Scale = 1;
        activePopup = PopupKind.None;
        isSearchSettingsOpen = false;
        popupOpenedFromCentral = false;

        if (wasSearchSettings && isSearchVisible) Dispatcher.Dispatch(() => SearchEntry.Focus());
    }
}
