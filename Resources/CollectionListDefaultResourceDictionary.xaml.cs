namespace CollectionLib.Resources;

/// <summary>
/// Centralized built-in resources and defaults for CollectionLib controls.
/// Application resources take precedence and do not require this dictionary to be merged.
/// </summary>
public partial class CollectionListDefaultResourceDictionary : ResourceDictionary
{
    private static readonly Lazy<CollectionListDefaultResourceDictionary> instance = new(() => new CollectionListDefaultResourceDictionary());

    /// <summary>
    /// Gets the lazily created dictionary containing the library's built-in list resources.
    /// </summary>
    public static CollectionListDefaultResourceDictionary Instance => instance.Value;

    /// <summary>
    /// Initializes the resource dictionary from its compiled XAML definition.
    /// </summary>
    public CollectionListDefaultResourceDictionary()
    {
        InitializeComponent();
    }
}
