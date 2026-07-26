#nullable enable
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using FoliCon.Modules.Overlays;
using FoliCon.ViewModels;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="OverlayCardViewModel"/> and <see cref="OverlayStoreViewModel"/>.
/// Uses stub implementations of IOverlayRepositoryService and IOverlayProvider.
/// </summary>
public class OverlayStoreViewModelTests
{
    [Fact]
    public void CardViewModel_MapsFieldsFromCatalogEntry()
    {
        var entry = CreateEntry("neon-glow", "Neon Glow", "Author", "2.0.0", ["neon"], 150000);
        var card = new OverlayCardViewModel(entry);

        Assert.Equal("neon-glow", card.Id);
        Assert.Equal("Neon Glow", card.DisplayName);
        Assert.Equal("Author", card.Author);
        Assert.Equal("2.0.0", card.OverlayVersion);
        Assert.Single(card.Tags);
        Assert.Equal("neon", card.Tags[0]);
        Assert.Equal(150000, card.SizeBytes);
        Assert.False(card.IsInstalled);
        Assert.False(card.IsUpdateAvailable);
        Assert.False(card.IsLoading);
        Assert.Null(card.PreviewImage);
    }

    [Fact]
    public void CardViewModel_SizeDisplay_FormatsCorrectly()
    {
        var small = new OverlayCardViewModel(CreateEntry("a", "A", "A", "1.0.0", [], 500));
        Assert.Equal("500 B", small.SizeDisplay);

        var medium = new OverlayCardViewModel(CreateEntry("b", "B", "B", "1.0.0", [], 15360));
        Assert.Equal("15.0 KB", medium.SizeDisplay);

        var large = new OverlayCardViewModel(CreateEntry("c", "C", "C", "1.0.0", [], 2097152));
        Assert.Equal("2.0 MB", large.SizeDisplay);
    }

    [Fact]
    public void CardViewModel_VersionDisplay_ShowsVPrefix()
    {
        var card = new OverlayCardViewModel(CreateEntry("x", "X", "X", "3.1.0", [], 1000));
        Assert.Equal("v3.1.0", card.VersionDisplay);
    }

    [Fact]
    public void CardViewModel_PropertyChanges_RaiseNotifications()
    {
        var card = new OverlayCardViewModel(CreateEntry("x", "X", "X", "1.0.0", [], 0));

        var notified = new List<string>();
        card.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        card.IsInstalled = true;
        card.IsUpdateAvailable = true;
        card.IsLoading = true;
        card.ProgressPercentage = 42;
        card.OperationMessage = "Downloading files";

        Assert.Contains("IsInstalled", notified);
        Assert.Contains("IsUpdateAvailable", notified);
        Assert.Contains("IsLoading", notified);
        Assert.Contains("ProgressPercentage", notified);
        Assert.Contains("OperationMessage", notified);
    }

    [Fact]
    public void CardViewModel_InstallAndUninstall_TogglesState()
    {
        var card = new OverlayCardViewModel(CreateEntry("x", "X", "X", "1.0.0", [], 0));

        Assert.False(card.IsInstalled);
        card.IsInstalled = true;
        Assert.True(card.IsInstalled);
        card.IsInstalled = false;
        Assert.False(card.IsInstalled);
    }

    [Fact]
    public async Task StoreViewModel_FiltersWithoutRecreatingCards_AndKeepsSearchAcrossViews()
    {
        var entries = new List<OverlayCatalogEntry>
        {
            CreateEntry("neon-glow", "Neon Glow", "Alice", "1.0.0", ["neon"], 1000),
            CreateEntry("dvd-case", "DVD Case", "Bob", "1.0.0", ["dvd", "classic"], 2000),
            CreateEntry("retro-wave", "Retro Wave", "Alice", "1.0.0", ["retro", "neon"], 3000),
        };
        var service = new StubRepositoryService(catalogEntries: entries);
        service.MarkInstalled("neon-glow");
        service.MarkUpdateAvailable("neon-glow", "2.0.0");

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(service));
        await vm.CatalogLoaded;

