#nullable enable
namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel for the Overlay Store dialog. Shows available overlays from the catalog,
/// supports search/filter, and handles install/update/uninstall operations.
/// </summary>
[Localizable(false)]
public class OverlayStoreViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IOverlayRepositoryService _repositoryService;
    private readonly Func<OverlayCardViewModel, bool> _confirmRemoval;

    private readonly Dictionary<string, (bool IsInstalled, bool IsUpdateAvailable)> _cardState = new(StringComparer.OrdinalIgnoreCase);

    public OverlayStoreViewModel(
        DialogCloseListener requestClose,
        IOverlayRepositoryService repositoryService)
        : this(repositoryService, ConfirmRemoval)
    {
        RequestClose = requestClose;
    }

    public OverlayStoreViewModel(IOverlayRepositoryService repositoryService)
        : this(repositoryService, ConfirmRemoval)
    {
    }

    private OverlayStoreViewModel(
        IOverlayRepositoryService repositoryService,
        Func<OverlayCardViewModel, bool> confirmRemoval)
    {
        RequestClose = default;
        _repositoryService = repositoryService;
        _confirmRemoval = confirmRemoval;
        VisibleOverlays = CollectionViewSource.GetDefaultView(Overlays);
        VisibleOverlays.Filter = FilterOverlay;

        RefreshCommand = new DelegateCommand(async () => await LoadCatalogAsync(forceRefresh: true));
        InstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await InstallOverlayAsync(o), o => o is { IsInstalled: false, IsLoading: false });
        UpdateCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UpdateOverlayAsync(o), o => o is { IsUpdateAvailable: true, IsLoading: false });
        UninstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UninstallOverlayAsync(o), o => o is { IsInstalled: true, IsLoading: false });
        UpdateAllCommand = new DelegateCommand(async () => await UpdateAllAsync(), () => UpdatesCount > 0 && !IsUpdatingAll);

        CatalogLoaded = LoadCatalogAsync();
    }

    public static OverlayStoreViewModel Create(
        IOverlayRepositoryService repositoryService,
        Func<OverlayCardViewModel, bool> confirmRemoval) => new(repositoryService, confirmRemoval);

    #region Properties

    public static string Title => "Overlay Store";

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

    public string SelectedTag
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

    public ObservableCollection<string> AvailableStatusFilters { get; } =
    [
        "All overlays",
        "Installed",
        "Not installed",
        "Update available"
    ];

    public string SelectedStatusFilter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = "All overlays";

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
    public string InstalledSectionTitle => $"Installed ({InstalledCount})";
    public string UpdatesSectionTitle => $"Updates ({UpdatesCount})";

    public bool HasVisibleOverlays { get; private set; }

    public string EmptyStateTitle => CurrentSection switch
    {
        OverlayStoreSection.Installed => "No installed overlays",
        OverlayStoreSection.Updates => "Everything is up to date",
        _ => "No overlays found"
    };

    public string EmptyStateMessage => CurrentSection switch
    {
        OverlayStoreSection.Installed => "Install an overlay from Discover and it will appear here.",
        OverlayStoreSection.Updates => "Installed overlays will appear here when an update is available.",
        _ => "Try another search, tag, or installation filter."
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
    public ObservableCollection<string> AvailableTags { get; } = [];

    #endregion

    #region Commands

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand<OverlayCardViewModel> InstallCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UpdateCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UninstallCommand { get; }
    public DelegateCommand UpdateAllCommand { get; }

    #endregion

    #region Catalog Loading

    private List<OverlayCatalogEntry> _allEntries = [];

    private async Task LoadCatalogAsync(bool forceRefresh = false)
    {
        try
        {
            IsLoading = true;
            HasError = false;
            StatusMessage = forceRefresh ? "Refreshing catalog..." : "Loading catalog...";

            if (forceRefresh)
            {
                _repositoryService.InvalidateCache();
            }

            var catalog = await _repositoryService.FetchCatalogAsync();
            _allEntries = catalog.Overlays;

            // Build tag list
            AvailableTags.Clear();
            AvailableTags.Add(""); // "All" tag
            var tags = _allEntries
                .SelectMany(e => e.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);
            foreach (var tag in tags)
            {
                AvailableTags.Add(tag);
            }

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
            StatusMessage = $"{cards.Count} overlays available";
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load overlay catalog");
            HasError = true;
            ErrorMessage = $"Failed to load catalog: {ex.Message}";
            StatusMessage = "Error loading catalog";
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
                "Installed" => !card.IsInstalled,
                "Not installed" => card.IsInstalled,
                "Update available" => !card.IsUpdateAvailable,
                _ => false
            })
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedTag) &&
            !card.Tags.Contains(SelectedTag, StringComparer.OrdinalIgnoreCase))
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
                         !string.IsNullOrWhiteSpace(SelectedTag) ||
                         SelectedStatusFilter != "All overlays";
        StatusMessage = $"{visibleCount} overlay{(visibleCount == 1 ? string.Empty : "s")}" +
                        (hasFilters ? " shown" : string.Empty);
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
            BeginOperation(card, $"Installing {card.DisplayName}...");
            StatusMessage = $"Installing {card.DisplayName}...";

            var progress = new Progress<(int Percent, string Status)>(p =>
            {
                card.ProgressPercentage = p.Percent;
                card.OperationMessage = $"{p.Status} ({p.Percent}%)";
                StatusMessage = $"Installing {card.DisplayName}: {p.Status} ({p.Percent}%)";
            });

            await _repositoryService.InstallOverlayAsync(card.CatalogEntry, progress);

            card.IsInstalled = true;
            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            CompleteOperation(card, $"{card.DisplayName} installed successfully");
            StatusMessage = $"{card.DisplayName} installed successfully";
            Logger.Info("Installed overlay '{Id}' from store", card.Id);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}'", card.Id);
            FailOperation(card, $"Install failed: {ex.Message}");
            StatusMessage = $"Install failed: {ex.Message}";
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
            BeginOperation(card, $"Updating {card.DisplayName}...");
            StatusMessage = $"Updating {card.DisplayName}...";

            var progress = new Progress<(int Percent, string Status)>(p =>
            {
                card.ProgressPercentage = p.Percent;
                card.OperationMessage = $"{p.Status} ({p.Percent}%)";
                StatusMessage = $"Updating {card.DisplayName}: {p.Status} ({p.Percent}%)";
            });

            await _repositoryService.UpdateOverlayAsync(card.Id, progress);

            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            CompleteOperation(card, $"{card.DisplayName} is up to date");
            StatusMessage = $"{card.DisplayName} updated successfully";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update overlay '{Id}'", card.Id);
            FailOperation(card, $"Update failed: {ex.Message}");
            StatusMessage = $"Update failed: {ex.Message}";
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
            BeginOperation(card, $"Removing {card.DisplayName}...");
            StatusMessage = $"Removing {card.DisplayName}...";

            await _repositoryService.UninstallOverlayAsync(card.Id);

            card.IsInstalled = false;
            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            CompleteOperation(card, $"{card.DisplayName} removed");
            StatusMessage = $"{card.DisplayName} removed";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to uninstall overlay '{Id}'", card.Id);
            FailOperation(card, $"Remove failed: {ex.Message}");
            StatusMessage = $"Remove failed: {ex.Message}";
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
                StatusMessage = $"Updating {index + 1} of {pendingUpdates.Count}: {pendingUpdates[index].DisplayName}";
                await UpdateOverlayAsync(pendingUpdates[index]);
            }

            StatusMessage = "All overlays are up to date";
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
            $"Remove {card.DisplayName}? You can install it again later.",
            "Remove overlay")) == MessageBoxResult.Yes;

    #endregion

    #region IDialogAware

    public DialogCloseListener RequestClose { get; }

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed() { }

    public virtual void OnDialogOpened(IDialogParameters parameters) { }

    #endregion
}
