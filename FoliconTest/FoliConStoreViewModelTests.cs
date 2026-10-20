#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Data;
using FoliCon.Models.Themes;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Themes;
using FoliCon.ViewModels;
using Prism.Dialogs;
using Xunit;

namespace FoliconTest;

public class FoliConStoreViewModelTests : IDisposable
{
    private readonly string _testDir;
    private readonly FakeThemeStorage _fakeStorage;
    private readonly FakeThemeRepo _fakeThemeRepo;
    private readonly FakeOverlayRepo _fakeOverlayRepo;
    private readonly HybridThemeParser _themeParser;

    public FoliConStoreViewModelTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "FoliCon_StoreVMTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);

        _fakeStorage = new FakeThemeStorage(_testDir);
        _fakeThemeRepo = new FakeThemeRepo();
        _fakeOverlayRepo = new FakeOverlayRepo();
        _themeParser = new HybridThemeParser();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void ThemeCardViewModel_ConstructsFromManifest_ExtractsSwatches()
    {
        var manifest = new ThemeManifest
        {
            Id = "nordic-frost",
            Name = "Nordic Frost",
            Author = "ArcticDev",
            Version = "1.0.0",
            BaseTheme = "Dark",
            Colors = new ThemeColorPalette
            {
                Background = "#1E222A",
                CardSurface = "#282C34",
                Primary = "#61AFEF",
                Accent = "#98C379",
                PrimaryText = "#ABB2BF"
            }
        };

        var card = WpfTestHost.Invoke(() => new ThemeCardViewModel(manifest));

        Assert.Equal("nordic-frost", card.Id);
        Assert.Equal("Nordic Frost", card.Name);
        Assert.Equal("ArcticDev", card.Author);
        Assert.NotNull(card.SwatchBackground);
        Assert.NotNull(card.SwatchPrimary);
        Assert.True(card.IsInstalled);
    }

    [Fact]
    public async Task FoliConStoreViewModel_LoadsCatalogs_CombinesInstalledAndRemote()
    {
        _fakeStorage.InstalledThemes.Add(new ThemeManifest
        {
            Id = "local-custom",
            Name = "Local Custom",
            Version = "1.0.0"
        });

        _fakeThemeRepo.Catalog.Themes.Add(new ThemeCatalogEntry
        {
            Id = "cyberpunk-2077",
            Name = "Cyberpunk 2077",
            Version = "1.2.0"
        });

        _fakeOverlayRepo.Catalog.Overlays.Add(new OverlayCatalogEntry
        {
            Id = "liaher",
            DisplayName = "Liaher Style",
            OverlayVersion = "1.0.0"
        });

        var vm = WpfTestHost.Invoke(() => new FoliConStoreViewModel(
            default,
            _fakeOverlayRepo,
            _fakeThemeRepo,
            _fakeStorage,
            _themeParser,
            null!));

        await WpfTestHost.Invoke(() => vm.LoadAllCatalogsAsync());

        Assert.Equal(2, vm.Themes.Count); // 1 local + 1 remote
        Assert.Single(vm.Overlays);
        Assert.Equal("cyberpunk-2077", vm.Themes[0].Id);
        Assert.Equal("local-custom", vm.Themes[1].Id);
    }

    [Fact]
    public void LivePreview_RollsBackOnDialogClosed_WhenNotApplied()
    {
        var vm = WpfTestHost.Invoke(() => new FoliConStoreViewModel(
            default,
            _fakeOverlayRepo,
            _fakeThemeRepo,
            _fakeStorage,
            _themeParser,
            null!));

        var previewCard = WpfTestHost.Invoke(() => new ThemeCardViewModel(new ThemeManifest
        {
            Id = "preview-theme",
            Name = "Preview Theme",
            Colors = new ThemeColorPalette { Background = "#FF0000", CardSurface = "#00FF00", Primary = "#0000FF" }
        }));

        WpfTestHost.Invoke(() =>
        {
            vm.PreviewThemeCommand.Execute(previewCard);
            Assert.True(vm.IsPreviewing);
            Assert.Equal("Preview Theme", vm.PreviewingThemeName);

            // Simulate closing the store window without applying
            vm.OnDialogClosed();

            Assert.False(vm.IsPreviewing);
        });
    }