        var originalCard = host.Invoke(() => vm.Overlays.Single(o => o.Id == "neon-glow"));
        host.Invoke(() => vm.SearchQuery = "Alice");

        var discoverResults = host.Invoke(() => vm.VisibleOverlays.Cast<OverlayCardViewModel>().ToList());
        Assert.Equal(2, discoverResults.Count);
        Assert.Contains(originalCard, discoverResults);

        host.Invoke(() => vm.CurrentSection = OverlayStoreSection.Updates);
        var updateResults = host.Invoke(() => vm.VisibleOverlays.Cast<OverlayCardViewModel>().ToList());
        Assert.Single(updateResults);
        Assert.Same(originalCard, updateResults[0]);
        Assert.Equal("Alice", vm.SearchQuery);
    }

    /// <summary>
    /// Filtering must run on the enum, never on the dropdown label. It previously compared
    /// against the English text, so translating the dropdown matched nothing.
    /// </summary>
    [Theory]
    [InlineData(OverlayStatusFilter.All, 3)]
    [InlineData(OverlayStatusFilter.Installed, 1)]
    [InlineData(OverlayStatusFilter.NotInstalled, 2)]
    [InlineData(OverlayStatusFilter.UpdateAvailable, 1)]
    public async Task StoreViewModel_StatusFilter_SelectsByValue(OverlayStatusFilter filter, int expected)
    {
        using var host = new WpfTestHost();
        var vm = await CreateFilterFixture(host);

        host.Invoke(() => vm.SelectedStatusFilter = filter);

        var results = host.Invoke(() => vm.VisibleOverlays.Cast<OverlayCardViewModel>().ToList());
        Assert.Equal(expected, results.Count);
    }

    /// <summary>
    /// The regression guard for the localization fix: translating every label must leave
    /// filtering untouched, because the labels no longer participate in it.
    /// </summary>
    [Fact]
    public async Task StoreViewModel_StatusFilter_SurvivesTranslatedLabels()
    {
        using var host = new WpfTestHost();
        var vm = await CreateFilterFixture(host);

        host.Invoke(() =>
        {
            // Stand in for a language switch: same values, entirely different display text.
            var translated = vm.AvailableStatusFilters
                .Select(o => new OverlayStatusFilterOption(o.Value, $"translated-{(int)o.Value}"))
                .ToList();
            vm.AvailableStatusFilters.Clear();
            foreach (var option in translated)
            {
                vm.AvailableStatusFilters.Add(option);
            }

            vm.SelectedStatusFilter = OverlayStatusFilter.Installed;
        });

        var results = host.Invoke(() => vm.VisibleOverlays.Cast<OverlayCardViewModel>().ToList());
        Assert.Single(results);
        Assert.Equal("neon-glow", results[0].Id);
    }

    private static async Task<OverlayStoreViewModel> CreateFilterFixture(WpfTestHost host)
    {
        var entries = new List<OverlayCatalogEntry>
        {
            CreateEntry("neon-glow", "Neon Glow", "Alice", "1.0.0", ["neon"], 1000),
            CreateEntry("dvd-case", "DVD Case", "Bob", "1.0.0", ["dvd"], 2000),
            CreateEntry("retro-wave", "Retro Wave", "Alice", "1.0.0", ["retro"], 3000),
        };
        var service = new StubRepositoryService(catalogEntries: entries);
        service.MarkInstalled("neon-glow");
        service.MarkUpdateAvailable("neon-glow", "2.0.0");

        var vm = host.Invoke(() => new OverlayStoreViewModel(service));
        await vm.CatalogLoaded;
        return vm;
    }

    [Fact]
    public async Task StoreViewModel_ReportsInstalledAndUpdateCounts()
    {
        var service = new StubRepositoryService([
            CreateEntry("one", "One", "A", "2.0.0", [], 100),
            CreateEntry("two", "Two", "B", "1.0.0", [], 100),
            CreateEntry("three", "Three", "C", "1.0.0", [], 100)
        ]);
        service.MarkInstalled("one");
        service.MarkInstalled("two");
        service.MarkUpdateAvailable("one", "2.0.0");

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(service));
        await vm.CatalogLoaded;

        Assert.Equal(2, vm.InstalledCount);
        Assert.Equal(1, vm.UpdatesCount);
        Assert.Equal("Installed (2)", vm.InstalledSectionTitle);
        Assert.Equal("Updates (1)", vm.UpdatesSectionTitle);
    }

    [Fact]
    public async Task StoreViewModel_UpdateAll_UpdatesEveryAvailableOverlay()
    {
        var service = new StubRepositoryService([
            CreateEntry("one", "One", "A", "2.0.0", [], 100),
            CreateEntry("two", "Two", "B", "2.0.0", [], 100)
        ]);
        service.MarkInstalled("one");
        service.MarkInstalled("two");
        service.MarkUpdateAvailable("one", "2.0.0");
        service.MarkUpdateAvailable("two", "2.0.0");

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(service));
        await vm.CatalogLoaded;

        host.Invoke(() => vm.UpdateAllCommand.Execute());
        await WaitUntilAsync(() => service.UpdatedIds.Count == 2 && !vm.IsUpdatingAll);

        Assert.Equal(2, service.UpdatedIds.Count);
        Assert.Equal(0, vm.UpdatesCount);
        Assert.All(vm.Overlays, card => Assert.False(card.IsUpdateAvailable));
    }

    [Fact]
    public async Task StoreViewModel_RemoveRequiresConfirmation()
    {
        var service = new StubRepositoryService([CreateEntry("one", "One", "A", "1.0.0", [], 100)]);
        service.MarkInstalled("one");

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => OverlayStoreViewModel.Create(service, _ => false));
        await vm.CatalogLoaded;
        var card = vm.Overlays.Single();

        host.Invoke(() => vm.UninstallCommand.Execute(card));
        await Task.Delay(100);

        Assert.True(card.IsInstalled);
        Assert.Empty(service.RemovedIds);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }

    #region Tag filtering

    [Fact]
    public async Task TagFilters_AreBuiltFromTheCatalogWithCounts()
    {
        var vm = await LoadTagFixtureAsync();

        var dvd = vm.TagFilters.Single(t => t.Tag == "dvd");
        Assert.Equal(2, dvd.Count);
        Assert.Equal("dvd (2)", dvd.DisplayText);
    }

    [Fact]
    public async Task TagFilters_AreOrderedByPopularity()
    {
        // The most useful filters should be reachable first.
        var vm = await LoadTagFixtureAsync();

        Assert.Equal("dvd", vm.TagFilters[0].Tag);
    }

    [Fact]
    public async Task TagFilters_ContainNoBlankEntry()
    {
        // The old ComboBox needed an empty "All" item; chips express that by selecting none.
        var vm = await LoadTagFixtureAsync();

        Assert.DoesNotContain(vm.TagFilters, t => string.IsNullOrWhiteSpace(t.Tag));
    }

    [Fact]
    public async Task NoTagsSelected_ShowsEverything()
    {
        var vm = await LoadTagFixtureAsync();

        Assert.False(vm.HasSelectedTags);
        Assert.Equal(3, Visible(vm).Count);
    }

    [Fact]
    public async Task SelectingOneTag_FiltersToThatTag()
    {
        var vm = await LoadTagFixtureAsync();

        Select(vm, "modern");

        var visible = Visible(vm);
        Assert.Single(visible);
        Assert.Equal("modern-only", visible[0].Id);
    }

    [Fact]
    public async Task SelectingTwoTags_NarrowsToOverlaysCarryingBoth()
    {
        // This is the capability a single-select dropdown could not offer at all.
        var vm = await LoadTagFixtureAsync();

        Select(vm, "dvd");
        Select(vm, "classic");

        var visible = Visible(vm);
        Assert.Single(visible);
        Assert.Equal("dvd-classic", visible[0].Id);
    }

    [Fact]
    public async Task SelectingMutuallyExclusiveTags_YieldsNothing()
    {
        var vm = await LoadTagFixtureAsync();

        Select(vm, "modern");
        Select(vm, "classic");

        Assert.Empty(Visible(vm));
    }

    [Fact]
    public async Task DeselectingATag_WidensTheResultsAgain()
    {
        var vm = await LoadTagFixtureAsync();
        Select(vm, "dvd");
        Select(vm, "classic");

        Select(vm, "classic", selected: false);

        Assert.Equal(2, Visible(vm).Count);
    }

    [Fact]
    public async Task TagMatchingIsCaseInsensitive()
    {
        var vm = await LoadTagFixtureAsync(("upper", "Upper", ["DVD"]));

        Select(vm, "dvd");

        Assert.Contains(Visible(vm), c => c.Id == "upper");
    }

    [Fact]
    public async Task ClearTagFilters_DeselectsEverything()
    {
        var vm = await LoadTagFixtureAsync();
        Select(vm, "dvd");
        Select(vm, "classic");

        Assert.True(vm.ClearTagFiltersCommand.CanExecute());
        vm.ClearTagFiltersCommand.Execute();

        Assert.False(vm.HasSelectedTags);
        Assert.Equal(3, Visible(vm).Count);
    }

    [Fact]
    public async Task ClearTagFilters_IsDisabledWhenNothingIsSelected()
    {
        var vm = await LoadTagFixtureAsync();

        Assert.False(vm.ClearTagFiltersCommand.CanExecute());
    }

    [Fact]
    public async Task Refresh_PreservesTheActiveTagSelection()
    {
        // Losing the filter on refresh would silently change what the author is looking at.
        var vm = await LoadTagFixtureAsync();
        Select(vm, "dvd");

        vm.RefreshCommand.Execute();
        await WaitUntilAsync(() => vm.TagFilters.Count > 0 && !vm.IsLoading);

        Assert.Contains("dvd", vm.SelectedTags);
        Assert.True(vm.TagFilters.Single(t => t.Tag == "dvd").IsSelected);
    }

    [Fact]
    public async Task TagFilterCombinesWithSearch()
    {
        var vm = await LoadTagFixtureAsync();

        Select(vm, "dvd");
        vm.SearchQuery = "Classic";

        var visible = Visible(vm);
        Assert.Single(visible);
        Assert.Equal("dvd-classic", visible[0].Id);
    }

    private static List<OverlayCardViewModel> Visible(OverlayStoreViewModel vm) =>
        [.. vm.VisibleOverlays.Cast<OverlayCardViewModel>()];

    private static void Select(OverlayStoreViewModel vm, string tag, bool selected = true) =>
        vm.TagFilters.Single(t => string.Equals(t.Tag, tag, StringComparison.OrdinalIgnoreCase)).IsSelected = selected;

    /// <summary>
    /// Three overlays: one tagged dvd+classic, one dvd only, one modern only.
    /// </summary>
    private static async Task<OverlayStoreViewModel> LoadTagFixtureAsync(
        params (string Id, string Name, string[] Tags)[] extra)
    {
        var entries = new List<OverlayCatalogEntry>
        {
            CreateEntry("dvd-classic", "DVD Classic", "A", "1.0.0", ["dvd", "classic"], 1000),
            CreateEntry("dvd-only", "DVD Only", "B", "1.0.0", ["dvd"], 1000),
            CreateEntry("modern-only", "Modern Only", "C", "1.0.0", ["modern"], 1000)
        };

        entries.AddRange(extra.Select(e => CreateEntry(e.Id, e.Name, "D", "1.0.0", e.Tags, 1000)));

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(new StubRepositoryService(entries)));
        await vm.CatalogLoaded;
        return vm;
    }

    #endregion

    private static OverlayCatalogEntry CreateEntry(string id, string name, string author, string version, string[] tags, long size)
    {
        return new OverlayCatalogEntry
        {
            Id = id,
            DisplayName = name,
            Author = author,
            OverlayVersion = version,
            Tags = tags,
            SizeBytes = size,
            Description = $"Description for {name}",
            PreviewUrl = $"https://example.com/{id}/preview.png",
            OverlayBaseUrl = "https://example.com/overlays",
            OverlayPath = id
        };
    }

    [Fact]
    public void CreateOverlayCommand_IsDisabled_WithoutADialogService()
    {
        // The bare constructor (tests, design-time previews) has no way to launch the
        // designer, so the button must not appear enabled and then do nothing.
        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(new StubRepositoryService()));

        Assert.False(vm.CreateOverlayCommand.CanExecute());
    }

    [Fact]
    public void CreateOverlayCommand_IsEnabled_WhenOpenedThroughTheDialogService()
    {
        using var host = new WpfTestHost();
        var vm = host.Invoke(() => new OverlayStoreViewModel(
            new DialogCloseListener(), new StubRepositoryService(), new RecordingDialogService()));

        Assert.True(vm.CreateOverlayCommand.CanExecute());
    }

    [Fact]
    public void CreateOverlayCommand_OpensTheDesigner()
    {
        using var host = new WpfTestHost();
        var dialogService = new RecordingDialogService();
        var vm = host.Invoke(() => new OverlayStoreViewModel(
            new DialogCloseListener(), new StubRepositoryService(), dialogService));

        host.Invoke(() => vm.CreateOverlayCommand.Execute());

        Assert.Equal("OverlayDesigner", dialogService.LastDialogName);
    }
}

