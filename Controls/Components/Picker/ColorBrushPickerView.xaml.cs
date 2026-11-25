using System.Globalization;
using CollectionLib.Resources.ExtensionResources;

namespace CollectionLib.Controls.Components.Picker;

/// <summary>
/// Interactive color and linear-gradient editor. The visual tree is defined in XAML;
/// this class owns only color conversion, pointer interaction and gradient-stop state.
/// </summary>
public partial class ColorBrushPickerView : ContentView
{
    private readonly List<EditableGradientStop> gradientStops = [];
    private Color selectedColor = CollectionListColors.Get("SearchAccent1");
    private double hue;
    private double saturation = 0.65;
    private double brightness = 1;
    private bool isGradient;
    private bool isUpdatingControls;
    private int selectedStopIndex;
    private double gradientAngle;
    private Point gradientStart = new(0, 0.5);
    private Point gradientEnd = new(1, 0.5);

    public event EventHandler<Brush>? BrushChanged;

    public Brush SelectedBrush => CreateBrush();

    public ColorBrushPickerView()
    {
        InitializeComponent();
        ColorPlane.Drawable = new SaturationValueDrawable(this);
        HueSpectrum.Drawable = new HueSpectrumDrawable(this);

        Color defaultEnd = CollectionListColors.Get("PickerDefaultEnd");
        gradientStops.Add(new EditableGradientStop(0, selectedColor));
        gradientStops.Add(new EditableGradientStop(1, defaultEnd));

        UpdateControls();
    }

    /// <summary>
    /// Loads an existing solid-color or linear-gradient brush without raising BrushChanged.
    /// </summary>
    public void Configure(Brush? initialBrush)
    {
        isGradient = initialBrush is LinearGradientBrush;
        gradientAngle = 0;
        gradientStart = new Point(0, 0.5);
        gradientEnd = new Point(1, 0.5);
        gradientStops.Clear();
        selectedStopIndex = 0;

        if (initialBrush is SolidColorBrush solidBrush)
        {
            selectedColor = solidBrush.Color;
        }
        else if (initialBrush is LinearGradientBrush gradientBrush)
        {
            foreach (GradientStop stop in gradientBrush.GradientStops.OrderBy(stop => stop.Offset))
                gradientStops.Add(new EditableGradientStop(stop.Offset, stop.Color));

            if (gradientStops.Count < 2)
            {
                gradientStops.Clear();
                gradientStops.Add(new EditableGradientStop(0, CollectionListColors.Get("SearchAccent1")));
                gradientStops.Add(new EditableGradientStop(1, CollectionListColors.Get("PickerDefaultEnd")));
            }

            gradientStart = gradientBrush.StartPoint;
            gradientEnd = gradientBrush.EndPoint;
            gradientAngle = GetGradientAngle(gradientStart, gradientEnd);
            selectedColor = gradientStops[0].Color;
        }
        else
        {
            selectedColor = CollectionListColors.Get("SearchAccent1");
        }

        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
    }

    private void OnColorModeClicked(object? sender, EventArgs e)
    {
        isGradient = false;

        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnGradientModeClicked(object? sender, EventArgs e)
    {
        EnsureGradientStops();

        if (!isGradient && selectedStopIndex >= 0 && selectedStopIndex < gradientStops.Count)
            gradientStops[selectedStopIndex].Color = selectedColor;

        isGradient = true;
        selectedStopIndex = Math.Clamp(selectedStopIndex, 0, gradientStops.Count - 1);
        selectedColor = gradientStops[selectedStopIndex].Color;
        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnColorPlaneInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any() || ColorPlane.Width <= 0 || ColorPlane.Height <= 0) return;

        PointF point = e.Touches.First();
        saturation = Math.Clamp(point.X / ColorPlane.Width, 0, 1);
        brightness = Math.Clamp(1 - point.Y / ColorPlane.Height, 0, 1);

        SetSelectedColor(FromHsv(hue, saturation, brightness));
    }

    private void OnHueInteraction(object? sender, TouchEventArgs e)
    {
        if (!e.Touches.Any() || HueSpectrum.Width <= 0) return;

        hue = Math.Clamp(e.Touches.First().X / HueSpectrum.Width, 0, 1) * 360;

        SetSelectedColor(FromHsv(hue, saturation, brightness));

        ColorPlane.Invalidate();
        HueSpectrum.Invalidate();
    }

