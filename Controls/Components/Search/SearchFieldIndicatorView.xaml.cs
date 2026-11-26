namespace CollectionLib.Controls.Components.Search;

/// <summary>
/// Displays a colored indicator for a selected search field.
/// </summary>
public partial class SearchFieldIndicatorView : ContentView
{
    /// <summary>
    /// Initializes the search field indicator.
    /// </summary>
    public SearchFieldIndicatorView() => InitializeComponent();

    /// <summary>
    /// Sets the indicator caption and its accent styling.
    /// </summary>
    /// <param name="text">The search field name displayed in the indicator.</param>
    /// <param name="accent">The accent color used for the text, border, and background.</param>
    public void Configure(string text, Color accent)
    {
        IndicatorText.Text = text;
        IndicatorText.TextColor = accent;
        IndicatorBorder.Stroke = accent.WithAlpha(0.58f);
        IndicatorBorder.BackgroundColor = accent.WithAlpha(0.12f);
    }
}