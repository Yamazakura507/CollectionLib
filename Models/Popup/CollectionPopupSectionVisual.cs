namespace CollectionLib.Models.Popup
{
    /// <summary>
    /// Visual state for a collapsible section in a collection-list context.
    /// </summary>
    public sealed class CollectionPopupSectionVisual
    {
        /// <summary>
        /// Gets the outer layout containing the section header and body.
        /// </summary>
        public VerticalStackLayout Container { get; }
        /// <summary>
        /// Gets the layout containing the section's option content.
        /// </summary>
        public VerticalStackLayout Body { get; }
        /// <summary>
        /// Gets the image indicating the expanded or collapsed state.
        /// </summary>
        public Image Chevron { get; }
        /// <summary>
        /// Gets the predicate that determines whether the section is available, if one was supplied.
        /// </summary>
        public Func<bool>? IsAvailable { get; }

        internal CollectionPopupSectionVisual(VerticalStackLayout container, VerticalStackLayout body, Image chevron, Func<bool>? isAvailable)
        {
            Container = container;
            Body = body;
            Chevron = chevron;
            IsAvailable = isAvailable;
        }

        /// <summary>
        /// Re-evaluates the availability predicate and updates the section container visibility.
        /// </summary>
        public void RefreshVisibility() => Container.IsVisible = IsAvailable?.Invoke() ?? true;
    }
}
