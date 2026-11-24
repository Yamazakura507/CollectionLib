using SortLib.Enums;
using WhereLib.Enums;

namespace CollectionLib.Interfaces;

/// <summary>
/// Non-generic contract used by reusable list controls. It exposes view state and object-keyed
/// operations, while the concrete controller retains strongly typed parameter APIs.
/// </summary>
public interface ICollectionListViewController : IDisposable
{
    /// <summary>
    /// Gets the number of items in the associated source collection.
    /// </summary>
    int ItemCount { get; }
    /// <summary>
    /// Gets whether a non-empty search query is active.
    /// </summary>
    bool HasSearch { get; }
    /// <summary>
    /// Gets whether at least one filter is active.
    /// </summary>
    bool HasFilters { get; }
    /// <summary>
    /// Gets whether at least one sort rule is active.
    /// </summary>
    bool HasSorting { get; }
    /// <summary>
    /// Gets whether at least one grouping rule is active.
    /// </summary>
    bool HasGrouping { get; }

    /// <summary>
    /// Gets the current search query.
    /// </summary>
    string SearchQuery { get; }
    /// <summary>
    /// Gets the search algorithm used by the source collection.
    /// </summary>
    SearchMode SearchMode { get; }
    /// <summary>
    /// Gets the maximum Levenshtein distance used by fuzzy search.
    /// </summary>
    int MaxLevenshteinDistance { get; }
    /// <summary>
    /// Gets whether strict-search mode is enabled.
    /// </summary>
    bool IsStrictSearch { get; }
    /// <summary>
    /// Gets whether search comparisons are case-sensitive.
    /// </summary>
    bool IsCaseSensitiveSearch { get; }

    /// <summary>
    /// Determines whether the supplied search parameter is selected.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's search-parameter enum.</param>
    /// <returns><see langword="true"/> when the parameter is selected.</returns>
    bool IsSearchParameterSelected(object parameter);

    /// <summary>
    /// Toggles a search parameter without allowing the last selected parameter to be removed.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's search-parameter enum.</param>
    /// <returns><see langword="true"/> when the selection changed.</returns>
    bool ToggleSearchParameter(object parameter);

    /// <summary>
    /// Recreates the controller-owned search rules for the supplied query.
    /// </summary>
    /// <param name="query">Query text; null or whitespace clears the owned rules.</param>
    void RebuildSearchRules(string? query);

    /// <summary>
    /// Clears the active query and controller-owned search rules.
    /// </summary>
    void ClearSearch();

    /// <summary>
    /// Toggles strict-search mode.
    /// </summary>
    /// <returns>The new strict-search state.</returns>
    bool ToggleStrictSearch();

    /// <summary>
    /// Sets the search algorithm used by the source collection.
    /// </summary>
    /// <param name="mode">Search algorithm to apply.</param>
    void SetSearchMode(SearchMode mode);

    /// <summary>
    /// Toggles case-sensitive search comparisons.
    /// </summary>
    /// <returns>The new case-sensitivity state.</returns>
    bool ToggleCaseSensitivity();

    /// <summary>
    /// Sets the maximum edit distance for fuzzy search.
    /// </summary>
    /// <param name="distance">Maximum allowed Levenshtein distance.</param>
    void SetMaximumLevenshteinDistance(int distance);

    /// <summary>
    /// Gets the value of the filter associated with a parameter.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's filter-parameter enum.</param>
    /// <returns>The configured value, or null when no owned rule exists.</returns>
    object? GetFilterValue(object parameter);

    /// <summary>
    /// Creates, updates or removes the filter associated with a parameter.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's filter-parameter enum.</param>
    /// <param name="value">New filter value; null removes the controller-owned rule.</param>
    void SetFilterValue(object parameter, object? value);

    /// <summary>
    /// Cycles a nullable boolean filter through true, false and unset.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's filter-parameter enum.</param>
    /// <returns>The new filter value, or null for an unrestricted filter.</returns>
    bool? CycleBooleanFilter(object parameter);

    /// <summary>
    /// Removes all filter rules from the source collection.
    /// </summary>
    void ClearFilters();

    /// <summary>
    /// Cycles a sort parameter through disabled, ascending and descending.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's sort-parameter enum.</param>
    void CycleSort(object parameter);

    /// <summary>
    /// Determines whether sorting by a parameter is enabled.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's sort-parameter enum.</param>
    /// <returns><see langword="true"/> when the parameter has an active sort rule.</returns>
    bool IsSortEnabled(object parameter);

    /// <summary>
    /// Gets the active sort direction for a parameter.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's sort-parameter enum.</param>
    /// <returns>The sort direction, or null when the parameter is not sorted.</returns>
    SortType? GetSortDirection(object parameter);

    /// <summary>
    /// Removes all sort rules.
    /// </summary>
    void ClearSorting();

    /// <summary>
    /// Sets the sorting algorithm for active and future sort rules.
    /// </summary>
    /// <param name="kind">Sorting algorithm to apply.</param>
    void SetSortKind(SortKind kind);

    /// <summary>
    /// Moves a sort parameter and updates the active sort-rule order.
    /// </summary>
    /// <param name="oldIndex">Current zero-based parameter index.</param>
    /// <param name="newIndex">Target zero-based parameter index.</param>
    void ReorderSortParameters(int oldIndex, int newIndex);

    /// <summary>
    /// Adds or removes the grouping rule for a parameter.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's group-parameter enum.</param>
    void ToggleGroup(object parameter);

    /// <summary>
    /// Determines whether grouping by a parameter is enabled.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's group-parameter enum.</param>
    /// <returns><see langword="true"/> when the parameter has an active grouping rule.</returns>
    bool IsGroupEnabled(object parameter);

    /// <summary>
    /// Gets the hierarchy level assigned to a grouping parameter.
    /// </summary>
    /// <param name="parameter">A value of the concrete controller's group-parameter enum.</param>
    /// <returns>The zero-based group level, or null when grouping is disabled for the parameter.</returns>
    int? GetGroupLevel(object parameter);

    /// <summary>
    /// Removes all grouping rules.
    /// </summary>
    void ClearGrouping();

    /// <summary>
    /// Moves a grouping parameter and recalculates active group levels.
    /// </summary>
    /// <param name="oldIndex">Current zero-based parameter index.</param>
    /// <param name="newIndex">Target zero-based parameter index.</param>
    void ReorderGroupParameters(int oldIndex, int newIndex);

    /// <summary>
    /// Occurs when the source collection or controller state changes.
    /// </summary>
    event EventHandler? StateChanged;
}
