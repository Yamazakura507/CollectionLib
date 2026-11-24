namespace CollectionLib.Models.Popup
{
    /// <summary>
    /// Visual state for one reusable option row in a collection-list context.
    /// </summary>
    public sealed class CollectionPopupOptionVisual
    {
        /// <summary>
        /// Gets the outer layout containing the option row and optional divider.
        /// </summary>
        public VerticalStackLayout Container { get; }
        /// <summary>
        /// Gets the grid representing the interactive option row.
        /// </summary>
        public Grid Row { get; }
        /// <summary>
        /// Gets the image displaying the option's current state or icon.
        /// </summary>
        public Image StateIcon { get; }
        /// <summary>
        /// Gets the label displaying the option text.
        /// </summary>
        public Label Label { get; }
        /// <summary>
        /// Gets the predicate that determines whether the option is available, if one was supplied.
        /// </summary>
        public Func<bool>? IsAvailable { get; }

        internal CollectionPopupOptionVisual(VerticalStackLayout container, Grid row, Image stateIcon, Label label, Func<bool>? isAvailable)
        {
            Container = container;
            Row = row;
            StateIcon = stateIcon;
            Label = label;
            IsAvailable = isAvailable;
        }

        /// <summary>
        /// Re-evaluates the availability predicate and updates the option container visibility.
        /// </summary>
        public void RefreshVisibility() => Container.IsVisible = IsAvailable?.Invoke() ?? true;

        /// <summary>
        /// Replaces the image displayed by the option's state icon.
        /// </summary>
        /// <param name="source">New image source.</param>
        public void SetIcon(ImageSource source) => StateIcon.Source = source;
    }
}
