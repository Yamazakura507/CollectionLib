namespace CollectionLib.Enums;

/// <summary>
/// Selects the visual transition used when a group in a collection list expands or collapses.
/// </summary>
public enum CollectionGroupExpandEffect
{
    /// <summary>
    /// Show or hide the group content immediately.
    /// </summary>
    None,

    /// <summary>
    /// Fade the group content in or out.
    /// </summary>
    Fade,

    /// <summary>
    /// Reveal or hide the group content with a short vertical movement.
    /// </summary>
    Slide,

    /// <summary>
    /// Reveal or hide the group content with a subtle scale transition.
    /// </summary>
    Scale
}
