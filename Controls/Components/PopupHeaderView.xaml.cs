namespace CollectionLib.Controls.Components;

/// <summary>
/// Displays the title and navigation controls for a collection-list popup.
/// </summary>
public partial class PopupHeaderView : ContentView
{
    /// <summary>
    /// Initializes the popup header.
    /// </summary>
    public PopupHeaderView() => InitializeComponent();

    /// <summary>
    /// Sets the popup title and configures its back and close icons.
    /// </summary>
    /// <param name="title">The text displayed in the popup header.</param>
    /// <param name="showBack">Indicates whether the back button is visible.</param>
    /// <param name="backIcon">The image used by the back button, when visible.</param>
    /// <param name="closeIcon">The image used by the close button.</param>
    public void Configure(string title, bool showBack, ImageSource? backIcon, ImageSource closeIcon)
    {
        TitleLabel.Text = title;
        BackButton.IsVisible = showBack;
        BackButton.Source = backIcon;
        CloseButton.Source = closeIcon;
    }
}
