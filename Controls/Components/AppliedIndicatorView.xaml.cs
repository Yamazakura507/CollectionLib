namespace CollectionLib.Controls.Components;

/// <summary>
/// Displays an icon indicating that a collection operation is currently applied.
/// </summary>
public partial class AppliedIndicatorView : ContentView
{
    /// <summary>
    /// Initializes the applied-option indicator.
    /// </summary>
    public AppliedIndicatorView() => InitializeComponent();

    /// <summary>
    /// Sets the indicator icon and its tooltip.
    /// </summary>
    /// <param name="source">The image displayed by the indicator.</param>
    /// <param name="tooltip">The tooltip describing the applied operation.</param>
    public void Configure(ImageSource source, string tooltip)
    {
        IndicatorImage.Source = source;

        ToolTipProperties.SetText(IndicatorImage, tooltip);
    }
}