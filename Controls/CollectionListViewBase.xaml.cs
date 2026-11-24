using CollectionLib.Enums;
using CollectionLib.Extensions;
using CollectionLib.Interfaces;
using CollectionLib.Models;
using CollectionLib.Models.Popup;

namespace CollectionLib.Controls;

/// <summary>
/// Shared MAUI base for collection-specific list controls. Shared popup presentation is declared
/// in XAML, while this class manages controller state, reusable popup composition and gestures.
/// Derived controls provide their domain-specific item templates, filter descriptors and rendering.
/// </summary>
public abstract partial class CollectionListViewBase : ContentView
{
    private const string PopupReorderPayloadKey = "CollectionListViewBase.ReorderPayload";

    private readonly HashSet<VisualElement> itemBackgroundSurfaces = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<VisualElement> transitioningGroups = new(ReferenceEqualityComparer.Instance);
    private Border? listViewportBorder;
    private Image? listBackgroundImage;
    private List<(VisualElement Element, Color Light, Color Dark)> themedBackgroundSurfaces = [];
    private VisualElement[] transparentBackgroundSurfaces = [];

    /// <summary>
    /// Gets the registered popup option visuals, indexed by descriptor key.
    /// </summary>
    protected Dictionary<string, CollectionPopupOptionVisual> PopupOptionVisuals { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the popup section visuals created by the derived control.
    /// </summary>
    protected List<CollectionPopupSectionVisual> PopupSectionVisuals { get; } = [];

    /// <summary>
    /// Initializes the shared XAML resources used by popup controls.
    /// </summary>
    protected CollectionListViewBase()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets a theme-aware foreground color for popup text.
    /// </summary>
    protected Color PopupTextColor => 
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#F1F3F5") : Color.FromArgb("#252B33");

    /// <summary>
    /// Gets a theme-aware muted foreground color for secondary popup text.
    /// </summary>
    protected Color PopupMutedColor => 
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#ADB5C0") : Color.FromArgb("#68717D");

    /// <summary>
    /// Gets a theme-aware color for popup separators.
    /// </summary>
    protected Color PopupDividerColor => 
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#3E444E") : Color.FromArgb("#E7EAF0");

    /// <summary>
    /// Gets or sets the controller that supplies this list's shared search, filter, sort and group state.
    /// </summary>
    public ICollectionListViewController? ViewController
    {
        get => (ICollectionListViewController?)GetValue(ViewControllerProperty);
        set => SetValue(ViewControllerProperty, value);
    }

    /// <summary>
    /// Gets or sets the animation used when the search panel appears or disappears.
    /// </summary>
    public CollectionSearchEffect SearchEffect
    {
        get => (CollectionSearchEffect)GetValue(SearchEffectProperty);
        set => SetValue(SearchEffectProperty, value);
    }

    /// <summary>
    /// Gets or sets the presentation mode used for collection groups.
    /// </summary>
    public CollectionGroupDisplayMode GroupDisplayMode
    {
        get => (CollectionGroupDisplayMode)GetValue(GroupDisplayModeProperty);
        set => SetValue(GroupDisplayModeProperty, value);
    }

    /// <summary>
    /// Gets or sets the animation used when a group expands or collapses.
    /// </summary>
    public CollectionGroupExpandEffect GroupExpandEffect
    {
        get => (CollectionGroupExpandEffect)GetValue(GroupExpandEffectProperty);
        set => SetValue(GroupExpandEffectProperty, value);
    }

    /// <summary>
    /// Gets the current source item count, or zero when no controller is attached.
    /// </summary>
    public int ItemCount => ViewController?.ItemCount ?? 0;

    /// <summary>
    /// Gets whether a search query is active.
    /// </summary>
    public bool HasSearch => ViewController?.HasSearch ?? false;

    /// <summary>
    /// Gets whether any filters are active.
    /// </summary>
    public bool HasFilters => ViewController?.HasFilters ?? false;

    /// <summary>
    /// Gets whether any sort rules are active.
    /// </summary>
    public bool HasSorting => ViewController?.HasSorting ?? false;

    /// <summary>
    /// Gets whether any grouping rules are active.
    /// </summary>
    public bool HasGrouping => ViewController?.HasGrouping ?? false;

    /// <summary>
    /// Gets or sets the primary text displayed in the list header.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the icon displayed in the list header.
    /// </summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary title displayed by the list when docked.
    /// </summary>
    public string DockTitleText
    {
        get => (string)GetValue(DockTitleTextProperty);
        set => SetValue(DockTitleTextProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Background mode applied to each item.
    /// </summary>
    public CollectionItemBackgroundMode ItemBackgroundMode
    {
        get => (CollectionItemBackgroundMode)GetValue(ItemBackgroundModeProperty);
        set => SetValue(ItemBackgroundModeProperty, value);
    }

    /// <summary>
    /// Custom item surface color.
    /// </summary>
    public Color ItemBackgroundColor
    {
        get => (Color)GetValue(ItemBackgroundColorProperty);
        set => SetValue(ItemBackgroundColorProperty, value);
    }

    /// <summary>
    /// Background mode applied to the complete list viewport.
    /// </summary>
    public CollectionListBackgroundMode ListBackgroundMode
    {
        get => (CollectionListBackgroundMode)GetValue(ListBackgroundModeProperty);
        set => SetValue(ListBackgroundModeProperty, value);
    }

    /// <summary>
    /// Custom list viewport color.
    /// </summary>
    public Color ListBackgroundColor
    {
        get => (Color)GetValue(ListBackgroundColorProperty);
        set => SetValue(ListBackgroundColorProperty, value);
    }

    /// <summary>
    /// Image source used when <see cref="ListBackgroundMode"/> is Image.
    /// </summary>
    public ImageSource? ListBackgroundImageSource
    {
        get => (ImageSource?)GetValue(ListBackgroundImageSourceProperty);
        set => SetValue(ListBackgroundImageSourceProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="ViewController"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ViewControllerProperty =
        BindableProperty.Create(nameof(ViewController), typeof(ICollectionListViewController),
            typeof(CollectionListViewBase), default(ICollectionListViewController),
            propertyChanged: OnViewControllerChanged);

    /// <summary>
    /// Identifies the <see cref="SearchEffect"/> bindable property.
    /// </summary>
    public static readonly BindableProperty SearchEffectProperty =
        BindableProperty.Create(nameof(SearchEffect), typeof(CollectionSearchEffect),
            typeof(CollectionListViewBase), CollectionSearchEffect.Fade);

    /// <summary>
    /// Identifies the <see cref="GroupDisplayMode"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GroupDisplayModeProperty =
        BindableProperty.Create(nameof(GroupDisplayMode), typeof(CollectionGroupDisplayMode),
            typeof(CollectionListViewBase), CollectionGroupDisplayMode.Blocks,
            propertyChanged: OnGroupDisplayModeChanged);

    /// <summary>
    /// Identifies the <see cref="GroupExpandEffect"/> bindable property.
    /// </summary>
    public static readonly BindableProperty GroupExpandEffectProperty =
        BindableProperty.Create(nameof(GroupExpandEffect), typeof(CollectionGroupExpandEffect),
            typeof(CollectionListViewBase), CollectionGroupExpandEffect.Fade);

    /// <summary>
    /// Identifies the <see cref="Title"/> bindable property.
    /// </summary>
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CollectionListViewBase), string.Empty, propertyChanged: OnHeaderConfigurationChanged);

    /// <summary>
    /// Identifies the <see cref="Icon"/> bindable property.
    /// </summary>
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(CollectionListViewBase), default(ImageSource), propertyChanged: OnHeaderConfigurationChanged);

    /// <summary>
    /// Identifies the <see cref="DockTitleText"/> bindable property.
    /// </summary>
    public static readonly BindableProperty DockTitleTextProperty =
        BindableProperty.Create(nameof(DockTitleText), typeof(string), typeof(CollectionListViewBase), string.Empty, propertyChanged: OnHeaderConfigurationChanged);

    /// <summary>
    /// Identifies the <see cref="ItemBackgroundMode"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ItemBackgroundModeProperty =
        BindableProperty.Create(nameof(ItemBackgroundMode), typeof(CollectionItemBackgroundMode),
            typeof(CollectionListViewBase), CollectionItemBackgroundMode.Automatic,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                CollectionListViewBase control = (CollectionListViewBase)bindable;
                control.RefreshItemBackgroundSurfaces();
                control.OnItemBackgroundConfigurationChanged();
            });

    /// <summary>
    /// Identifies the <see cref="ItemBackgroundColor"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ItemBackgroundColorProperty =
        BindableProperty.Create(nameof(ItemBackgroundColor), typeof(Color),
            typeof(CollectionListViewBase), Color.FromArgb("#F7F8FA"),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                CollectionListViewBase control = (CollectionListViewBase)bindable;
                control.RefreshItemBackgroundSurfaces();
                control.OnItemBackgroundConfigurationChanged();
            });

