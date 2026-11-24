namespace CollectionLib.Models;

/// <summary>
/// Describes a collapsible collection-list context section. Options are rendered by the
/// shared control infrastructure; an additional content builder can append domain-specific
/// editors such as text entries or sliders.
/// </summary>
/// <param name="Title">Text displayed in the section header.</param>
/// <param name="Options">Options rendered in the section body, in display order.</param>
/// <param name="AppendContent">Optional callback for appending custom content after the described options.</param>
/// <param name="InitiallyExpanded">Whether the section body is initially visible.</param>
/// <param name="AddDividerAfter">Whether to append a separator after the section.</param>
/// <param name="IsAvailable">Optional predicate that determines whether the complete section is visible.</param>
public sealed record CollectionListSectionDescriptor(
    string Title,
    IReadOnlyList<CollectionListOptionDescriptor> Options,
    Action<VerticalStackLayout>? AppendContent = null,
    bool InitiallyExpanded = false,
    bool AddDividerAfter = true,
    Func<bool>? IsAvailable = null);
