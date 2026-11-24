using CollectionLib.Controls.Popup;
using CollectionLib.Models.Popup;

namespace CollectionLib.Extensions;

/// <summary>
/// Creates the shared popup visuals from XAML-backed controls.
/// This class only applies runtime data, callbacks and optional compatibility overrides;
/// the visual tree and layout properties are defined by the corresponding XAML files.
/// </summary>
public static class CollectionListPopupBuilder
{
    /// <summary>
    /// Creates a collapsible section with its layout supplied by <see cref="CollectionPopupSectionView"/>.
    /// Theme colors are provided by the resource styles declared in CollectionListViewBase.xaml.
    /// </summary>
    public static CollectionPopupSectionVisual CreateSection(
        string title,
        Action<VerticalStackLayout> buildContent,
        Func<bool, ImageSource> chevronSource,
        bool initiallyExpanded = false,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
        => CreateSectionCore(
            title, buildContent, chevronSource,
            initiallyExpanded, addDivider, isAvailable,
            textColor: null, dividerColor: null);

    /// <summary>
    /// Creates a collapsible section and explicitly overrides its XAML theme colors.
    /// This overload is retained for callers using the original builder API.
    /// </summary>
    public static CollectionPopupSectionVisual CreateSection(
        string title,
        Action<VerticalStackLayout> buildContent,
        Func<bool, ImageSource> chevronSource,
        Color textColor,
        Color dividerColor,
        bool initiallyExpanded = false,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
        => CreateSectionCore(
            title, buildContent, chevronSource,
            initiallyExpanded, addDivider, isAvailable,
            textColor, dividerColor);

    private static CollectionPopupSectionVisual CreateSectionCore(
        string title,
        Action<VerticalStackLayout> buildContent,
        Func<bool, ImageSource> chevronSource,
        bool initiallyExpanded,
        bool addDivider,
        Func<bool>? isAvailable,
        Color? textColor,
        Color? dividerColor)
    {
        ArgumentNullException.ThrowIfNull(buildContent);
        ArgumentNullException.ThrowIfNull(chevronSource);

        CollectionPopupSectionView view = new();
        view.Configure(title, chevronSource, initiallyExpanded, addDivider, textColor, dividerColor);
        buildContent(view.BodyLayout);

        return new CollectionPopupSectionVisual(
            view,
            view.BodyLayout,
            view.ChevronImage,
            isAvailable);
    }

    /// <summary>
    /// Creates a tappable option row using the XAML-defined shared option view.
    /// Theme colors are provided by the resource styles declared in CollectionListViewBase.xaml.
    /// </summary>
    public static CollectionPopupOptionVisual CreateOption(
        string text,
        ImageSource iconSource,
        Action action,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
        => CreateOptionCore(text, iconSource, action, addDivider, isAvailable, null, null);

    /// <summary>
    /// Creates a tappable option row and explicitly overrides its XAML theme colors.
    /// This overload is retained for callers using the original builder API.
    /// </summary>
    public static CollectionPopupOptionVisual CreateOption(
        string text,
        ImageSource iconSource,
        Action action,
        Color textColor,
        Color dividerColor,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
        => CreateOptionCore(text, iconSource, action, addDivider, isAvailable, textColor, dividerColor);

    private static CollectionPopupOptionVisual CreateOptionCore(
        string text,
        ImageSource iconSource,
        Action action,
        bool addDivider,
        Func<bool>? isAvailable,
        Color? textColor,
        Color? dividerColor)
    {
        ArgumentNullException.ThrowIfNull(iconSource);
        ArgumentNullException.ThrowIfNull(action);

        CollectionPopupOptionView view = new();
        view.Configure(text, iconSource, action, addDivider, textColor, dividerColor);

        return new CollectionPopupOptionVisual(
            view,
            view.Row,
            view.StateIcon,
            view.Label,
            isAvailable);
    }

    /// <summary>
    /// Appends a horizontal separator using the theme-aware style declared in XAML.
    /// </summary>
    /// <param name="host">Layout that receives the separator.</param>
    /// <param name="indent">Whether to indent the separator to align with option labels.</param>
    public static void AddDivider(this VerticalStackLayout host, bool indent = true)
        => AddDividerCore(host, color: null, indent);

    /// <summary>
    /// Appends a horizontal separator with an explicit color.
    /// This overload is retained for callers using the original builder API.
    /// </summary>
    public static void AddDivider(this VerticalStackLayout host, Color color, bool indent = true)
        => AddDividerCore(host, color, indent);

    private static void AddDividerCore(VerticalStackLayout host, Color? color, bool indent)
    {
        ArgumentNullException.ThrowIfNull(host);

        CollectionPopupDividerView divider = new()
        {
            Indent = indent
        };

        if (color is not null)
            divider.BackgroundColor = color;

        host.Children.Add(divider);
    }

    /// <summary>
    /// Creates a tappable command row using the XAML-defined shared command view.
    /// The command label uses the theme-aware style declared in CollectionListViewBase.xaml.
    /// </summary>
    public static Grid CreateCommand(string text, ImageSource iconSource, Action action)
        => CreateCommandCore(text, iconSource, action, textColor: null);

    /// <summary>
    /// Creates a tappable command row and explicitly overrides its label color.
    /// This overload is retained for callers using the original builder API.
    /// </summary>
    public static Grid CreateCommand(string text, ImageSource iconSource, Action action, Color textColor)
        => CreateCommandCore(text, iconSource, action, textColor);

    private static Grid CreateCommandCore(string text, ImageSource iconSource, Action action, Color? textColor)
    {
        ArgumentNullException.ThrowIfNull(iconSource);
        ArgumentNullException.ThrowIfNull(action);

        CollectionPopupCommandView view = new();
        view.Configure(text, iconSource, action, textColor);
        return view;
    }
}
