namespace CollectionLib.Controls.Components.Picker;

public partial class ListImagePickerView : VerticalStackLayout
{
    private ImageSource? saveEnabledIcon;
    private ImageSource? saveDisabledIcon;
    private bool isSaving;

    public event EventHandler? ChooseRequested;
    public event Action<bool>? SaveToggled;

    public ListImagePickerView() => InitializeComponent();

    public void Configure(
        ImageSource icon,
        bool hasImage,
        bool canSave,
        bool isSavingEnabled,
        ImageSource saveEnabled,
        ImageSource saveDisabled)
    {
        PickerIcon.Source = icon;
        saveEnabledIcon = saveEnabled;
        saveDisabledIcon = saveDisabled;
        SaveToggleRow.IsVisible = canSave;
        SetStatus(hasImage ? "Изображение выбрано" : "Изображение не выбрано");
        SetSaveState(isSavingEnabled);
    }

    public void SetStatus(string value) => StatusLabel.Text = value;

    public void SetImagePickerAvailability(bool available)
    {
        ChooseSurface.IsVisible = available;
        StatusLabel.IsVisible = available;
    }

    public void SetSaveAvailability(bool available) => SaveToggleRow.IsVisible = available;

    public void SetSaveState(bool enabled)
    {
        isSaving = enabled;
        SaveToggleButton.Source = enabled ? saveEnabledIcon : saveDisabledIcon;
        ToolTipProperties.SetText(
            SaveToggleButton,
            enabled ? "Отключить сохранение фона" : "Включить сохранение фона");
    }

    private void OnChooseTapped(object? sender, TappedEventArgs e)
        => ChooseRequested?.Invoke(this, EventArgs.Empty);

    private void OnSaveToggleClicked(object? sender, EventArgs e)
        => SaveToggled?.Invoke(!isSaving);
}
