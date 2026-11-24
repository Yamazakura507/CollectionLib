namespace CollectionLib.Controls.Popup;

/// <summary>
/// XAML-defined visual for a tappable popup option, including its optional separator.
/// </summary>
public partial class CollectionPopupOptionView : VerticalStackLayout
{
    /// <summary>
    /// Initializes the option layout from its XAML definition.
    /// </summary>
    public CollectionPopupOptionView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the interactive option row.
    /// </summary>
    public Grid Row => OptionRow;

    /// <summary>
    /// Gets the image used for the option state.
    /// </summary>
    public Image StateIcon => OptionStateIcon;

    /// <summary>
    /// Gets the option label.
    /// </summary>
    public Label Label => OptionLabel;

    /// <summary>
    /// Gets the optional separator displayed after the row.
    /// </summary>
    public CollectionPopupDividerView DividerView => OptionDivider;

    /// <summary>
    /// Configures content and action without constructing visual elements in code.
    /// </summary>
    internal void Configure(
        string text,
        ImageSource iconSource,
        Action action,
        bool addDivider,
        Color? textColor = null,
        Color? dividerColor = null)
    {
        OptionLabel.Text = text;
        OptionStateIcon.Source = iconSource;
        OptionDivider.IsVisible = addDivider;

        if (textColor is not null) OptionLabel.TextColor = textColor;
        if (dividerColor is not null) OptionDivider.BackgroundColor = dividerColor;

        OptionRow.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(action) });
    }
}
