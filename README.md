# CollectionLib

Shared .NET MAUI infrastructure for collection-backed list controls.

## Collection pipeline

Derive a collection from:

`CollectionViewCollection<TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter, TFilterRule, TSearchRule, TGroupRule, TSortRule>`.

The derived collection supplies only domain selectors:

- sort key selector and default sort parameter;
- filter matcher;
- searchable values selector;
- grouping key selector.

The base class provides observable source items, ordered sorting, filter/search/group rule collections, shared search options, an observable read-only filtered view, and hierarchical group nodes. Items and rule objects are observed for property changes. Call `RefreshViews()` or `RefreshGroups()` if a selector depends on external state that is not observable from the item or rule.

## Shared view controller

`CollectionViewController<T...>` manages the interaction state for any `CollectionViewCollection<T...>`:

- selecting search fields and rebuilding search rules;
- strict/contains/Levenshtein search settings and case sensitivity;
- setting, cycling and clearing filter rules;
- cycling sort direction, choosing the sort algorithm and reordering sort priority;
- enabling, clearing and reordering hierarchical grouping levels.

It implements `ICollectionListViewController`. A typed `CollectionListView<T...>` creates and owns its controller when `ItemsSource` changes; `CollectionListViewBase.ViewController` remains available for lower-level scenarios. The shared UI exposes `ItemCount`, `HasSearch`, `HasFilters`, `HasSorting` and `HasGrouping`.

## Shared MAUI control infrastructure

Use `CollectionListViewBase` as the XAML layout layer and derive a typed control from `CollectionListView<T...>`:

```csharp
public sealed class ProductListView : CollectionListView<
    Product, ProductFilter, ProductSearch, ProductGroup, ProductSort,
    ProductFilterRule, ProductSearchRule, ProductGroupRule, ProductSortRule>
{
    public ProductListView()
        : base(searchParameters, sortParameters, filterParameters, groupParameters,
               GetSearchName, GetSortName, GetFilterName, GetGroupName, FormatGroupValue)
    {
        InitializeComponent();
    }

    protected override View CreateItemView(Product item) => new ProductRow { Item = item };
}
```

The generic control owns the `ItemsSource` binding, controller and collection subscriptions, item-view cache, flat/grouped rendering, search/sort/filter/group menus, popup lifecycle, applied-state indicators and common background settings. It wraps each domain view in the shared background surface. Override `WrapItemView(...)` only when a domain item needs extra actions such as swipe or delete.

The domain control supplies its enum/rule types, parameter lists, display-name/value selectors and the item-view factory. Optional hooks add domain-specific filter fields or popup entries without rebuilding standard menus. A domain-specific header strip can be injected through `AdditionalHeaderContent`.

The non-generic base owns reusable bindable presentation properties:

- `Title`, `Icon`, `DockTitleText`;
- `SearchEffect` (`CollectionSearchEffect`);
- `GroupDisplayMode` (`CollectionGroupDisplayMode`);
- `GroupExpandEffect` (`CollectionGroupExpandEffect`);
- `ItemBackgroundMode` (`CollectionItemBackgroundMode`), `ItemBackgroundColor`, `ItemBackgroundBrush`;
- `ListBackgroundMode` (`CollectionListBackgroundMode`), `ListBackgroundColor`, `ListBackgroundBrush`, `ListBackgroundImageSource`;
- `ViewController`.

The base control owns the behavior behind these settings. It executes common search-panel and group expand/collapse animations, resolves item and viewport background colors, and applies image, custom, base and transparent background modes. `CollectionListView<T...>` automatically wraps and registers item surfaces. A derived component calls `ConfigureListBackground(...)` once for its shared viewport and `RegisterThemedBackgroundSurface(...)` only for additional domain-specific surfaces.

