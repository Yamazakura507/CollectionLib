namespace CollectionLib.Controls.Components.Search;

public partial class SearchAccuracyOptionsView : ContentView
{
    public event EventHandler<int>? AccuracyChanged;

    public SearchAccuracyOptionsView()
    {
        InitializeComponent();
    }

    public void SetAccuracy(int value)
    {
        AccuracyLabel.Text = $"Fuzzy search accuracy: {value}";
        AccuracySlider.Value = value;
    }

    private void OnAccuracyChanged(object? sender, ValueChangedEventArgs e)
    {
        int value = (int)Math.Round(e.NewValue);
        AccuracyLabel.Text = $"Fuzzy search accuracy: {value}";

        AccuracyChanged?.Invoke(this, value);
    }
}