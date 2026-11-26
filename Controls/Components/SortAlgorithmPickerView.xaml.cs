namespace CollectionLib.Controls.Components;

/// <summary>
/// Provides a picker for selecting the sorting algorithm.
/// </summary>
public partial class SortAlgorithmPickerView : VerticalStackLayout
{
    /// <summary>
    /// Occurs when the selected sorting algorithm changes.
    /// </summary>
    /// <remarks>
    /// The event argument is the selected option index.
    /// </remarks>
    public event EventHandler<int>? SelectionChanged;

    /// <summary>
    /// Initializes the sorting algorithm picker.
    /// </summary>
    public SortAlgorithmPickerView() => InitializeComponent();

    /// <summary>
    /// Sets the available sorting algorithms and the initially selected option.
    /// </summary>
    /// <param name="options">The display names shown in the picker.</param>
    /// <param name="selectedIndex">The index of the initially selected option.</param>
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