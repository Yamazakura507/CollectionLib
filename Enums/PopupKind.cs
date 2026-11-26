namespace CollectionLib.Enums
{
    /// <summary>
    /// Identifies the popup views that can be displayed by the collection list.
    /// </summary>
    public enum PopupKind
    {
        /// <summary>
        /// No popup is currently active.
        /// </summary>
        None,
        /// <summary>
        /// The central collection actions popup.
        /// </summary>
        Central,
        /// <summary>
        /// The popup containing available item or message types.
        /// </summary>
        Types,
        /// <summary>
        /// The popup containing search field and search behavior options.
        /// </summary>
        SearchOptions,
        /// <summary>
        /// The popup containing sorting options.
        /// </summary>
        Sort,
        /// <summary>
        /// The popup containing filtering options.
        /// </summary>
        Filter,
        /// <summary>
        /// The popup containing grouping options.
        /// </summary>
        Group,
        /// <summary>
        /// The popup containing view and background options.
        /// </summary>
        View
    }
}
