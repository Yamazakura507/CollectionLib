using System.ComponentModel;
using GroupLib.Models;
using SortLib.Models;
using WhereLib.Models;
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using ResourcesLib.Resolvers;
using WhereLib.Enums;
using CollectionLib.Controls.Components.Search;

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
    protected enum PopupKind
    {
        None,
        Central,
        Types,
        SearchOptions,
        Sort,
        Filter,
        Group,
        View
    }

    private readonly Dictionary<TSearchParameter, Color> searchParameterColors = [];
    private static readonly Color[] SearchParameterPalette =
    [
        Color.FromArgb("#4387C7"),
        Color.FromArgb("#269B83"),
        Color.FromArgb("#9A70C8"),
        Color.FromArgb("#C17A43"),
        Color.FromArgb("#C15E7A"),
        Color.FromArgb("#6D8E45")
    ];

    protected bool isSearchVisible;
    protected bool isSearchSettingsOpen;
    private bool isSearchActionPressed;
    private bool isProgrammaticSearchHide;

    protected virtual IEnumerable<string> GetAdditionalImageResourceKeys(string resourceKey)
    {
        yield return resourceKey;
    }

    protected ImageSource GetIcon(string resourceKey, string defaultFileName) => ImageResourceResolver.Get(
            resourceKey,
            defaultFileName,
            CollectionListDefaultResourceDictionary.Instance,
            GetAdditionalImageResourceKeys(resourceKey));

    protected ImageButton CreateIconButton(string resourceKey, string defaultFileName, string tooltip, double size = 34)
    {
        ImageButton button = new()
        {
            Source = GetIcon(resourceKey, defaultFileName),
            WidthRequest = size,
            HeightRequest = size,
            MinimumWidthRequest = size,
            MinimumHeightRequest = size,
            Padding = new Thickness(7),
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Aspect = Aspect.AspectFit
        };

        ToolTipProperties.SetText(button, tooltip);

        return button;
    }

    private void WireControlEvents()
    {
        Root.SizeChanged += RootOnSizeChanged;
        SearchButton.Clicked += SearchButtonOnClicked;
        SortButton.Clicked += (_, _) => OnSortRequested();
        FilterButton.Clicked += (_, _) => OnFilterRequested();
        GroupButton.Clicked += (_, _) => OnGroupRequested();
        ViewButton.Clicked += (_, _) => OnViewRequested();
        ClearHistoryButton.Clicked += ClearHistoryButtonOnClicked;
        SearchEntry.TextChanged += SearchEntryOnTextChanged;
        SearchEntry.Focused += SearchEntryOnFocused;
        SearchEntry.Unfocused += SearchEntryOnUnfocused;

        if (SearchEntry.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault() is { } searchEntryTap)
            searchEntryTap.Tapped += (_, e) => OnSearchOptionsRequested();

        SearchClearButton.Pressed += SearchActionButtonOnPressed;
        SearchClearButton.Clicked += SearchClearButtonOnClicked;
        SearchOptionsButton.Pressed += SearchActionButtonOnPressed;
        SearchOptionsButton.Clicked += (_, _) => OnSearchOptionsRequested();
        SearchModeButton.Pressed += SearchActionButtonOnPressed;
        SearchModeButton.Clicked += SearchModeButtonOnClicked;

        if (SearchAccuracyButton.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault() is { } accuracyTap)
            accuracyTap.Tapped += SearchAccuracyButtonOnTapped;
        if (SearchCaseButtonBorder.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault() is { } caseTap)
            caseTap.Tapped += SearchCaseButtonOnTapped;
        if (MessagesScroll.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault() is { } messagesTap)
            messagesTap.Tapped += MessagesOnSecondaryTapped;
        if (PopupLayer.Children.OfType<BoxView>().FirstOrDefault()?.GestureRecognizers.OfType<TapGestureRecognizer>().FirstOrDefault() is { } backdropTap)
            backdropTap.Tapped += (_, _) => OnPopupBackdropRequested();
    }

    private void UpdateIcons()
    {
        SearchButton.Source = GetIcon(CollectionListImageKeys.ListSearch, "list_search.svg");
        SortButton.Source = GetIcon(CollectionListImageKeys.ListSort, "list_sort.svg");
        FilterButton.Source = GetIcon(CollectionListImageKeys.ListFilter, "list_filter.svg");
        GroupButton.Source = GetIcon(CollectionListImageKeys.ListGroup, "list_group.svg");
        ViewButton.Source = GetIcon(CollectionListImageKeys.ListView, "list_view.svg");
        ClearHistoryButton.Source = GetIcon(CollectionListImageKeys.ListClearHistory, "list_clear_history.svg");
        SearchOptionsButton.Source = GetIcon(CollectionListImageKeys.ListSearchOptions, "list_search_options.svg");
        SearchClearButton.Source = GetIcon(CollectionListImageKeys.ListClose, "list_close.svg");

        ToolTipProperties.SetText(SearchButton, "Поиск");
        ToolTipProperties.SetText(SortButton, "Сортировка");
        ToolTipProperties.SetText(FilterButton, "Фильтр");
        ToolTipProperties.SetText(GroupButton, "Группировка");
        ToolTipProperties.SetText(ViewButton, "Вид и фон");
        ToolTipProperties.SetText(ClearHistoryButton, "Очистить историю");
        ToolTipProperties.SetText(SearchOptionsButton, "Параметры поиска");
        ToolTipProperties.SetText(SearchClearButton, "Очистить поисковый запрос");
    }

    private void UpdateHeader()
    {
        HeaderTitle.Text = Title;
        HeaderDockTitle.Text = DockTitleText;
        HeaderDockTitle.IsVisible = !string.IsNullOrWhiteSpace(DockTitleText);
        HeaderIcon.Source = Icon;
        HeaderIcon.IsVisible = Icon is not null;
    }

    protected override void OnHeaderConfigurationChanged() => UpdateHeader();

    protected virtual void OnAdditionalHeaderSizeChanged() { }
    protected virtual void OnSortRequested() { }
    protected virtual void OnFilterRequested() { }
    protected virtual void OnGroupRequested() { }
    protected virtual void OnViewRequested() { }
    protected virtual void OnSearchOptionsRequested() { }
    protected virtual void OnPopupBackdropRequested() { }
    protected virtual void OnAdditionalControlsAvailabilityChanged(bool hasItems) { }

    private void RootOnSizeChanged(object? sender, EventArgs e)
    {
        UpdateHeader();
        OnAdditionalHeaderSizeChanged();
    }

    private void MessagesOnSecondaryTapped(object? sender, TappedEventArgs e) => OnCentralContextRequested(e.GetPosition(PopupLayer));

    protected virtual void OnCentralContextRequested(Point? position) { }

    private void ClearHistoryButtonOnClicked(object? sender, EventArgs e) => _ = ClearHistoryAsync();

    private async Task ClearHistoryAsync()
    {
        if (ItemsSource is null || ItemsSource.Count == 0) return;

        Page? page = FindParentPage();
        bool confirmed = page is null
            || await page.DisplayAlertAsync(
                "Очистить историю",
                "Все элементы будут удалены из этой коллекции. Продолжить?",
                "Очистить",
                "Отмена");

        if (confirmed) ItemsSource.Clear();
    }

    protected Page? FindParentPage()
    {
        Element? current = this;

        while (current is not null)
        {
            if (current is Page page) return page;

            current = current.Parent;
        }

        return Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    protected bool IsSearchPanelVisible => isSearchVisible;

    protected virtual bool IsSearchParameterAvailable(TSearchParameter parameter) => ItemsSource is not null && ItemsSource.Count > 0;

    protected void RenderSearchFieldIndicators()
    {
        SearchFieldsStrip.Children.Clear();

        foreach (TSearchParameter parameter in SearchParameterValues.Where(selectedSearchParameters.Contains))
        {
            Color accent = GetSearchParameterColor(parameter);
            SearchFieldIndicatorView indicator = new();

            indicator.Configure(SearchParameterNameSelector(parameter), accent);
            SearchFieldsStrip.Children.Add(indicator);
        }
    }

    private Color GetSearchParameterColor(TSearchParameter parameter)
    {
        if (searchParameterColors.TryGetValue(parameter, out Color? cachedColor) && cachedColor is not null) return cachedColor;

        Color color = SearchParameterPalette[Random.Shared.Next(SearchParameterPalette.Length)];

        searchParameterColors.Add(parameter, color);

        return color;
    }

    protected void SetPopupOptionState(string stateKey, string resourceKey, string fileName) => SetPopupOptionIcon(stateKey, GetIcon(resourceKey, fileName));

    protected void ToggleSearchParameter(TSearchParameter parameter)
    {
        if (viewController?.ToggleSearchParameter(parameter) != true) return;

        RebuildSearchRules();
        RenderSearchFieldIndicators();

        bool selected = selectedSearchParameters.Contains(parameter);

        SetPopupOptionState($"search:{parameter}",
            selected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            selected ? "list_state_enabled.svg" : "list_state_disabled.svg");
    }

    protected void ToggleStrictSearch()
    {
        if (viewController is null) return;

        bool isStrict = viewController.ToggleStrictSearch();
        SetPopupOptionState("search:strict",
            isStrict ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            isStrict ? "list_state_enabled.svg" : "list_state_disabled.svg");
    }

    protected void RebuildSearchRules()
    {
        if (viewController is null) return;

        viewController.RebuildSearchRules(SearchEntry.Text);

        OnCollectionControllerStateChanged();
    }

    private async ValueTask ShowSearchPanelAsync()
    {
        if (isSearchVisible)
        {
            SearchEntry.Focus();

            return;
        }

        isSearchVisible = true;
        SearchClearButton.IsVisible = !string.IsNullOrEmpty(SearchEntry.Text);

        await ShowSearchPanelEffectAsync(SearchHost);

        SearchEntry.Focus();
    }

    private async ValueTask HideSearchPanelAsync()
    {
        if (!isSearchVisible || isSearchSettingsOpen) return;

        isProgrammaticSearchHide = true;
        SearchEntry.Unfocus();

        await HideSearchPanelEffectAsync(SearchHost);

        isSearchVisible = false;
        isProgrammaticSearchHide = false;
    }

    private void SearchButtonOnClicked(object? sender, EventArgs e)
    {
        if (isSearchVisible) _ = HideSearchPanelAsync();
        else _ = ShowSearchPanelAsync();
    }

    private void SearchEntryOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        SearchClearButton.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);

        RebuildSearchRules();
    }

    private void SearchClearButtonOnClicked(object? sender, EventArgs e)
    {
        isSearchActionPressed = true;
        SearchEntry.Text = string.Empty;
        SearchEntry.Focus();
        SearchClearButton.IsVisible = false;
        _ = RestoreSearchEntryFocusAsync();
    }

    private void SearchEntryOnFocused(object? sender, FocusEventArgs e)
    {
        isSearchActionPressed = false;
        SearchInputBorder.Stroke = Color.FromArgb("#5987C8");
        SearchInputBorder.StrokeThickness = 1.5;
    }

    private async void SearchEntryOnUnfocused(object? sender, FocusEventArgs e)
    {
        SearchInputBorder.StrokeThickness = 1;

        if (isProgrammaticSearchHide || isSearchSettingsOpen || isSearchActionPressed) return;

        await Task.Delay(120);

        if (!SearchEntry.IsFocused && !isSearchSettingsOpen && !isSearchActionPressed) await HideSearchPanelAsync();
    }

    private void SearchActionButtonOnPressed(object? sender, EventArgs e) => isSearchActionPressed = true;

    private async Task RestoreSearchEntryFocusAsync()
    {
        if (!isSearchVisible) return;

        SearchEntry.Focus();
        await Task.Delay(80);

        if (!SearchEntry.IsFocused && isSearchVisible) SearchEntry.Focus();

        await Task.Delay(80);

        isSearchActionPressed = false;
    }

    private void SearchModeButtonOnClicked(object? sender, EventArgs e)
    {
        if (ItemsSource is null) return;

        viewController?.SetSearchMode(ItemsSource.SearchOptions.Mode == SearchMode.Contains ? SearchMode.Levenshtein : SearchMode.Contains);
        UpdateSearchSettingsVisuals();
        _ = RestoreSearchEntryFocusAsync();
    }

    private void SearchAccuracyButtonOnTapped(object? sender, TappedEventArgs e)
    {
        if (ItemsSource is null) return;

        isSearchActionPressed = true;

        viewController?.SetMaximumLevenshteinDistance((ItemsSource.SearchOptions.MaxLevenshteinDistance + 1) % 9);
        UpdateSearchSettingsVisuals();
        _ = RestoreSearchEntryFocusAsync();
    }

    private void SearchCaseButtonOnTapped(object? sender, TappedEventArgs e)
    {
        if (ItemsSource is null) return;

        viewController?.ToggleCaseSensitivity();
        UpdateSearchSettingsVisuals();
        _ = RestoreSearchEntryFocusAsync();
    }

    protected void UpdateSearchSettingsVisuals()
    {
        SearchMode mode = ItemsSource?.SearchOptions.Mode ?? SearchMode.Contains;
        int accuracy = ItemsSource?.SearchOptions.MaxLevenshteinDistance ?? 2;
        bool caseSensitive = ItemsSource?.SearchOptions.IsCaseSensitive ?? false;

        SearchModeButton.Source = mode == SearchMode.Contains 
            ? GetIcon(CollectionListImageKeys.ListSearchModeText, "list_search_text.svg") 
            : GetIcon(CollectionListImageKeys.ListSearchModeFuzzy, "list_search_fuzzy.svg");
        SearchModeButton.Opacity = mode == SearchMode.Levenshtein ? 1 : 0.78;
        SearchAccuracyIcon.Source = GetIcon(CollectionListImageKeys.ListSearchAccuracy, "list_search_accuracy.svg");
        SearchAccuracyValue.Text = accuracy.ToString();
        SearchAccuracyButton.IsVisible = mode == SearchMode.Levenshtein;
        SearchAccuracyButton.IsEnabled = mode == SearchMode.Levenshtein;
        SearchAccuracyButton.Opacity = 1;
        SearchCaseButton.Source = GetIcon(CollectionListImageKeys.ListSearchCase, "list_search_case.svg");
        SearchCaseButton.Opacity = caseSensitive ? 1 : 0.68;
        SearchCaseButtonBorder.Stroke = caseSensitive ? Color.FromArgb("#5987C8") : Color.FromArgb("#00000000");
        SearchCaseButtonBorder.StrokeThickness = caseSensitive ? 1.25 : 1;
        SearchCaseButtonBorder.BackgroundColor = caseSensitive ? Color.FromArgb("#205987C8") : Colors.Transparent;

        ToolTipProperties.SetText(SearchModeButton, mode == SearchMode.Contains ? "Тип поиска: Содержит" : "Тип поиска: Левенштейн");
        ToolTipProperties.SetText(SearchAccuracyButton, $"Допустимое расстояние: {accuracy}");
        ToolTipProperties.SetText(SearchCaseButtonBorder, caseSensitive ? "Регистр учитывается" : "Регистр не учитывается");
    }
}