/// <summary>Captures which dialog was requested instead of showing one.</summary>
internal sealed class RecordingDialogService : IDialogService
{
    public string? LastDialogName { get; private set; }

    public void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback)
    {
        LastDialogName = name;
    }
}

/// <summary>
/// Stub IOverlayRepositoryService for testing. Returns configurable data without network calls.
/// </summary>
internal class StubRepositoryService(List<OverlayCatalogEntry>? catalogEntries = null) : IOverlayRepositoryService
{
    private readonly List<OverlayCatalogEntry> _catalogEntries = catalogEntries ?? [];
    private readonly HashSet<string> _installed = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _updates = new(StringComparer.OrdinalIgnoreCase);
    public List<string> UpdatedIds { get; } = [];
    public List<string> RemovedIds { get; } = [];

    public Task<OverlayCatalog> FetchCatalogAsync() => FetchCatalogAsync(default);
    // Virtual so tests can simulate the catalog being unreachable.
    public virtual Task<OverlayCatalog> FetchCatalogAsync(CancellationToken ct) => Task.FromResult(new OverlayCatalog { SchemaVersion = 1, Overlays = _catalogEntries });

    public Task<OverlayManifest> FetchManifestAsync(string overlayId) => FetchManifestAsync(overlayId, default);
    public Task<OverlayManifest> FetchManifestAsync(string overlayId, CancellationToken ct) => Task.FromResult(new OverlayManifest { Id = overlayId });

