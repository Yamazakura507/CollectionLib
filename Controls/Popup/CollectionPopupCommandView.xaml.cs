namespace CollectionLib.Controls.Popup;

/// <summary>
/// XAML-defined visual for a tappable popup command row.
/// </summary>
public partial class CollectionPopupCommandView : Grid
{
    /// <summary>
    /// Initializes the command row from its XAML definition.
    /// </summary>
    public CollectionPopupCommandView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Configures command content and action.
    /// </summary>
    internal void Configure(string text, ImageSource iconSource, Action action, Color? textColor = null)
    {
        CommandLabel.Text = text;
        CommandIcon.Source = iconSource;

        if (textColor is not null) CommandLabel.TextColor = textColor;

        GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(action) });
    }
}
