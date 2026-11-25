namespace CollectionLib.Controls.Components.Picker;

public partial class HsvColorPickerView : VerticalStackLayout
{
    private bool isConfiguring;

    public event EventHandler<Color>? ColorChanged;

    public HsvColorPickerView() => InitializeComponent();

    public void Configure(Color initialColor)
    {
        (double hue, double saturation, double brightness) = GetHsv(initialColor);
        isConfiguring = true;
        HueSlider.Value = hue;
        SaturationSlider.Value = saturation;
        BrightnessSlider.Value = Math.Max(0.15, brightness);
        isConfiguring = false;

        UpdatePreview(initialColor);
    }

    private void OnSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (isConfiguring) return;

        Color color = ConvertHsvToColor(
            (float)HueSlider.Value,
            (float)SaturationSlider.Value,
            (float)BrightnessSlider.Value);

        UpdatePreview(color);
        ColorChanged?.Invoke(this, color);
    }

    private void UpdatePreview(Color color)
    {
        Preview.BackgroundColor = color;
        ValueLabel.Text = color.ToString();
    }

    private static Color ConvertHsvToColor(float hue, float saturation, float value)
    {
        float chroma = value * saturation;
        float sector = (hue % 360f) / 60f;
        float secondComponent = chroma * (1 - Math.Abs(sector % 2 - 1));

        (float red, float green, float blue) = (int)sector switch
        {
            0 => (chroma, secondComponent, 0f),
            1 => (secondComponent, chroma, 0f),
            2 => (0f, chroma, secondComponent),
            3 => (0f, secondComponent, chroma),
            4 => (secondComponent, 0f, chroma),
            _ => (chroma, 0f, secondComponent)
        };

        float offset = value - chroma;

        return new Color(red + offset, green + offset, blue + offset);
    }

    private static (double Hue, double Saturation, double Brightness) GetHsv(Color color)
    {
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;

        double hue = 0;
        if (delta > 0)
        {
            if (maximum == red) hue = 60 * (((green - blue) / delta) % 6);
            else if (maximum == green) hue = 60 * (((blue - red) / delta) + 2);
            else hue = 60 * (((red - green) / delta) + 4);
        }

        if (hue < 0) hue += 360;
        double saturation = maximum <= 0 ? 0 : delta / maximum;
        return (hue, saturation, maximum);
    }
}