`GroupDisplayMode` is a shared bindable property. When it changes, the base refreshes automatic item backgrounds and calls `OnGroupDisplayModeChanged()`. The base also owns the generic block and connected-line group renderers, recursive group counting, tree connectors and expansion transitions. These renderers consume `GroupNode<TItem, TGroupParameter>`; the derived control supplies only the item-view factory and group-parameter label/value formatters. Shared chevrons are resolved through CollectionLib's resource keys.

The common popup building APIs are also available to subclasses:

- `CollectionListOptionDescriptor` describes an option using text, state icon, action, availability and optional reorder callback;
- `CollectionListSectionDescriptor` describes a collapsible section, its option descriptors and optional custom content;
- `AddPopupOption(...)` and `AddPopupSection(...)` compose the descriptors, track their visual state and attach shared drag-and-drop reordering;
- `CreatePopupSection(...)` and `CreatePopupDivider(...)` provide reusable section and separator helpers.

Popup presentation is XAML-first. `CollectionListViewBase.xaml` loads the shared theme styles, while `CollectionPopupOptionView`, `CollectionPopupSectionView`, `CollectionPopupCommandView` and `CollectionPopupDividerView` define the actual visual trees in their own XAML files. `CollectionListPopupBuilder` now only creates/configures those XAML-backed views and applies runtime callbacks; it no longer constructs grids, labels, images or separators imperatively. Drag-and-drop payloads, option availability predicates and domain actions remain in C# because they are runtime behavior rather than value conversion.

A derived list remains responsible for its domain-specific item template and descriptors (for example, message-type chips or document-status filters). It does not need to reimplement the collection pipeline, rule orchestration, popup presentation, option availability tracking or drag-and-drop mechanics.

## Shared control resources

CollectionLib packages its own neutral list icons and shared close animation. The generic MAUI resource-resolution implementation lives in the sibling `ResourcesLib` project. CollectionLib retains only its resource keys, default resource dictionary and packaged assets; it does not add another resolver facade.

Resolve images directly through `ImageResourceResolver`:

```csharp
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using ResourcesLib.Resolvers;

ImageSource searchIcon = ImageResourceResolver.Get(
    CollectionListImageKeys.ListSearch,
    "list_search.svg",
    CollectionListDefaultResourceDictionary.Instance);
```

Application values may be an `ImageSource` or a file-name string. Application resources take precedence over the CollectionLib defaults. When no resource value is available, the supplied file name is used; SVG paths are normalized to the PNG files generated by the MAUI image pipeline.

Resolve Lottie settings directly through `LottieResourceResolver`:

```csharp
using CollectionLib.Resources;
using CollectionLib.Resources.ExtensionResources;
using ResourcesLib.Models;
using ResourcesLib.Resolvers;

LottieResourceSettings closeSettings = LottieResourceResolver.GetSettings(
    CollectionListLottieKeys.Close,
    defaultResources: CollectionListDefaultResourceDictionary.Instance,
    defaultSource: "Lotties/close.json",
    defaultRestingProgress: 1d);
```

Application overrides use the generic `LottieResourceSettings` model (or a source string):

```csharp
Application.Current!.Resources[CollectionListImageKeys.ListSearch] = "custom_search.png";

Application.Current.Resources[CollectionListLottieKeys.Close] =
    new LottieResourceSettings
    {
        Source = "Lotties/custom_close.json",
        RestingProgress = 0.85,
        Speed = 1.2
    };
```

Custom images and Lottie files must be included in the consuming application's MAUI resources. The default resource dictionary is passed explicitly to the resolver; applications do not need to merge it into `Application.Resources`.

## Typical setup

The control creates a controller when its collection is assigned:

```csharp
var controller = new CollectionViewController<
    TItem, TFilterParameter, TSearchParameter, TGroupParameter, TSortParameter,
    TFilterRule, TSearchRule, TGroupRule, TSortRule>(
        collection,
        sortParameters,
        groupParameters,
        searchParameters);

ViewController = controller;
```

