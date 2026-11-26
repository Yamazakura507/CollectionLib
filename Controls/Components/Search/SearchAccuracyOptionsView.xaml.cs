namespace CollectionLib.Controls.Components.Search;

/// <summary>
/// Provides a slider for configuring the accuracy threshold of fuzzy search.
/// </summary>
public partial class SearchAccuracyOptionsView : ContentView
{
    /// <summary>
    /// Occurs when the fuzzy search accuracy value changes.
    /// </summary>
    /// <remarks>
    /// The event argument contains the rounded accuracy value.
    /// </remarks>
    public event EventHandler<int>? AccuracyChanged;

    /// <summary>
    /// Initializes the fuzzy search accuracy control.
    /// </summary>
    public SearchAccuracyOptionsView() => InitializeComponent();

    /// <summary>
    /// Sets the displayed accuracy value and updates the slider position.
    /// </summary>
    /// <param name="value">The accuracy value to display.</param>
    public void SetAccuracy(int value)
    {
        AccuracyLabel.Text = $"Длинна неточности: {value}";
        AccuracySlider.Value = value;
    }

    private void OnAccuracyChanged(object? sender, ValueChangedEventArgs e)
    {
        int value = (int)Math.Round(e.NewValue);

        AccuracyLabel.Text = $"Длинна ошибки: {value}";

        AccuracyChanged?.Invoke(this, value);
    }
}