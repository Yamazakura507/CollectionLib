namespace CollectionLib.Controls.Components.Picker;

public partial class ListImagePickerView : VerticalStackLayout
{
    public event EventHandler? ChooseRequested;

    public ListImagePickerView() => InitializeComponent();

    public void Configure(ImageSource icon, bool hasImage)
    {
        PickerIcon.Source = icon;

        SetStatus(hasImage ? "Изображение выбрано" : "Изображение не выбрано");
    }

    public void SetStatus(string value) => StatusLabel.Text = value;

    private void OnChooseTapped(object? sender, TappedEventArgs e) => ChooseRequested?.Invoke(this, EventArgs.Empty);
}