The controller should be disposed when the owning control replaces its collection or is disposed. The derived control provides its own item renderer and adds options with `AddPopupOption`, supplying domain selectors and actions.

Color properties remain supported for existing callers. Setting `ItemBackgroundColor` or `ListBackgroundColor` clears the corresponding custom brush; setting a brush takes precedence over its legacy color property.

## Color and gradient backgrounds

The view-options popup includes a shared color editor with a saturation/value plane, hue spectrum, HEX and RGB inputs, and preset swatches. Color and gradient modes use icon buttons with tooltips. Gradient mode supports 2–5 colors, controlled by a color-count slider or the add/remove buttons; the maximum is fixed in the control. Quick gradients replace the quick-color palette while gradient mode is active. Gradient motion can be switched between static (default), flowing, rotating and pulsing effects using icon buttons with tooltips. Motion settings are exposed as `ItemGradientMotionMode` and `ListGradientMotionMode` and are stored with the optional background persistence settings. Stops use circular selectors, a position slider and an angle slider. The picker draws its saturation/value plane and hue spectrum with gradient paints rather than per-cell color loops.

Both picker instances return a `Brush`, so gradients can be used for item surfaces and the list viewport. The same properties are also available to callers:

```csharp
ListBackgroundMode = CollectionListBackgroundMode.Custom;
ListBackgroundBrush = new LinearGradientBrush(
    new GradientStopCollection
    {
        new(Colors.SteelBlue, 0),
        new(Colors.MediumPurple, 1)
    },
    new Point(0, 0.5),
    new Point(1, 0.5));

ItemBackgroundMode = CollectionItemBackgroundMode.Custom;
ItemBackgroundBrush = new SolidColorBrush(Colors.AliceBlue);
```

The old `HsvColorPickerView` type remains as a compatibility wrapper; new code should use `ColorBrushPickerView`.

## Background persistence

Background persistence is opt-in and scoped to one list instance. Leave `SavingsBackgroundOptions` unset to hide the save toggle and keep the existing non-persistent behavior. When configured, `Path` is the directory for this list, and `SaveFileName` is its dedicated image file name. CollectionLib stores the image at that path and writes the brush/mode settings to a JSON sidecar named `SaveFileName + ".json"`.

```csharp
using CollectionLib.Models;
using Microsoft.Maui.Storage;

var backgroundStorage = new SavingsBackgroundOptions
{
    Path = Path.Combine(FileSystem.AppDataDirectory, "CollectionBackgrounds", "orders"),
    SaveFileName = "orders-background.png"
};

ordersList.SavingsBackgroundOptions = backgroundStorage;
```

The view-options section then shows an icon toggle for saving. Enabling it persists the selected image and both item/list background modes, solid colors and linear gradients. The save-enabled state is also remembered, so the same list restores its settings after the application restarts. Give every list a separate directory or file name to keep its settings independent. Disabling persistence stops automatic restore on the next launch; previously written files are left in place.

## Notification handling

`HandlerAction` centralizes notifications raised by the library. Its static `Progress` field uses an alert dialog by default, while an application can replace it with any `IProgress<HandlerAction.Message>` implementation:

```csharp
using CollectionLib.Handlers;

HandlerAction.Progress = new Progress<HandlerAction.Message>(message =>
{
    // Forward the notification to the application's toast, snackbar or notification service.
    Debug.WriteLine($"{message.Title}: {message.Text}");
});
```

A notification can also be reported directly:

```csharp
HandlerAction.Report(
    "Background image",
    "The image could not be loaded.",
    page: currentPage);
```

The message contains its title, text, default dialog button caption and an optional MAUI page. The default handler uses that page (or the current application's active window page) to display the dialog on the UI thread. Call `HandlerAction.ResetProgress()` to restore the default dialog handler. Confirmation dialogs that require a `bool` result remain explicit dialogs and are not routed through `IProgress`.