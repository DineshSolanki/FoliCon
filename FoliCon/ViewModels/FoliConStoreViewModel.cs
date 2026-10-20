#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
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

        VisibleThemes = CollectionViewSource.GetDefaultView(Themes);
        VisibleThemes.Filter = FilterTheme;

        VisibleOverlays = CollectionViewSource.GetDefaultView(Overlays);
        VisibleOverlays.Filter = FilterOverlay;

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

        // Overlay Commands
        InstallOverlayCommand = new DelegateCommand<OverlayCardViewModel>(async o => await InstallOverlayAsync(o));
        UpdateOverlayCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UpdateOverlayAsync(o));
        UninstallOverlayCommand = new DelegateCommand<OverlayCardViewModel>(async o => await UninstallOverlayAsync(o));

        RefreshCommand = new DelegateCommand(async () => await LoadAllCatalogsAsync(forceRefresh: true));
        CloseCommand = new DelegateCommand(ExecuteClose);
    }

    #region Tab & Navigation Properties

    public int SelectedTabIndex
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = 0; // 0 = Themes, 1 = Overlays

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

    #region Overlays Collections & Commands

    public ObservableCollection<OverlayCardViewModel> Overlays { get; } = [];
    public ICollectionView VisibleOverlays { get; }

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
        }

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

            // Load Overlays
            await LoadOverlaysAsync();
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
        Themes.Clear();

        // 1. Installed themes (Built-in + Local Custom)
        var installedThemes = _themeStorageService.GetInstalledThemes();
        var installedDict = installedThemes.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        // 2. Remote catalog themes
        ThemeCatalog remoteCatalog;
        try
        {
            remoteCatalog = await _themeRepositoryService.FetchCatalogAsync();
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to fetch remote theme catalog; falling back to installed themes only.");
            remoteCatalog = new ThemeCatalog();
        }

        var activeId = MainWindowViewModel.ActiveThemeId;

        // Process remote catalog entries
        var processedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in remoteCatalog.Themes)
        {
            processedIds.Add(entry.Id);
            var isInstalled = installedDict.TryGetValue(entry.Id, out var localManifest);
            var isUpdate = _themeRepositoryService.IsUpdateAvailable(entry.Id);
            var card = new ThemeCardViewModel(entry, isInstalled, isUpdate, localManifest?.Version)
            {
                IsActive = string.Equals(activeId, entry.Id, StringComparison.OrdinalIgnoreCase)
            };
            Themes.Add(card);
        }

        // Add remaining local themes not in remote catalog
        foreach (var local in installedThemes)
        {
            if (!processedIds.Contains(local.Id))
            {
                var card = new ThemeCardViewModel(local, isInstalled: true)
                {
                    IsActive = string.Equals(activeId, local.Id, StringComparison.OrdinalIgnoreCase)
                };
                Themes.Add(card);
            }
        }

        VisibleThemes.Refresh();
    }

    private async Task LoadOverlaysAsync()
    {
        Overlays.Clear();
        try
        {
            var catalog = await _overlayRepositoryService.FetchCatalogAsync();
            foreach (var entry in catalog.Overlays)
            {
                var isInstalled = _overlayRepositoryService.IsOverlayInstalled(entry.Id);
                var isUpdate = _overlayRepositoryService.IsUpdateAvailable(entry.Id);
                var installedVer = _overlayRepositoryService.GetInstalledVersion(entry.Id);

                var card = new OverlayCardViewModel(entry, installedVer)
                {
                    IsInstalled = isInstalled,
                    IsUpdateAvailable = isUpdate
                };
                Overlays.Add(card);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to load overlay catalog");
        }

        VisibleOverlays.Refresh();
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
        }
    }

    private async Task ExecuteExportThemeAsync(ThemeCardViewModel? card)
    {
        if (card == null) return;

        var saveDialog = new SaveFileDialog
        {
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

    #region Overlay Operations

    private async Task InstallOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null) return;
        card.IsLoading = true;
        try
        {
            await _overlayRepositoryService.InstallOverlayAsync(card.CatalogEntry);
            card.IsInstalled = true;
            card.IsUpdateAvailable = false;
            card.OperationMessage = "Installed";
        }
        catch (Exception ex)
        {
            card.OperationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private async Task UpdateOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null) return;
        card.IsLoading = true;
        try
        {
            await _overlayRepositoryService.UpdateOverlayAsync(card.Id);
            card.IsUpdateAvailable = false;
            card.OperationMessage = "Updated";
        }
        catch (Exception ex)
        {
            card.OperationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    private async Task UninstallOverlayAsync(OverlayCardViewModel? card)
    {
        if (card == null) return;
        card.IsLoading = true;
        try
        {
            await _overlayRepositoryService.UninstallOverlayAsync(card.Id);
            card.IsInstalled = false;
            card.OperationMessage = "Removed";
        }
        catch (Exception ex)
        {
            card.OperationMessage = $"Error: {ex.Message}";
        }
        finally
        {
            card.IsLoading = false;
        }
    }

    #endregion

    #region Filtering

    private void ApplyFilter()
    {
        VisibleThemes.Refresh();
        VisibleOverlays.Refresh();
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

    private bool FilterOverlay(object item)
    {
        if (item is not OverlayCardViewModel card) return false;
        if (string.IsNullOrWhiteSpace(SearchQuery)) return true;

        var q = SearchQuery.Trim();
        return card.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
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
