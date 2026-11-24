using System.Collections.Specialized;
using System.ComponentModel;
using CollectionLib.Collections;
using CollectionLib.Interfaces;
using GroupLib.Models;
using SortLib.Enums;
using SortLib.Models;
using WhereLib.Enums;
using WhereLib.Models;

namespace CollectionLib.Controllers;

/// <summary>
/// Reusable state and rule orchestration for a collection-backed list. It contains no domain
/// knowledge: parameter enums and rule types are supplied by the concrete collection.
/// </summary>
/// <typeparam name="TItem">The item type stored in the source collection.</typeparam>
/// <typeparam name="TFilterParameter">Enum identifying filterable item properties.</typeparam>
/// <typeparam name="TSearchParameter">Enum identifying searchable item properties.</typeparam>
/// <typeparam name="TGroupParameter">Enum identifying grouping properties.</typeparam>
/// <typeparam name="TSortParameter">Enum identifying sortable item properties.</typeparam>
/// <typeparam name="TFilterRule">Rule type used to filter source items.</typeparam>
/// <typeparam name="TSearchRule">Rule type used to search source items.</typeparam>
/// <typeparam name="TGroupRule">Rule type used to group source items.</typeparam>
/// <typeparam name="TSortRule">Rule type used to sort source items.</typeparam>
public sealed class CollectionViewController
    <TItem,TFilterParameter,TSearchParameter,TGroupParameter,TSortParameter,TFilterRule,TSearchRule,TGroupRule,TSortRule> 
    : ICollectionListViewController
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
    private readonly CollectionViewCollection
        <TItem,TFilterParameter,TSearchParameter,TGroupParameter,TSortParameter,TFilterRule,TSearchRule,TGroupRule,TSortRule> source;

    private bool disposed;

    /// <summary>
    /// Gets the user-configurable order of sort parameters.
    /// </summary>
    public List<TSortParameter> SortParameterOrder { get; }
    /// <summary>
    /// Gets the user-configurable order of grouping parameters.
    /// </summary>
    public List<TGroupParameter> GroupParameterOrder { get; }
    /// <summary>
    /// Gets the order in which searchable fields are considered when search rules are rebuilt.
    /// </summary>
    public List<TSearchParameter> SearchParameterOrder { get; }
    /// <summary>
    /// Gets the set of searchable fields currently included in the query.
    /// </summary>
    public HashSet<TSearchParameter> SelectedSearchParameters { get; } = [];
    /// <summary>
    /// Gets the filter rules created and owned by this controller, indexed by parameter.
    /// </summary>
    public Dictionary<TFilterParameter, TFilterRule> OwnedFilterRules { get; } = [];
    /// <summary>
    /// Gets the search rules created and owned by this controller.
    /// </summary>
    public List<TSearchRule> OwnedSearchRules { get; } = [];

    /// <summary>
    /// Gets the sorting algorithm applied to rules created by this controller.
    /// </summary>
    public SortKind SelectedSortKind { get; private set; } = SortKind.Merge;
    /// <inheritdoc/>
    public string SearchQuery { get; private set; } = string.Empty;
    /// <inheritdoc/>
    public SearchMode SearchMode => source.SearchOptions.Mode;
    /// <inheritdoc/>
    public int MaxLevenshteinDistance => source.SearchOptions.MaxLevenshteinDistance;
    /// <inheritdoc/>
    public bool IsStrictSearch => source.SearchOptions.IsStrict;
    /// <inheritdoc/>
    public bool IsCaseSensitiveSearch => source.SearchOptions.IsCaseSensitive;

    /// <inheritdoc/>
    public int ItemCount => source.Count;
    /// <inheritdoc/>
    public bool HasSearch => source.SearchRules.Any(rule => !string.IsNullOrWhiteSpace(rule.SearchText));
    /// <inheritdoc/>
    public bool HasFilters => source.FilterRules.Count > 0;
    /// <inheritdoc/>
    public bool HasSorting => source.SortRules.Count > 0;
    /// <inheritdoc/>
    public bool HasGrouping => source.GroupRules.Count > 0;

    /// <inheritdoc/>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Creates a controller for a collection and initializes the available parameter orders.
    /// </summary>
    /// <param name="source">Collection whose search, filter, sort and group rules are controlled.</param>
    /// <param name="sortParameters">Sortable parameters in their initial priority order.</param>
    /// <param name="groupParameters">Groupable parameters in their initial priority order.</param>
    /// <param name="searchParameters">Searchable parameters in their initial display order.</param>
    public CollectionViewController(
        CollectionViewCollection
        <TItem,TFilterParameter,TSearchParameter,TGroupParameter,TSortParameter,TFilterRule,TSearchRule,TGroupRule,TSortRule> source,
        IEnumerable<TSortParameter> sortParameters,
        IEnumerable<TGroupParameter> groupParameters,
        IEnumerable<TSearchParameter> searchParameters)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));

        SortParameterOrder = sortParameters?.Distinct().ToList() ?? throw new ArgumentNullException(nameof(sortParameters));
        GroupParameterOrder = groupParameters?.Distinct().ToList() ?? throw new ArgumentNullException(nameof(groupParameters));
        SearchParameterOrder = searchParameters?.Distinct().ToList() ?? throw new ArgumentNullException(nameof(searchParameters));

        if (SearchParameterOrder.Count > 0) SelectedSearchParameters.Add(SearchParameterOrder[0]);

        source.CollectionChanged += SourceOnCollectionChanged;
        source.FilterRules.CollectionChanged += RulesOnCollectionChanged;
        source.SearchRules.CollectionChanged += RulesOnCollectionChanged;
        source.SortRules.CollectionChanged += RulesOnCollectionChanged;
        source.GroupRules.CollectionChanged += RulesOnCollectionChanged;
        source.SearchOptions.PropertyChanged += SearchOptionsOnPropertyChanged;
    }

    /// <inheritdoc/>
    public bool IsSearchParameterSelected(TSearchParameter parameter) => SelectedSearchParameters.Contains(parameter);

    /// <inheritdoc/>
    public bool ToggleSearchParameter(TSearchParameter parameter)
    {
        if (SelectedSearchParameters.Contains(parameter))
        {
            if (SelectedSearchParameters.Count <= 1) return false;

            SelectedSearchParameters.Remove(parameter);
        }
        else
        {
            SelectedSearchParameters.Add(parameter);
        }

        RaiseStateChanged();

        return true;
    }

    /// <inheritdoc/>
    public void RebuildSearchRules(string? query)
    {
        SearchQuery = query?.Trim() ?? string.Empty;

        foreach (TSearchRule rule in OwnedSearchRules) source.SearchRules.Remove(rule);

        OwnedSearchRules.Clear();

        if (SearchQuery.Length > 0)
        {
            foreach (TSearchParameter parameter in SearchParameterOrder.Where(SelectedSearchParameters.Contains))
            {
                TSearchRule rule = new()
                {
                    WhereParametr = parameter,
                    SearchText = SearchQuery
                };

                source.SearchRules.Add(rule);
                OwnedSearchRules.Add(rule);
            }
        }

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public void ClearSearch() => RebuildSearchRules(string.Empty);

    /// <inheritdoc/>
    public bool ToggleStrictSearch()
    {
        source.SearchOptions.IsStrict = !source.SearchOptions.IsStrict;

        return source.SearchOptions.IsStrict;
    }

    /// <inheritdoc/>
    public void SetSearchMode(WhereLib.Enums.SearchMode mode) => source.SearchOptions.Mode = mode;

    /// <inheritdoc/>
    public bool ToggleCaseSensitivity()
    {
        source.SearchOptions.IsCaseSensitive = !source.SearchOptions.IsCaseSensitive;

        return source.SearchOptions.IsCaseSensitive;
    }

    /// <inheritdoc/>
    public void SetMaximumLevenshteinDistance(int distance) => source.SearchOptions.MaxLevenshteinDistance = distance;

    /// <inheritdoc/>
    public void SetFilterValue(TFilterParameter parameter, object? value)
    {
        if (value is null)
        {
            if (OwnedFilterRules.Remove(parameter, out TFilterRule? previous)) source.FilterRules.Remove(previous);

            RaiseStateChanged();

            return;
        }

        if (!OwnedFilterRules.TryGetValue(parameter, out TFilterRule? rule))
        {
            rule = new TFilterRule
            {
                WhereParametr = parameter,
                Value = value
            };

            OwnedFilterRules.Add(parameter, rule);
            source.FilterRules.Add(rule);
        }
        else
        {
            rule.Value = value;
        }

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public object? GetFilterValue(TFilterParameter parameter) => OwnedFilterRules.TryGetValue(parameter, out TFilterRule? rule) ? rule.Value : null;

    /// <summary>
    /// Gets a boolean filter value, or null when no boolean value is configured.
    /// </summary>
    /// <param name="parameter">Filter parameter to inspect.</param>
    /// <returns><see langword="true"/>, <see langword="false"/> or null when unset or non-boolean.</returns>
    public bool? GetBooleanFilterValue(TFilterParameter parameter) => GetFilterValue(parameter) is bool value ? value : null;

    /// <inheritdoc/>
    public bool? CycleBooleanFilter(TFilterParameter parameter)
    {
        bool? current = GetBooleanFilterValue(parameter);
        bool? next = current is null ? true : current.Value ? false : null;

        SetFilterValue(parameter, next);

        return next;
    }

    /// <inheritdoc/>
    public void ClearFilters()
    {
        source.FilterRules.Clear();
        OwnedFilterRules.Clear();

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public void CycleSort(TSortParameter parameter)
    {
        TSortRule? rule = source.SortRules.FirstOrDefault(item => EqualityComparer<TSortParameter>.Default.Equals(item.SortParametr, parameter));
        
        if (rule is null)
        {
            TSortRule newRule = new()
            {
                SortParametr = parameter,
                SortType = SortType.Ascending,
                SortKind = SelectedSortKind
            };

            int parameterPriority = SortParameterOrder.IndexOf(parameter);
            int insertIndex = source.SortRules.Count;

            for (int index = 0; index < source.SortRules.Count; index++)
            {
                int existingPriority = SortParameterOrder.IndexOf(source.SortRules[index].SortParametr);

                if (existingPriority > parameterPriority)
                {
                    insertIndex = index;

                    break;
                }
            }

            source.SortRules.Insert(insertIndex, newRule);
        }
        else if (rule.SortType == SortType.Ascending)
        {
            rule.SortType = SortType.Descending;
        }
        else
        {
            source.SortRules.Remove(rule);
        }

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public void ClearSorting()
    {
        source.SortRules.Clear();

        SelectedSortKind = SortKind.Merge;

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public void SetSortKind(SortKind kind)
    {
        SelectedSortKind = kind;

        foreach (TSortRule rule in source.SortRules) rule.SortKind = kind;

        RaiseStateChanged();
    }

    /// <summary>
    /// Reads the currently active sort algorithm without changing existing rules.
    /// </summary>
    public void SynchronizeSelectedSortKind()
    {
        TSortRule? firstRule = source.SortRules.FirstOrDefault();

        if (firstRule is not null) SelectedSortKind = firstRule.SortKind;
    }

    /// <summary>
    /// Aligns the parameter order with the active sort-rule order, preserving inactive parameters afterwards.
    /// </summary>
    public void SynchronizeSortParameterOrder()
    {
        List<TSortParameter> activeOrder = source.SortRules
            .Select(rule => rule.SortParametr)
            .Where(SortParameterOrder.Contains)
            .Distinct()
            .ToList();

        if (activeOrder.Count == 0) return;

        ReplaceOrder(SortParameterOrder, activeOrder.Concat(SortParameterOrder.Where(parameter => !activeOrder.Contains(parameter))));
    }

    /// <inheritdoc/>
    public void ReorderSortParameters(int oldIndex, int newIndex)
    {
        MoveItem(SortParameterOrder, oldIndex, newIndex);

        List<TSortRule> desiredRules = source.SortRules
            .OrderBy(rule =>
            {
                int index = SortParameterOrder.IndexOf(rule.SortParametr);

                return index < 0 ? int.MaxValue : index;
            })
            .ToList();

        for (int targetIndex = 0; targetIndex < desiredRules.Count; targetIndex++)
        {
            int currentIndex = source.SortRules.IndexOf(desiredRules[targetIndex]);

            if (currentIndex >= 0 && currentIndex != targetIndex) source.SortRules.Move(currentIndex, targetIndex);
        }

        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public bool IsSortEnabled(TSortParameter parameter) => source.SortRules.Any(rule => EqualityComparer<TSortParameter>.Default.Equals(rule.SortParametr, parameter));

    /// <inheritdoc/>
    public SortType? GetSortDirection(TSortParameter parameter) => source.SortRules.FirstOrDefault(rule => EqualityComparer<TSortParameter>.Default.Equals(rule.SortParametr, parameter))?.SortType;

    /// <inheritdoc/>
    public void ToggleGroup(TGroupParameter parameter)
    {
        TGroupRule? existing = source.GroupRules.FirstOrDefault(rule => EqualityComparer<TGroupParameter>.Default.Equals(rule.GroupParametr, parameter));
        
        if (existing is not null)
        {
            source.GroupRules.Remove(existing);
        }
        else
        {
            source.GroupRules.Add(new TGroupRule
            {
                GroupParametr = parameter,
                LevelIndex = GroupParameterOrder.IndexOf(parameter)
            });
        }

        NormalizeGroupLevels();
        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public void ClearGrouping()
    {
        source.GroupRules.Clear();

        RaiseStateChanged();
    }

    /// <summary>
    /// Aligns the parameter order with active group levels, preserving inactive parameters afterwards.
    /// </summary>
    public void SynchronizeGroupParameterOrder()
    {
        List<TGroupParameter> activeOrder = source.GroupRules
            .OrderBy(rule => rule.LevelIndex ?? int.MaxValue)
            .Select(rule => rule.GroupParametr)
            .Where(GroupParameterOrder.Contains)
            .Distinct()
            .ToList();

        if (activeOrder.Count == 0) return;

        ReplaceOrder(GroupParameterOrder, activeOrder.Concat(GroupParameterOrder.Where(parameter => !activeOrder.Contains(parameter))));
    }

    /// <inheritdoc/>
    public void ReorderGroupParameters(int oldIndex, int newIndex)
    {
        MoveItem(GroupParameterOrder, oldIndex, newIndex);
        NormalizeGroupLevels();
        RaiseStateChanged();
    }

    /// <inheritdoc/>
    public bool IsGroupEnabled(TGroupParameter parameter) => source.GroupRules.Any(rule => EqualityComparer<TGroupParameter>.Default.Equals(rule.GroupParametr, parameter));

    /// <inheritdoc/>
    public int? GetGroupLevel(TGroupParameter parameter) => source.GroupRules.FirstOrDefault(rule => EqualityComparer<TGroupParameter>.Default.Equals(rule.GroupParametr, parameter))?.LevelIndex;

    /// <summary>
    /// Reassigns contiguous group levels to match <see cref="GroupParameterOrder"/>.
    /// </summary>
    public void NormalizeGroupLevels()
    {
        List<TGroupRule> orderedRules = source.GroupRules.OrderBy(rule => GroupParameterOrder.IndexOf(rule.GroupParametr)).ToList();

        for (int index = 0; index < orderedRules.Count; index++)
        {
            if (orderedRules[index].LevelIndex != index) orderedRules[index].LevelIndex = index;
        }  
    }

    bool ICollectionListViewController.IsSearchParameterSelected(object parameter)
        => IsSearchParameterSelected(ReadParameter<TSearchParameter>(parameter, nameof(parameter)));

    bool ICollectionListViewController.ToggleSearchParameter(object parameter)
        => ToggleSearchParameter(ReadParameter<TSearchParameter>(parameter, nameof(parameter)));

    object? ICollectionListViewController.GetFilterValue(object parameter)
        => GetFilterValue(ReadParameter<TFilterParameter>(parameter, nameof(parameter)));

    void ICollectionListViewController.SetFilterValue(object parameter, object? value)
        => SetFilterValue(ReadParameter<TFilterParameter>(parameter, nameof(parameter)), value);

    bool? ICollectionListViewController.CycleBooleanFilter(object parameter)
        => CycleBooleanFilter(ReadParameter<TFilterParameter>(parameter, nameof(parameter)));

    void ICollectionListViewController.CycleSort(object parameter)
        => CycleSort(ReadParameter<TSortParameter>(parameter, nameof(parameter)));

    bool ICollectionListViewController.IsSortEnabled(object parameter)
        => IsSortEnabled(ReadParameter<TSortParameter>(parameter, nameof(parameter)));

    SortType? ICollectionListViewController.GetSortDirection(object parameter)
        => GetSortDirection(ReadParameter<TSortParameter>(parameter, nameof(parameter)));

    void ICollectionListViewController.ToggleGroup(object parameter)
        => ToggleGroup(ReadParameter<TGroupParameter>(parameter, nameof(parameter)));

    bool ICollectionListViewController.IsGroupEnabled(object parameter)
        => IsGroupEnabled(ReadParameter<TGroupParameter>(parameter, nameof(parameter)));

    int? ICollectionListViewController.GetGroupLevel(object parameter)
        => GetGroupLevel(ReadParameter<TGroupParameter>(parameter, nameof(parameter)));

    private static TParameter ReadParameter<TParameter>(object parameter, string parameterName) where TParameter : struct, Enum
    {
        if (parameter is TParameter typed) return typed;

        throw new ArgumentException($"Parameter must be a value of type {typeof(TParameter).Name}.", parameterName);
    }

    private static void ReplaceOrder<T>(List<T> target, IEnumerable<T> desired)
    {
        target.Clear();
        target.AddRange(desired);
    }

    private static void MoveItem<T>(IList<T> items, int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex || oldIndex < 0 || newIndex < 0 || oldIndex >= items.Count || newIndex >= items.Count) return;

        T item = items[oldIndex];

        items.RemoveAt(oldIndex);
        items.Insert(newIndex, item);
    }

    private void SourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RaiseStateChanged();

    private void RulesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RaiseStateChanged();

    private void SearchOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RaiseStateChanged();

    private void RaiseStateChanged()
    {
        if (!disposed) StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Detaches collection and rule event handlers and prevents further state notifications.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        source.CollectionChanged -= SourceOnCollectionChanged;
        source.FilterRules.CollectionChanged -= RulesOnCollectionChanged;
        source.SearchRules.CollectionChanged -= RulesOnCollectionChanged;
        source.SortRules.CollectionChanged -= RulesOnCollectionChanged;
        source.GroupRules.CollectionChanged -= RulesOnCollectionChanged;
        source.SearchOptions.PropertyChanged -= SearchOptionsOnPropertyChanged;
        StateChanged = null;
    }
}
