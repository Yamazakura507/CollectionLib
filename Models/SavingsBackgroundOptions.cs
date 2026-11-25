using System.ComponentModel;

namespace CollectionLib.Models;

/// <summary>
/// Optional, per-list storage configuration for its background image and color brushes.
/// <see cref="Path"/> is a directory; <see cref="SaveFileName"/> is the image file name.
/// A JSON sidecar with the same name plus ".json" stores the remaining background settings.
/// </summary>
public sealed class SavingsBackgroundOptions : INotifyPropertyChanged
{
    private string path = string.Empty;
    private string saveFileName = "collection-list-background.png";
    private bool isSave;

    /// <summary>Directory in which the image and its settings file are stored.</summary>
    public string Path
    {
        get => path;
        set => SetField(ref path, value ?? string.Empty);
    }

    /// <summary>Stable image file name used by this list instance.</summary>
    public string SaveFileName
    {
        get => saveFileName;
        set => SetField(ref saveFileName, value ?? string.Empty);
    }

    /// <summary>Whether this list persists its background configuration.</summary>
    public bool IsSave
    {
        get => isSave;
        set
        {
            IsSaveExplicitlySet = true;
            SetField(ref isSave, value);
        }
    }

    internal bool IsSaveExplicitlySet { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetIsSaveFromStorage(bool value) => SetField(ref isSave, value, nameof(IsSave));

    private void SetField<T>(
        ref T field,
        T value,
        [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
