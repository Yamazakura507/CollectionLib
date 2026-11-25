namespace CollectionLib.Controls.Components;

public partial class AppliedIndicatorView : ContentView
{
    public AppliedIndicatorView() => InitializeComponent();

    public void Configure(ImageSource source, string tooltip)
    {
        IndicatorImage.Source = source;

        ToolTipProperties.SetText(IndicatorImage, tooltip);
    }
}