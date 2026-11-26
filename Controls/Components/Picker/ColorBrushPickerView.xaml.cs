using System.Globalization;
using CollectionLib.Enums;
using CollectionLib.Models.Draw;
using CollectionLib.Models.Piker;
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
    internal double hue;
    internal double saturation = 0.65;
    internal double brightness = 1;
    private bool isGradient;
    private bool isUpdatingControls;
    private int selectedStopIndex;
    private const int MaximumGradientStops = 5;

    private GradientMotionMode gradientMotionMode;
    private LinearGradientBrush? currentGradientBrush;

    private static readonly GradientPreset[] QuickGradientPresets =
    [
        new("Закат", Color.FromArgb("#F97316"), Color.FromArgb("#EC4899"), Color.FromArgb("#7C3AED")),
        new("Океан", Color.FromArgb("#0EA5E9"), Color.FromArgb("#2563EB"), Color.FromArgb("#312E81")),
        new("Лес", Color.FromArgb("#14532D"), Color.FromArgb("#16A34A"), Color.FromArgb("#BEF264")),
        new("Лаванда", Color.FromArgb("#6366F1"), Color.FromArgb("#A78BFA"), Color.FromArgb("#F0ABFC")),
        new("Жар", Color.FromArgb("#BE123C"), Color.FromArgb("#F97316"), Color.FromArgb("#FDE047")),
        new("Лёд", Color.FromArgb("#0E7490"), Color.FromArgb("#67E8F9"), Color.FromArgb("#EFF6FF")),
        new("Рассвет", Color.FromArgb("#FBCFE8"), Color.FromArgb("#FB7185"), Color.FromArgb("#FDBA74")),
        new("Ночь", Color.FromArgb("#111827"), Color.FromArgb("#4338CA"), Color.FromArgb("#A21CAF"))
    ];
    private double gradientAngle;
    private Point gradientStart = new(0, 0.5);
    private Point gradientEnd = new(1, 0.5);

    /// <summary>
    /// Occurs when the selected solid color or gradient brush changes.
    /// </summary>
    public event EventHandler<Brush>? BrushChanged;
    /// <summary>
    /// Occurs when the selected gradient motion mode changes.
    /// </summary>
    public event Action<GradientMotionMode>? GradientMotionModeChanged;

    /// <summary>
    /// Gets the currently selected solid-color or gradient brush.
    /// </summary>
    public Brush SelectedBrush => CreateBrush();
    /// <summary>
    /// Gets the currently selected gradient animation mode.
    /// </summary>
    public GradientMotionMode SelectedGradientMotionMode => gradientMotionMode;

    /// <summary>
    /// Initializes the color and gradient brush picker.
    /// </summary>
    public ColorBrushPickerView()
    {
        InitializeComponent();
        ColorPlane.Drawable = new SaturationValueDrawable(this);
        HueSpectrum.Drawable = new HueSpectrumDrawable(this);

        Color defaultEnd = CollectionListColors.Get("PickerDefaultEnd");
        gradientStops.Add(new EditableGradientStop(0, selectedColor));
        gradientStops.Add(new EditableGradientStop(1, defaultEnd));

        ToolTipProperties.SetText(ColorModeButton, "Сплошной цвет");
        ToolTipProperties.SetText(GradientModeButton, "Градиент");
        ToolTipProperties.SetText(AddGradientStopButton, "Добавить цвет");
        ToolTipProperties.SetText(RemoveGradientStopButton, "Удалить выбранный цвет");
        ToolTipProperties.SetText(GradientStopCountSlider, "Количество цветов в градиенте");
        ToolTipProperties.SetText(MotionNoneButton, "Без движения");
        ToolTipProperties.SetText(MotionFlowButton, "Скольжение цветов");
        ToolTipProperties.SetText(MotionRotateButton, "Вращение градиента");
        ToolTipProperties.SetText(MotionPulseButton, "Пульсация градиента");

        BuildQuickGradientPresets();

        UpdateControls();
    }

    /// <summary>
    /// Loads an existing brush without changing the owning control's background.
    /// </summary>
    public void Configure(Brush? initialBrush) => Configure(initialBrush, GradientMotionMode.None);

    /// <summary>
    /// Loads an existing brush and its optional gradient-motion mode.
    /// </summary>
    public void Configure(Brush? initialBrush, GradientMotionMode initialMotionMode)
    {
        isGradient = initialBrush is LinearGradientBrush;
        gradientMotionMode = initialMotionMode;
        currentGradientBrush = initialBrush as LinearGradientBrush;
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

            TrimGradientStopsToMaximum();

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

        SetSelectedColor(FromHsv(hue, saturation, brightness), invalidateHueSpectrum: true);
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

    private void OnGradientMotionModeClicked(object? sender, EventArgs e)
    {
        if (sender is not ImageButton { CommandParameter: string value }
            || !Enum.TryParse(value, true, out GradientMotionMode mode)) return;
        if (gradientMotionMode == mode) return;

        gradientMotionMode = mode;

        Brush previewBrush = CreateBrush();
        BrushPreview.Background = previewBrush;
        GradientPreview.Background = previewBrush;

        RaiseBrushChanged(previewBrush);
        GradientMotionModeChanged?.Invoke(mode);
        UpdateControls();
    }

    private void BuildQuickGradientPresets()
    {
        foreach (GradientPreset preset in QuickGradientPresets)
        {
            Button button = new()
            {
                Text = string.Empty,
                WidthRequest = 60,
                HeightRequest = 26,
                MinimumWidthRequest = 60,
                MinimumHeightRequest = 26,
                Margin = new Thickness(3),
                Padding = 0,
                CornerRadius = 6,
                BorderWidth = 1,
                BorderColor = CollectionListColors.ForTheme("PopupBorderLight", "PopupBorderDark"),
                BackgroundColor = Colors.Transparent,
                Background = new LinearGradientBrush(
                    [
                        new GradientStop(preset.Start, 0),
                        new GradientStop(preset.Middle, 0.5f),
                        new GradientStop(preset.End, 1)
                    ],
                    new Point(0, 0.5),
                    new Point(1, 0.5))
            };

            ToolTipProperties.SetText(button, preset.Name);

            button.Clicked += (_, _) => ApplyGradientPreset(preset);

            QuickGradientsHost.Children.Add(button);
        }
    }

    private void ApplyGradientPreset(GradientPreset preset)
    {
        isGradient = true;

        gradientStops.Clear();
        gradientStops.Add(new EditableGradientStop(0, preset.Start));
        gradientStops.Add(new EditableGradientStop(0.5, preset.Middle));
        gradientStops.Add(new EditableGradientStop(1, preset.End));

        selectedStopIndex = 0;
        selectedColor = preset.Start;

        SetGradientAngle(0);

        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
        RaiseBrushChanged();
    }

    private void OnGradientStopCountChanged(object? sender, ValueChangedEventArgs e)
    {
        if (isUpdatingControls) return;

        int requestedCount = Math.Clamp((int)Math.Round(e.NewValue), 2, MaximumGradientStops);

        while (gradientStops.Count < requestedCount) AddGradientStop();
        while (gradientStops.Count > requestedCount) RemoveGradientStop();

        selectedStopIndex = Math.Clamp(selectedStopIndex, 0, gradientStops.Count - 1);
        selectedColor = gradientStops[selectedStopIndex].Color;
        (hue, saturation, brightness) = GetHsv(selectedColor);

        UpdateControls();
        RaiseBrushChanged();
    }

    private void AddGradientStop()
    {
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

        gradientStops.Add(new EditableGradientStop(newOffset, newColor));
    }

    private void RemoveGradientStop()
    {
        if (gradientStops.Count <= 2) return;

        int removeIndex = gradientStops.Count - 2;

        gradientStops.RemoveAt(removeIndex);

        if (selectedStopIndex == removeIndex) selectedStopIndex = Math.Min(removeIndex, gradientStops.Count - 1);
        else if (selectedStopIndex > removeIndex) selectedStopIndex--;
    }

    private void TrimGradientStopsToMaximum()
    {
        while (gradientStops.Count > MaximumGradientStops) RemoveGradientStop();
    }

    private void OnGradientAngleValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (isUpdatingControls) return;

        SetGradientAngle(e.NewValue);
        UpdateGradientAngleValue();

        Brush previewBrush = CreateBrush();

        GradientPreview.Background = previewBrush;
        BrushPreview.Background = previewBrush;

        RaiseBrushChanged(previewBrush);
    }

    private void OnAddGradientStopClicked(object? sender, EventArgs e)
    {
        EnsureGradientStops();

        if (gradientStops.Count >= MaximumGradientStops) return;

        AddGradientStop();

        selectedStopIndex = gradientStops.Count - 1;
        selectedColor = gradientStops[selectedStopIndex].Color;
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

    private void OnGradientStopPositionChanged(object? sender, ValueChangedEventArgs e)
    {
        if (isUpdatingControls || selectedStopIndex < 0 || selectedStopIndex >= gradientStops.Count) return;

        gradientStops[selectedStopIndex].Offset = Math.Clamp(e.NewValue, 0, 100) / 100d;
        GradientStopPositionValue.Text = $"{Math.Round(e.NewValue):0}%";
        Brush previewBrush = CreateBrush();
        GradientPreview.Background = previewBrush;
        BrushPreview.Background = previewBrush;

        RaiseBrushChanged(previewBrush);
    }

    private void SetSelectedColor(Color color, bool invalidateHueSpectrum = false)
    {
        selectedColor = color;
        (hue, saturation, brightness) = GetHsv(color);

        if (isGradient && selectedStopIndex >= 0 && selectedStopIndex < gradientStops.Count)
        {
            gradientStops[selectedStopIndex].Color = color;

            UpdateSelectedGradientStop();
        }

        UpdateColorValueEntries();

        Brush previewBrush = CreateBrush();
        BrushPreview.Background = previewBrush;

        if (isGradient) GradientPreview.Background = previewBrush;

        ColorPlane.Invalidate();

        if (invalidateHueSpectrum) HueSpectrum.Invalidate();

        RaiseBrushChanged(previewBrush);
    }

    private void UpdateControls()
    {
        isUpdatingControls = true;

        if (isGradient)
        {
            ColorModeButton.BackgroundColor = CollectionListColors.ForTheme("PopupSurfaceLight", "PopupSurfaceDark");
            GradientModeButton.BackgroundColor = CollectionListColors.ForTheme("AccentSoftLight", "AccentSoftDark");
            ColorModeButton.Opacity = 0.72;
            GradientModeButton.Opacity = 1;
        }
        else
        {
            ColorModeButton.BackgroundColor = CollectionListColors.ForTheme("AccentSoftLight", "AccentSoftDark");
            GradientModeButton.BackgroundColor = CollectionListColors.ForTheme("PopupSurfaceLight", "PopupSurfaceDark");
            ColorModeButton.Opacity = 1;
            GradientModeButton.Opacity = 0.72;
        }
        

        UpdateColorValueEntries();

        Brush previewBrush = isGradient && currentGradientBrush is not null ? currentGradientBrush : CreateBrush();

        BrushPreview.Background = previewBrush;

        if (isGradient) GradientPreview.Background = previewBrush;

        ColorPlane.Invalidate();
        HueSpectrum.Invalidate();

        GradientEditor.IsVisible = isGradient;
        QuickColorsSection.IsVisible = !isGradient;
        QuickGradientsSection.IsVisible = isGradient;
        GradientMotionSection.IsVisible = isGradient;

        UpdateGradientMotionModeButtons();

        GradientStopCountSlider.Value = gradientStops.Count;
        GradientStopCountValue.Text = gradientStops.Count.ToString(CultureInfo.InvariantCulture);
        AddGradientStopButton.IsEnabled = gradientStops.Count < MaximumGradientStops;
        RemoveGradientStopButton.IsEnabled = gradientStops.Count > 2;

        if (isGradient)
        {
            UpdateGradientStops();
            UpdateGradientAngleValue();
        }

        isUpdatingControls = false;
    }

    private void UpdateGradientMotionModeButtons()
    {
        UpdateMotionButton(MotionNoneButton, gradientMotionMode == GradientMotionMode.None);
        UpdateMotionButton(MotionFlowButton, gradientMotionMode == GradientMotionMode.Flow);
        UpdateMotionButton(MotionRotateButton, gradientMotionMode == GradientMotionMode.Rotate);
        UpdateMotionButton(MotionPulseButton, gradientMotionMode == GradientMotionMode.Pulse);
    }

    private static void UpdateMotionButton(ImageButton button, bool selected)
    {
        if (selected) 
        {
            button.BackgroundColor = CollectionListColors.ForTheme("AccentSoftLight", "AccentSoftDark");
            button.Opacity = 1;
        }
        else
        {
            button.BackgroundColor = Colors.Transparent;
            button.Opacity = 0.68;
        }
    }

    private void UpdateColorValueEntries()
    {
        bool wasUpdating = isUpdatingControls;

        isUpdatingControls = true;
        HexEntry.Text = ToHex(selectedColor);
        RgbEntry.Text = $"{ToByte(selectedColor.Red)}, {ToByte(selectedColor.Green)}, {ToByte(selectedColor.Blue)}";
        isUpdatingControls = wasUpdating;
    }

    private void UpdateGradientStops()
    {
        GradientStopsHost.Children.Clear();

        for (int index = 0; index < gradientStops.Count; index++)
        {
            EditableGradientStop stop = gradientStops[index];
            Button button = new()
            {
                Text = string.Empty,
                CommandParameter = index,
                BackgroundColor = stop.Color,

                BorderColor = index == selectedStopIndex
                    ? CollectionListColors.Get("Accent")
                    : CollectionListColors.ForTheme("PopupBorderLight", "PopupBorderDark"),
                BorderWidth = index == selectedStopIndex ? 2.5 : 1,
                CornerRadius = 13,
                Padding = 0,
                WidthRequest = 26,
                HeightRequest = 26,
                MinimumWidthRequest = 26,
                MinimumHeightRequest = 26
            };

            button.Clicked += OnGradientStopClicked;

            ToolTipProperties.SetText(button, $"Цвет {index + 1}, позиция {Math.Round(stop.Offset * 100):0}%");
            GradientStopsHost.Children.Add(button);
        }

        UpdateGradientStopPositionValue();
    }

    private void UpdateSelectedGradientStop()
    {
        if (selectedStopIndex < 0 || selectedStopIndex >= GradientStopsHost.Children.Count) return;
        if (GradientStopsHost.Children[selectedStopIndex] is Button button)
            button.BackgroundColor = gradientStops[selectedStopIndex].Color;
    }

    private void UpdateGradientStopPositionValue()
    {
        if (selectedStopIndex < 0 || selectedStopIndex >= gradientStops.Count) return;

        double value = gradientStops[selectedStopIndex].Offset * 100;
        GradientStopPositionSlider.Value = value;
        GradientStopPositionValue.Text = $"{Math.Round(value):0}%";
    }

    private void UpdateGradientAngleValue()
    {
        GradientAngleSlider.Value = gradientAngle;
        GradientAngleValue.Text = $"{Math.Round(gradientAngle):0}°";
    }

    private void SetGradientAngle(double angle)
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

    private void RaiseBrushChanged(Brush? brush = null)
    {
        Brush selectedBrush = brush ?? CreateBrush();
        currentGradientBrush = isGradient ? selectedBrush as LinearGradientBrush : null;

        BrushPreview.Background = selectedBrush;
        if (isGradient) GradientPreview.Background = selectedBrush;

        BrushChanged?.Invoke(this, selectedBrush);
    }

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

    internal static Color FromHsv(double hueValue, double saturationValue, double brightnessValue)
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
}