    private void OnHexTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (isUpdatingControls || !TryParseHex(e.NewTextValue, out Color color)) return;

        SetSelectedColor(color);
    }

    private void OnRgbTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (isUpdatingControls || string.IsNullOrWhiteSpace(e.NewTextValue)) return;

        string[] parts = e.NewTextValue.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3
            || !byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte red)
            || !byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte green)
            || !byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte blue)) return;

        SetSelectedColor(new Color(red / 255f, green / 255f, blue / 255f, selectedColor.Alpha));
    }

    private void OnPresetClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Color color }) return;

        SetSelectedColor(color);
    }

    private void OnGradientAngleClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button
            || !int.TryParse(
                    button.CommandParameter?.ToString(), 
                    NumberStyles.Integer, 
                    CultureInfo.InvariantCulture, 
                    out int angle)) return;

        SetGradientAngle(angle);
        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnGradientAngleTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (isUpdatingControls
            || !int.TryParse(e.NewTextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int angle)) return;

        SetGradientAngle(angle);
        UpdateGradientPreview();
        RaiseBrushChanged();
    }

    private void OnAddGradientStopClicked(object? sender, EventArgs e)
    {
        EnsureGradientStops();

        if (gradientStops.Count >= 8) return;

        List<EditableGradientStop> ordered = gradientStops.OrderBy(stop => stop.Offset).ToList();
        double largestGap = -1;
        double newOffset = 0.5;
        Color newColor = selectedColor;

        for (int index = 0; index < ordered.Count - 1; index++)
        {
            double gap = ordered[index + 1].Offset - ordered[index].Offset;

            if (gap <= largestGap) continue;

            largestGap = gap;
            newOffset = (ordered[index].Offset + ordered[index + 1].Offset) / 2;
            newColor = Interpolate(ordered[index].Color, ordered[index + 1].Color, 0.5);
        }

        EditableGradientStop stop = new(newOffset, newColor);
        gradientStops.Add(stop);
        selectedStopIndex = gradientStops.IndexOf(stop);
        selectedColor = newColor;
        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnRemoveGradientStopClicked(object? sender, EventArgs e)
    {
        if (gradientStops.Count <= 2 || selectedStopIndex < 0 || selectedStopIndex >= gradientStops.Count) return;

        gradientStops.RemoveAt(selectedStopIndex);
        selectedStopIndex = Math.Clamp(selectedStopIndex, 0, gradientStops.Count - 1);
        selectedColor = gradientStops[selectedStopIndex].Color;
        (hue, saturation, brightness) = GetHsv(selectedColor);
        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnGradientStopClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: int index }
            || index < 0
            || index >= gradientStops.Count) return;

        selectedStopIndex = index;
        selectedColor = gradientStops[index].Color;
        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
    }

    private void OnGradientStopPositionChanged(object? sender, TextChangedEventArgs e)
    {
        if (isUpdatingControls
            || selectedStopIndex < 0
            || selectedStopIndex >= gradientStops.Count
            || !int.TryParse(e.NewTextValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int position)) return;

        gradientStops[selectedStopIndex].Offset = Math.Clamp(position, 0, 100) / 100d;

        UpdateGradientStops();
        UpdateGradientPreview();
        RaiseBrushChanged();
    }

    private void SetSelectedColor(Color color)
    {
        selectedColor = color;
        (hue, saturation, brightness) = GetHsv(color);

        if (isGradient && selectedStopIndex >= 0 && selectedStopIndex < gradientStops.Count)
            gradientStops[selectedStopIndex].Color = color;

        UpdateControls();
        RaiseBrushChanged();
    }

    private void UpdateControls()
    {
        isUpdatingControls = true;
        ColorModeButton.BackgroundColor = isGradient
            ? CollectionListColors.ForTheme("PopupSurfaceLight", "PopupSurfaceDark")
            : CollectionListColors.ForTheme("AccentSoftLight", "AccentSoftDark");
        GradientModeButton.BackgroundColor = isGradient
            ? CollectionListColors.ForTheme("AccentSoftLight", "AccentSoftDark")
            : CollectionListColors.ForTheme("PopupSurfaceLight", "PopupSurfaceDark");
        ColorModeButton.BorderColor = isGradient
            ? CollectionListColors.ForTheme("PopupBorderLight", "PopupBorderDark")
            : CollectionListColors.Get("Accent");
        GradientModeButton.BorderColor = isGradient
            ? CollectionListColors.Get("Accent")
            : CollectionListColors.ForTheme("PopupBorderLight", "PopupBorderDark");
        HexEntry.Text = ToHex(selectedColor);
        RgbEntry.Text = $"{ToByte(selectedColor.Red)}, {ToByte(selectedColor.Green)}, {ToByte(selectedColor.Blue)}";
        BrushPreview.Background = CreateBrush();

        ColorPlane.Invalidate();
        HueSpectrum.Invalidate();

        GradientEditor.IsVisible = isGradient;
        GradientAngleEntry.Text = Math.Round(gradientAngle).ToString(CultureInfo.InvariantCulture);

        if (isGradient)
        {
            UpdateGradientStops();
            UpdateGradientPreview();
        }

        isUpdatingControls = false;
    }

    private void UpdateGradientStops()
    {
        GradientStopsHost.Children.Clear();

        for (int index = 0; index < gradientStops.Count; index++)
        {
            EditableGradientStop stop = gradientStops[index];
            Button button = new()
            {
                Text = $"{Math.Round(stop.Offset * 100):0}",
                CommandParameter = index,
                BackgroundColor = stop.Color,
                TextColor = GetBrightness(stop.Color) > 0.55 ? Colors.Black : Colors.White,
                BorderColor = index == selectedStopIndex
                    ? CollectionListColors.Get("Accent")
                    : CollectionListColors.ForTheme("PopupBorderLight", "PopupBorderDark"),
                BorderWidth = index == selectedStopIndex ? 2 : 1,
                CornerRadius = 7,
                FontSize = 10,
                Padding = new Thickness(7, 3),
                MinimumWidthRequest = 34,
                HeightRequest = 28
            };
            button.Clicked += OnGradientStopClicked;
            GradientStopsHost.Children.Add(button);
        }

        if (selectedStopIndex >= 0 && selectedStopIndex < gradientStops.Count)
        {
            GradientStopPositionEntry.Text = Math.Round(gradientStops[selectedStopIndex].Offset * 100)
                .ToString(CultureInfo.InvariantCulture);
        }
    }

    private void UpdateGradientPreview() => GradientPreview.Background = CreateBrush();

    private void SetGradientAngle(int angle)
    {
        gradientAngle = ((angle % 360) + 360) % 360;

        double radians = gradientAngle * Math.PI / 180d;
        double horizontal = Math.Cos(radians);
        double vertical = Math.Sin(radians);

        gradientStart = new Point(0.5 - horizontal * 0.5, 0.5 + vertical * 0.5);
        gradientEnd = new Point(0.5 + horizontal * 0.5, 0.5 - vertical * 0.5);
    }

    private static double GetGradientAngle(Point start, Point end)
    {
        double horizontal = end.X - start.X;
        double vertical = start.Y - end.Y;

        if (Math.Abs(horizontal) < double.Epsilon && Math.Abs(vertical) < double.Epsilon) return 0;

        return (Math.Atan2(vertical, horizontal) * 180d / Math.PI + 360d) % 360d;
    }

    private Brush CreateBrush()
    {
        if (!isGradient) return new SolidColorBrush(selectedColor);

        GradientStopCollection stops = [];

        foreach (EditableGradientStop stop in gradientStops.OrderBy(stop => stop.Offset))
            stops.Add(new GradientStop(stop.Color, (float)stop.Offset));

        return new LinearGradientBrush(stops, gradientStart, gradientEnd);
    }

    private void RaiseBrushChanged() => BrushChanged?.Invoke(this, CreateBrush());

    private void EnsureGradientStops()
    {
        if (gradientStops.Count >= 2) return;

        gradientStops.Clear();
        gradientStops.Add(new EditableGradientStop(0, selectedColor));
        gradientStops.Add(new EditableGradientStop(1, CollectionListColors.Get("PickerDefaultEnd")));
    }

    private static bool TryParseHex(string? value, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(value)) return false;

        string hex = value.Trim();

        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length is not (6 or 8)
            || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsed)) return false;

        byte alpha = byte.MaxValue;

        if (hex.Length == 8)
        {
            alpha = (byte)(parsed >> 24);
            parsed &= 0x00FFFFFF;
        }

        byte red = (byte)(parsed >> 16);
        byte green = (byte)(parsed >> 8);
        byte blue = (byte)parsed;

        color = new Color(red / 255f, green / 255f, blue / 255f, alpha / 255f);

        return true;
    }

    private static string ToHex(Color color)
    {
        string rgb = $"#{ToByte(color.Red):X2}{ToByte(color.Green):X2}{ToByte(color.Blue):X2}";

        return color.Alpha >= 0.999f ? rgb : $"#{ToByte(color.Alpha):X2}{rgb[1..]}";
    }

    private static int ToByte(float channel) => (int)Math.Round(Math.Clamp(channel, 0, 1) * 255);

    private static double GetBrightness(Color color)
        => 0.2126 * color.Red + 0.7152 * color.Green + 0.0722 * color.Blue;

    private static Color Interpolate(Color start, Color end, double amount)
    {
        float factor = (float)Math.Clamp(amount, 0, 1);

        return new Color(
            start.Red + (end.Red - start.Red) * factor,
            start.Green + (end.Green - start.Green) * factor,
            start.Blue + (end.Blue - start.Blue) * factor,
            start.Alpha + (end.Alpha - start.Alpha) * factor);
    }

    private static (double Hue, double Saturation, double Brightness) GetHsv(Color color)
    {
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;
        double valueHue = 0;

        if (delta > 0)
        {
            if (maximum == red) valueHue = 60 * (((green - blue) / delta) % 6);
            else if (maximum == green) valueHue = 60 * (((blue - red) / delta) + 2);
            else valueHue = 60 * (((red - green) / delta) + 4);
        }

        if (valueHue < 0) valueHue += 360;

        return (valueHue, maximum <= 0 ? 0 : delta / maximum, maximum);
    }

    private static Color FromHsv(double hueValue, double saturationValue, double brightnessValue)
    {
        float chroma = (float)(brightnessValue * saturationValue);
        float sector = (float)(hueValue % 360) / 60f;
        float second = chroma * (1 - Math.Abs(sector % 2 - 1));
        (float red, float green, float blue) = (int)sector switch
        {
            0 => (chroma, second, 0f),
            1 => (second, chroma, 0f),
            2 => (0f, chroma, second),
            3 => (0f, second, chroma),
            4 => (second, 0f, chroma),
            _ => (chroma, 0f, second)
        };

        float offset = (float)brightnessValue - chroma;

        return new Color(red + offset, green + offset, blue + offset);
    }

    private sealed class EditableGradientStop(double offset, Color color)
    {
        public double Offset { get; set; } = offset;
        public Color Color { get; set; } = color;
    }

    private sealed class SaturationValueDrawable(ColorBrushPickerView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            int columns = Math.Clamp((int)(dirtyRect.Width / 3), 40, 110);
            int rows = Math.Clamp((int)(dirtyRect.Height / 3), 24, 65);
            float cellWidth = dirtyRect.Width / columns;
            float cellHeight = dirtyRect.Height / rows;

            for (int row = 0; row < rows; row++)
            {
                double value = 1 - (row + 0.5) / rows;
                for (int column = 0; column < columns; column++)
                {
                    double saturationValue = (column + 0.5) / columns;
                    canvas.FillColor = FromHsv(owner.hue, saturationValue, value);
                    canvas.FillRectangle(
                        column * cellWidth,
                        row * cellHeight,
                        cellWidth + 0.6f,
                        cellHeight + 0.6f);
                }
            }

            float markerX = (float)(owner.saturation * dirtyRect.Width);
            float markerY = (float)((1 - owner.brightness) * dirtyRect.Height);

            canvas.StrokeSize = 3;
            canvas.StrokeColor = Colors.Black;
            canvas.DrawCircle(markerX, markerY, 6);
            canvas.StrokeSize = 1.5f;
            canvas.StrokeColor = Colors.White;
            canvas.DrawCircle(markerX, markerY, 6);
        }
    }

    private sealed class HueSpectrumDrawable(ColorBrushPickerView owner) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0) return;

            int columns = Math.Max(120, (int)dirtyRect.Width);
            float cellWidth = dirtyRect.Width / columns;

            for (int column = 0; column < columns; column++)
            {
                canvas.FillColor = FromHsv(column * 360d / columns, 1, 1);
                canvas.FillRectangle(column * cellWidth, 0, cellWidth + 0.6f, dirtyRect.Height);
            }

            float markerX = (float)(owner.hue / 360d * dirtyRect.Width);
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 3;
            canvas.DrawLine(markerX, 0, markerX, dirtyRect.Height);
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 1;
            canvas.DrawLine(markerX, 0, markerX, dirtyRect.Height);
        }
    }
}