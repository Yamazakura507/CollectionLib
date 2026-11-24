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

namespace CollectionLib.Controls;

/// <summary>
/// Typed collection-list implementation. It owns the collection/controller lifecycle, item-view
/// caching, flat/grouped rendering and common appearance behavior. A domain control supplies only
/// its rule/enum types, parameter lists, group labels and item-view factory.
/// </summary>
public abstract partial class CollectionListView<
    TItem,
    TFilterParameter,
    TSearchParameter,
    TGroupParameter,
    TSortParameter,
    TFilterRule,
    TSearchRule,
    TGroupRule,
    TSortRule> : CollectionListViewBase
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

    protected IReadOnlyList<TSearchParameter> SearchParameterValues { get; }
    protected IReadOnlyList<TSortParameter> SortParameterValues { get; }
    protected IReadOnlyList<TFilterParameter> BooleanFilterParameterValues { get; }
    protected IReadOnlyList<TGroupParameter> GroupParameterValues { get; }

    protected Func<TSearchParameter, string> SearchParameterNameSelector { get; }
    protected Func<TSortParameter, string> SortParameterNameSelector { get; }
    protected Func<TFilterParameter, string> FilterParameterNameSelector { get; }
    protected Func<TGroupParameter, string> GroupParameterNameSelector { get; }
    protected Func<TGroupParameter, object?, string> GroupValueFormatter { get; }

    protected CollectionViewController<
        TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
        TFilterRule, TSearchRule, TGroupRule, TSortRule>? viewController;

    protected List<TSortParameter> sortParameterOrder;
    protected List<TGroupParameter> groupParameterOrder;
    protected HashSet<TSearchParameter> selectedSearchParameters;
    protected Dictionary<TFilterParameter, TFilterRule> ownedFilterRules = [];
    protected List<TSearchRule> ownedSearchRules = [];

    protected SortKind SelectedSortKind
    {
        get => viewController?.SelectedSortKind ?? SortKind.Merge;
        set => viewController?.SetSortKind(value);
    }

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

    protected override void OnCollectionSourceChanged(NotifyCollectionChangedEventArgs e)
    {
        RefreshItemViews();
        RenderItems();
        UpdateAppliedIndicators();
        OnSourceCollectionChanged();
    }

    protected override void OnFilteredItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (ItemsSource?.GroupRules.Count == 0) RenderItems();

        UpdateAppliedIndicators();
        OnFilteredCollectionChanged();
    }

    protected override void OnCollectionSourcePropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CollectionViewCollection<
                TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
                TFilterRule, TSearchRule, TGroupRule, TSortRule>.Groups)
            && ItemsSource is not null
            && (ItemsSource.GroupRules.Count > 0 || hasRenderedGroupedView))
        {
            RenderItems();
        }
    }

    protected override void OnGroupDisplayModeChanged()
    {
        RenderItems();
        base.OnGroupDisplayModeChanged();
        UpdateViewOptionStates();
    }

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
