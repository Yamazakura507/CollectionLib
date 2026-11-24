namespace CollectionLib.Controls.Popup;

/// <summary>
/// XAML-defined popup separator with optional alignment beneath the option label.
/// </summary>
public partial class CollectionPopupDividerView : BoxView
{
    /// <summary>
    /// Identifies the <see cref="Indent"/> bindable property.</summary>
    public static readonly BindableProperty IndentProperty =

        BindableProperty.Create(nameof(Indent), typeof(bool), typeof(CollectionPopupDividerView), true, propertyChanged: OnIndentChanged);

    /// <summary>
    /// Initializes the separator from its XAML definition.
    /// </summary>
    public CollectionPopupDividerView()
    {
        InitializeComponent();
        UpdateIndent();
    }

    /// <summary>
    /// Gets or sets whether the separator is indented below the option icon.
    /// </summary>
    public bool Indent
    {
        get => (bool)GetValue(IndentProperty);
        set => SetValue(IndentProperty, value);
    }

    private static void OnIndentChanged(BindableObject bindable, object oldValue, object newValue) => ((CollectionPopupDividerView)bindable).UpdateIndent();

    private void UpdateIndent() => Margin = new Thickness(Indent ? 27 : 0, 0, 0, 0);
}
