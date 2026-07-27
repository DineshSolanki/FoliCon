#nullable enable
using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayTemplateProvider"/> — the "New from template" creation path.
/// </summary>
public class OverlayTemplateProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly WpfTestHost _host = new();

    public OverlayTemplateProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconTemplateTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _host.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    #region Template listing

    [Fact]
    public void GetTemplates_ReturnsEveryAvailableOverlay()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider(
            MakeDefinition("liaher", builtIn: true),
            MakeDefinition("community-one", builtIn: false)));

        var templates = provider.GetTemplates();

        Assert.Equal(2, templates.Count);
        Assert.Contains(templates, t => t.Id == "liaher");
        Assert.Contains(templates, t => t.Id == "community-one");
    }

    [Fact]
    public void GetTemplates_SuppliesFallbackDescription()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider(
            MakeDefinition("liaher", builtIn: true)));

        Assert.Equal("Built-in overlay", provider.GetTemplates()[0].Description);
    }

    #endregion

    #region ID availability

    [Theory]
    [InlineData("legacy")]
    [InlineData("alternate")]
    [InlineData("liaher")]
    [InlineData("faelpessoal")]
    [InlineData("faelpessoal-horizontal")]
    [InlineData("windows11")]
    public void IsIdAvailable_RejectsBuiltInIds(string reservedId)
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());

        Assert.False(provider.IsIdAvailable(reservedId));
    }

    [Fact]
    public void IsIdAvailable_RejectsInstalledIds()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider(MakeDefinition("taken", builtIn: false)));

        Assert.False(provider.IsIdAvailable("taken"));
        Assert.True(provider.IsIdAvailable("free"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsIdAvailable_RejectsBlankIds(string id)
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());

        Assert.False(provider.IsIdAvailable(id));
    }

    #endregion

    #region ID suggestion

    [Theory]
    [InlineData("My Cool Overlay", "my-cool-overlay")]
    [InlineData("Retro VHS!", "retro-vhs")]
    [InlineData("  Spaced  Out  ", "spaced-out")]
    [InlineData("Ünïcödé Frame", "n-c-d-frame")]
    public void SuggestId_SlugifiesDisplayName(string displayName, string expected)
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());

        Assert.Equal(expected, provider.SuggestId(displayName));
    }

    [Fact]
    public void SuggestId_OnCollision_AppendsNumericSuffix()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider(MakeDefinition("my-overlay", builtIn: false)));

        Assert.Equal("my-overlay-2", provider.SuggestId("My Overlay"));
    }

    [Fact]
    public void SuggestId_EmptyName_FallsBackToDefault()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());

        Assert.Equal("my-overlay", provider.SuggestId(""));
    }

    #endregion

    #region Cloning

    [Fact]
    public void CreateFromTemplate_AppliesFreshIdentity()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var template = MakeTemplate("liaher", MakeDefinition("liaher", builtIn: true));

        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(template, _tempDir, "my-clone", "My Clone", "Me"));

        Assert.Equal("my-clone", document.Id);
        Assert.Equal("My Clone", document.DisplayName);
        Assert.Equal("Me", document.Author);
        Assert.Equal("1.0.0", document.OverlayVersion);
    }

    [Fact]
    public void CreateFromTemplate_ClearsTemplateSpecificMetadata()
    {
        // The clone is a different overlay; carrying over the template's description
        // and tags would mislabel it in the store.
        var source = MakeDefinition("liaher", builtIn: true);
        source.Description = "Liaher style overlay";
        source.Tags = ["rounded", "classic"];

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("liaher", source), _tempDir, "clone", "Clone", "Me"));

        Assert.Empty(document.Description);
        Assert.Empty(document.Tags);
    }

    [Fact]
    public void CreateFromTemplate_PreservesGeometry()
    {
        var source = MakeDefinition("liaher", builtIn: true);
        source.Poster.Margin = "34,10,43,21";
        source.RootMargin = "0,0,0,-11";
        source.DesignWidth = 265;

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("liaher", source), _tempDir, "clone", "Clone", "Me"));

        var snapshot = document.CreateSnapshot();
        Assert.Equal("34,10,43,21", snapshot.Poster.Margin);
        Assert.Equal("0,0,0,-11", snapshot.RootMargin);
        Assert.Equal(265, snapshot.DesignWidth);
    }

    [Fact]
    public void CreateFromTemplate_ExtractsEmbeddedAssetsAndRewritesPaths()
    {
        // Built-in overlays reference compiled pack resources that have no file on disk.
        // Without extraction the clone would export with unresolvable assets.
        var source = MakeDefinition("liaher", builtIn: true);
        source.BaseLayer = new LayerDefinition
        {
            ImagePath = "/Resources/poster_mockups/liaher/mockup liaher base.png",
            Margin = "-8,0,0,10"
        };

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("liaher", source), _tempDir, "clone", "Clone", "Me"));

        Assert.Equal("base.png", document.BaseLayerImagePath);

        var extracted = Path.Combine(_tempDir, "base.png");
        Assert.True(File.Exists(extracted));
        Assert.True(new FileInfo(extracted).Length > 0);
    }

    [Fact]
    public void CreateFromTemplate_ExtractsOpacityMask()
    {
        var source = MakeDefinition("windows11", builtIn: true);
        source.Poster.OpacityMaskPath = "/Resources/poster_mockups/win11/front.png";

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("windows11", source), _tempDir, "clone", "Clone", "Me"));

        Assert.Equal("mask.png", document.PosterOpacityMaskPath);
        Assert.True(File.Exists(Path.Combine(_tempDir, "mask.png")));
    }

    [Fact]
    public void CreateFromTemplate_CopiesLooseAssetsFromCommunityTemplate()
    {
        var sourceFolder = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(sourceFolder);
        File.WriteAllBytes(Path.Combine(sourceFolder, "base.png"), [1, 2, 3, 4]);

        var source = MakeDefinition("community", builtIn: false);
        source.OverlayFolderPath = sourceFolder;
        source.BaseLayer = new LayerDefinition { ImagePath = "base.png", Margin = "0,0,0,0" };

        var destination = Path.Combine(_tempDir, "clone");
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("community", source), destination, "clone", "Clone", "Me"));

        Assert.Equal("base.png", document.BaseLayerImagePath);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, File.ReadAllBytes(Path.Combine(destination, "base.png")));
    }

    [Fact]
    public void CreateFromTemplate_DistinctLayers_GetDistinctFiles()
    {
        var source = MakeDefinition("liaher", builtIn: true);
        source.BaseLayer = new LayerDefinition
        {
            ImagePath = "/Resources/poster_mockups/liaher/mockup liaher base.png", Margin = "0,0,0,0"
        };
        source.FrontLayer = new LayerDefinition
        {
            ImagePath = "/Resources/poster_mockups/liaher/mockup liaher front.png", Margin = "0,0,0,0"
        };

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() =>
            provider.CreateFromTemplate(MakeTemplate("liaher", source), _tempDir, "clone", "Clone", "Me"));

        Assert.NotEqual(document.BaseLayerImagePath, document.FrontLayerImagePath);
        Assert.True(File.Exists(Path.Combine(_tempDir, document.BaseLayerImagePath)));
        Assert.True(File.Exists(Path.Combine(_tempDir, document.FrontLayerImagePath)));
    }

    [Fact]
    public void CreateFromTemplate_CreatesDestinationFolder()
    {
        var destination = Path.Combine(_tempDir, "nested", "new-overlay");
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());

        WpfTestHost.Invoke(() => provider.CreateFromTemplate(
            MakeTemplate("liaher", MakeDefinition("liaher", builtIn: true)),
            destination, "clone", "Clone", "Me"));

        Assert.True(Directory.Exists(destination));
    }

    [Fact]
    public void CreateFromTemplate_CollidingId_Throws()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider(MakeDefinition("taken", builtIn: false)));
        var template = MakeTemplate("liaher", MakeDefinition("liaher", builtIn: true));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.CreateFromTemplate(template, _tempDir, "taken", "Taken", "Me"));

        Assert.Contains("already in use", ex.Message);
    }

    [Fact]
    public void CreateFromTemplate_ReservedBuiltInId_Throws()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var template = MakeTemplate("liaher", MakeDefinition("liaher", builtIn: true));

        Assert.Throws<InvalidOperationException>(() =>
            provider.CreateFromTemplate(template, _tempDir, "liaher", "Liaher", "Me"));
    }

    [Fact]
    public void CreateFromTemplate_DoesNotMutateTheTemplate()
    {
        var source = MakeDefinition("liaher", builtIn: true);
        source.Description = "Original";
        var template = MakeTemplate("liaher", source);

        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        WpfTestHost.Invoke(() => provider.CreateFromTemplate(template, _tempDir, "clone", "Clone", "Me"));

        Assert.Equal("liaher", source.Id);
        Assert.Equal("Original", source.Description);
        Assert.True(source.IsBuiltIn);
    }

    [Fact]
    public void CreateFromTemplate_CloneIsNotBuiltIn()
    {
        var provider = new OverlayTemplateProvider(new StubOverlayProvider());
        var document = WpfTestHost.Invoke(() => provider.CreateFromTemplate(
            MakeTemplate("liaher", MakeDefinition("liaher", builtIn: true)),
            _tempDir, "clone", "Clone", "Me"));

        Assert.False(document.CreateSnapshot().IsBuiltIn);
    }

    #endregion

    private static OverlayTemplate MakeTemplate(string id, PosterOverlayDefinition definition) =>
        new(id, definition.DisplayName, definition.Description, definition);

    private static PosterOverlayDefinition MakeDefinition(string id, bool builtIn) => new()
    {
        Id = id,
        DisplayName = id,
        Author = "FoliCon",
        OverlayVersion = "1.0.0",
        IsBuiltIn = builtIn,
        Poster = new PosterConfig { Margin = "10,10,10,10", ClipRadius = "0" },
        Rating = new RatingConfig(),
        Title = new TitleConfig()
    };

    /// <summary>
    /// Minimal in-memory <see cref="IOverlayProvider"/> so template tests never touch
    /// the user's real %AppData% overlay folder.
    /// </summary>
    private sealed class StubOverlayProvider(params PosterOverlayDefinition[] overlays) : IOverlayProvider
    {
        private readonly List<PosterOverlayDefinition> _overlays = [.. overlays];

        public IReadOnlyList<PosterOverlayDefinition> GetAllOverlays() => _overlays;

        public IReadOnlyList<PosterOverlayDefinition> GetUserOverlays() =>
            _overlays.Where(o => !o.IsBuiltIn).ToList();

        public PosterOverlayDefinition? GetOverlayById(string id) =>
            _overlays.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        public PosterOverlayDefinition ResolveActiveOverlayOrDefault(string? activeOverlayId) =>
            GetOverlayById(activeOverlayId ?? "") ?? _overlays[0];

        public bool IsOverlayInstalled(string id) => GetOverlayById(id) != null;

        public string GetOverlayFolderPath(string id) => Path.Combine(Path.GetTempPath(), id);

        public void Refresh() { /* No-op: stub for interface; templates are static in tests */ }
    }
}
