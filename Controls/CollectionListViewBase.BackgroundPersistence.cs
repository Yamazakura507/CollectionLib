using System.ComponentModel;
using System.Text.Json;
using CollectionLib.Enums;
using CollectionLib.Models;

namespace CollectionLib.Controls;

/// <summary>
/// Optional per-instance persistence for the list viewport and item background.
/// The image is stored at Path/SaveFileName; a JSON sidecar stores modes and brushes.
/// </summary>
public abstract partial class CollectionListViewBase
{
    private static readonly JsonSerializerOptions BackgroundJsonOptions = new()
    {
        WriteIndented = true
    };

    private CancellationTokenSource? backgroundSaveCancellation;
    private string? backgroundImageFilePath;
    private bool isRestoringBackgroundSettings;
    private bool isHandlingBackgroundOptionsChange;

    private static void OnSavingsBackgroundOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        CollectionListViewBase control = (CollectionListViewBase)bindable;

        if (oldValue is SavingsBackgroundOptions oldOptions)
            oldOptions.PropertyChanged -= control.SavingsBackgroundOptionsOnPropertyChanged;

        if (newValue is SavingsBackgroundOptions newOptions)
            newOptions.PropertyChanged += control.SavingsBackgroundOptionsOnPropertyChanged;

        control.RestoreBackgroundSettings();
        control.OnListBackgroundConfigurationChanged();

