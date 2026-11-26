namespace CollectionLib.Controls.Components.Picker;

/// <summary>
/// Provides controls for choosing a list background image and its persistence setting.
/// </summary>
public partial class ListImagePickerView : VerticalStackLayout
{
    private ImageSource? saveEnabledIcon;
    private ImageSource? saveDisabledIcon;
    private bool isSaving;

    /// <summary>
    /// Occurs when the user requests to choose a background image.
    /// </summary>
    public event EventHandler? ChooseRequested;
    /// <summary>
    /// Occurs when the user changes whether the background image should be saved.
    /// </summary>
    /// <remarks>
    /// The event argument is the requested save-enabled state.
    /// </remarks>
    public event Action<bool>? SaveToggled;

    /// <summary>
    /// Initializes the list background image picker.
    /// </summary>
    public ListImagePickerView() => InitializeComponent();

    /// <summary>
    /// Configures the picker icons, current image state, and save availability.
    /// </summary>
    /// <param name="icon">The icon displayed for the image picker.</param>
    /// <param name="hasImage">Indicates whether a background image is currently selected.</param>
    /// <param name="canSave">Indicates whether the save option is available.</param>
    /// <param name="isSavingEnabled">Indicates whether saving is currently enabled.</param>
    /// <param name="saveEnabled">The icon displayed when saving is enabled.</param>
    /// <param name="saveDisabled">The icon displayed when saving is disabled.</param>
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

    /// <summary>
    /// Sets the status text shown below the image picker.
    /// </summary>
    /// <param name="value">The status text to display.</param>
    public void SetStatus(string value) => StatusLabel.Text = value;

    /// <summary>
    /// Shows or hides the image selection surface and its status label.
    /// </summary>
    /// <param name="available">Indicates whether image selection is available.</param>
    public void SetImagePickerAvailability(bool available)
    {
        ChooseSurface.IsVisible = available;
        StatusLabel.IsVisible = available;
    }

    /// <summary>
    /// Shows or hides the save-background toggle.
    /// </summary>
    /// <param name="available">Indicates whether the save option is available.</param>
    public void SetSaveAvailability(bool available) => SaveToggleRow.IsVisible = available;

    /// <summary>
    /// Updates the save toggle icon and tooltip to reflect its current state.
    /// </summary>
    /// <param name="enabled">Indicates whether background persistence is enabled.</param>
    public void SetSaveState(bool enabled)
    {
        isSaving = enabled;

        if (enabled)
        {
            SaveToggleButton.Source = saveEnabledIcon;
            ToolTipProperties.SetText(SaveToggleButton, "Отключить сохранение фона");
        }
        else
        {
            SaveToggleButton.Source = saveDisabledIcon;
            ToolTipProperties.SetText(SaveToggleButton, "Включить сохранение фона");
        }
    }

    private void OnChooseTapped(object? sender, TappedEventArgs e) => ChooseRequested?.Invoke(this, EventArgs.Empty);

    private void OnSaveToggleClicked(object? sender, EventArgs e) => SaveToggled?.Invoke(!isSaving);
}
