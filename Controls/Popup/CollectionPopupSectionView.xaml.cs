namespace CollectionLib.Controls.Popup;

/// <summary>
/// XAML-defined visual for a collapsible popup section.
/// </summary>
public partial class CollectionPopupSectionView : VerticalStackLayout
{
    private Func<bool, ImageSource>? chevronSource;

    /// <summary>
    /// Initializes the section layout from its XAML definition.
    /// </summary>
    public CollectionPopupSectionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the interactive section header.
    /// </summary>
    public Grid HeaderRow => Header;

    /// <summary>
    /// Gets the layout populated by the owning list control.
    /// </summary>
    public VerticalStackLayout BodyLayout => Body;

    /// <summary>
    /// Gets the expanded/collapsed state image.
    /// </summary>
    public Image ChevronImage => Chevron;

    /// <summary>
    /// Gets the optional separator displayed after the section.
    /// </summary>
    public CollectionPopupDividerView DividerView => Divider;

    /// <summary>
    /// Configures section state and its chevron image source factory.
    /// </summary>
    internal void Configure(
        string title,
        Func<bool, ImageSource> chevronSourceFactory,
        bool initiallyExpanded,
        bool addDivider,
        Color? textColor = null,
        Color? dividerColor = null)
    {
        ArgumentNullException.ThrowIfNull(chevronSourceFactory);

        Heading.Text = title;
        chevronSource = chevronSourceFactory;
        Body.IsVisible = initiallyExpanded;
        Chevron.Source = chevronSource(initiallyExpanded);
        Divider.IsVisible = addDivider;

        if (textColor is not null) Heading.TextColor = textColor;
        if (dividerColor is not null) Divider.BackgroundColor = dividerColor;
    }

    private void HeaderOnTapped(object? sender, TappedEventArgs e)
    {
        Body.IsVisible = !Body.IsVisible;

        if (chevronSource is not null) Chevron.Source = chevronSource(Body.IsVisible);
    }
}
