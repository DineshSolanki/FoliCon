using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;

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

    private readonly Dictionary<string, (bool IsInstalled, bool IsUpdateAvailable)> _cardState = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasChanges;

    public OverlayStoreViewModel(
        DialogCloseListener requestClose,
        IOverlayRepositoryService repositoryService)
    {
        RequestClose = requestClose;
        _repositoryService = repositoryService;

        RefreshCommand = new DelegateCommand(async () => await LoadCatalogAsync(forceRefresh: true));
        InstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await InstallOverlayAsync(o), o => o is not { IsInstalled: true });
        UpdateCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UpdateOverlayAsync(o), o => o is { IsUpdateAvailable: true });
        UninstallCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UninstallOverlayAsync(o), o => o is { IsInstalled: true });

        _ = LoadCatalogAsync();
    }

    #region Properties

    public string Title => "Overlay Store";

    public ObservableCollection<OverlayCardViewModel> Overlays
    {
        get;
        set => SetProperty(ref field, value);
    } = [];

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
                ApplyFilter();
        }
    } = string.Empty;

    public string SelectedTag
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                ApplyFilter();
        }
    } = string.Empty;

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
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
                _repositoryService.InvalidateCache();

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
                AvailableTags.Add(tag);

            // Build card ViewModels
            var cards = _allEntries.Select(CreateCardViewModel).ToList();

            // Load previews in background (fire-and-forget)
            _ = Task.WhenAll(cards.Select(c => c.LoadPreviewAsync()));

            Overlays = new ObservableCollection<OverlayCardViewModel>(cards);
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

        var card = new OverlayCardViewModel(entry)
        {
            IsInstalled = _repositoryService.IsOverlayInstalled(entry.Id), IsUpdateAvailable = _repositoryService.IsUpdateAvailable(entry.Id)
        };

        if (!_cardState.TryGetValue(entry.Id, out var state)) return card;
        card.IsInstalled = state.IsInstalled;
        card.IsUpdateAvailable = state.IsUpdateAvailable;

        return card;
    }

    #endregion

    #region Filter

    private void ApplyFilter()
    {
        var filtered = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedTag))
        {
            filtered = filtered.Where(e =>
                e.Tags.Contains(SelectedTag, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var query = SearchQuery.Trim();
            filtered = filtered.Where(e =>
                e.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Author.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        var cards = filtered.Select(CreateCardViewModel).ToList();
        _ = Task.WhenAll(cards.Select(c => c.LoadPreviewAsync()));
        Overlays = new ObservableCollection<OverlayCardViewModel>(cards);
        StatusMessage = $"{cards.Count} overlays" + (string.IsNullOrWhiteSpace(SearchQuery) && string.IsNullOrWhiteSpace(SelectedTag) ? "" : " (filtered)");
    }

    #endregion

    #region Install/Update/Uninstall

    private async Task InstallOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null) return;

        try
        {
            card.IsLoading = true;
            StatusMessage = $"Installing {card.DisplayName}...";

            var progress = new Progress<(int Percent, string Status)>(p =>
                StatusMessage = $"Installing {card.DisplayName}: {p.Status} ({p.Percent}%)");

            await _repositoryService.InstallOverlayAsync(card.CatalogEntry, progress);

            card.IsInstalled = true;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            _hasChanges = true;
            StatusMessage = $"{card.DisplayName} installed successfully";
            Logger.Info("Installed overlay '{Id}' from store", card.Id);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}'", card.Id);
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
        if (card == null) return;

        try
        {
            card.IsLoading = true;
            StatusMessage = $"Updating {card.DisplayName}...";

            var progress = new Progress<(int Percent, string Status)>(p =>
                StatusMessage = $"Updating {card.DisplayName}: {p.Status} ({p.Percent}%)");

            await _repositoryService.UpdateOverlayAsync(card.Id, progress);

            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            _hasChanges = true;
            StatusMessage = $"{card.DisplayName} updated successfully";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update overlay '{Id}'", card.Id);
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
        if (card == null) return;

        try
        {
            card.IsLoading = true;
            StatusMessage = $"Removing {card.DisplayName}...";

            await _repositoryService.UninstallOverlayAsync(card.Id);

            card.IsInstalled = false;
            card.IsUpdateAvailable = false;
            _cardState[card.Id] = (card.IsInstalled, card.IsUpdateAvailable);
            _hasChanges = true;
            StatusMessage = $"{card.DisplayName} removed";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to uninstall overlay '{Id}'", card.Id);
            StatusMessage = $"Remove failed: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
            InstallCommand.RaiseCanExecuteChanged();
            UninstallCommand.RaiseCanExecuteChanged();
        }
    }

    #endregion

    #region IDialogAware

    public DialogCloseListener RequestClose { get; }

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed() { }

    public virtual void OnDialogOpened(IDialogParameters parameters) { }

    #endregion
}
