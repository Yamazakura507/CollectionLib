using System.Collections.Specialized;
using System.ComponentModel;
using CollectionLib.Collections;
using CollectionLib.Controllers;
using CollectionLib.Enums;
using CollectionLib.Resources.ExtensionResources;
using GroupLib.Models;
using Microsoft.Maui.Controls.Shapes;
using SortLib.Enums;
using SortLib.Models;
using WhereLib.Models;

namespace CollectionLib.Controls.Abstract;

/// <summary>
/// Typed collection-list implementation. It owns the collection/controller lifecycle, item-view
/// caching, flat/grouped rendering and common appearance behavior. A domain control supplies only
/// its rule/enum types, parameter lists, group labels and item-view factory.
/// </summary>
public abstract partial class CollectionListView<
    TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter, TFilterRule, TSearchRule, TGroupRule, TSortRule> : CollectionListViewBase
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
    private readonly Dictionary<TItem, View> itemViews = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TItem> observedItems = new(ReferenceEqualityComparer.Instance);
    private bool hasRenderedGroupedView;

    /// <summary>
    /// Initializes the generic collection list and configures its available search, sort, filter, and grouping parameters.
    /// </summary>
    /// <param name="searchParameters">The search fields supported by the collection.</param>
    /// <param name="sortParameters">The sort fields supported by the collection.</param>
    /// <param name="booleanFilterParameters">The Boolean filter fields supported by the collection.</param>
    /// <param name="groupParameters">The fields that can be used to group items.</param>
    /// <param name="getSearchParameterName">Resolves a display name for a search field.</param>
    /// <param name="getSortParameterName">Resolves a display name for a sort field.</param>
    /// <param name="getFilterParameterName">Resolves a display name for a filter field.</param>
    /// <param name="getGroupParameterName">Resolves a display name for a grouping field.</param>
    /// <param name="formatGroupValue">Formats a grouping field value for display.</param>
    /// <exception cref="ArgumentNullException">A parameter collection or display-name formatter is <see langword="null"/>.</exception>
    protected CollectionListView(
        IEnumerable<TSearchParameter> searchParameters,
        IEnumerable<TSortParameter> sortParameters,
        IEnumerable<TFilterParameter> booleanFilterParameters,
        IEnumerable<TGroupParameter> groupParameters,
        Func<TSearchParameter, string> getSearchParameterName,
        Func<TSortParameter, string> getSortParameterName,
        Func<TFilterParameter, string> getFilterParameterName,
        Func<TGroupParameter, string> getGroupParameterName,
        Func<TGroupParameter, object?, string> formatGroupValue)
    {
        SearchParameterValues = searchParameters?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(searchParameters));
        SortParameterValues = sortParameters?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(sortParameters));
        BooleanFilterParameterValues = booleanFilterParameters?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(booleanFilterParameters));
        GroupParameterValues = groupParameters?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(groupParameters));
        SearchParameterNameSelector = getSearchParameterName ?? throw new ArgumentNullException(nameof(getSearchParameterName));
        SortParameterNameSelector = getSortParameterName ?? throw new ArgumentNullException(nameof(getSortParameterName));
        FilterParameterNameSelector = getFilterParameterName ?? throw new ArgumentNullException(nameof(getFilterParameterName));
        GroupParameterNameSelector = getGroupParameterName ?? throw new ArgumentNullException(nameof(getGroupParameterName));
        GroupValueFormatter = formatGroupValue ?? throw new ArgumentNullException(nameof(formatGroupValue));
        sortParameterOrder = SortParameterValues.ToList();
        groupParameterOrder = GroupParameterValues.ToList();
        selectedSearchParameters = SearchParameterValues.Take(1).ToHashSet();

        ConfigureListBackground(ListViewportBorder, ListBackgroundImage, null, MessagesScroll, MessagesHost);
        WireControlEvents();
        UpdateIcons();
        UpdateHeader();
        UpdateSearchSettingsVisuals();
    }

    /// <summary>
    /// Identifies the source collection bindable property.
    /// </summary>
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>),
            typeof(CollectionListView<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>),
            default(CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>),
            propertyChanged: OnItemsSourceChanged);

    /// <summary>
    /// Gets or sets the typed collection rendered by this list.
    /// </summary>
    public CollectionViewCollection<
        TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
        TFilterRule, TSearchRule, TGroupRule, TSortRule>? ItemsSource
    {
        get => (CollectionViewCollection<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets the distinct search fields supported by this collection list.
    /// </summary>
    protected IReadOnlyList<TSearchParameter> SearchParameterValues { get; }
    /// <summary>
    /// Gets the distinct sort fields supported by this collection list.
    /// </summary>
    protected IReadOnlyList<TSortParameter> SortParameterValues { get; }
    /// <summary>
    /// Gets the distinct Boolean filter fields supported by this collection list.
    /// </summary>
    protected IReadOnlyList<TFilterParameter> BooleanFilterParameterValues { get; }
    /// <summary>
    /// Gets the distinct grouping fields supported by this collection list.
    /// </summary>
    protected IReadOnlyList<TGroupParameter> GroupParameterValues { get; }

    /// <summary>
    /// Gets the delegate that resolves display names for search fields.
    /// </summary>
    protected Func<TSearchParameter, string> SearchParameterNameSelector { get; }
    /// <summary>
    /// Gets the delegate that resolves display names for sort fields.
    /// </summary>
    protected Func<TSortParameter, string> SortParameterNameSelector { get; }
    /// <summary>
    /// Gets the delegate that resolves display names for filter fields.
    /// </summary>
    protected Func<TFilterParameter, string> FilterParameterNameSelector { get; }
    /// <summary>
    /// Gets the delegate that resolves display names for grouping fields.
    /// </summary>
    protected Func<TGroupParameter, string> GroupParameterNameSelector { get; }
    /// <summary>
    /// Gets the delegate that formats grouping values for display.
    /// </summary>
    protected Func<TGroupParameter, object?, string> GroupValueFormatter { get; }

    /// <summary>
    /// Gets the controller attached to the current source collection, or <see langword="null"/> when no collection is assigned.
    /// </summary>
    protected CollectionViewController<
        TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
        TFilterRule, TSearchRule, TGroupRule, TSortRule>? viewController;

    /// <summary>
    /// Gets the mutable ordering of sort fields shown in the sorting options.
    /// </summary>
    protected List<TSortParameter> sortParameterOrder;
    /// <summary>
    /// Gets the mutable ordering of grouping fields used to build nested groups.
    /// </summary>
    protected List<TGroupParameter> groupParameterOrder;
    /// <summary>
    /// Gets the set of search fields currently selected for searching.
    /// </summary>
    protected HashSet<TSearchParameter> selectedSearchParameters;
    /// <summary>
    /// Gets the filter rules created and managed by this list control.
    /// </summary>
    protected Dictionary<TFilterParameter, TFilterRule> ownedFilterRules = [];
    /// <summary>
    /// Gets the search rules created and managed by this list control.
    /// </summary>
    protected List<TSearchRule> ownedSearchRules = [];

    /// <summary>
    /// Gets or sets the sorting algorithm used by the attached collection controller.
    /// </summary>
    protected SortKind SelectedSortKind
    {
        get => viewController?.SelectedSortKind ?? SortKind.Merge;
        set => viewController?.SetSortKind(value);
    }

    /// <summary>
    /// Gets or sets the selected sorting algorithm through the legacy protected member name.
    /// </summary>
    protected SortKind selectedSortKind
    {
        get => SelectedSortKind;
        set => SelectedSortKind = value;
    }

    /// <summary>
    /// Creates the domain-specific item visual (for example MessageProvider).
    /// </summary>
    protected abstract View CreateItemView(TItem item);

    /// <summary>
    /// Wraps a domain item view with optional item-specific actions.
    /// </summary>
    protected virtual View WrapItemView(TItem item, View itemView) => itemView;

    /// <summary>
    /// Called before the base replaces the current source collection.
    /// </summary>
    protected virtual void OnItemsSourceReplacing(
        CollectionViewCollection<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule>? previous,
        CollectionViewCollection<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule>? current) { }

    /// <summary>
    /// Called after the new source collection is attached and rendered.
    /// </summary>
    protected virtual void OnItemsSourceReplaced() { }

    /// <summary>
    /// Called when an item property changes; derived controls can refresh domain-specific UI.
    /// </summary>
    protected virtual void OnSourceItemPropertyChanged(TItem item, PropertyChangedEventArgs e) { }

    /// <summary>
    /// Called after the source collection changes.
    /// </summary>
    protected virtual void OnSourceCollectionChanged() { }

    /// <summary>
    /// Called after the filtered result changes.
    /// </summary>
    protected virtual void OnFilteredCollectionChanged() { }

    /// <summary>
    /// Refreshes cached item views and renders the list after the source collection changes.
    /// </summary>
    /// <param name="e">The collection change notification.</param>
    protected override void OnCollectionSourceChanged(NotifyCollectionChangedEventArgs e)
    {
        RefreshItemViews();
        RenderItems();
        UpdateAppliedIndicators();
        OnSourceCollectionChanged();
    }

    /// <summary>
    /// Renders the updated flat result and refreshes applied-option indicators after filtering.
    /// </summary>
    /// <param name="e">The filtered-item collection change notification.</param>
    protected override void OnFilteredItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (ItemsSource?.GroupRules.Count == 0) RenderItems();

        UpdateAppliedIndicators();
        OnFilteredCollectionChanged();
    }

    /// <summary>
    /// Refreshes grouped rendering when the source collection group structure changes.
    /// </summary>
    /// <param name="e">Details about the changed source-collection property.</param>
    protected override void OnCollectionSourcePropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>.Groups)
            && ItemsSource is not null
            && (ItemsSource.GroupRules.Count > 0 || hasRenderedGroupedView)) RenderItems();
    }

    /// <summary>
    /// Rebuilds grouped items using the newly selected group presentation mode.
    /// </summary>
    protected override void OnGroupDisplayModeChanged()
    {
        RenderItems();
        base.OnGroupDisplayModeChanged();
        UpdateViewOptionStates();
    }

    /// <summary>
    /// Refreshes applied-option indicators and notifies derived controls when controller state changes.
    /// </summary>
    protected override void OnViewControllerStateChanged()
    {
        base.OnViewControllerStateChanged();
        UpdateAppliedIndicators();
        OnCollectionControllerStateChanged();
    }

    /// <summary>
    /// Called after search, filter, sort, group or source state changes.
    /// </summary>
    protected virtual void OnCollectionControllerStateChanged() { }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        CollectionListView<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule> control =
            (CollectionListView<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>) bindable;

        control.SetItemsSource(
            oldValue as CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>,
            newValue as CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>);
    }

    private void SetItemsSource(
        CollectionViewCollection<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule>? previous,
        CollectionViewCollection<
            TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
            TFilterRule, TSearchRule, TGroupRule, TSortRule>? current)
    {
        OnItemsSourceReplacing(previous, current);

        if (previous is not null)
        {
            foreach (TFilterRule rule in ownedFilterRules.Values) previous.FilterRules.Remove(rule);
            foreach (TSearchRule rule in ownedSearchRules) previous.SearchRules.Remove(rule);
        }

        DetachCollectionSource();

        foreach (View view in itemViews.Values)
        {
            if (view.Parent is Layout parent) parent.Remove(view);
        }

        itemViews.Clear();

        ClearItemBackgroundSurfaces();

        sortParameterOrder = SortParameterValues.ToList();
        groupParameterOrder = GroupParameterValues.ToList();
        selectedSearchParameters = SearchParameterValues.Take(1).ToHashSet();
        ownedFilterRules = [];
        ownedSearchRules = [];
        hasRenderedGroupedView = false;
        hasSynchronizedSortParameterOrder = false;
        hasSynchronizedGroupParameterOrder = false;

        viewController = current is null ? null : AttachCollectionSource(current, SortParameterValues, GroupParameterValues, SearchParameterValues);

        if (viewController is not null)
        {
            sortParameterOrder = viewController.SortParameterOrder;
            groupParameterOrder = viewController.GroupParameterOrder;
            selectedSearchParameters = viewController.SelectedSearchParameters;
            ownedFilterRules = viewController.OwnedFilterRules;
            ownedSearchRules = viewController.OwnedSearchRules;
        }

        RefreshItemViews();
        RenderItems();
        OnItemsSourceReplaced();
    }

    private void RefreshItemViews()
    {
        if (ItemsSource is null)
        {
            foreach (TItem item in observedItems) item.PropertyChanged -= SourceItemOnPropertyChanged;

            observedItems.Clear();
            itemViews.Clear();
            ClearItemBackgroundSurfaces();

            return;
        }

        HashSet<TItem> currentItems = new(ItemsSource, ReferenceEqualityComparer.Instance);

        foreach (TItem staleItem in observedItems.Where(item => !currentItems.Contains(item)).ToList())
        {
            staleItem.PropertyChanged -= SourceItemOnPropertyChanged;
            observedItems.Remove(staleItem);
        }
        foreach (TItem currentItem in currentItems)
        {
            if (observedItems.Add(currentItem)) currentItem.PropertyChanged += SourceItemOnPropertyChanged;
        }
        foreach (TItem staleItem in itemViews.Keys.Where(item => !currentItems.Contains(item)).ToList())
        {
            View staleView = itemViews[staleItem];

            if (staleView.Parent is Layout parent) parent.Remove(staleView);

            itemViews.Remove(staleItem);

            if (staleView is Border border) UnregisterItemBackgroundSurface(border);
        }
    }

    private void SourceItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TItem item) return;
        if (Dispatcher.IsDispatchRequired) Dispatcher.Dispatch(() => OnSourceItemPropertyChanged(item, e));
        else OnSourceItemPropertyChanged(item, e);
    }

    private View GetOrCreateItemSurface(TItem item)
    {
        if (itemViews.TryGetValue(item, out View? cached)) return cached;

        View itemView = WrapItemView(item, CreateItemView(item));
        Border surface = new()
        {
            Padding = new Thickness(7, 4),
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            Content = itemView
        };

        RegisterItemBackgroundSurface(surface);
        itemViews.Add(item, surface);

        return surface;
    }

    /// <summary>
    /// Rebuilds the item host using either the filtered flat collection or its grouped hierarchy.
    /// </summary>
    protected void RenderItems()
    {
        if (MessagesHost is null || EmptyStateLabel is null) return;

        foreach (View itemView in itemViews.Values)
        {
            if (itemView.Parent is Layout parent) parent.Remove(itemView);
        }

        MessagesHost.Children.Clear();
        MessagesHost.Children.Add(EmptyStateLabel);

        if (ItemsSource is null)
        {
            hasRenderedGroupedView = false;
            EmptyStateLabel.Text = "Нет элементов";
            EmptyStateLabel.IsVisible = true;

            return;
        }

        hasRenderedGroupedView = ItemsSource.GroupRules.Count > 0;
        bool hasItems = false;

        if (hasRenderedGroupedView)
        {
            for (int index = 0; index < ItemsSource.Groups.Count; index++)
            {
                GroupNode<TItem, TGroupParameter> group = ItemsSource.Groups[index];
                Func<bool, ImageSource> getChevron = expanded => GetIcon(
                    expanded ? CollectionListImageKeys.ListChevronDown : CollectionListImageKeys.ListChevronRight,
                    expanded ? "list_chevron_down.svg" : "list_chevron_right.svg");

                MessagesHost.Children.Add(GroupDisplayMode == CollectionGroupDisplayMode.Blocks
                    ? CreateBlockGroupView(group, 0, GetOrCreateItemSurface, GroupParameterNameSelector, GroupValueFormatter, getChevron)
                    : CreateLineGroupView(group, 0, index == ItemsSource.Groups.Count - 1, 
                GetOrCreateItemSurface, GroupParameterNameSelector, GroupValueFormatter, getChevron));

                hasItems = true;
            }
        }
        else
        {
            foreach (TItem item in ItemsSource.FilteredItems)
            {
                MessagesHost.Children.Add(GetOrCreateItemSurface(item));

                hasItems = true;
            }
        }

        EmptyStateLabel.Text = ItemsSource.Count == 0 ? "Нет элементов" : "Нет элементов по выбранным условиям";
        EmptyStateLabel.IsVisible = !hasItems;
    }
}
