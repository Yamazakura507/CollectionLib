using System.ComponentModel;
using CollectionLib.Controls.Components;
using CollectionLib.Controls.Components.Picker;
using CollectionLib.Controls.Components.Search;
using CollectionLib.Enums;
using CollectionLib.Models;
using CollectionLib.Resources.ExtensionResources;
using GroupLib.Models;
using SortLib.Enums;
using SortLib.Models;
using WhereLib.Models;

namespace CollectionLib.Controls;

public abstract partial class CollectionListView<
    TItem,TFilterParameter,TSearchParameter,TGroupParameter,TSortParameter,TFilterRule,TSearchRule,TGroupRule,TSortRule>
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
            AddPopupText("Нет сообщений для поиска.");

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

        AddPopupOptionSection("Поля поиска", searchFieldOptions);

        bool strict = ItemsSource.SearchOptions.IsStrict;
        CollectionListOptionDescriptor strictOption = new(
            "search:strict",
            "Точное совпадение целого значения",
            GetIcon(
                strict ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
                strict ? "list_state_enabled.svg" : "list_state_disabled.svg"),
            ToggleStrictSearch);

        AddPopupOptionSection(
            "Сопоставление",
            new[] { strictOption },
            addDivider: false,
            appendContent: section =>
            {
                SearchAccuracyOptionsView accuracyOptions = new();
                accuracyOptions.SetAccuracy(ItemsSource.SearchOptions.MaxLevenshteinDistance);
                accuracyOptions.AccuracyChanged += (_, value) =>
                {
                    viewController?.SetMaximumLevenshteinDistance(value);
                    UpdateSearchSettingsVisuals();
                };

                section.Children.Add(accuracyOptions);
            });

        AddPopupText("Режим и регистр переключаются иконками справа от строки поиска.");
        AddPopupText("Последнее выбранное поле поиска нельзя отключить.");
    }

    protected virtual void OnBuildSortContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("Нет сообщений для сортировки.");

            return;
        }

        AddPopupText("Нажатие переключает состояние: выключено → по возрастанию → по убыванию → выключено.");

        viewController?.SynchronizeSelectedSortKind();

        SortKind[] algorithms = Enum.GetValues<SortKind>();

        AddCollapsibleSection("Sorting algorithm", section =>
        {
            SortAlgorithmPickerView picker = new();

            picker.Configure(algorithms.Select(GetSortKindDisplayName), Array.IndexOf(algorithms, selectedSortKind));

            picker.SelectionChanged += (_, selectedIndex) =>
            {
                if (ItemsSource is null) return;

                selectedSortKind = algorithms[selectedIndex];
            };

            section.Children.Add(picker);
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

        AddPopupOptionSection("Критерии сортировки", sortOptions, addDivider: false);

        ImageButton clearButton = CreateIconButton(CollectionListImageKeys.ListReset, "list_reset.svg", "Очистить сортировку", 34);

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
            AddPopupText("Нет сообщений для фильтрации.");

            return;
        }

        AddPopupText("Для каждого атрибута переключается состояние: все → да → нет.");

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

        AddPopupOptionSection("Атрибуты сообщений", filterOptions);
        OnAppendAdditionalFilterOptions();

        clearFilterButton = AddPopupResetButton("Сбросить фильтрацию", ClearFilters);
        clearFilterButton.IsVisible = ItemsSource.FilterRules.Count > 0;
    }

    protected virtual void OnBuildGroupContext()
    {
        if (ItemsSource is not { Count: > 0 })
        {
            AddPopupText("Нет сообщений для группировки.");

            return;
        }

        AddPopupText("Выбранные параметры образуют последовательные уровни дерева. Перетаскивайте строки, чтобы изменить их порядок.");

        CollectionListOptionDescriptor[] groupDisplayOptions =
        [
            new(
                "group-display:blocks",
                "Блоки",
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
                "Линии",
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

        AddPopupOptionSection("Вид отображения", groupDisplayOptions, isAvailable: () => ItemsSource?.GroupRules.Count > 0);
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

        AddPopupOptionSection("Уровни группировки", groupOptions, addDivider: false);

        clearGroupButton = AddPopupResetButton("Сбросить группировку", ClearGrouping);
        clearGroupButton.IsVisible = ItemsSource.GroupRules.Count > 0;
    }

    protected virtual void OnBuildViewContext()
    {
        AddPopupText("Настройте подложку сообщений и фоновую поверхность списка.");

        CollectionListOptionDescriptor[] itemBackgroundOptions =
        [
            new(
                "view:item:auto",
                "Автоматически",
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
                "Прозрачный",
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
                "Базовый",
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
                "Настраиваемый",
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
            "Фон элементов списка",
            itemBackgroundOptions,
            appendContent: section =>
            {
                itemColorPickerPanel = BuildColorPickerPanel(
                    ItemBackgroundBrush ?? new SolidColorBrush(ItemBackgroundColor),
                    brush => ApplySelectedBrush(brush, value => ItemBackgroundBrush = value, value => ItemBackgroundColor = value));
                section.Children.Add(itemColorPickerPanel);
            });

        CollectionListOptionDescriptor[] listBackgroundOptions =
        [
            new(
                "view:list:transparent",
                "Прозрачный",
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
                "Базовый",
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
                "Настраиваемый",
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
                "Изображение",
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
            "Фон самого списка",
            listBackgroundOptions,
            addDivider: false,
            appendContent: section =>
            {
                listColorPickerPanel = BuildColorPickerPanel(
                    ListBackgroundBrush ?? new SolidColorBrush(ListBackgroundColor),
                    brush => ApplySelectedBrush(brush, value => ListBackgroundBrush = value, value => ListBackgroundColor = value));
               
                section.Children.Add(listColorPickerPanel);
                
                listImagePickerPanel = BuildListImagePickerPanel();

                section.Children.Add(listImagePickerPanel);
            });
        UpdateViewOptionStates();
    }

    private ColorBrushPickerView BuildColorPickerPanel(Brush initialBrush, Action<Brush> brushChanged)
    {
        ColorBrushPickerView picker = new();

        picker.Configure(initialBrush);

        picker.BrushChanged += (_, brush) => brushChanged(brush);

        return picker;
    }

    private static void ApplySelectedBrush(Brush brush, Action<Brush> setBrush, Action<Color> setLegacyColor)
    {
        Color? representativeColor = brush switch
        {
            SolidColorBrush solid => solid.Color,
            LinearGradientBrush gradient when gradient.GradientStops.Count > 0
                => gradient.GradientStops.OrderBy(stop => stop.Offset).First().Color,
            _ => null
        };

        if (representativeColor is Color color) setLegacyColor(color);

        setBrush(brush);
    }

    private ListImagePickerView BuildListImagePickerPanel()
    {
        ListImagePickerView picker = new();
        picker.Configure(
            GetIcon(CollectionListImageKeys.ListImage, "list_image.svg"),
            ListBackgroundImageSource is not null);

        picker.ChooseRequested += async (_, _) => await PickListBackgroundImageAsync();

        return picker;
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

            if (platformWindow is null) throw new InvalidOperationException("Не удалось получить окно приложения для выбора файла.");

            Microsoft.Windows.Storage.Pickers.FileOpenPicker picker = new(platformWindow.AppWindow.Id)
            {
                CommitButtonText = "Выбрать",
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
                PickerTitle = "Выберите изображение для фона списка",
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

            listImagePickerPanel?.SetStatus(selectedFileName);

            ListBackgroundMode = CollectionListBackgroundMode.Image;

            ListBackgroundImage.InvalidateMeasure();
            UpdateViewOptionStates();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to select list background image: {exception}");
            Page? page = FindParentPage();
            if (page is not null)
                await page.DisplayAlertAsync("Ошибка выбора изображения",
                    $"Не удалось загрузить изображение. {exception.Message}", "OK");
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

        void UpdateOption(string id, CollectionGroupDisplayMode targetMode)
        {
            bool isSelected = mode == targetMode;
            SetPopupOptionState(id,
                isSelected ? CollectionListImageKeys.ListStateEnabled : CollectionListImageKeys.ListStateDisabled,
                isSelected ? "list_state_enabled.svg" : "list_state_disabled.svg");
        }

        UpdateOption("group-display:blocks", CollectionGroupDisplayMode.Blocks);
        UpdateOption("group-display:lines", CollectionGroupDisplayMode.Lines);
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

        AppliedIndicatorView indicator = new();

        indicator.Configure(GetIcon(resourceKey, fileName), tooltip);
        AppliedIndicators.Children.Add(indicator);
    }
}