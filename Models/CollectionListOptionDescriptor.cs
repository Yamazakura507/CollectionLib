namespace CollectionLib.Models;

/// <summary>
/// Describes one reusable option in a collection-list context. The owning list supplies
/// the domain-specific state image, action and optional reorder operation.
/// </summary>
/// <param name="Key">Optional stable key used to register the visual for later icon or visibility updates.</param>
/// <param name="Text">Text displayed for the option.</param>
/// <param name="IconSource">Image displayed beside the option text.</param>
/// <param name="Action">Action invoked when the option is selected.</param>
/// <param name="AddDivider">Whether to add a separator after the option.</param>
/// <param name="ReorderAction">Optional callback that receives the source and destination indices when the option is dragged.</param>
/// <param name="IsAvailable">Optional predicate that determines whether the option is visible.</param>
public sealed record CollectionListOptionDescriptor(
    string? Key,
    string Text,
    ImageSource IconSource,
    Action Action,
    bool AddDivider = true,
    Action<int, int>? ReorderAction = null,
    Func<bool>? IsAvailable = null);
