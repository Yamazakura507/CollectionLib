namespace CollectionLib.Enums;

/// <summary>
/// Describes the animated motion applied to a linear-gradient background.
/// </summary>
public enum GradientMotionMode
{
    /// <summary>Keep gradient colors still.</summary>
    None,

    /// <summary>Move the gradient back and forth along its direction.</summary>
    Flow,

    /// <summary>Rotate the gradient around its center.</summary>
    Rotate,

    /// <summary>Expand and contract the gradient.</summary>
    Pulse
}