    /// <summary>
    /// Identifies the <see cref="ListBackgroundMode"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ListBackgroundModeProperty =
        BindableProperty.Create(nameof(ListBackgroundMode), typeof(CollectionListBackgroundMode),
            typeof(CollectionListViewBase), CollectionListBackgroundMode.Base,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                CollectionListViewBase control = (CollectionListViewBase)bindable;
                control.ApplyListBackgroundConfiguration();
                control.OnListBackgroundConfigurationChanged();
            });

    /// <summary>
    /// Identifies the <see cref="ListBackgroundColor"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ListBackgroundColorProperty =
        BindableProperty.Create(nameof(ListBackgroundColor), typeof(Color),
            typeof(CollectionListViewBase), Color.FromArgb("#FFFCFA"),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                CollectionListViewBase control = (CollectionListViewBase)bindable;
                control.ApplyListBackgroundConfiguration();
                control.OnListBackgroundConfigurationChanged();
            });

    /// <summary>
    /// Identifies the <see cref="ListBackgroundImageSource"/> bindable property.
    /// </summary>
    public static readonly BindableProperty ListBackgroundImageSourceProperty =
        BindableProperty.Create(nameof(ListBackgroundImageSource), typeof(ImageSource),
            typeof(CollectionListViewBase), default(ImageSource),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                CollectionListViewBase control = (CollectionListViewBase)bindable;
                control.ApplyListBackgroundConfiguration();
                control.OnListBackgroundConfigurationChanged();
            });

    /// <summary>
    /// Registers an item surface so the base control can apply the configured background mode.
    /// </summary>
    protected void RegisterItemBackgroundSurface(VisualElement surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        itemBackgroundSurfaces.Add(surface);
        ApplyItemBackground(surface);
    }

    /// <summary>
    /// Unregisters an item surface that is no longer owned by the collection view.
    /// </summary>
    protected void UnregisterItemBackgroundSurface(VisualElement surface)
    {
        if (surface is not null) itemBackgroundSurfaces.Remove(surface);
    }

    /// <summary>
    /// Removes all item surfaces registered by the derived collection view.
    /// </summary>
    protected void ClearItemBackgroundSurfaces() => itemBackgroundSurfaces.Clear();

    /// <summary>
    /// Connects the generic list background renderer to the visual parts supplied by a derived view.
    /// Theme surfaces use the supplied light and dark colors when no image background is active.
    /// Transparent surfaces remain transparent in every background mode.
    /// </summary>
    protected void ConfigureListBackground(
        Border viewport,
        Image backgroundImage,
        List<(VisualElement Element, Color Light, Color Dark)>? themeSurfaces = null,
        params VisualElement[] transparentSurfaces)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(backgroundImage);

        listViewportBorder = viewport;
        listBackgroundImage = backgroundImage;
        themedBackgroundSurfaces = themeSurfaces?.ToList() ?? [];
        transparentBackgroundSurfaces = transparentSurfaces ?? [];
        ApplyListBackgroundConfiguration();
    }

    /// <summary>
    /// Applies the selected search-panel entrance animation to a supplied visual element.
    /// </summary>
    protected async Task ShowSearchPanelEffectAsync(VisualElement panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        panel.IsVisible = true;
        panel.Opacity = 1;

        switch (SearchEffect)
        {
            case CollectionSearchEffect.Slide:
                panel.Opacity = 0;
                panel.TranslationY = -12;

                await Task.WhenAll(
                    panel.FadeToAsync(1, 170, Easing.CubicOut),
                    panel.TranslateToAsync(0, 0, 190, Easing.CubicOut));
                break;
            case CollectionSearchEffect.Scale:
                panel.Opacity = 0;
                panel.Scale = 0.94;

                await Task.WhenAll(
                    panel.FadeToAsync(1, 160, Easing.CubicOut),
                    panel.ScaleToAsync(1, 180, Easing.CubicOut));
                break;
            default:
                panel.Opacity = 0;

                await panel.FadeToAsync(1, 160, Easing.CubicOut);
                break;
        }

        panel.Opacity = 1;
        panel.TranslationY = 0;
        panel.Scale = 1;
    }

    /// <summary>
    /// Applies the selected search-panel exit animation to a supplied visual element.
    /// </summary>
    protected async Task HideSearchPanelEffectAsync(VisualElement panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        switch (SearchEffect)
        {
            case CollectionSearchEffect.Slide:
                await Task.WhenAll(
                    panel.FadeToAsync(0, 110, Easing.CubicIn),
                    panel.TranslateToAsync(0, -10, 130, Easing.CubicIn));
                break;
            case CollectionSearchEffect.Scale:
                await Task.WhenAll(
                    panel.FadeToAsync(0, 110, Easing.CubicIn),
                    panel.ScaleToAsync(0.96, 120, Easing.CubicIn));
                break;
            default:
                await panel.FadeToAsync(0, 120, Easing.CubicIn);
                break;
        }

        panel.IsVisible = false;
        panel.Opacity = 1;
        panel.TranslationY = 0;
        panel.Scale = 1;
    }

    /// <summary>
    /// Toggles a group's children using the common collection group expansion animation.
    /// Returns null when another transition is already running for the same visual element.
    /// </summary>
    protected async Task<bool?> ToggleGroupChildrenEffectAsync(VisualElement children, Action<bool>? onExpandedChanged = null)
    {
        ArgumentNullException.ThrowIfNull(children);

        if (!transitioningGroups.Add(children)) return null;

        bool expand = !children.IsVisible;
        const uint expandDuration = 180;
        const uint collapseDuration = 130;

        try
        {
            if (expand)
            {
                children.IsVisible = true;

                switch (GroupExpandEffect)
                {
                    case CollectionGroupExpandEffect.Fade:
                        children.Opacity = 0;
                        await children.FadeToAsync(1, expandDuration, Easing.CubicOut);
                        break;
                    case CollectionGroupExpandEffect.Slide:
                        children.Opacity = 0;
                        children.TranslationY = -9;
                        await Task.WhenAll(
                            children.FadeToAsync(1, expandDuration, Easing.CubicOut),
                            children.TranslateToAsync(0, 0, expandDuration, Easing.CubicOut));
                        break;
                    case CollectionGroupExpandEffect.Scale:
                        children.Opacity = 0;
                        children.Scale = 0.97;
                        await Task.WhenAll(
                            children.FadeToAsync(1, expandDuration, Easing.CubicOut),
                            children.ScaleToAsync(1, expandDuration, Easing.CubicOut));
                        break;
                }
            }
            else
            {
                switch (GroupExpandEffect)
                {
                    case CollectionGroupExpandEffect.Fade:
                        await children.FadeToAsync(0, collapseDuration, Easing.CubicIn);
                        break;
                    case CollectionGroupExpandEffect.Slide:
                        await Task.WhenAll(
                            children.FadeToAsync(0, collapseDuration, Easing.CubicIn),
                            children.TranslateToAsync(0, -7, collapseDuration, Easing.CubicIn));
                        break;
                    case CollectionGroupExpandEffect.Scale:
                        await Task.WhenAll(
                            children.FadeToAsync(0, collapseDuration, Easing.CubicIn),
                            children.ScaleToAsync(0.97, collapseDuration, Easing.CubicIn));
                        break;
                }

                children.IsVisible = false;
            }

            return expand;
        }
        finally
        {
            children.Opacity = 1;
            children.TranslationY = 0;
            children.Scale = 1;
            children.IsVisible = expand;
            transitioningGroups.Remove(children);
            onExpandedChanged?.Invoke(expand);
        }
    }

    /// <summary>
    /// Clears the visual registries before rebuilding the popup content.
    /// </summary>
    protected void ResetPopupVisuals()
    {
        PopupOptionVisuals.Clear();
        PopupSectionVisuals.Clear();
    }

    /// <summary>
    /// Re-evaluates availability predicates for all registered popup options and sections.
    /// </summary>
    protected void RefreshPopupVisualAvailability()
    {
        foreach (CollectionPopupOptionVisual option in PopupOptionVisuals.Values) option.RefreshVisibility();
        foreach (CollectionPopupSectionVisual section in PopupSectionVisuals) section.RefreshVisibility();
    }

    /// <summary>
    /// Updates the state icon for a registered popup option.
    /// </summary>
    /// <param name="key">Key used when the option was registered.</param>
    /// <param name="source">New icon source.</param>
    protected void SetPopupOptionIcon(string key, ImageSource source)
    {
        if (PopupOptionVisuals.TryGetValue(key, out CollectionPopupOptionVisual? visual)) visual.SetIcon(source);
    }

    /// <summary>
    /// Creates a popup section using the XAML-defined shared section view.
    /// </summary>
    /// <param name="title">Section heading.</param>
    /// <param name="buildContent">Callback that populates the section body.</param>
    /// <param name="chevronSource">Factory for the expanded and collapsed state icons.</param>
    /// <param name="initiallyExpanded">Whether the section is initially expanded.</param>
    /// <param name="addDivider">Whether to append a separator.</param>
    /// <param name="isAvailable">Optional predicate controlling section visibility.</param>
    /// <returns>The created section's visual state.</returns>
    protected CollectionPopupSectionVisual CreatePopupSection(
        string title,
        Action<VerticalStackLayout> buildContent,
        Func<bool, ImageSource> chevronSource,
        bool initiallyExpanded = false,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
    {
        CollectionPopupSectionVisual section = 
            CollectionListPopupBuilder.CreateSection(title, buildContent, chevronSource, initiallyExpanded, addDivider, isAvailable);
        section.RefreshVisibility();

        return section;
    }

    /// <summary>
    /// Creates a popup option using the XAML-defined shared option view.
    /// </summary>
    /// <param name="text">Option label.</param>
    /// <param name="iconSource">Option icon source.</param>
    /// <param name="action">Action invoked when the option is selected.</param>
    /// <param name="addDivider">Whether to append a separator.</param>
    /// <param name="isAvailable">Optional predicate controlling option visibility.</param>
    /// <returns>The created option's visual state.</returns>
    protected CollectionPopupOptionVisual CreatePopupOption(
        string text,
        ImageSource iconSource,
        Action action,
        bool addDivider = true,
        Func<bool>? isAvailable = null)
    {
        CollectionPopupOptionVisual option = 
            CollectionListPopupBuilder.CreateOption(text, iconSource, action, addDivider, isAvailable);
        option.RefreshVisibility();

        return option;
    }

    /// <summary>
    /// Creates and registers a context option, including optional drag-and-drop reordering.
    /// </summary>
    /// <param name="host">Layout that receives the option container.</param>
    /// <param name="descriptor">Option text, icon, action, availability and optional reorder callback.</param>
    /// <returns>The created option visual, registered by key when the descriptor supplies one.</returns>
    protected CollectionPopupOptionVisual AddPopupOption(VerticalStackLayout host, CollectionListOptionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(descriptor.IconSource);
        ArgumentNullException.ThrowIfNull(descriptor.Action);

        CollectionPopupOptionVisual visual = 
            CreatePopupOption(descriptor.Text, descriptor.IconSource, descriptor.Action, descriptor.AddDivider, descriptor.IsAvailable);

        host.Children.Add(visual.Container);

        if (!string.IsNullOrWhiteSpace(descriptor.Key)) PopupOptionVisuals[descriptor.Key] = visual;
        if (descriptor.ReorderAction is not null)
        {
            DateTime suppressTapUntil = DateTime.MinValue;

            visual.Row.GestureRecognizers.Clear();
            visual.Row.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    if (DateTime.UtcNow >= suppressTapUntil) descriptor.Action();
                })
            });

            AttachPopupReorderGesture(visual.Row, visual.Container, host, descriptor.ReorderAction,
                () => suppressTapUntil = DateTime.UtcNow.AddMilliseconds(600));
        }

        return visual;
    }

    /// <summary>
    /// Builds a whole option section from descriptors, retaining any custom content.
    /// </summary>
    /// <param name="host">Layout that receives the section container.</param>
    /// <param name="descriptor">Section heading, options, optional content and visibility configuration.</param>
    /// <param name="chevronSource">Factory for the expanded and collapsed state icons.</param>
    /// <returns>The created section visual.</returns>
    protected CollectionPopupSectionVisual AddPopupSection(
        VerticalStackLayout host,
        CollectionListSectionDescriptor descriptor,
        Func<bool, ImageSource> chevronSource)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(descriptor);

        CollectionPopupSectionVisual section = CreatePopupSection(
            descriptor.Title,
            body =>
            {
                foreach (CollectionListOptionDescriptor option in descriptor.Options) AddPopupOption(body, option);

                descriptor.AppendContent?.Invoke(body);
            },
            chevronSource,
            descriptor.InitiallyExpanded,
            descriptor.AddDividerAfter,
            descriptor.IsAvailable);

        host.Children.Add(section.Container);
        PopupSectionVisuals.Add(section);

        return section;
    }

    private void AttachPopupReorderGesture(
        Grid row,
        VerticalStackLayout itemContainer,
        VerticalStackLayout host,
        Action<int, int> reorderAction,
        Action onDragDetected)
    {
        DragGestureRecognizer drag = new();

        drag.DragStarting += (_, args) =>
        {
            int sourceIndex = host.Children.IndexOf(itemContainer);

            if (sourceIndex < 0)
            {
                args.Cancel = true;

                return;
            }

            onDragDetected();

            row.Opacity = 0.65;
            row.Scale = 1.015;

            args.Data.Properties.Add(
                PopupReorderPayloadKey,
                new PopupReorderPayload(this, sourceIndex, reorderAction, host, itemContainer));
        };
        drag.DropCompleted += (_, _) =>
        {
            row.Opacity = 1;
            row.Scale = 1;
            row.BackgroundColor = Colors.Transparent;
        };

        DropGestureRecognizer drop = new();

        drop.DragOver += (_, args) =>
        {
            args.AcceptedOperation = DataPackageOperation.Copy;
            row.BackgroundColor = 
            Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#394657") : Color.FromArgb("#E8F1FC");
        };
        drop.DragLeave += (_, _) => row.BackgroundColor = Colors.Transparent;
        drop.Drop += (_, args) =>
        {
            row.BackgroundColor = Colors.Transparent;

            if (!args.Data.Properties.TryGetValue(PopupReorderPayloadKey, out object? data)
                || data is not PopupReorderPayload payload
                || !ReferenceEquals(payload.Owner, this)
                || !ReferenceEquals(payload.Host, host)) return;

            int targetIndex = host.Children.IndexOf(itemContainer);

            if (targetIndex >= 0 && payload.SourceIndex != targetIndex)
            {
                payload.ReorderAction(payload.SourceIndex, targetIndex);

                MovePopupOptionContainer(host, payload.SourceContainer, targetIndex);
            }

            args.Handled = true;
        };

        row.GestureRecognizers.Add(drag);
        row.GestureRecognizers.Add(drop);

        ToolTipProperties.SetText(row, "Перетащите строку, чтобы изменить порядок");
    }

    private static void MovePopupOptionContainer(VerticalStackLayout host, VerticalStackLayout container, int targetIndex)
    {
        int currentIndex = host.Children.IndexOf(container);

        if (currentIndex < 0 || currentIndex == targetIndex) return;

        host.Children.Remove(container);
        host.Children.Insert(Math.Clamp(targetIndex, 0, host.Children.Count), container);
    }

    /// <summary>
    /// Appends a divider using the XAML-defined separator view.
    /// </summary>
    /// <param name="host">Layout that receives the divider.</param>
    /// <param name="indent">Whether to indent the divider beneath the option icon column.</param>
    protected void CreatePopupDivider(VerticalStackLayout host, bool indent = true) => CollectionListPopupBuilder.AddDivider(host, indent);

    /// <summary>
    /// Creates a popup command row using the XAML-defined shared command view.
    /// </summary>
    /// <param name="text">Command label.</param>
    /// <param name="iconSource">Command icon.</param>
    /// <param name="action">Action invoked when the row is tapped.</param>
    /// <returns>The created command grid.</returns>
    protected Grid CreatePopupCommand(string text, ImageSource iconSource, Action action) => CollectionListPopupBuilder.CreateCommand(text, iconSource, action);

    private static void OnViewControllerChanged(BindableObject bindable, object oldValue, object newValue)
    {
        CollectionListViewBase control = (CollectionListViewBase)bindable;

        if (oldValue is ICollectionListViewController previous) previous.StateChanged -= control.ViewControllerOnStateChanged;
        if (newValue is ICollectionListViewController current) current.StateChanged += control.ViewControllerOnStateChanged;

        control.OnViewControllerStateChanged();
    }

    private static void OnHeaderConfigurationChanged(BindableObject bindable, object oldValue, object newValue)
        => ((CollectionListViewBase)bindable).OnHeaderConfigurationChanged();

    private static void OnGroupDisplayModeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        CollectionListViewBase control = (CollectionListViewBase)bindable;
        control.RefreshItemBackgroundSurfaces();
        control.OnGroupDisplayModeChanged();
    }

    private void ViewControllerOnStateChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.IsDispatchRequired) Dispatcher.Dispatch(OnViewControllerStateChanged);
        else OnViewControllerStateChanged();
    }

    /// <summary>
    /// Called whenever the attached collection controller changes its view state.
    /// </summary>
    protected virtual void OnViewControllerStateChanged() { }

    /// <summary>
    /// Called when title, icon or secondary title changes.
    /// </summary>
    protected virtual void OnHeaderConfigurationChanged() { }

    /// <summary>
    /// Called when the group display mode changes.
    /// Derived controls should rebuild their domain-specific group templates.
    /// </summary>
    protected virtual void OnGroupDisplayModeChanged() { }

    /// <summary>
    /// Called when the item background configuration changes.
    /// </summary>
    protected virtual void OnItemBackgroundConfigurationChanged() { }

    /// <summary>
    /// Called when the viewport background configuration changes.
    /// </summary>
    protected virtual void OnListBackgroundConfigurationChanged() { }

    private void ApplyItemBackground(VisualElement surface)
    {
        Color baseColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2D323A") : Color.FromArgb("#F7F8FA");

        bool automaticTransparent = GroupDisplayMode == CollectionGroupDisplayMode.Lines
            || (ListBackgroundMode == CollectionListBackgroundMode.Image && ListBackgroundImageSource is not null);

        surface.BackgroundColor = ItemBackgroundMode switch
        {
            CollectionItemBackgroundMode.Transparent => Colors.Transparent,
            CollectionItemBackgroundMode.Base => baseColor,
            CollectionItemBackgroundMode.Custom => ItemBackgroundColor,
            _ => automaticTransparent ? Colors.Transparent : baseColor
        };
    }

    private void RefreshItemBackgroundSurfaces()
    {
        foreach (VisualElement surface in itemBackgroundSurfaces) ApplyItemBackground(surface);
    }

    private void ApplyListBackgroundConfiguration()
    {
        if (listViewportBorder is null || listBackgroundImage is null) return;

        Color baseColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#24272D") : Color.FromArgb("#FFFCFA");

        bool hasImageBackground = ListBackgroundMode == CollectionListBackgroundMode.Image && ListBackgroundImageSource is not null;

        listViewportBorder.BackgroundColor = hasImageBackground
            ? Colors.Transparent
            : ListBackgroundMode switch
            {
                CollectionListBackgroundMode.Transparent => Colors.Transparent,
                CollectionListBackgroundMode.Custom => ListBackgroundColor,
                _ => baseColor
            };

        foreach ((VisualElement element, Color light, Color dark) in themedBackgroundSurfaces)
        {
            element.BackgroundColor = hasImageBackground
                ? Colors.Transparent
                : Application.Current?.RequestedTheme == AppTheme.Dark ? dark : light;
        }

        foreach (VisualElement element in transparentBackgroundSurfaces) element.BackgroundColor = Colors.Transparent;

        listBackgroundImage.Source = ListBackgroundImageSource;
        listBackgroundImage.IsVisible = hasImageBackground;

        RefreshItemBackgroundSurfaces();
    }
}
