using System.ComponentModel;
using GroupLib.Models;
using SortLib.Models;
using WhereLib.Models;
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using ResourcesLib.Resolvers;
using WhereLib.Enums;
using CollectionLib.Controls.Components.Search;

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
    private readonly Dictionary<TSearchParameter, Color> searchParameterColors = [];
    private static readonly Color[] SearchParameterPalette = CollectionListColors.GetPalette(
        "SearchAccent1", "SearchAccent2", "SearchAccent3",
        "SearchAccent4", "SearchAccent5", "SearchAccent6");

    /// <summary>
    /// Gets or sets whether the search panel is currently visible.
    /// </summary>
    protected bool isSearchVisible;
    /// <summary>
    /// Gets or sets whether the search settings popup is currently open.
    /// </summary>
    protected bool isSearchSettingsOpen;
    private bool isSearchActionPressed;
    private bool isProgrammaticSearchHide;

    /// <summary>
    /// Provides additional resource keys to try when resolving a shared collection-list icon.
    /// </summary>
    /// <param name="resourceKey">The primary icon resource key.</param>
    /// <returns>The resource keys to try in addition to the default dictionary.</returns>
    protected virtual IEnumerable<string> GetAdditionalImageResourceKeys(string resourceKey)
    {
        yield return resourceKey;
    }

    /// <summary>
    /// Resolves an icon from the collection-list resources and configured fallback sources.
    /// </summary>
    /// <param name="resourceKey">The resource key of the requested icon.</param>
    /// <param name="defaultFileName">The fallback image file name.</param>
    /// <returns>The resolved image source.</returns>
    protected ImageSource GetIcon(string resourceKey, string defaultFileName) => ImageResourceResolver.Get(
            resourceKey,
            defaultFileName,
            CollectionListDefaultResourceDictionary.Instance,
            GetAdditionalImageResourceKeys(resourceKey));

    /// <summary>
    /// Creates a consistently styled icon button for collection-list actions.
    /// </summary>
    /// <param name="resourceKey">The resource key of the button icon.</param>
    /// <param name="defaultFileName">The fallback image file name.</param>
    /// <param name="tooltip">The tooltip displayed for the button.</param>
    /// <param name="size">The requested width and height of the button.</param>
    /// <returns>The configured icon button.</returns>
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

    /// <summary>
    /// Refreshes the visible header after its title, icon, or dock-title configuration changes.
    /// </summary>
    protected override void OnHeaderConfigurationChanged() => UpdateHeader();

    /// <summary>
    /// Provides a hook for derived controls to respond to changes in the additional header area size.
    /// </summary>
    protected virtual void OnAdditionalHeaderSizeChanged() { }
    /// <summary>
    /// Provides a hook invoked when the sort action is requested.
    /// </summary>
    protected virtual void OnSortRequested() { }
    /// <summary>
    /// Provides a hook invoked when the filter action is requested.
    /// </summary>
    protected virtual void OnFilterRequested() { }
    /// <summary>
    /// Provides a hook invoked when the group action is requested.
    /// </summary>
    protected virtual void OnGroupRequested() { }
    /// <summary>
    /// Provides a hook invoked when the view and background action is requested.
    /// </summary>
    protected virtual void OnViewRequested() { }
    /// <summary>
    /// Provides a hook invoked when the search settings action is requested.
    /// </summary>
    protected virtual void OnSearchOptionsRequested() { }
    /// <summary>
    /// Provides a hook invoked when the active popup backdrop is tapped.
    /// </summary>
    protected virtual void OnPopupBackdropRequested() { }
    /// <summary>
    /// Provides a hook for updating domain-specific controls when the source collection availability changes.
    /// </summary>
    /// <param name="hasItems">Indicates whether the source collection currently contains items.</param>
    protected virtual void OnAdditionalControlsAvailabilityChanged(bool hasItems) { }

    private void RootOnSizeChanged(object? sender, EventArgs e)
    {
        UpdateHeader();
        OnAdditionalHeaderSizeChanged();
    }

    private void MessagesOnSecondaryTapped(object? sender, TappedEventArgs e) => OnCentralContextRequested(e.GetPosition(PopupLayer));

    /// <summary>
    /// Provides a hook invoked when the central context action is requested.
    /// </summary>
    /// <param name="position">The optional position of the pointer or tap within the popup layer.</param>
    protected virtual void OnCentralContextRequested(Point? position) { }

    private void ClearHistoryButtonOnClicked(object? sender, EventArgs e) => _ = ClearHistoryAsync();

    private async ValueTask ClearHistoryAsync()
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

    /// <summary>
    /// Finds the page that hosts this control, falling back to the first application window page.
    /// </summary>
    /// <returns>The hosting page, or <see langword="null"/> when no page is available.</returns>
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

    /// <summary>
    /// Gets a value indicating whether the search panel is currently visible.
    /// </summary>
    protected bool IsSearchPanelVisible => isSearchVisible;

    /// <summary>
    /// Determines whether the specified search field is available for the current collection.
    /// </summary>\n/// <param name="parameter">The search field to check.</param>
    /// <returns><see langword="true"/> when the field can be selected for searching.</returns>
    protected virtual bool IsSearchParameterAvailable(TSearchParameter parameter) => ItemsSource is not null && ItemsSource.Count > 0;

    /// <summary>
    /// Rebuilds the indicators for the search fields currently selected.
    /// </summary>
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

    /// <summary>
    /// Updates the icon associated with a popup option state key.</summary>
    /// <param name="stateKey">The key identifying the option visual.</param>
    /// <param name="resourceKey">The resource key of the state icon.</param>
    /// <param name="fileName">The fallback icon file name.</param>
    protected void SetPopupOptionState(string stateKey, string resourceKey, string fileName) => SetPopupOptionIcon(stateKey, GetIcon(resourceKey, fileName));

    /// <summary>
    /// Toggles the selected state of a search field and rebuilds the active search rules.
    /// </summary>
    /// <param name="parameter">The search field to toggle.</param>
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

    /// <summary>
    /// Toggles strict search behavior in the attached collection controller.
    /// </summary>
    protected void ToggleStrictSearch()
    {
        if (viewController is null) return;

        bool isStrict = viewController.ToggleStrictSearch();
        SetPopupOptionState("search:strict",
            isStrict ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            isStrict ? "list_state_enabled.svg" : "list_state_disabled.svg");
    }

    /// <summary>
    /// Rebuilds the search rules using the current search entry text and selected fields.
    /// </summary>
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
        SearchInputBorder.Stroke = CollectionListColors.Get("Accent");
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

    private async ValueTask RestoreSearchEntryFocusAsync()
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

    /// <summary>
    /// Synchronizes search mode, accuracy, and case-sensitivity controls with the collection settings.
    /// </summary>
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
        SearchCaseButtonBorder.Stroke = caseSensitive ? CollectionListColors.Get("Accent") : Colors.Transparent;
        SearchCaseButtonBorder.StrokeThickness = caseSensitive ? 1.25 : 1;
        SearchCaseButtonBorder.BackgroundColor = caseSensitive ? CollectionListColors.Get("CaseButtonAccentBackground") : Colors.Transparent;

        ToolTipProperties.SetText(SearchModeButton, mode == SearchMode.Contains ? "Тип поиска: Содержит" : "Тип поиска: Левенштейн");
        ToolTipProperties.SetText(SearchAccuracyButton, $"Допустимое расстояние: {accuracy}");
        ToolTipProperties.SetText(SearchCaseButtonBorder, caseSensitive ? "Регистр учитывается" : "Регистр не учитывается");
    }
}
