using System.ComponentModel;
using CollectionLib.Models;
using CollectionLib.Models.Popup;
using GroupLib.Models;
using SortLib.Models;
using WhereLib.Models;
using CollectionLib.Resources.ExtensionResources;
using CollectionLib.Controls.Components;
using CollectionLib.Controls.Components.Picker;
using CollectionLib.Enums;

namespace CollectionLib.Controls.Abstract;

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
    /// <summary>
    /// Gets the reset button currently displayed for sorting options, if present.
    /// </summary>
    protected ImageButton? clearSortButton;
    /// <summary>
    /// Gets the reset button currently displayed for filtering options, if present.
    /// </summary>
    protected ImageButton? clearFilterButton;
    /// <summary>
    /// Gets the reset button currently displayed for grouping options, if present.
    /// </summary>
    protected ImageButton? clearGroupButton;
    /// <summary>
    /// Gets the item color-picker panel currently hosted by the view popup, if present.
    /// </summary>
    protected View? itemColorPickerPanel;
    /// <summary>
    /// Gets the list color-picker panel currently hosted by the view popup, if present.
    /// </summary>
    protected View? listColorPickerPanel;
    /// <summary>
    /// Gets the list background image picker currently hosted by the view popup, if present.
    /// </summary>
    protected ListImagePickerView? listImagePickerPanel;
    /// <summary>
    /// Gets the status label associated with the list background image picker, if present.
    /// </summary>
    protected Label? listImageStatusLabel;

    /// <summary>
    /// Gets the kind of popup that is currently active.
    /// </summary>
    protected PopupKind activePopup;
    /// <summary>
    /// Gets a value indicating whether the active popup was opened from the central options popup.
    /// </summary>
    protected bool popupOpenedFromCentral;
    /// <summary>
    /// Gets or sets the most recent position used to anchor the central context popup.
    /// </summary>
    protected Point? centralContextPosition;

    /// <summary>
    /// Builds and displays the requested popup, optionally positioning it at a supplied point.
    /// </summary>
    /// <param name="kind">The popup content to display.</param>
    /// <param name="fromCentral">Indicates whether the popup was opened from the central options popup.</param>
    /// <param name="position">The optional point used to position the central popup.</param>
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

    /// <summary>
    /// Gets the localized title for the specified popup kind.
    /// </summary>
    /// <param name="kind">The popup kind whose title is requested.</param>
    /// <returns>The title displayed in the popup header.</returns>
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

    /// <summary>
    /// Provides a hook for derived controls to populate the message or item type popup.
    /// </summary>
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

    /// <summary>
    /// Refreshes popup option visibility according to the active controller state.
    /// </summary>
    protected void UpdatePopupOptionAvailability()
    {
        RefreshPopupVisualAvailability();

        if (clearSortButton is not null) clearSortButton.IsVisible = HasSorting;
        if (clearFilterButton is not null) clearFilterButton.IsVisible = HasFilters;
        if (clearGroupButton is not null) clearGroupButton.IsVisible = HasGrouping;

        OnPopupOptionAvailabilityChanged();
    }

    /// <summary>
    /// Adds a muted text message to the current popup.
    /// </summary>
    /// <param name="value">The text displayed in the popup.</param>
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

    /// <summary>
    /// Adds an icon command to the current popup.
    /// </summary>
    /// <param name="text">The command caption.</param>
    /// <param name="resourceKey">The resource key used to resolve the command icon.</param>
    /// <param name="fileName">The fallback icon file name.</param>
    /// <param name="action">The action invoked when the command is selected.</param>
    /// <param name="addDivider">Indicates whether a divider is added after the command.</param>
    protected void AddPopupButton(string text, string resourceKey, string fileName, Action action, bool addDivider = true)
    {
        PopupContent.Children.Add(CreatePopupCommand(text, GetIcon(resourceKey, fileName), action));

        if (addDivider) AddPopupDivider(PopupContent, indent: false);
    }

    /// <summary>
    /// Adds a reset icon button to the current popup.
    /// </summary>
    /// <param name="tooltip">The tooltip displayed for the button.</param>
    /// <param name="action">The action invoked when the button is selected.</param>
    /// <returns>The created reset button.</returns>
    protected ImageButton AddPopupResetButton(string tooltip, Action action)
    {
        ImageButton button = CreateIconButton(CollectionListImageKeys.ListReset, "list_reset.svg", tooltip, 34);
        button.HorizontalOptions = LayoutOptions.End;
        button.Clicked += (_, _) => action();

        PopupContent.Children.Add(button);

        return button;
    }

    /// <summary>
    /// Adds an expandable section whose content is created on demand.
    /// </summary>
    /// <param name="title">The section header text.</param>
    /// <param name="buildContent">The callback that creates the section content.</param>
    /// <param name="initiallyExpanded">Indicates whether the section starts expanded.</param>
    /// <param name="addDivider">Indicates whether a divider is added after the section.</param>
    /// <param name="isAvailable">An optional predicate that determines whether the section is available.</param>
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

    /// <summary>
    /// Adds a divider to the specified popup layout.
    /// </summary>
    /// <param name="host">The layout that receives the divider.</param>
    /// <param name="indent">Indicates whether the divider is indented to align with popup option content.</param>
    protected void AddPopupDivider(VerticalStackLayout host, bool indent = true) => CreatePopupDivider(host, indent);

    /// <summary>
    /// Adds an option section from a collection of option descriptors.
    /// </summary>
    /// <param name="title">The section header text.</param>
    /// <param name="options">The options displayed inside the section.</param>
    /// <param name="addDivider">Indicates whether a divider is added after the section.</param>
    /// <param name="isAvailable">An optional predicate that determines whether the section is available.</param>
    /// <param name="appendContent">An optional callback that appends additional custom content to the section.</param>
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

    /// <summary>
    /// Adds one option row to the specified popup layout.
    /// </summary>
    /// <param name="host">The layout that receives the option row.</param>
    /// <param name="text">The option caption.</param>
    /// <param name="iconResourceKey">The resource key used to resolve the option icon.</param>
    /// <param name="iconFileName">The fallback icon file name.</param>
    /// <param name="action">The action invoked when the option is selected.</param>
    /// <param name="addDivider">Indicates whether a divider is added after the option.</param>
    /// <param name="reorderAction">An optional callback used to reorder the option.</param>
    /// <param name="stateKey">An optional key used to update the option state indicator.</param>
    /// <param name="isAvailable">An optional predicate that determines whether the option is available.</param>
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

    /// <summary>
    /// Begins the closing animation for the active popup.
    /// </summary>
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