    public Task InstallOverlayAsync(OverlayCatalogEntry entry) => InstallOverlayAsync(entry, null, default);
    public Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress) => InstallOverlayAsync(entry, progress, default);
    public Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress, CancellationToken ct)
    {
        _installed.Add(entry.Id);
        return Task.CompletedTask;
    }

    public Task UpdateOverlayAsync(string overlayId) => UpdateOverlayAsync(overlayId, null, default);
    public Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress) => UpdateOverlayAsync(overlayId, progress, default);
    public Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress, CancellationToken ct)
    {
        progress?.Report((50, "Downloading"));
        _updates.Remove(overlayId);
        UpdatedIds.Add(overlayId);
        return Task.CompletedTask;
    }

    public Task UninstallOverlayAsync(string overlayId)
    {
        _installed.Remove(overlayId);
        RemovedIds.Add(overlayId);
        return Task.CompletedTask;
    }

    public bool IsOverlayInstalled(string overlayId) => _installed.Contains(overlayId);
    public bool IsUpdateAvailable(string overlayId) => _updates.ContainsKey(overlayId);
    public string? GetInstalledVersion(string overlayId) => _installed.Contains(overlayId) ? "1.0.0" : null;
    public string BaseUrl => "https://example.com/overlays";
    public void InvalidateCache()
    {
        // No-op for testing
    }

    public void MarkInstalled(string id) => _installed.Add(id);
    public void MarkUpdateAvailable(string id, string version) => _updates[id] = version;
}
