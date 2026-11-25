namespace CollectionLib.Controls.Components.Search;

public partial class SearchFieldIndicatorView : ContentView
{
    public SearchFieldIndicatorView() => InitializeComponent();

    public void Configure(string text, Color accent)
    {
        IndicatorText.Text = text;
        IndicatorText.TextColor = accent;
        IndicatorBorder.Stroke = accent.WithAlpha(0.58f);
        IndicatorBorder.BackgroundColor = accent.WithAlpha(0.12f);
    }
}