    [Fact]
    public void SelectedTheme_TriggersLivePreview_OnNavigation()
    {
        var vm = WpfTestHost.Invoke(() => new FoliConStoreViewModel(
            default,
            _fakeOverlayRepo,
            _fakeThemeRepo,
            _fakeStorage,
            _themeParser,
            null!));

        var card = WpfTestHost.Invoke(() => new ThemeCardViewModel(new ThemeManifest
        {
            Id = "nav-theme",
            Name = "Nav Theme",
            Colors = new ThemeColorPalette { Background = "#123456", CardSurface = "#654321", Primary = "#AABBCC" }
        }));

        WpfTestHost.Invoke(() =>
        {
            // Navigating via arrow keys or clicking sets SelectedTheme
            vm.SelectedTheme = card;
            Assert.True(vm.IsPreviewing);
            Assert.Equal("Nav Theme", vm.PreviewingThemeName);

            // Applying locks it in
            vm.ApplyThemeCommand.Execute(card);
            Assert.False(vm.IsPreviewing);

            // Dialog closed after applying does not rollback
            vm.OnDialogClosed();
            Assert.False(vm.IsPreviewing);
        });
    }

    private sealed class FakeThemeStorage(string dir) : IThemeStorageService
    {
        public string ThemesDirectory => dir;
        public List<ThemeManifest> InstalledThemes { get; } = [];

        public IReadOnlyList<ThemeManifest> GetInstalledThemes() => InstalledThemes;
        public IReadOnlyList<ThemeManifest> GetUserThemes() => InstalledThemes;
        public ThemeManifest? GetThemeById(string themeId) => InstalledThemes.Find(t => t.Id == themeId);
        public bool IsThemeInstalled(string themeId) => InstalledThemes.Exists(t => t.Id == themeId);
        public string GetThemeFolderPath(string themeId) => Path.Combine(dir, themeId);
        public Task<ThemeManifest> InstallThemeFromJsonAsync(string json, byte[]? previewImageBytes = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<string> ExportThemePackageAsync(string themeId, string destinationZipPath, CancellationToken ct = default) => Task.FromResult(destinationZipPath);
        public bool DeleteTheme(string themeId) => InstalledThemes.RemoveAll(t => t.Id == themeId) > 0;
        public void Refresh() { }
    }

    private sealed class FakeThemeRepo : IThemeRepositoryService
    {
        public string BaseUrl => "https://fake.url";
        public ThemeCatalog Catalog { get; } = new();

        public Task<ThemeCatalog> FetchCatalogAsync(CancellationToken ct = default) => Task.FromResult(Catalog);
        public Task<ThemeManifest> FetchManifestAsync(string themeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ThemeManifest> InstallThemeAsync(ThemeCatalogEntry entry, IProgress<(int Percent, string Status)>? progress = null, CancellationToken ct = default)
            => Task.FromResult(new ThemeManifest { Id = entry.Id, Name = entry.Name, Version = entry.Version });
        public Task<ThemeManifest> UpdateThemeAsync(string themeId, IProgress<(int Percent, string Status)>? progress = null, CancellationToken ct = default)
            => Task.FromResult(new ThemeManifest { Id = themeId, Version = "2.0.0" });
        public bool IsUpdateAvailable(string themeId) => false;
        public string? GetAvailableVersion(string themeId) => null;
        public void InvalidateCache() { }
    }

    private sealed class FakeOverlayRepo : IOverlayRepositoryService
    {
        public string BaseUrl => "https://fake.url";
        public OverlayCatalog Catalog { get; } = new();

        public Task<OverlayCatalog> FetchCatalogAsync() => Task.FromResult(Catalog);
        public Task<OverlayCatalog> FetchCatalogAsync(CancellationToken ct) => Task.FromResult(Catalog);
        public Task<OverlayManifest> FetchManifestAsync(string overlayId) => throw new NotImplementedException();
        public Task<OverlayManifest> FetchManifestAsync(string overlayId, CancellationToken ct) => throw new NotImplementedException();
        public Task InstallOverlayAsync(OverlayCatalogEntry entry) => Task.CompletedTask;
        public Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress) => Task.CompletedTask;
        public Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateOverlayAsync(string overlayId) => Task.CompletedTask;
        public Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress) => Task.CompletedTask;
        public Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress, CancellationToken ct) => Task.CompletedTask;
        public Task UninstallOverlayAsync(string overlayId) => Task.CompletedTask;
        public bool IsOverlayInstalled(string overlayId) => false;
        public bool IsUpdateAvailable(string overlayId) => false;
        public string? GetInstalledVersion(string overlayId) => null;
        public void InvalidateCache() { }
        public void MarkUpdateAvailable(string overlayId, string availableVersion) { }
        public void SyncAvailableUpdates(OverlayCatalog catalog) { }
    }
}
