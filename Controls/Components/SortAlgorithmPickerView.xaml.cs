namespace CollectionLib.Controls.Components;

public partial class SortAlgorithmPickerView : VerticalStackLayout
{
    public event EventHandler<int>? SelectionChanged;

    public SortAlgorithmPickerView() => InitializeComponent();

    public void Configure(IEnumerable<string> options, int selectedIndex)
    {
        AlgorithmPicker.ItemsSource = options.ToArray();
        AlgorithmPicker.SelectedIndex = selectedIndex;
    }

    private void OnSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (AlgorithmPicker.SelectedIndex >= 0) SelectionChanged?.Invoke(this, AlgorithmPicker.SelectedIndex);
    }
}