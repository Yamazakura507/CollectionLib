using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using GroupLib.Models;
using GroupLib.Services;
using SortLib.Collections;
using SortLib.Models;
using WhereLib.Models;
using WhereLib.Services;

namespace CollectionLib.Collections;

/// <summary>
/// Observable collection with sorting, filtering, text search and hierarchical grouping.
/// Domain-specific property access is supplied through selectors, so this type has no knowledge
/// of the concrete item model.
/// </summary>
/// <typeparam name="TItem">Reference-type item represented by the collection.</typeparam>
/// <typeparam name="TFilterParameter">Enum identifying filterable item properties.</typeparam>
/// <typeparam name="TSearchParameter">Enum identifying searchable item properties.</typeparam>
/// <typeparam name="TGroupParameter">Enum identifying groupable item properties.</typeparam>
/// <typeparam name="TSortParameter">Enum identifying sortable item properties.</typeparam>
/// <typeparam name="TFilterRule">Rule type used to filter items.</typeparam>
/// <typeparam name="TSearchRule">Rule type used to search items.</typeparam>
/// <typeparam name="TGroupRule">Rule type used to group items.</typeparam>
/// <typeparam name="TSortRule">Rule type used to sort items.</typeparam>
public abstract class CollectionViewCollection
    <TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter, TFilterRule, TSearchRule, TGroupRule, TSortRule>
    : SortableObservableCollection<TItem, TSortParameter, TSortRule>
    where TItem : class, INotifyPropertyChanged
    where TFilterParameter : struct, Enum
    where TSearchParameter : struct, Enum
    where TGroupParameter : struct, Enum
    where TSortParameter : struct, Enum
    where TFilterRule : FilterRule<TFilterParameter>
    where TSearchRule : SearchRule<TSearchParameter>
    where TGroupRule : GroupRule<TGroupParameter>
    where TSortRule : SortRule<TSortParameter>, new()
{
    private readonly Func<TItem, TFilterRule, bool> filterMatcher;
    private readonly Func<TItem, TSearchRule, IEnumerable<string?>?> searchValuesSelector;
    private readonly Func<TItem, TGroupParameter, object?> groupKeySelector;
    private readonly ObservableCollection<TFilterRule> filterRules = [];
    private readonly ObservableCollection<TSearchRule> searchRules = [];
    private readonly ObservableCollection<TGroupRule> groupRules = [];
    private readonly ObservableCollection<TItem> filteredItems = [];
    private readonly ReadOnlyObservableCollection<TItem> filteredItemsView;
    private readonly HashSet<TFilterRule> observedFilters = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TSearchRule> observedSearches = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<TGroupRule> observedGroups = new(ReferenceEqualityComparer.Instance);
    private bool isDisposed;
    private SearchOptions searchOptions = new();
    private IReadOnlyList<GroupNode<TItem, TGroupParameter>> groups = Array.Empty<GroupNode<TItem, TGroupParameter>>();

    /// <summary>
    /// Filter rules combined with AND by default.
    /// </summary>
    public ObservableCollection<TFilterRule> FilterRules => filterRules;
    /// <summary>
    /// Text search rules combined with OR by default.
    /// </summary>
    public ObservableCollection<TSearchRule> SearchRules => searchRules;
    /// <summary>
    /// Hierarchical grouping rules ordered by LevelIndex.
    /// </summary>
    public ObservableCollection<TGroupRule> GroupRules => groupRules;
    /// <summary>
    /// Current sorted, filtered and searched items. This view is read-only.
    /// </summary>
    public ReadOnlyObservableCollection<TItem> FilteredItems => filteredItemsView;
    /// <summary>
    /// Current hierarchical grouping snapshot.
    /// </summary>
    public IReadOnlyList<GroupNode<TItem, TGroupParameter>> Groups => groups;

    /// <summary>
    /// Shared search options. Changes automatically refresh the view.
    /// </summary>
    public SearchOptions SearchOptions
    {
        get => searchOptions;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(searchOptions, value)) return;

            searchOptions.PropertyChanged -= SearchOptionsOnPropertyChanged;
            searchOptions = value;
            searchOptions.PropertyChanged += SearchOptionsOnPropertyChanged;

            RefreshViews();
        }
    }

    /// <summary>
    /// Initializes an empty collection with domain-specific selectors.
    /// </summary>
    /// <param name="sortKeySelector">Returns the value used to compare an item for the requested sort parameter.</param>
    /// <param name="defaultSortParameter">Sort parameter applied by the base sortable collection initially.</param>
    /// <param name="filterMatcher">Determines whether an item satisfies a filter rule.</param>
    /// <param name="searchValuesSelector">Returns the text values to inspect for a search rule.</param>
    /// <param name="groupKeySelector">Returns the grouping key for an item and group parameter.</param>
    protected CollectionViewCollection(
        Func<TItem, TSortParameter, object?> sortKeySelector,
        TSortParameter defaultSortParameter,
        Func<TItem, TFilterRule, bool> filterMatcher,
        Func<TItem, TSearchRule, IEnumerable<string?>?> searchValuesSelector,
        Func<TItem, TGroupParameter, object?> groupKeySelector) : base(sortKeySelector, defaultSortParameter)
    {
        this.filterMatcher = filterMatcher ?? throw new ArgumentNullException(nameof(filterMatcher));
        this.searchValuesSelector = searchValuesSelector ?? throw new ArgumentNullException(nameof(searchValuesSelector));
        this.groupKeySelector = groupKeySelector ?? throw new ArgumentNullException(nameof(groupKeySelector));
        filteredItemsView = new ReadOnlyObservableCollection<TItem>(filteredItems);

        InitializeViewTracking();
    }

    /// <summary>
    /// Initializes the collection from an existing sequence.
    /// </summary>
    /// <param name="items">Initial items to add to the collection.</param>
    /// <param name="sortKeySelector">Returns the value used to compare an item for the requested sort parameter.</param>
    /// <param name="defaultSortParameter">Sort parameter applied by the base sortable collection initially.</param>
    /// <param name="filterMatcher">Determines whether an item satisfies a filter rule.</param>
    /// <param name="searchValuesSelector">Returns the text values to inspect for a search rule.</param>
    /// <param name="groupKeySelector">Returns the grouping key for an item and group parameter.</param>
    protected CollectionViewCollection(
        IEnumerable<TItem> items,
        Func<TItem, TSortParameter, object?> sortKeySelector,
        TSortParameter defaultSortParameter,
        Func<TItem, TFilterRule, bool> filterMatcher,
        Func<TItem, TSearchRule, IEnumerable<string?>?> searchValuesSelector,
        Func<TItem, TGroupParameter, object?> groupKeySelector) : base(items, sortKeySelector, defaultSortParameter)
    {
        this.filterMatcher = filterMatcher ?? throw new ArgumentNullException(nameof(filterMatcher));
        this.searchValuesSelector = searchValuesSelector ?? throw new ArgumentNullException(nameof(searchValuesSelector));
        this.groupKeySelector = groupKeySelector ?? throw new ArgumentNullException(nameof(groupKeySelector));
        filteredItemsView = new ReadOnlyObservableCollection<TItem>(filteredItems);

        InitializeViewTracking();
    }

    /// <summary>
    /// Releases all item, rule and search-option subscriptions owned by this collection.
    /// The collection remains readable but no longer maintains derived views.
    /// </summary>
    public override void Dispose()
    {
        if (isDisposed) return;

        isDisposed = true;

        filterRules.CollectionChanged -= FilterRulesOnCollectionChanged;
        searchRules.CollectionChanged -= SearchRulesOnCollectionChanged;
        groupRules.CollectionChanged -= GroupRulesOnCollectionChanged;
        searchOptions.PropertyChanged -= SearchOptionsOnPropertyChanged;

        foreach (TFilterRule rule in observedFilters) rule.PropertyChanged -= FilterRuleOnPropertyChanged;

        observedFilters.Clear();

        foreach (TSearchRule rule in observedSearches) rule.PropertyChanged -= SearchRuleOnPropertyChanged;

        observedSearches.Clear();

        foreach (TGroupRule rule in observedGroups) rule.PropertyChanged -= GroupRuleOnPropertyChanged;

        observedGroups.Clear();

        base.Dispose();
    }

    /// <summary>
    /// Recalculates the filtered/search view and dependent groups.
    /// </summary>
    public void RefreshViews()
    {
        if (isDisposed) return;

        IReadOnlyList<TItem> result = this.ApplyMany<TItem, TFilterParameter, TSearchParameter, TFilterRule, TSearchRule>
            (FilterRules, filterMatcher, SearchRules, searchValuesSelector, SearchOptions);

        SyncFilteredItems(result);
        RefreshGroups();
    }

    /// <summary>
    /// Rebuilds the grouping snapshot.
    /// </summary>
    public void RefreshGroups()
    {
        if (isDisposed) return;

        groups = groupRules.Count == 0 
            ? Array.Empty<GroupNode<TItem, TGroupParameter>>() 
            : FilteredItems.Group(groupRules, groupKeySelector);
        
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Groups)));
    }

    /// <summary>
    /// Refreshes filters and search after external selector state changes.
    /// </summary>
    public void RefreshWhere() => RefreshViews();

    /// <summary>
    /// Refreshes the filtered and grouped views after an external source change.
    /// Intermediate Move events raised by the base sorting operation are suppressed.
    /// </summary>
    /// <param name="e">Details of the source collection change.</param>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (!isDisposed && !IsApplyingSort && filteredItemsView is not null) RefreshViews();
    }

    /// <inheritdoc />
    protected override void OnItemPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnItemPropertyChanged(e);
        RefreshViews();
    }

    /// <inheritdoc />
    protected override void OnSortRulesChanged()
    {
        base.OnSortRulesChanged();
        RefreshViews();
    }

    private void InitializeViewTracking()
    {
        filterRules.CollectionChanged += FilterRulesOnCollectionChanged;
        searchRules.CollectionChanged += SearchRulesOnCollectionChanged;
        groupRules.CollectionChanged += GroupRulesOnCollectionChanged;
        searchOptions.PropertyChanged += SearchOptionsOnPropertyChanged;

        RefreshViews();
    }

    private void FilterRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshObserved(filterRules, observedFilters, FilterRuleOnPropertyChanged);
        RefreshViews();
    }

    private void SearchRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshObserved(searchRules, observedSearches, SearchRuleOnPropertyChanged);
        RefreshViews();
    }

    private void GroupRulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshObserved(groupRules, observedGroups, GroupRuleOnPropertyChanged);
        RefreshGroups();
    }

    private static void RefreshObserved<TRule>(
        IEnumerable<TRule> rules,
        HashSet<TRule> observed,
        PropertyChangedEventHandler handler) where TRule : class, INotifyPropertyChanged
    {
        foreach (TRule rule in observed) rule.PropertyChanged -= handler;

        observed.Clear();

        foreach (TRule rule in rules)
        {
            if (observed.Add(rule)) rule.PropertyChanged += handler;
        }
    }

    private void FilterRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void SearchRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void GroupRuleOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshGroups();

    private void SearchOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshViews();

    private void SyncFilteredItems(IReadOnlyList<TItem> desired)
    {
        for (int targetIndex = 0; targetIndex < desired.Count; targetIndex++)
        {
            if (targetIndex < filteredItems.Count && ReferenceEquals(filteredItems[targetIndex], desired[targetIndex])) continue;

            int existingIndex = IndexOfReference(desired[targetIndex], targetIndex + 1);

            if (existingIndex >= 0) filteredItems.Move(existingIndex, targetIndex);
            else filteredItems.Insert(targetIndex, desired[targetIndex]);
        }

        while (filteredItems.Count > desired.Count) filteredItems.RemoveAt(filteredItems.Count - 1);
    }

    private int IndexOfReference(TItem item, int startIndex)
    {
        for (int index = startIndex; index < filteredItems.Count; index++)
        {
            if (ReferenceEquals(filteredItems[index], item)) return index;
        }

        return -1;
    }
}
