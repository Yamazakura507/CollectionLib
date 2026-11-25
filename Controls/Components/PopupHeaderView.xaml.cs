namespace CollectionLib.Controls.Components;

public partial class PopupHeaderView : ContentView
{
    public PopupHeaderView() => InitializeComponent();

    public void Configure(string title, bool showBack, ImageSource? backIcon, ImageSource closeIcon)
    {
        TitleLabel.Text = title;
        BackButton.IsVisible = showBack;
        BackButton.Source = backIcon;
        CloseButton.Source = closeIcon;
    }
}
