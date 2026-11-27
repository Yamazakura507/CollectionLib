namespace CollectionLib.Controls.Abstract;

/// <summary>
/// XAML-defined muted text row used by collection popups.
/// </summary>
public partial class CollectionPopupTextView : ContentView
{
    /// <summary>
    /// Identifies the popup row text.
    /// </summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(CollectionPopupTextView), string.Empty);

    /// <summary>
    /// Identifies the muted text color supplied by the owning popup.
    /// </summary>
    public static readonly BindableProperty MutedColorProperty = BindableProperty.Create(
        nameof(MutedColor), typeof(Color), typeof(CollectionPopupTextView), Colors.Gray);

    /// <summary>
    /// Gets or sets the displayed text.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the color provided by the owning list's current theme.
    /// </summary>
    public Color MutedColor
    {
        get => (Color)GetValue(MutedColorProperty);
        set => SetValue(MutedColorProperty, value);
    }

    /// <summary>
    /// Initializes a popup text row.
    /// </summary>
    public CollectionPopupTextView()
    {
        InitializeComponent();
    }
}
