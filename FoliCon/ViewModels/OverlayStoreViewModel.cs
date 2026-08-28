#nullable enable
namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel for the Overlay Store dialog. Shows available overlays from the catalog,
/// supports search/filter, and handles install/update/uninstall operations.
/// </summary>
public class OverlayStoreViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IOverlayRepositoryService _repositoryService;

    /// <summary>Null when constructed outside the dialog service (tests, previews).</summary>
    private readonly IDialogService? _dialogService;
    private readonly Func<OverlayCardViewModel, bool> _confirmRemoval;

    private readonly Dictionary<string, (bool IsInstalled, bool IsUpdateAvailable)> _cardState = new(StringComparer.OrdinalIgnoreCase);

    public OverlayStoreViewModel(
        DialogCloseListener requestClose,
        IOverlayRepositoryService repositoryService,
        IDialogService dialogService)
        : this(repositoryService, ConfirmRemoval, dialogService)
    {
        RequestClose = requestClose;
    }

    public OverlayStoreViewModel(IOverlayRepositoryService repositoryService)
        : this(repositoryService, ConfirmRemoval, dialogService: null)
    {
    }

    private OverlayStoreViewModel(
        IOverlayRepositoryService repositoryService,
        Func<OverlayCardViewModel, bool> confirmRemoval,
        IDialogService? dialogService)
    {
        RequestClose = default;
        _repositoryService = repositoryService;
        _confirmRemoval = confirmRemoval;
        _dialogService = dialogService;
        VisibleOverlays = CollectionViewSource.GetDefaultView(Overlays);
        VisibleOverlays.Filter = FilterOverlay;
        ApplySort();

        RefreshCommand = new DelegateCommand(async () => await LoadCatalogAsync(forceRefresh: true));
        InstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await InstallOverlayAsync(o), o => o is { IsInstalled: false, IsLoading: false });
        UpdateCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UpdateOverlayAsync(o), o => o is { IsUpdateAvailable: true, IsLoading: false });
        UninstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UninstallOverlayAsync(o), o => o is { IsInstalled: true, IsLoading: false });
        UpdateAllCommand = new DelegateCommand(async () => await UpdateAllAsync(), () => UpdatesCount > 0 && !IsUpdatingAll);

        // Only offered when the store was opened through the dialog service; the
        // bare test/preview constructor has no way to launch another dialog.
        CreateOverlayCommand = new DelegateCommand(OpenDesigner, () => _dialogService != null);
        ClearTagFiltersCommand = new DelegateCommand(ClearTagFilters, () => HasSelectedTags);

        CatalogLoaded = LoadCatalogAsync();
    }

    public static OverlayStoreViewModel Create(
        IOverlayRepositoryService repositoryService,
        Func<OverlayCardViewModel, bool> confirmRemoval) => new(repositoryService, confirmRemoval, dialogService: null);

    #region Properties

    public static string Title => Lang.OverlayStore;

    public ObservableCollection<OverlayCardViewModel> Overlays { get; } = [];

    public ICollectionView VisibleOverlays { get; }

    public Task CatalogLoaded { get; }

    public OverlayCardViewModel? SelectedOverlay
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string SearchQuery
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    /// <summary>
    /// Tag chips, each independently selectable. Multiple active tags narrow the results
    /// (AND), which a single-select dropdown could not express.
    /// </summary>
    public ObservableCollection<OverlayTagFilterViewModel> TagFilters { get; } = [];

    /// <summary>Currently active tags, in display order.</summary>
    public IReadOnlyList<string> SelectedTags =>
        [.. TagFilters.Where(t => t.IsSelected).Select(t => t.Tag)];

    public bool HasSelectedTags => TagFilters.Any(t => t.IsSelected);

    public bool HasTagFilters => TagFilters.Count > 0;

    /// <summary>
    /// The status dropdown's entries. This is the only place the filter labels appear;
    /// filtering itself runs on <see cref="OverlayStatusFilterOption.Value"/> so translating
    /// these strings cannot break it.
    /// </summary>
    public ObservableCollection<OverlayStatusFilterOption> AvailableStatusFilters { get; } =
    [
        new(OverlayStatusFilter.All, Lang.OverlayStoreFilterAll),
        new(OverlayStatusFilter.Installed, Lang.OverlayStoreFilterInstalled),
        new(OverlayStatusFilter.NotInstalled, Lang.OverlayStoreFilterNotInstalled),
        new(OverlayStatusFilter.UpdateAvailable, Lang.OverlayStoreFilterUpdateAvailable)
    ];

    public OverlayStatusFilter SelectedStatusFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = OverlayStatusFilter.All;

    /// <summary>
    /// The sort dropdown's entries. Same reasoning as <see cref="AvailableStatusFilters"/>:
    /// labels are translatable, the underlying <see cref="OverlaySortOption"/> is not.
    /// </summary>
    public ObservableCollection<OverlaySortOptionItem> AvailableSortOptions { get; } =
    [
        new(OverlaySortOption.Newest, Lang.OverlayStoreSortNewest),
        new(OverlaySortOption.NameAscending, Lang.OverlayStoreSortNameAsc),
        new(OverlaySortOption.Author, Lang.OverlayStoreSortAuthor)
    ];

    public OverlaySortOption SelectedSortOption
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplySort();
            }
        }
    } = OverlaySortOption.Newest;

    public OverlayStoreSection CurrentSection
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(CurrentSectionIndex));
            RaisePropertyChanged(nameof(IsDiscoverSection));
            ApplyFilter();
        }
    }

    public int CurrentSectionIndex
    {
        get => (int)CurrentSection;
        set => CurrentSection = (OverlayStoreSection)value;
    }

    public bool IsDiscoverSection => CurrentSection == OverlayStoreSection.Discover;

    public int InstalledCount { get; private set; }
    public int UpdatesCount { get; private set; }
    public string InstalledSectionTitle => string.Format(Lang.OverlayStoreInstalledTab, InstalledCount);
    public string UpdatesSectionTitle => string.Format(Lang.OverlayStoreUpdatesTab, UpdatesCount);

    public bool HasVisibleOverlays { get; private set; }

    public string EmptyStateTitle => CurrentSection switch
    {
        OverlayStoreSection.Installed => Lang.OverlayStoreEmptyInstalledTitle,
        OverlayStoreSection.Updates => Lang.OverlayStoreEmptyUpdatesTitle,
        _ => Lang.OverlayStoreEmptyDiscoverTitle
    };

    public string EmptyStateMessage => CurrentSection switch
    {
        OverlayStoreSection.Installed => Lang.OverlayStoreEmptyInstalledMessage,
        OverlayStoreSection.Updates => Lang.OverlayStoreEmptyUpdatesMessage,
        _ => Lang.OverlayStoreEmptyDiscoverMessage
    };

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsUpdatingAll
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateAllCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    public bool HasError
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string ErrorMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// All unique tags from the catalog for the tag filter dropdown.
    /// </summary>

    #endregion

    #region Commands

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<OverlayCardViewModel> InstallCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UpdateCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UninstallCommand { get; }
    public DelegateCommand UpdateAllCommand { get; }

    /// <summary>Opens the Overlay Designer so a browsing author can start their own overlay.</summary>
    public DelegateCommand CreateOverlayCommand { get; }

    /// <summary>Deselects every tag chip in one action.</summary>
    public DelegateCommand ClearTagFiltersCommand { get; }

    #endregion

    #region Catalog Loading

    private List<OverlayCatalogEntry> _allEntries = [];

    private async Task LoadCatalogAsync(bool forceRefresh = false)
    {
        try
        {
            IsLoading = true;
            HasError = false;
            StatusMessage = forceRefresh ? Lang.OverlayStoreRefreshingCatalog : Lang.OverlayStoreLoadingCatalog;

            if (forceRefresh)
            {
                _repositoryService.InvalidateCache();
            }

            var catalog = await _repositoryService.FetchCatalogAsync();
            _allEntries = catalog.Overlays;

            BuildTagFilters();

            var cards = _allEntries.Select(CreateCardViewModel).ToList();
            foreach (var existingCard in Overlays)
            {
                existingCard.PropertyChanged -= OnCardPropertyChanged;
            }

            Overlays.Clear();
            foreach (var card in cards)
            {
                card.PropertyChanged += OnCardPropertyChanged;
                Overlays.Add(card);
            }

            _ = Task.WhenAll(cards.Select(c => c.LoadPreviewAsync()));
            StatusMessage = string.Format(Lang.OverlayStoreOverlaysAvailable, cards.Count);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load overlay catalog");
            HasError = true;
            ErrorMessage = string.Format(Lang.OverlayStoreCatalogLoadFailed, ex.Message);
            StatusMessage = Lang.OverlayStoreCatalogLoadError;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private OverlayCardViewModel CreateCardViewModel(OverlayCatalogEntry entry)
    {
        // Rewrite preview URL to use the configured base URL (supports local testing)
        var baseUrl = _repositoryService.BaseUrl;
        if (!string.IsNullOrEmpty(baseUrl) && !baseUrl.StartsWith("https://raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
        {
            entry = new OverlayCatalogEntry
            {
                Id = entry.Id,
                DisplayName = entry.DisplayName,
                Author = entry.Author,
                Description = entry.Description,
                OverlayVersion = entry.OverlayVersion,
                Tags = entry.Tags,
                PreviewUrl = $"{baseUrl}/overlays/{entry.Id}/preview.png",
                OverlayBaseUrl = entry.OverlayBaseUrl,
                OverlayPath = entry.OverlayPath,
                SizeBytes = entry.SizeBytes,
                Sha256 = entry.Sha256,
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt
            };
        }

        var card = new OverlayCardViewModel(entry, _repositoryService.GetInstalledVersion(entry.Id))
        {
            IsInstalled = _repositoryService.IsOverlayInstalled(entry.Id), IsUpdateAvailable = _repositoryService.IsUpdateAvailable(entry.Id)
        };

        if (!_cardState.TryGetValue(entry.Id, out var state))
        {
            return card;
        }
        card.IsInstalled = state.IsInstalled;
        card.IsUpdateAvailable = state.IsUpdateAvailable;

        return card;
    }

    #endregion

    #region Filter

    private void ApplyFilter()
    {
        VisibleOverlays.Refresh();
        UpdateViewState();
    }

    /// <summary>
    /// Applies <see cref="SelectedSortOption"/> as the grid's sort order. Newest sorts by
    /// creation date descending; the other two are plain alphabetical ascending.
    /// </summary>
    private void ApplySort()
    {
        VisibleOverlays.SortDescriptions.Clear();
        var (property, direction) = SelectedSortOption switch
        {
            OverlaySortOption.NameAscending => (nameof(OverlayCardViewModel.DisplayName), ListSortDirection.Ascending),
            OverlaySortOption.Author => (nameof(OverlayCardViewModel.Author), ListSortDirection.Ascending),
            _ => (nameof(OverlayCardViewModel.CreatedAt), ListSortDirection.Descending)
        };
        VisibleOverlays.SortDescriptions.Add(new SortDescription(property, direction));
    }

    /// <summary>
    /// Rebuilds the chip list from the catalog, preserving which tags were already active so a
    /// refresh does not silently reset the author's filter.
    /// </summary>
    private void BuildTagFilters()
    {
        var previouslySelected = new HashSet<string>(SelectedTags, StringComparer.OrdinalIgnoreCase);

        foreach (var existing in TagFilters)
        {
            existing.PropertyChanged -= OnTagFilterChanged;
        }
        TagFilters.Clear();

        var counts = _allEntries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in counts)
        {
            var chip = new OverlayTagFilterViewModel(group.Key, group.Count())
            {
                IsSelected = previouslySelected.Contains(group.Key)
            };
            chip.PropertyChanged += OnTagFilterChanged;
            TagFilters.Add(chip);
        }

        RaisePropertyChanged(nameof(HasTagFilters));
        RaisePropertyChanged(nameof(HasSelectedTags));
    }

    private void OnTagFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OverlayTagFilterViewModel.IsSelected))
        {
            return;
        }

        RaisePropertyChanged(nameof(SelectedTags));
        RaisePropertyChanged(nameof(HasSelectedTags));
        ClearTagFiltersCommand.RaiseCanExecuteChanged();
        ApplyFilter();
    }

    private void ClearTagFilters()
    {
        foreach (var chip in TagFilters)
        {
            chip.IsSelected = false;
        }
    }

    private bool FilterOverlay(object item)
    {
        if (item is not OverlayCardViewModel card)
        {
            return false;
        }

        if (CurrentSection == OverlayStoreSection.Installed && !card.IsInstalled ||
            CurrentSection == OverlayStoreSection.Updates && !card.IsUpdateAvailable)
        {
            return false;
        }

        if (CurrentSection == OverlayStoreSection.Discover && SelectedStatusFilter switch
            {
                OverlayStatusFilter.Installed => !card.IsInstalled,
                OverlayStatusFilter.NotInstalled => card.IsInstalled,
                OverlayStatusFilter.UpdateAvailable => !card.IsUpdateAvailable,
                _ => false
            })
        {
            return false;
        }

        // Multiple selected tags narrow the results: an overlay must carry all of them.
        var selectedTags = SelectedTags;
        if (selectedTags.Count > 0 &&
            !selectedTags.All(tag => card.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return true;
        }

        var query = SearchQuery.Trim();
        return card.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               card.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               card.Author.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               card.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OverlayCardViewModel.IsInstalled) or nameof(OverlayCardViewModel.IsUpdateAvailable))
        {
            ApplyFilter();
        }
    }

    private void UpdateViewState()
    {
        InstalledCount = Overlays.Count(card => card.IsInstalled);
        UpdatesCount = Overlays.Count(card => card.IsUpdateAvailable);
        HasVisibleOverlays = !VisibleOverlays.IsEmpty;

        RaisePropertyChanged(nameof(InstalledCount));
        RaisePropertyChanged(nameof(UpdatesCount));
        RaisePropertyChanged(nameof(InstalledSectionTitle));
        RaisePropertyChanged(nameof(UpdatesSectionTitle));
        RaisePropertyChanged(nameof(HasVisibleOverlays));
        RaisePropertyChanged(nameof(EmptyStateTitle));
        RaisePropertyChanged(nameof(EmptyStateMessage));
        UpdateAllCommand.RaiseCanExecuteChanged();

        var visibleCount = VisibleOverlays.Cast<object>().Count();
        var hasFilters = !string.IsNullOrWhiteSpace(SearchQuery) ||
                         HasSelectedTags ||
                         SelectedStatusFilter != OverlayStatusFilter.All;

        // "Noun: count" instead of "{n} overlay(s)": English pluralises with a trailing "s",
        // which has no equivalent in ru/ar/ja/hi. This phrasing needs no plural form at all.
        StatusMessage = hasFilters
            ? string.Format(Lang.OverlayStoreVisibleCount, visibleCount)
            : string.Format(Lang.OverlayStoreTotalCount, visibleCount);
    }

    #endregion

    #region Install/Update/Uninstall

    private async Task InstallOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null)
        {
            return;
        }

        try
        {
            var installing = string.Format(Lang.OverlayStoreInstalling, card.DisplayName);
            BeginOperation(card, installing);
            StatusMessage = installing;

            var progress = new Progress<(int Percent, string Status)>(p =>
            {
                card.ProgressPercentage = p.Percent;
                card.OperationMessage = string.Format(Lang.OverlayStoreProgressFormat, p.Status, p.Percent);
                StatusMessage = string.Format(Lang.OverlayStoreInstallingProgress, card.DisplayName, p.Status, p.Percent);
            });

            await _repositoryService.InstallOverlayAsync(card.CatalogEntry, progress);

            card.IsInstalled = true;
            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            var installed = string.Format(Lang.OverlayStoreInstalledSuccess, card.DisplayName);
            CompleteOperation(card, installed);
            StatusMessage = installed;
            Logger.Info("Installed overlay '{Id}' from store", card.Id);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}'", card.Id);
            var installFailed = string.Format(Lang.OverlayStoreInstallFailed, ex.Message);
            FailOperation(card, installFailed);
            StatusMessage = installFailed;
        }
        finally
        {
            card.IsLoading = false;
            InstallCommand.RaiseCanExecuteChanged();
            UninstallCommand.RaiseCanExecuteChanged();
            UpdateCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task UpdateOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null)
        {
            return;
        }

        try
        {
            var updating = string.Format(Lang.OverlayStoreUpdating, card.DisplayName);
            BeginOperation(card, updating);
            StatusMessage = updating;

            var progress = new Progress<(int Percent, string Status)>(p =>
            {
                card.ProgressPercentage = p.Percent;
                card.OperationMessage = string.Format(Lang.OverlayStoreProgressFormat, p.Status, p.Percent);
                StatusMessage = string.Format(Lang.OverlayStoreUpdatingProgress, card.DisplayName, p.Status, p.Percent);
            });

            await _repositoryService.UpdateOverlayAsync(card.Id, progress);

            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            CompleteOperation(card, string.Format(Lang.OverlayStoreCardUpToDate, card.DisplayName));
            StatusMessage = string.Format(Lang.OverlayStoreUpdatedSuccess, card.DisplayName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update overlay '{Id}'", card.Id);
            var updateFailed = string.Format(Lang.OverlayStoreUpdateFailed, ex.Message);
            FailOperation(card, updateFailed);
            StatusMessage = updateFailed;
        }
        finally
        {
            card.IsLoading = false;
            UpdateCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task UninstallOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null)
        {
            return;
        }

        if (!_confirmRemoval(card))
        {
            return;
        }

        try
        {
            var removing = string.Format(Lang.OverlayStoreRemoving, card.DisplayName);
            BeginOperation(card, removing);
            StatusMessage = removing;

            await _repositoryService.UninstallOverlayAsync(card.Id);

            card.IsInstalled = false;
            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            var removed = string.Format(Lang.OverlayStoreRemoved, card.DisplayName);
            CompleteOperation(card, removed);
            StatusMessage = removed;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to uninstall overlay '{Id}'", card.Id);
            var removeFailed = string.Format(Lang.OverlayStoreRemoveFailed, ex.Message);
            FailOperation(card, removeFailed);
            StatusMessage = removeFailed;
        }
        finally
        {
            card.IsLoading = false;
            InstallCommand.RaiseCanExecuteChanged();
            UninstallCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task UpdateAllAsync()
    {
        if (IsUpdatingAll)
        {
            return;
        }

        var pendingUpdates = Overlays.Where(card => card.IsUpdateAvailable).ToList();
        if (pendingUpdates.Count == 0)
        {
            return;
        }

        IsUpdatingAll = true;
        try
        {
            for (var index = 0; index < pendingUpdates.Count; index++)
            {
                StatusMessage = string.Format(Lang.OverlayStoreUpdatingNOfM,
                    index + 1, pendingUpdates.Count, pendingUpdates[index].DisplayName);
                await UpdateOverlayAsync(pendingUpdates[index]);
            }

            StatusMessage = Lang.OverlayStoreAllUpToDate;
        }
        finally
        {
            IsUpdatingAll = false;
        }
    }

    private static void BeginOperation(OverlayCardViewModel card, string message)
    {
        card.IsLoading = true;
        card.ProgressPercentage = 0;
        card.OperationMessage = message;
        card.HasOperationError = false;
        card.IsOperationSuccessful = false;
    }

    private static void CompleteOperation(OverlayCardViewModel card, string message)
    {
        card.ProgressPercentage = 100;
        card.OperationMessage = message;
        card.HasOperationError = false;
        card.IsOperationSuccessful = true;
    }

    private static void FailOperation(OverlayCardViewModel card, string message)
    {
        card.OperationMessage = message;
        card.HasOperationError = true;
        card.IsOperationSuccessful = false;
    }

    private static bool ConfirmRemoval(OverlayCardViewModel card) =>
        MessageBox.Show(CustomMessageBox.Ask(
            string.Format(Lang.OverlayStoreConfirmRemoveBody, card.DisplayName),
            Lang.OverlayRemoveOverlayTitle)) == MessageBoxResult.Yes;

    /// <summary>
    /// Opens the designer on top of the store. The catalog is left as-is: a designed overlay
    /// is not published, so nothing in the listing changes.
    /// </summary>
    private void OpenDesigner() => _dialogService?.ShowOverlayDesigner(_ => { });

    #endregion

    #region IDialogAware

    public DialogCloseListener RequestClose { get; }

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed() { }

    public virtual void OnDialogOpened(IDialogParameters parameters) { }

    #endregion
}