        if (control.SavingsBackgroundOptions?.IsSave == true)
            _ = control.PersistCurrentBackgroundImageAndStateAsync();
    }

    private void SavingsBackgroundOptionsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Dispatcher.IsDispatchRequired)
        {
            Dispatcher.Dispatch(() => SavingsBackgroundOptionsOnPropertyChanged(sender, e));
            return;
        }

        if (isRestoringBackgroundSettings || isHandlingBackgroundOptionsChange
            || sender is not SavingsBackgroundOptions options)
            return;

        if (e.PropertyName is nameof(SavingsBackgroundOptions.Path) or nameof(SavingsBackgroundOptions.SaveFileName))
        {
            RestoreBackgroundSettings();
            OnListBackgroundConfigurationChanged();

            if (options.IsSave)
                _ = PersistCurrentBackgroundImageAndStateAsync();

            return;
        }

        if (e.PropertyName != nameof(SavingsBackgroundOptions.IsSave)) return;

        OnListBackgroundConfigurationChanged();

        if (options.IsSave)
            _ = PersistCurrentBackgroundImageAndStateAsync();
        else
            QueueBackgroundPersistence();
    }

    /// <summary>
    /// Loads the saved state only when persistence was enabled for this list instance.
    /// </summary>
    private void RestoreBackgroundSettings()
    {
        SavingsBackgroundOptions? options = SavingsBackgroundOptions;
        if (options is null || !TryGetBackgroundStoragePaths(options, out string imagePath, out string settingsPath))
            return;

        try
        {
            if (!File.Exists(settingsPath)) return;

            SavedBackgroundState? state = JsonSerializer.Deserialize<SavedBackgroundState>(
                File.ReadAllText(settingsPath), BackgroundJsonOptions);
            if (state is null) return;

            isHandlingBackgroundOptionsChange = true;
            if (!options.IsSaveExplicitlySet) options.SetIsSaveFromStorage(state.IsSave);
            isHandlingBackgroundOptionsChange = false;

            if (!options.IsSave) return;

            isRestoringBackgroundSettings = true;

            ItemBackgroundMode = state.ItemBackgroundMode;
            ItemBackgroundColor = ParseColor(state.ItemBackgroundColor, ItemBackgroundColor);
            ItemBackgroundBrush = RestoreBrush(state.ItemBackgroundBrush, ItemBackgroundColor);
            ItemGradientMotionMode = state.ItemGradientMotionMode;

            ListBackgroundMode = state.ListBackgroundMode;
            ListBackgroundColor = ParseColor(state.ListBackgroundColor, ListBackgroundColor);
            ListBackgroundBrush = RestoreBrush(state.ListBackgroundBrush, ListBackgroundColor);
            ListGradientMotionMode = state.ListGradientMotionMode;

            if (state.HasImage && File.Exists(imagePath))
            {
                backgroundImageFilePath = imagePath;
                ListBackgroundImageSource = ImageSource.FromFile(imagePath);
            }

            isRestoringBackgroundSettings = false;
            ApplyListBackgroundConfiguration();
            RefreshItemBackgroundSurfaces();
            OnListBackgroundConfigurationChanged();
        }
        catch (Exception exception)
        {
            isRestoringBackgroundSettings = false;
            isHandlingBackgroundOptionsChange = false;
            System.Diagnostics.Debug.WriteLine($"Unable to restore collection-list background: {exception}");
        }
    }

    /// <summary>
    /// Debounces writes while the user is dragging sliders or adjusting colors.
    /// The state is captured synchronously on the UI thread; only file I/O is asynchronous.
    /// </summary>
    protected void QueueBackgroundPersistence()
    {
        if (isRestoringBackgroundSettings || SavingsBackgroundOptions is not { } options
            || !TryGetBackgroundStoragePaths(options, out _, out string settingsPath))
            return;

        string payload;

        try
        {
            SavedBackgroundState state = CaptureBackgroundState(options);
            payload = JsonSerializer.Serialize(state, BackgroundJsonOptions);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to serialize collection-list background: {exception}");
            return;
        }

        backgroundSaveCancellation?.Cancel();
        backgroundSaveCancellation?.Dispose();

        CancellationTokenSource cancellation = new();
        backgroundSaveCancellation = cancellation;
        _ = SaveBackgroundStateAfterDelayAsync(settingsPath, payload, cancellation.Token);
    }

    private async Task SaveBackgroundStateAfterDelayAsync(
        string settingsPath,
        string payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken).ConfigureAwait(false);
            string? directory = System.IO.Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrWhiteSpace(directory)) return;

            Directory.CreateDirectory(directory);
            string temporaryPath = settingsPath + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, payload, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, settingsPath, true);
        }
        catch (OperationCanceledException)
        {
            // A newer UI change superseded this snapshot.
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to save collection-list background: {exception}");
        }
    }

    private async Task PersistCurrentBackgroundImageAndStateAsync()
    {
        await PersistCurrentBackgroundImageAsync();
        await MainThread.InvokeOnMainThreadAsync(QueueBackgroundPersistence);
    }

    /// <summary>
    /// Sets the currently selected image file and remembers its source path for this list instance.
    /// </summary>
    protected void SetBackgroundImageFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        backgroundImageFilePath = System.IO.Path.GetFullPath(filePath);
        ListBackgroundImageSource = ImageSource.FromFile(backgroundImageFilePath);
    }

    /// <summary>
    /// Copies the selected image into this list's dedicated storage location when saving is enabled.
    /// </summary>
    protected async ValueTask PersistCurrentBackgroundImageAsync()
    {
        SavingsBackgroundOptions? options = SavingsBackgroundOptions;
        if (options is null || !options.IsSave
            || !TryGetBackgroundStoragePaths(options, out string imagePath, out _))
            return;

        string? sourcePath = backgroundImageFilePath;

        if (string.IsNullOrWhiteSpace(sourcePath) && ListBackgroundImageSource is FileImageSource fileSource)
            sourcePath = fileSource.File;

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return;

        try
        {
            string? directory = System.IO.Path.GetDirectoryName(imagePath);
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);

            if (!string.Equals(
                    System.IO.Path.GetFullPath(sourcePath),
                    System.IO.Path.GetFullPath(imagePath),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                await using FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await using FileStream destination = new(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                await source.CopyToAsync(destination).ConfigureAwait(false);
                await destination.FlushAsync().ConfigureAwait(false);
            }

            backgroundImageFilePath = imagePath;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ListBackgroundImageSource = ImageSource.FromFile(imagePath);
                ApplyListBackgroundConfiguration();
            });
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to copy collection-list background image: {exception}");
        }
    }

    private SavedBackgroundState CaptureBackgroundState(SavingsBackgroundOptions options)
    {
        TryGetBackgroundStoragePaths(options, out string imagePath, out _);

        Color itemColor = ItemBackgroundBrush is SolidColorBrush itemSolid ? itemSolid.Color : ItemBackgroundColor;
        Color listColor = ListBackgroundBrush is SolidColorBrush listSolid ? listSolid.Color : ListBackgroundColor;

        return new SavedBackgroundState
        {
            IsSave = options.IsSave,
            ItemBackgroundMode = ItemBackgroundMode,
            ItemBackgroundColor = ToArgbHex(itemColor),
            ItemBackgroundBrush = CaptureBrush(ItemBackgroundBrush, ItemBackgroundColor),
            ItemGradientMotionMode = ItemGradientMotionMode,
            ListBackgroundMode = ListBackgroundMode,
            ListBackgroundColor = ToArgbHex(listColor),
            ListBackgroundBrush = CaptureBrush(ListBackgroundBrush, ListBackgroundColor),
            ListGradientMotionMode = ListGradientMotionMode,
            HasImage = options.IsSave && File.Exists(imagePath)
        };
    }

    private static SavedBrush CaptureBrush(Brush? brush, Color fallback)
    {
        if (brush is LinearGradientBrush gradient)
        {
            return new SavedBrush
            {
                Kind = "Gradient",
                StartX = gradient.StartPoint.X,
                StartY = gradient.StartPoint.Y,
                EndX = gradient.EndPoint.X,
                EndY = gradient.EndPoint.Y,
                Stops = gradient.GradientStops
                    .OrderBy(stop => stop.Offset)
                    .Select(stop => new SavedGradientStop
                    {
                        Offset = stop.Offset,
                        Color = ToArgbHex(stop.Color)
                    })
                    .ToList()
            };
        }

        Color color = brush is SolidColorBrush solid ? solid.Color : fallback;
        return new SavedBrush { Kind = "Solid", Color = ToArgbHex(color) };
    }

    private static Brush RestoreBrush(SavedBrush? saved, Color fallback)
    {
        if (saved is null || string.Equals(saved.Kind, "Solid", StringComparison.OrdinalIgnoreCase))
            return new SolidColorBrush(saved is null ? fallback : ParseColor(saved.Color, fallback));

        if (!string.Equals(saved.Kind, "Gradient", StringComparison.OrdinalIgnoreCase)
            || saved.Stops is not { Count: >= 2 })
            return new SolidColorBrush(fallback);

        GradientStopCollection stops = [];
        foreach (SavedGradientStop stop in saved.Stops)
            stops.Add(new GradientStop(ParseColor(stop.Color, fallback), (float)Math.Clamp(stop.Offset, 0, 1)));

        return new LinearGradientBrush(
            stops,
            new Point(saved.StartX, saved.StartY),
            new Point(saved.EndX, saved.EndY));
    }

    private static string ToArgbHex(Color color)
        => $"#{(byte)Math.Round(color.Alpha * 255):X2}{(byte)Math.Round(color.Red * 255):X2}{(byte)Math.Round(color.Green * 255):X2}{(byte)Math.Round(color.Blue * 255):X2}";

    private static Color ParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        string hex = value.TrimStart('#');
        if (hex.Length != 8 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint argb))
            return fallback;

        return new Color(
            ((argb >> 16) & 0xFF) / 255f,
            ((argb >> 8) & 0xFF) / 255f,
            (argb & 0xFF) / 255f,
            ((argb >> 24) & 0xFF) / 255f);
    }

    private static bool TryGetBackgroundStoragePaths(
        SavingsBackgroundOptions options,
        out string imagePath,
        out string settingsPath)
    {
        imagePath = string.Empty;
        settingsPath = string.Empty;

        if (string.IsNullOrWhiteSpace(options.Path)
            || string.IsNullOrWhiteSpace(options.SaveFileName))
            return false;

        string fileName = System.IO.Path.GetFileName(options.SaveFileName);
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        string directory = System.IO.Path.GetFullPath(options.Path);
        imagePath = System.IO.Path.Combine(directory, fileName);
        settingsPath = imagePath + ".json";

        return true;
    }

    private sealed class SavedBackgroundState
    {
        public bool IsSave { get; set; }
        public CollectionItemBackgroundMode ItemBackgroundMode { get; set; }
        public string ItemBackgroundColor { get; set; } = string.Empty;
        public SavedBrush? ItemBackgroundBrush { get; set; }
        public GradientMotionMode ItemGradientMotionMode { get; set; }
        public CollectionListBackgroundMode ListBackgroundMode { get; set; }
        public string ListBackgroundColor { get; set; } = string.Empty;
        public SavedBrush? ListBackgroundBrush { get; set; }
        public GradientMotionMode ListGradientMotionMode { get; set; }
        public bool HasImage { get; set; }
    }

    private sealed class SavedBrush
    {
        public string Kind { get; set; } = "Solid";
        public string? Color { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; } = 0.5;
        public double EndX { get; set; } = 1;
        public double EndY { get; set; } = 0.5;
        public List<SavedGradientStop> Stops { get; set; } = [];
    }

    private sealed class SavedGradientStop
    {
        public double Offset { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}
