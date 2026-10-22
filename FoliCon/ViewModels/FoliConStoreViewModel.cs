#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using FoliCon.Models.Data;
using FoliCon.Models.Themes;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Themes;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace FoliCon.ViewModels;

/// <summary>
/// Unified Store ViewModel managing community themes and icon overlays under a consolidated tabbed interface.
/// Supports transient live theme previews with automatic Memento rollback on exit/cancel.
/// </summary>
public class FoliConStoreViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IOverlayRepositoryService _overlayRepositoryService;
    private readonly IThemeRepositoryService _themeRepositoryService;
    private readonly IThemeStorageService _themeStorageService;
    private readonly IThemeParser _themeParser;
    private readonly IDialogService? _dialogService;

    // Theme Preview Memento Snapshot
    private ThemeStateSnapshot? _previewSnapshot;

    public FoliConStoreViewModel(
        DialogCloseListener requestClose,
        IOverlayRepositoryService overlayRepositoryService,
        IThemeRepositoryService themeRepositoryService,
        IThemeStorageService themeStorageService,
        IThemeParser themeParser,
        IDialogService dialogService)
    {
        RequestClose = requestClose;
        _overlayRepositoryService = overlayRepositoryService ?? throw new ArgumentNullException(nameof(overlayRepositoryService));
        _themeRepositoryService = themeRepositoryService ?? throw new ArgumentNullException(nameof(themeRepositoryService));
        _themeStorageService = themeStorageService ?? throw new ArgumentNullException(nameof(themeStorageService));
        _themeParser = themeParser ?? throw new ArgumentNullException(nameof(themeParser));
        _dialogService = dialogService;

        // Dedicated Child ViewModel for full Overlays catalog experience
        OverlaysViewModel = new OverlayStoreViewModel(_overlayRepositoryService, _dialogService);
        OverlaysViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(OverlayStoreViewModel.StatusMessage) && SelectedTabIndex == 1)
            {
                RaisePropertyChanged(nameof(StatusMessage));
            }
        };

        VisibleThemes = CollectionViewSource.GetDefaultView(Themes);
        VisibleThemes.Filter = FilterTheme;

        // Theme Commands
        PreviewThemeCommand = new DelegateCommand<ThemeCardViewModel>(ExecutePreviewTheme);
        RevertPreviewCommand = new DelegateCommand(ExecuteRevertPreview, () => IsPreviewing)
            .ObservesProperty(() => IsPreviewing);
        ApplyThemeCommand = new DelegateCommand<ThemeCardViewModel>(async t => await ExecuteApplyThemeAsync(t));
        InstallThemeCommand = new DelegateCommand<ThemeCardViewModel>(async t => await ExecuteInstallThemeAsync(t));
        UpdateThemeCommand = new DelegateCommand<ThemeCardViewModel>(async t => await ExecuteUpdateThemeAsync(t));
        UninstallThemeCommand = new DelegateCommand<ThemeCardViewModel>(ExecuteUninstallTheme);
        ExportThemeCommand = new DelegateCommand<ThemeCardViewModel>(async t => await ExecuteExportThemeAsync(t));
        OpenThemesFolderCommand = new DelegateCommand(ExecuteOpenThemesFolder);

        // Overlay Commands (delegated to OverlaysViewModel for full capability and compatibility)
        InstallOverlayCommand = OverlaysViewModel.InstallCommand;
        UpdateOverlayCommand = OverlaysViewModel.UpdateCommand;
        UninstallOverlayCommand = OverlaysViewModel.UninstallCommand;

        RefreshCommand = new DelegateCommand(async () => await LoadAllCatalogsAsync(forceRefresh: true));
        CloseCommand = new DelegateCommand(ExecuteClose);
    }

    #region Tab & Navigation Properties

    private int _selectedTabIndex = 0;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                ApplyFilter();
                RaisePropertyChanged(nameof(IsThemesTabSelected));
                RaisePropertyChanged(nameof(IsOverlaysTabSelected));
                RaisePropertyChanged(nameof(StatusMessage));
            }
        }
    }

    public bool IsThemesTabSelected => SelectedTabIndex == 0;
    public bool IsOverlaysTabSelected => SelectedTabIndex == 1;

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get
        {
            if (SelectedTabIndex == 1 && OverlaysViewModel != null)
            {
                return OverlaysViewModel.StatusMessage;
            }
            return string.IsNullOrEmpty(_statusMessage)
                ? $"{Themes.Count} themes available"
                : _statusMessage;
        }
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public bool IsPreviewing
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string? PreviewingThemeName
    {
        get;
        private set => SetProperty(ref field, value);
    }

    #endregion

    #region Themes Collections & Commands

    public ThemeCardViewModel? SelectedTheme
    {
        get;
        set
        {
            if (SetProperty(ref field, value) && value != null)
            {
                // Live preview as user navigates through themes (IntelliJ style)
                ExecutePreviewTheme(value);
            }
        }
    }

    public ObservableCollection<ThemeCardViewModel> Themes { get; } = [];
    public ICollectionView VisibleThemes { get; }

    public DelegateCommand<ThemeCardViewModel> PreviewThemeCommand { get; }
    public DelegateCommand RevertPreviewCommand { get; }
    public DelegateCommand<ThemeCardViewModel> ApplyThemeCommand { get; }
    public DelegateCommand<ThemeCardViewModel> InstallThemeCommand { get; }
    public DelegateCommand<ThemeCardViewModel> UpdateThemeCommand { get; }
    public DelegateCommand<ThemeCardViewModel> UninstallThemeCommand { get; }
    public DelegateCommand<ThemeCardViewModel> ExportThemeCommand { get; }
    public DelegateCommand OpenThemesFolderCommand { get; }

    #endregion

    #region Overlays Child ViewModel & Delegations

    public OverlayStoreViewModel OverlaysViewModel { get; }

    public ObservableCollection<OverlayCardViewModel> Overlays => OverlaysViewModel.Overlays;
    public ICollectionView VisibleOverlays => OverlaysViewModel.VisibleOverlays;

    public DelegateCommand<OverlayCardViewModel> InstallOverlayCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UpdateOverlayCommand { get; }
    public DelegateCommand<OverlayCardViewModel> UninstallOverlayCommand { get; }

    #endregion

    #region Shared Commands & Dialog Lifecycle

    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand CloseCommand { get; }
    public DialogCloseListener RequestClose { get; }

    public string Title { get; private set; } = "FoliCon Community Store";

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        // UI-01: Auto-revert transient preview if user closes dialog without applying
        if (IsPreviewing && _previewSnapshot != null)
        {
            Logger.Info("Reverting live theme preview on FoliConStore dialog close.");
            MainWindowViewModel.RestoreThemeSnapshot(_previewSnapshot, _themeParser);
            _previewSnapshot = null;
            IsPreviewing = false;
        }

        OverlaysViewModel.OnDialogClosed();
    }

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        if (parameters.TryGetValue<string>("tab", out var tabName))
        {
            SelectedTabIndex = string.Equals(tabName, "overlays", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        if (parameters.TryGetValue<string>("search", out var search))
        {
            SearchQuery = search;
            if (SelectedTabIndex == 1)
            {
                OverlaysViewModel.SearchQuery = search;
            }
        }

        OverlaysViewModel.OnDialogOpened(parameters);
        await LoadAllCatalogsAsync(forceRefresh: false);
    }

    #endregion

    #region Catalog Loading & Merging

    public async Task LoadAllCatalogsAsync(bool forceRefresh = false)
    {
        IsLoading = true;
        try
        {
            if (forceRefresh)
            {
                _themeRepositoryService.InvalidateCache();
                _overlayRepositoryService.InvalidateCache();
            }

            // Load Themes
            await LoadThemesAsync();

            // Load Overlays with full previews, tag chips, and categories
            await OverlaysViewModel.LoadCatalogAsync(forceRefresh);
            RaisePropertyChanged(nameof(StatusMessage));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load store catalogs");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadThemesAsync()
    {
        // 1. Immediately populate installed themes (built-in + local custom) so the store is NEVER blank
        PopulateInstalledThemes();

        // 2. Asynchronously fetch remote catalog with a 5-second timeout
        ThemeCatalog remoteCatalog;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            remoteCatalog = await _themeRepositoryService.FetchCatalogAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to fetch remote theme catalog; keeping installed themes.");
            remoteCatalog = new ThemeCatalog();
        }

        // 3. Merge remote catalog themes & updates
        MergeRemoteThemes(remoteCatalog);
        RaisePropertyChanged(nameof(StatusMessage));
    }

    private void PopulateInstalledThemes()
    {
        Themes.Clear();
        var installedThemes = _themeStorageService.GetInstalledThemes();
        var activeId = MainWindowViewModel.ActiveThemeId;

        foreach (var local in installedThemes)
        {
            var isUpdate = _themeRepositoryService.IsUpdateAvailable(local.Id);
            var card = new ThemeCardViewModel(local, isInstalled: true, isUpdateAvailable: isUpdate)
            {
                IsActive = string.Equals(activeId, local.Id, StringComparison.OrdinalIgnoreCase)
            };
            Themes.Add(card);
        }

        VisibleThemes.Refresh();
    }

    private void MergeRemoteThemes(ThemeCatalog remoteCatalog)
    {
        if (remoteCatalog == null || remoteCatalog.Themes.Count == 0) return;

        var installedThemes = _themeStorageService.GetInstalledThemes();
        var installedDict = installedThemes.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);
        var activeId = MainWindowViewModel.ActiveThemeId;
        var existingMap = Themes.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        // Process remote themes: update existing cards or insert new ones at top
        foreach (var entry in remoteCatalog.Themes)
        {
            var isInstalled = installedDict.TryGetValue(entry.Id, out var localManifest);
            var isUpdate = _themeRepositoryService.IsUpdateAvailable(entry.Id);

            if (existingMap.TryGetValue(entry.Id, out var existingCard))
            {
                existingCard.IsInstalled = isInstalled;
                existingCard.IsUpdateAvailable = isUpdate;
                existingCard.AvailableVersion = isUpdate ? _themeRepositoryService.GetAvailableVersion(entry.Id) : null;
            }
            else
            {
                var card = new ThemeCardViewModel(entry, isInstalled, isUpdate, localManifest?.Version)
                {
                    IsActive = string.Equals(activeId, entry.Id, StringComparison.OrdinalIgnoreCase)
                };
                Themes.Insert(0, card);
            }
        }

        VisibleThemes.Refresh();
    }

    #endregion

    #region Theme Operations

    private void ExecutePreviewTheme(ThemeCardViewModel? card)
    {
        if (card == null) return;

        // Capture snapshot if this is the start of a preview session
        _previewSnapshot ??= MainWindowViewModel.CaptureThemeSnapshot();

        ThemeManifest? manifest = card.InstalledManifest ?? _themeStorageService.GetThemeById(card.Id);
        if (manifest == null && card.CatalogEntry?.Palette != null)
        {
            manifest = new ThemeManifest
            {
                Id = card.Id,
                Name = card.Name,
                BaseTheme = card.BaseTheme,
                Colors = card.CatalogEntry.Palette
            };
        }

        if (manifest != null)
        {
            MainWindowViewModel.ApplyThemeManifest(manifest, _themeParser);
            IsPreviewing = true;
            PreviewingThemeName = card.Name;
            Logger.Info("Live previewing theme '{Id}'", card.Id);
        }
    }

    private void ExecuteRevertPreview()
    {
        if (_previewSnapshot != null)
        {
            MainWindowViewModel.RestoreThemeSnapshot(_previewSnapshot, _themeParser);
            _previewSnapshot = null;
        }
        IsPreviewing = false;
        PreviewingThemeName = null;
    }

    private async Task ExecuteApplyThemeAsync(ThemeCardViewModel? card)
    {
        if (card == null) return;

        var manifest = card.InstalledManifest ?? _themeStorageService.GetThemeById(card.Id);
        if (manifest == null)
        {
            await ExecuteInstallThemeAsync(card);
            manifest = _themeStorageService.GetThemeById(card.Id);
        }

        if (manifest != null)
        {
            MainWindowViewModel.ApplyThemeManifest(manifest, _themeParser);

            // Confirm active choice (clear rollback snapshot)
            _previewSnapshot = null;
            IsPreviewing = false;
            PreviewingThemeName = null;

            foreach (var t in Themes)
            {
                t.IsActive = string.Equals(t.Id, card.Id, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private async Task ExecuteInstallThemeAsync(ThemeCardViewModel? card)
    {
        if (card?.CatalogEntry == null) return;

        card.IsLoading = true;
        card.OperationMessage = "Installing...";

        var progress = new Progress<(int Percent, string Status)>(p =>
        {
            card.ProgressPercentage = p.Percent;
            card.OperationMessage = p.Status;
        });

        try
        {
            var manifest = await _themeRepositoryService.InstallThemeAsync(card.CatalogEntry, progress);
            card.IsInstalled = true;
            card.IsUpdateAvailable = false;
            card.InstalledVersion = manifest.Version;
            card.OperationMessage = "Installed";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to install theme '{Id}'", card.Id);
            card.OperationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private async Task ExecuteUpdateThemeAsync(ThemeCardViewModel? card)
    {
        if (card == null) return;

        card.IsLoading = true;
        card.OperationMessage = "Updating...";

        var progress = new Progress<(int Percent, string Status)>(p =>
        {
            card.ProgressPercentage = p.Percent;
            card.OperationMessage = p.Status;
        });

        try
        {
            var manifest = await _themeRepositoryService.UpdateThemeAsync(card.Id, progress);
            card.IsUpdateAvailable = false;
            card.InstalledVersion = manifest.Version;
            card.OperationMessage = "Updated";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to update theme '{Id}'", card.Id);
            card.OperationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private void ExecuteUninstallTheme(ThemeCardViewModel? card)
    {
        if (card == null || card.IsBuiltIn) return;

        if (_themeStorageService.DeleteTheme(card.Id))
        {
            card.IsInstalled = false;
            card.IsActive = false;
            card.OperationMessage = "Uninstalled";

            if (card.CatalogEntry == null)
            {
                Themes.Remove(card);
            }
            VisibleThemes.Refresh();
            RaisePropertyChanged(nameof(StatusMessage));
        }
    }

    private async Task ExecuteExportThemeAsync(ThemeCardViewModel? card)
    {
        if (card == null) return;

        var saveDialog = new SaveFileDialog
        {
            Title = "Export FoliCon Theme",
            Filter = "FoliCon Theme Package (*.folicontheme)|*.folicontheme",
            FileName = $"{card.Id}.folicontheme"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                await _themeStorageService.ExportThemePackageAsync(card.Id, saveDialog.FileName);
                card.OperationMessage = "Exported package";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to export theme {Id}", card.Id);
                card.OperationMessage = "Export failed";
            }
        }
    }

    private void ExecuteOpenThemesFolder()
    {
        try
        {
            var dir = _themeStorageService.ThemesDirectory;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to open themes directory");
        }
    }

    #endregion

    #region Filtering

    private void ApplyFilter()
    {
        VisibleThemes.Refresh();
    }

    private bool FilterTheme(object item)
    {
        if (item is not ThemeCardViewModel card) return false;
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

        var q = SearchQuery.Trim();
        return card.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               card.Author.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               card.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
               card.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    private void ExecuteClose()
    {
        RequestClose.Invoke(ButtonResult.OK);
    }

    #endregion
}
