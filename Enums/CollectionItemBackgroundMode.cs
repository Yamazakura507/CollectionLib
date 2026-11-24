namespace CollectionLib.Enums;

/// <summary>
/// Background rendering mode for each item in a collection list.
/// </summary>
public enum CollectionItemBackgroundMode
{
    /// <summary>
    /// Let the control choose based on its display mode.
    /// </summary>
    Automatic,
    /// <summary>
    /// Don't draw an item surface.
    /// </summary>
    Transparent,
    /// <summary>
    /// Use the control's default item surface.
    /// </summary>
    Base,
    /// <summary>
    /// Use the configured custom item color.
    /// </summary>
    Custom
}
