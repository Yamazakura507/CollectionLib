using System.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using CollectionLib.Enums;
using CollectionLib.Models;
using CollectionLib.Resources.ExtensionResources;
using GroupLib.Models;
using SortLib.Enums;
using SortLib.Models;
using WhereLib.Enums;
using WhereLib.Models;

namespace CollectionLib.Controls;

public abstract partial class CollectionListView<
    TItem,
    TFilterParameter,
    TSearchParameter,
    TGroupParameter,
    TSortParameter,
    TFilterRule,
    TSearchRule,
    TGroupRule,
    TSortRule>
    where TItem : class, INotifyPropertyChanged
    where TFilterParameter : struct, Enum
    where TSearchParameter : struct, Enum
    where TGroupParameter : struct, Enum
    where TSortParameter : struct, Enum
    where TFilterRule : FilterRule<TFilterParameter>, new()
    where TSearchRule : SearchRule<TSearchParameter>, new()
    where TGroupRule : GroupRule<TGroupParameter>, new()
    where TSortRule : SortRule<TSortParameter>, new()
{
    private bool hasSynchronizedSortParameterOrder;
    private bool hasSynchronizedGroupParameterOrder;

    protected virtual bool IsSortParameterAvailable(TSortParameter parameter) => ItemsSource?.Count > 0;

    protected virtual bool IsFilterParameterAvailable(TFilterParameter parameter) => ItemsSource?.Count > 0;

    protected virtual string GetSortKindDisplayName(SortKind kind) => kind.ToString();

    protected virtual void OnAppendAdditionalFilterOptions() { }
    protected virtual void ClearAdditionalFilterState() { }
    protected virtual void AppendAdditionalCentralOptions() { }

    protected virtual void OnBuildCentralContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("No items to configure.");

            return;
        }

        AddPopupButton("Search", CollectionListImageKeys.ListSearch, "list_search.svg", () => ShowPopup(PopupKind.SearchOptions, true));
        AddPopupButton("Sort", CollectionListImageKeys.ListSort, "list_sort.svg", () => ShowPopup(PopupKind.Sort, true));
        AddPopupButton("Filter", CollectionListImageKeys.ListFilter, "list_filter.svg", () => ShowPopup(PopupKind.Filter, true));
        AddPopupButton("Group", CollectionListImageKeys.ListGroup, "list_group.svg", () => ShowPopup(PopupKind.Group, true));
        AddPopupButton("View and background", CollectionListImageKeys.ListView, "list_view.svg", () => ShowPopup(PopupKind.View, true));
        AppendAdditionalCentralOptions();
    }

    protected virtual void OnBuildSearchOptionsContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("РќРµС‚ СЃРѕРѕР±С‰РµРЅРёР№ РґР»СЏ РїРѕРёСЃРєР°.");

            return;
        }

        CollectionListOptionDescriptor[] searchFieldOptions = SearchParameterValues
            .Select((parameter, index) =>
            {
                bool selected = selectedSearchParameters.Contains(parameter);

                return new CollectionListOptionDescriptor(
                    $"search:{parameter}",
                    SearchParameterNameSelector(parameter),
                    GetIcon(
                        selected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
                        selected ? "list_state_enabled.svg" : "list_state_disabled.svg"),
                    () => ToggleSearchParameter(parameter),
                    AddDivider: SearchParameterValues.Skip(index + 1).Any(IsSearchParameterAvailable),
                    IsAvailable: () => IsSearchParameterAvailable(parameter));
            })
            .ToArray();

        AddPopupOptionSection("РџРѕР»СЏ РїРѕРёСЃРєР°", searchFieldOptions);

        bool strict = ItemsSource.SearchOptions.IsStrict;
        CollectionListOptionDescriptor strictOption = new(
            "search:strict",
            "РўРѕС‡РЅРѕРµ СЃРѕРІРїР°РґРµРЅРёРµ С†РµР»РѕРіРѕ Р·РЅР°С‡РµРЅРёСЏ",
            GetIcon(
                strict ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
                strict ? "list_state_enabled.svg" : "list_state_disabled.svg"),
            ToggleStrictSearch);

        AddPopupOptionSection(
            "РЎРѕРїРѕСЃС‚Р°РІР»РµРЅРёРµ",
            [ strictOption ],
            addDivider: false,
            appendContent: section =>
            {
                Label accuracyLabel = new()
                {
                    Text = $"РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ СЂР°СЃСЃС‚РѕСЏРЅРёРµ Р›РµРІРµРЅС€С‚РµР№РЅР°: {ItemsSource.SearchOptions.MaxLevenshteinDistance}",
                    FontSize = 12,
                    Margin = new Thickness(2, 7, 2, 1),
                    TextColor = PopupTextColor
                };
                Slider accuracySlider = new()
                {
                    Minimum = 0,
                    Maximum = 8,
                    Value = ItemsSource.SearchOptions.MaxLevenshteinDistance,
                    MinimumTrackColor = Color.FromArgb("#5987C8"),
                    ThumbColor = Color.FromArgb("#5987C8")
                };
                accuracySlider.ValueChanged += (_, args) =>
                {
                    int value = (int)Math.Round(args.NewValue);
                    viewController?.SetMaximumLevenshteinDistance(value);
                    accuracyLabel.Text = $"РњР°РєСЃРёРјР°Р»СЊРЅРѕРµ СЂР°СЃСЃС‚РѕСЏРЅРёРµ Р›РµРІРµРЅС€С‚РµР№РЅР°: {value}";
                    UpdateSearchSettingsVisuals();
                };

                section.Children.Add(accuracyLabel);
                section.Children.Add(accuracySlider);
            });

        AddPopupText("Р РµР¶РёРј Рё СЂРµРіРёСЃС‚СЂ РїРµСЂРµРєР»СЋС‡Р°СЋС‚СЃСЏ РёРєРѕРЅРєР°РјРё СЃРїСЂР°РІР° РѕС‚ СЃС‚СЂРѕРєРё РїРѕРёСЃРєР°.");
        AddPopupText("РџРѕСЃР»РµРґРЅРµРµ РІС‹Р±СЂР°РЅРЅРѕРµ РїРѕР»Рµ РїРѕРёСЃРєР° РЅРµР»СЊР·СЏ РѕС‚РєР»СЋС‡РёС‚СЊ.");
    }

    protected virtual void OnBuildSortContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("РќРµС‚ СЃРѕРѕР±С‰РµРЅРёР№ РґР»СЏ СЃРѕСЂС‚РёСЂРѕРІРєРё.");

            return;
        }

        AddPopupText("РќР°Р¶Р°С‚РёРµ РїРµСЂРµРєР»СЋС‡Р°РµС‚ СЃРѕСЃС‚РѕСЏРЅРёРµ: РІС‹РєР»СЋС‡РµРЅРѕ в†’ РїРѕ РІРѕР·СЂР°СЃС‚Р°РЅРёСЋ в†’ РїРѕ СѓР±С‹РІР°РЅРёСЋ в†’ РІС‹РєР»СЋС‡РµРЅРѕ.");

        viewController?.SynchronizeSelectedSortKind();

        AddCollapsibleSection("РђР»РіРѕСЂРёС‚Рј СЃРѕСЂС‚РёСЂРѕРІРєРё", section =>
        {
            SortKind[] algorithms = Enum.GetValues<SortKind>();

            Picker algorithmPicker = new()
            {
                Title = "Р’С‹Р±РµСЂРёС‚Рµ Р°Р»РіРѕСЂРёС‚Рј",
                FontSize = 12,
                ItemsSource = algorithms.Select(GetSortKindDisplayName).ToList(),
                SelectedIndex = Array.IndexOf(algorithms, selectedSortKind)
            };
            algorithmPicker.SelectedIndexChanged += (_, _) =>
            {
                int selectedIndex = algorithmPicker.SelectedIndex;

                if (selectedIndex < 0 || ItemsSource is null) return;

                selectedSortKind = algorithms[selectedIndex];
            };

            section.Children.Add(algorithmPicker);
        });

        SynchronizeSortParameterOrder();

        CollectionListOptionDescriptor[] sortOptions = sortParameterOrder
            .Select((parameter, index) =>
            {
                SortType? direction = viewController?.GetSortDirection(parameter);
                string resourceKey = direction is null
                    ? CollectionListImageKeys.ListStateDisabled
                    : direction == SortType.Ascending
                        ? CollectionListImageKeys.ListStateAscending
                        : CollectionListImageKeys.ListStateDescending;
                string iconFile = direction is null
                    ? "list_state_disabled.svg"
                    : direction == SortType.Ascending
                        ? "list_state_ascending.svg"
                        : "list_state_descending.svg";

                return new CollectionListOptionDescriptor(
                    $"sort:{parameter}",
                    SortParameterNameSelector(parameter),
                    GetIcon(resourceKey, iconFile),
                    () => CycleSort(parameter),
                    AddDivider: sortParameterOrder.Skip(index + 1).Any(IsSortParameterAvailable),
                    ReorderAction: ReorderSortParameters,
                    IsAvailable: () => IsSortParameterAvailable(parameter));
            })
            .ToArray();

        AddPopupOptionSection("РљСЂРёС‚РµСЂРёРё СЃРѕСЂС‚РёСЂРѕРІРєРё", sortOptions, addDivider: false);

        ImageButton clearButton = CreateIconButton(CollectionListImageKeys.ListReset, "list_reset.svg", "РћС‡РёСЃС‚РёС‚СЊ СЃРѕСЂС‚РёСЂРѕРІРєСѓ", 34);
        clearButton.Clicked += (_, _) => ClearSorting();
        clearSortButton = clearButton;
        clearButton.IsVisible = ItemsSource.SortRules.Count > 0;

        PopupContent.Children.Add(clearButton);
    }

    private void CycleSort(TSortParameter parameter)
    {
        if (viewController is null) return;

        viewController.CycleSort(parameter);
        UpdateSortOptionState(parameter);
        UpdateAppliedIndicators();
        RenderItems();
    }

    private void UpdateSortOptionState(TSortParameter parameter)
    {
        SortType? direction = viewController?.GetSortDirection(parameter);
        string resourceKey = direction is null
            ? CollectionListImageKeys.ListStateDisabled
            : direction == SortType.Ascending
                ? CollectionListImageKeys.ListStateAscending
                : CollectionListImageKeys.ListStateDescending;
        string iconFile = direction is null
            ? "list_state_disabled.svg"
            : direction == SortType.Ascending
                ? "list_state_ascending.svg"
                : "list_state_descending.svg";

        SetPopupOptionState($"sort:{parameter}", resourceKey, iconFile);
    }

    protected virtual void OnBuildFilterContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("РќРµС‚ СЃРѕРѕР±С‰РµРЅРёР№ РґР»СЏ С„РёР»СЊС‚СЂР°С†РёРё.");
            return;
        }

        AddPopupText("Р”Р»СЏ РєР°Р¶РґРѕРіРѕ Р°С‚СЂРёР±СѓС‚Р° РїРµСЂРµРєР»СЋС‡Р°РµС‚СЃСЏ СЃРѕСЃС‚РѕСЏРЅРёРµ: РІСЃРµ в†’ РґР° в†’ РЅРµС‚.");

        TFilterParameter[] availableParameters = BooleanFilterParameterValues.Where(IsFilterParameterAvailable).ToArray();
        CollectionListOptionDescriptor[] filterOptions = availableParameters
            .Select((parameter, index) =>
            {
                bool? value = GetOwnedBooleanFilter(parameter);
                string resourceKey = value is null
                    ? CollectionListImageKeys.ListStateAll
                    : value.Value
                        ? CollectionListImageKeys.ListStateYes
                        : CollectionListImageKeys.ListStateNo;
                string iconFile = value is null
                    ? "list_state_all.svg"
                    : value.Value
                        ? "list_state_yes.svg"
                        : "list_state_no.svg";

                return new CollectionListOptionDescriptor(
                    $"filter:{parameter}",
                    FilterParameterNameSelector(parameter),
                    GetIcon(resourceKey, iconFile),
                    () => CycleBooleanFilter(parameter),
                    AddDivider: index != availableParameters.Length - 1,
                    IsAvailable: () => IsFilterParameterAvailable(parameter));
            })
            .ToArray();

        AddPopupOptionSection("РђС‚СЂРёР±СѓС‚С‹ СЃРѕРѕР±С‰РµРЅРёР№", filterOptions);
        OnAppendAdditionalFilterOptions();

        clearFilterButton = AddPopupResetButton("РЎР±СЂРѕСЃРёС‚СЊ С„РёР»СЊС‚СЂР°С†РёСЋ", ClearFilters);
        clearFilterButton.IsVisible = ItemsSource.FilterRules.Count > 0;
    }

    protected virtual void OnBuildGroupContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("РќРµС‚ СЃРѕРѕР±С‰РµРЅРёР№ РґР»СЏ РіСЂСѓРїРїРёСЂРѕРІРєРё.");

            return;
        }

        AddPopupText("Р’С‹Р±СЂР°РЅРЅС‹Рµ РїР°СЂР°РјРµС‚СЂС‹ РѕР±СЂР°Р·СѓСЋС‚ РїРѕСЃР»РµРґРѕРІР°С‚РµР»СЊРЅС‹Рµ СѓСЂРѕРІРЅРё РґРµСЂРµРІР°. РџРµСЂРµС‚Р°СЃРєРёРІР°Р№С‚Рµ СЃС‚СЂРѕРєРё, С‡С‚РѕР±С‹ РёР·РјРµРЅРёС‚СЊ РёС… РїРѕСЂСЏРґРѕРє.");

        CollectionListOptionDescriptor[] groupDisplayOptions =
        [
            new(
                "group-display:blocks",
                "Р‘Р»РѕРєРё",
                GetIcon(
                    GroupDisplayMode == CollectionGroupDisplayMode.Blocks
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    GroupDisplayMode == CollectionGroupDisplayMode.Blocks
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetGroupDisplayMode(CollectionGroupDisplayMode.Blocks)),
            new(
                "group-display:lines",
                "Р›РёРЅРёРё",
                GetIcon(
                    GroupDisplayMode == CollectionGroupDisplayMode.Lines
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    GroupDisplayMode == CollectionGroupDisplayMode.Lines
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetGroupDisplayMode(CollectionGroupDisplayMode.Lines),
                AddDivider: false)
        ];

        AddPopupOptionSection("Р’РёРґ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ", groupDisplayOptions, isAvailable: () => ItemsSource?.GroupRules.Count > 0);
        SynchronizeGroupParameterOrder();

        CollectionListOptionDescriptor[] groupOptions = groupParameterOrder
            .Select((parameter, index) =>
            {
                bool selected = viewController?.IsGroupEnabled(parameter) == true;

                return new CollectionListOptionDescriptor(
                    $"group:{parameter}",
                    GroupParameterNameSelector(parameter),
                    GetIcon(
                        selected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
                        selected ? "list_state_enabled.svg" : "list_state_disabled.svg"),
                    () => ToggleGroup(parameter),
                    AddDivider: index != groupParameterOrder.Count - 1,
                    ReorderAction: ReorderGroupParameters);
            })
            .ToArray();

        AddPopupOptionSection("РЈСЂРѕРІРЅРё РіСЂСѓРїРїРёСЂРѕРІРєРё", groupOptions, addDivider: false);

        clearGroupButton = AddPopupResetButton("РЎР±СЂРѕСЃРёС‚СЊ РіСЂСѓРїРїРёСЂРѕРІРєСѓ", ClearGrouping);
        clearGroupButton.IsVisible = ItemsSource.GroupRules.Count > 0;
    }

    protected virtual void OnBuildViewContext()
    {
        AddPopupText("РќР°СЃС‚СЂРѕР№С‚Рµ РїРѕРґР»РѕР¶РєСѓ СЃРѕРѕР±С‰РµРЅРёР№ Рё С„РѕРЅРѕРІСѓСЋ РїРѕРІРµСЂС…РЅРѕСЃС‚СЊ СЃРїРёСЃРєР°.");

        CollectionListOptionDescriptor[] itemBackgroundOptions =
        [
            new(
                "view:item:auto",
                "РђРІС‚РѕРјР°С‚РёС‡РµСЃРєРё",
                GetIcon(
                    ItemBackgroundMode == CollectionItemBackgroundMode.Automatic
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ItemBackgroundMode == CollectionItemBackgroundMode.Automatic
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetItemBackgroundMode(CollectionItemBackgroundMode.Automatic)),
            new(
                "view:item:transparent",
                "РџСЂРѕР·СЂР°С‡РЅС‹Р№",
                GetIcon(
                    ItemBackgroundMode == CollectionItemBackgroundMode.Transparent
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ItemBackgroundMode == CollectionItemBackgroundMode.Transparent
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetItemBackgroundMode(CollectionItemBackgroundMode.Transparent)),
            new(
                "view:item:base",
                "Р‘Р°Р·РѕРІС‹Р№",
                GetIcon(
                    ItemBackgroundMode == CollectionItemBackgroundMode.Base
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ItemBackgroundMode == CollectionItemBackgroundMode.Base
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetItemBackgroundMode(CollectionItemBackgroundMode.Base)),
            new(
                "view:item:custom",
                "РќР°СЃС‚СЂР°РёРІР°РµРјС‹Р№",
                GetIcon(
                    ItemBackgroundMode == CollectionItemBackgroundMode.Custom
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ItemBackgroundMode == CollectionItemBackgroundMode.Custom
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetItemBackgroundMode(CollectionItemBackgroundMode.Custom),
                AddDivider: false)
        ];

        AddPopupOptionSection(
            "Р¤РѕРЅ СЌР»РµРјРµРЅС‚РѕРІ СЃРїРёСЃРєР°",
            itemBackgroundOptions,
            appendContent: section =>
            {
                itemColorPickerPanel = BuildColorPickerPanel(
                    ItemBackgroundColor,
                    color => ItemBackgroundColor = color);
                section.Children.Add(itemColorPickerPanel);
            });

        CollectionListOptionDescriptor[] listBackgroundOptions =
        [
            new(
                "view:list:transparent",
                "РџСЂРѕР·СЂР°С‡РЅС‹Р№",
                GetIcon(
                    ListBackgroundMode == CollectionListBackgroundMode.Transparent
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ListBackgroundMode == CollectionListBackgroundMode.Transparent
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetListBackgroundMode(CollectionListBackgroundMode.Transparent)),
            new(
                "view:list:base",
                "Р‘Р°Р·РѕРІС‹Р№",
                GetIcon(
                    ListBackgroundMode == CollectionListBackgroundMode.Base
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ListBackgroundMode == CollectionListBackgroundMode.Base
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetListBackgroundMode(CollectionListBackgroundMode.Base)),
            new(
                "view:list:custom",
                "РќР°СЃС‚СЂР°РёРІР°РµРјС‹Р№",
                GetIcon(
                    ListBackgroundMode == CollectionListBackgroundMode.Custom
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ListBackgroundMode == CollectionListBackgroundMode.Custom
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetListBackgroundMode(CollectionListBackgroundMode.Custom)),
            new(
                "view:list:image",
                "РР·РѕР±СЂР°Р¶РµРЅРёРµ",
                GetIcon(
                    ListBackgroundMode == CollectionListBackgroundMode.Image
                        ? CollectionListImageKeys.ListStateEnabled
                        : CollectionListImageKeys.ListStateDisabled,
                    ListBackgroundMode == CollectionListBackgroundMode.Image
                        ? "list_state_enabled.svg"
                        : "list_state_disabled.svg"),
                () => SetListBackgroundMode(CollectionListBackgroundMode.Image),
                AddDivider: false)
        ];

        AddPopupOptionSection(
            "Р¤РѕРЅ СЃР°РјРѕРіРѕ СЃРїРёСЃРєР°",
            listBackgroundOptions,
            addDivider: false,
            appendContent: section =>
            {
                listColorPickerPanel = BuildColorPickerPanel(
                    ListBackgroundColor,
                    color => ListBackgroundColor = color);
                section.Children.Add(listColorPickerPanel);

                listImagePickerPanel = BuildListImagePickerPanel();
                section.Children.Add(listImagePickerPanel);
            });
        UpdateViewOptionStates();
    }

    private VerticalStackLayout BuildColorPickerPanel(Color initialColor, Action<Color> colorChanged)
    {
        VerticalStackLayout panel = new()
        {
            Spacing = 5,
            Padding = new Thickness(28, 4, 4, 9)
        };

        Border preview = new()
        {
            HeightRequest = 30,
            Stroke = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#606A78") : Color.FromArgb("#D2D8E0"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
            BackgroundColor = initialColor
        };
        Label valueLabel = new()
        {
            Text = initialColor.ToString(),
            FontSize = 10,
            TextColor = PopupMutedColor
        };

        (double initialHue, double initialSaturation, double initialBrightness) = GetHsv(initialColor);

        Slider hue = new()
        {
            Minimum = 0,
            Maximum = 360,
            Value = initialHue,
            MinimumTrackColor = Color.FromArgb("#5987C8"),
            ThumbColor = Color.FromArgb("#5987C8")
        };
        Slider saturation = new()
        {
            Minimum = 0,
            Maximum = 1,
            Value = initialSaturation,
            MinimumTrackColor = Color.FromArgb("#5987C8"),
            ThumbColor = Color.FromArgb("#5987C8")
        };
        Slider brightness = new()
        {
            Minimum = 0.15,
            Maximum = 1,
            Value = initialBrightness,
            MinimumTrackColor = Color.FromArgb("#5987C8"),
            ThumbColor = Color.FromArgb("#5987C8")
        };

        void AddSlider(string labelText, Slider slider)
        {
            panel.Children.Add(new Label
            {
                Text = labelText,
                FontSize = 10,
                TextColor = PopupMutedColor
            });
            panel.Children.Add(slider);
        }
        void ApplyColor()
        {
            Color color = ConvertHsvToColor(
                (float)hue.Value,
                (float)saturation.Value,
                (float)brightness.Value);
            preview.BackgroundColor = color;
            valueLabel.Text = color.ToString();
            colorChanged(color);
        }

        hue.ValueChanged += (_, _) => ApplyColor();
        saturation.ValueChanged += (_, _) => ApplyColor();
        brightness.ValueChanged += (_, _) => ApplyColor();

        panel.Children.Add(preview);
        panel.Children.Add(valueLabel);
        AddSlider("РћС‚С‚РµРЅРѕРє", hue);
        AddSlider("РќР°СЃС‹С‰РµРЅРЅРѕСЃС‚СЊ", saturation);
        AddSlider("РЇСЂРєРѕСЃС‚СЊ", brightness);
        return panel;
    }

    private static Color ConvertHsvToColor(float hue, float saturation, float value)
    {
        float chroma = value * saturation;
        float sector = (hue % 360f) / 60f;
        float secondComponent = chroma * (1 - Math.Abs(sector % 2 - 1));

        (float red, float green, float blue) = (int)sector switch
        {
            0 => (chroma, secondComponent, 0f),
            1 => (secondComponent, chroma, 0f),
            2 => (0f, chroma, secondComponent),
            3 => (0f, secondComponent, chroma),
            4 => (secondComponent, 0f, chroma),
            _ => (chroma, 0f, secondComponent)
        };

        float offset = value - chroma;

        return new Color(red + offset, green + offset, blue + offset);
    }

    private static (double Hue, double Saturation, double Brightness) GetHsv(Color color)
    {
        double red = color.Red;
        double green = color.Green;
        double blue = color.Blue;
        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;

        double hue = 0;
        if (delta > 0)
        {
            if (maximum == red) hue = 60 * (((green - blue) / delta) % 6);
            else if (maximum == green) hue = 60 * (((blue - red) / delta) + 2);
            else hue = 60 * (((red - green) / delta) + 4);
        }

        if (hue < 0) hue += 360;

        double saturation = maximum <= 0 ? 0 : delta / maximum;

        return (hue, saturation, maximum);
    }

    private VerticalStackLayout BuildListImagePickerPanel()
    {
        VerticalStackLayout panel = new()
        {
            Spacing = 7,
            Padding = new Thickness(28, 5, 4, 9)
        };

        bool darkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;

        Grid pickerContent = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star)
            },
            ColumnSpacing = 11,
            VerticalOptions = LayoutOptions.Center
        };

        Border iconTile = new()
        {
            WidthRequest = 42,
            HeightRequest = 42,
            Padding = 9,
            BackgroundColor = darkTheme ? Color.FromArgb("#35465B") : Color.FromArgb("#E7F0FC"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new Image
            {
                Source = GetIcon(CollectionListImageKeys.ListImage, "list_image.svg"),
                Aspect = Aspect.AspectFit
            }
        };
        pickerContent.Children.Add(iconTile);

        VerticalStackLayout labels = new()
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center
        };
        labels.Children.Add(new Label
        {
            Text = "Р’С‹Р±СЂР°С‚СЊ РёР·РѕР±СЂР°Р¶РµРЅРёРµ",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = PopupTextColor
        });
        labels.Children.Add(new Label
        {
            Text = "PNG, JPG, JPEG, BMP РёР»Рё GIF",
            FontSize = 10,
            TextColor = PopupMutedColor
        });
        Grid.SetColumn(labels, 1);
        pickerContent.Children.Add(labels);

        Border chooseSurface = new()
        {
            Padding = new Thickness(10),
            Stroke = darkTheme ? Color.FromArgb("#4A596B") : Color.FromArgb("#D5E0ED"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = darkTheme ? Color.FromArgb("#252E39") : Color.FromArgb("#F7FAFE"),
            Content = pickerContent
        };
        chooseSurface.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await PickListBackgroundImageAsync()) });
        ToolTipProperties.SetText(chooseSurface, "Р’С‹Р±СЂР°С‚СЊ Р»РѕРєР°Р»СЊРЅРѕРµ РёР·РѕР±СЂР°Р¶РµРЅРёРµ РґР»СЏ С„РѕРЅР° СЃРїРёСЃРєР°");

        listImageStatusLabel = new Label
        {
            Text = ListBackgroundImageSource is null ? "РР·РѕР±СЂР°Р¶РµРЅРёРµ РЅРµ РІС‹Р±СЂР°РЅРѕ" : "РР·РѕР±СЂР°Р¶РµРЅРёРµ РІС‹Р±СЂР°РЅРѕ",
            FontSize = 10,
            LineBreakMode = LineBreakMode.TailTruncation,
            TextColor = PopupMutedColor
        };

        panel.Children.Add(chooseSurface);
        panel.Children.Add(listImageStatusLabel);

        return panel;
    }

    protected void SetItemBackgroundMode(CollectionItemBackgroundMode mode)
    {
        ItemBackgroundMode = mode;

        UpdateViewOptionStates();
    }

    protected void SetListBackgroundMode(CollectionListBackgroundMode mode)
    {
        ListBackgroundMode = mode;

        UpdateViewOptionStates();
    }

    protected override void OnItemBackgroundConfigurationChanged()
    {
        base.OnItemBackgroundConfigurationChanged();
        UpdateViewOptionStates();
    }

    protected override void OnListBackgroundConfigurationChanged()
    {
        base.OnListBackgroundConfigurationChanged();
        UpdateViewOptionStates();
    }

    protected virtual void OnPopupOptionAvailabilityChanged() => UpdateViewOptionStates();

    protected void UpdateViewOptionStates()
    {
        SetPopupOptionState("view:item:auto",
            ItemBackgroundMode == CollectionItemBackgroundMode.Automatic ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ItemBackgroundMode == CollectionItemBackgroundMode.Automatic ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:item:transparent",
            ItemBackgroundMode == CollectionItemBackgroundMode.Transparent ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ItemBackgroundMode == CollectionItemBackgroundMode.Transparent ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:item:base",
            ItemBackgroundMode == CollectionItemBackgroundMode.Base ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ItemBackgroundMode == CollectionItemBackgroundMode.Base ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:item:custom",
            ItemBackgroundMode == CollectionItemBackgroundMode.Custom ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ItemBackgroundMode == CollectionItemBackgroundMode.Custom ? "list_state_enabled.svg" : "list_state_disabled.svg");

        SetPopupOptionState("view:list:transparent",
            ListBackgroundMode == CollectionListBackgroundMode.Transparent ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ListBackgroundMode == CollectionListBackgroundMode.Transparent ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:list:base",
            ListBackgroundMode == CollectionListBackgroundMode.Base ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ListBackgroundMode == CollectionListBackgroundMode.Base ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:list:custom",
            ListBackgroundMode == CollectionListBackgroundMode.Custom ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ListBackgroundMode == CollectionListBackgroundMode.Custom ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState("view:list:image",
            ListBackgroundMode == CollectionListBackgroundMode.Image ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            ListBackgroundMode == CollectionListBackgroundMode.Image ? "list_state_enabled.svg" : "list_state_disabled.svg");

        if (itemColorPickerPanel is not null) itemColorPickerPanel.IsVisible = ItemBackgroundMode == CollectionItemBackgroundMode.Custom;
        if (listColorPickerPanel is not null) listColorPickerPanel.IsVisible = ListBackgroundMode == CollectionListBackgroundMode.Custom;
        if (listImagePickerPanel is not null) listImagePickerPanel.IsVisible = ListBackgroundMode == CollectionListBackgroundMode.Image;
    }

    private async Task PickListBackgroundImageAsync()
    {
        try
        {
            #if WINDOWS
            string? selectedPath = null;
            #endif
            string? selectedFileName = null;

            #if WINDOWS
            Microsoft.UI.Xaml.Window? platformWindow = Window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

            if (platformWindow is null) throw new InvalidOperationException("РќРµ СѓРґР°Р»РѕСЃСЊ РїРѕР»СѓС‡РёС‚СЊ РѕРєРЅРѕ РїСЂРёР»РѕР¶РµРЅРёСЏ РґР»СЏ РІС‹Р±РѕСЂР° С„Р°Р№Р»Р°.");

            Microsoft.Windows.Storage.Pickers.FileOpenPicker picker = new(platformWindow.AppWindow.Id)
            {
                CommitButtonText = "Р’С‹Р±СЂР°С‚СЊ",
                SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.PicturesLibrary,
                ViewMode = Microsoft.Windows.Storage.Pickers.PickerViewMode.Thumbnail
            };

            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".gif");

            Microsoft.Windows.Storage.Pickers.PickFileResult? selectedFile = await picker.PickSingleFileAsync();

            if (selectedFile is null) return;

            selectedPath = selectedFile.Path;
            selectedFileName = System.IO.Path.GetFileName(selectedPath);
            #else
            FileResult? selectedFile = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Р’С‹Р±РµСЂРёС‚Рµ РёР·РѕР±СЂР°Р¶РµРЅРёРµ РґР»СЏ С„РѕРЅР° СЃРїРёСЃРєР°",
                FileTypes = FilePickerFileType.Images
            });

            if (selectedFile is null) return;

            selectedFileName = selectedFile.FileName;
            #endif
            string extension = System.IO.Path.GetExtension(selectedFileName);

            if (string.IsNullOrWhiteSpace(extension)) extension = ".img";

            string destinationPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, $"message-list-background-{Guid.NewGuid():N}{extension}");

            await using (FileStream destination = new(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                #if WINDOWS
                await using Stream source = File.OpenRead(selectedPath!);
                #else
                await using Stream source = await selectedFile!.OpenReadAsync();
                #endif

                await source.CopyToAsync(destination);
                await destination.FlushAsync();
            }

            ListBackgroundImageSource = ImageSource.FromStream(() => File.OpenRead(destinationPath));

            if (listImageStatusLabel is not null) listImageStatusLabel.Text = selectedFileName;

            ListBackgroundMode = CollectionListBackgroundMode.Image;

            ListBackgroundImage.InvalidateMeasure();
            UpdateViewOptionStates();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to select list background image: {exception}");
            Page? page = FindParentPage();
            if (page is not null)
                await page.DisplayAlertAsync("РћС€РёР±РєР° РІС‹Р±РѕСЂР° РёР·РѕР±СЂР°Р¶РµРЅРёСЏ",
                    $"РќРµ СѓРґР°Р»РѕСЃСЊ Р·Р°РіСЂСѓР·РёС‚СЊ РёР·РѕР±СЂР°Р¶РµРЅРёРµ. {exception.Message}", "OK");
        }
    }

    private void ToggleGroup(TGroupParameter parameter)
    {
        if (viewController is null) return;

        viewController.ToggleGroup(parameter);

        bool selected = viewController?.IsGroupEnabled(parameter) == true;

        SetPopupOptionState($"group:{parameter}",
            selected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            selected ? "list_state_enabled.svg" : "list_state_disabled.svg");

        UpdateAppliedIndicators();
    }

    private void SynchronizeSortParameterOrder()
    {
        if (viewController is null || hasSynchronizedSortParameterOrder) return;

        hasSynchronizedSortParameterOrder = true;
        viewController.SynchronizeSortParameterOrder();
    }

    private void ReorderSortParameters(int oldIndex, int newIndex) => viewController?.ReorderSortParameters(oldIndex, newIndex);

    private void SynchronizeGroupParameterOrder()
    {
        if (viewController is null || hasSynchronizedGroupParameterOrder) return;

        hasSynchronizedGroupParameterOrder = true;
        viewController.SynchronizeGroupParameterOrder();
    }

    private void ReorderGroupParameters(int oldIndex, int newIndex) => viewController?.ReorderGroupParameters(oldIndex, newIndex);

    private void NormalizeGroupLevels() => viewController?.NormalizeGroupLevels();

    private void SetGroupDisplayMode(CollectionGroupDisplayMode mode)
    {
        GroupDisplayMode = mode;

        bool blocksSelected = mode == CollectionGroupDisplayMode.Blocks;
        SetPopupOptionState(
            "group-display:blocks",
            blocksSelected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            blocksSelected ? "list_state_enabled.svg" : "list_state_disabled.svg");
        SetPopupOptionState(
            "group-display:lines",
            !blocksSelected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
            !blocksSelected ? "list_state_enabled.svg" : "list_state_disabled.svg");
    }

    protected void ClearFilters()
    {
        if (ItemsSource is null) return;

        viewController?.ClearFilters();
        ClearAdditionalFilterState();

        foreach (TFilterParameter parameter in BooleanFilterParameterValues)
            SetPopupOptionState($"filter:{parameter}", CollectionListImageKeys.ListStateAll, "list_state_all.svg");

        UpdateAppliedIndicators();
        UpdatePopupOptionAvailability();
        RenderItems();
    }

    protected void ClearSorting()
    {
        viewController?.ClearSorting();

        foreach (TSortParameter parameter in SortParameterValues)
            SetPopupOptionState($"sort:{parameter}", CollectionListImageKeys.ListStateDisabled, "list_state_disabled.svg");

        UpdateAppliedIndicators();
        RenderItems();
    }

    protected void ClearGrouping()
    {
        if (ItemsSource is null) return;

        viewController?.ClearGrouping();

        foreach (TGroupParameter parameter in GroupParameterValues)
            SetPopupOptionState($"group:{parameter}", CollectionListImageKeys.ListStateDisabled, "list_state_disabled.svg");

        UpdatePopupOptionAvailability();
        UpdateAppliedIndicators();
        RenderItems();
    }

    private bool? GetOwnedBooleanFilter(TFilterParameter parameter) => viewController?.GetBooleanFilterValue(parameter);

    private void CycleBooleanFilter(TFilterParameter parameter)
    {
        if (viewController is null) return;

        bool? next = viewController.CycleBooleanFilter(parameter);
        string resourceKey = next is null
            ? CollectionListImageKeys.ListStateAll
            : next.Value
                ? CollectionListImageKeys.ListStateYes
                : CollectionListImageKeys.ListStateNo;

        string iconFile = next is null
            ? "list_state_all.svg"
            : next.Value
                ? "list_state_yes.svg"
                : "list_state_no.svg";

        SetPopupOptionState($"filter:{parameter}", resourceKey, iconFile);
        UpdateAppliedIndicators();
        RenderItems();
    }

    protected void SetOwnedFilter(TFilterParameter parameter, object? value) => viewController?.SetFilterValue(parameter, value);

    private void UpdateControlAvailability()
    {
        bool hasItems = ItemCount > 0;
        SearchButton.IsVisible = hasItems;
        SortButton.IsVisible = hasItems;
        FilterButton.IsVisible = hasItems;
        GroupButton.IsVisible = hasItems;
        ViewButton.IsVisible = hasItems;
        ClearHistoryButton.IsVisible = hasItems;
        OnAdditionalControlsAvailabilityChanged(hasItems);

        if (!hasItems && isSearchVisible)
        {
            SearchEntry.Unfocus();
            SearchHost.IsVisible = false;
            isSearchVisible = false;
            isSearchSettingsOpen = false;
        }

        UpdatePopupOptionAvailability();

        if (!hasItems && PopupLayer.IsVisible)
        {
            PopupLayer.IsVisible = false;
            activePopup = PopupKind.None;
            isSearchSettingsOpen = false;
            popupOpenedFromCentral = false;
        }
    }

    protected void UpdateAppliedIndicators()
    {
        AppliedIndicators.Children.Clear();
        UpdateControlAvailability();

        if (ItemsSource is null)
        {
            AppliedIndicators.IsVisible = false;

            return;
        }

        AddAppliedIndicator(CollectionListImageKeys.ListSearch, "list_search.svg", "Search", HasSearch);
        AddAppliedIndicator(CollectionListImageKeys.ListFilter, "list_filter.svg", "Filter", HasFilters);
        AddAppliedIndicator(CollectionListImageKeys.ListSort, "list_sort.svg", "Sort", HasSorting);
        AddAppliedIndicator(CollectionListImageKeys.ListGroup, "list_group.svg", "Group", HasGrouping);

        AppliedIndicators.IsVisible = ItemsSource.Count > 0 && AppliedIndicators.Children.Count > 0;

        UpdateSearchSettingsVisuals();
    }

    private void AddAppliedIndicator(string resourceKey, string fileName, string tooltip, bool isActive)
    {
        if (!isActive) return;

        Image indicator = new()
        {
            Source = GetIcon(resourceKey, fileName),
            WidthRequest = 17,
            HeightRequest = 17,
            Aspect = Aspect.AspectFit,
            Margin = new Thickness(2, 0),
            Opacity = 0.9
        };

        ToolTipProperties.SetText(indicator, tooltip);
        AppliedIndicators.Children.Add(indicator);
    }
}