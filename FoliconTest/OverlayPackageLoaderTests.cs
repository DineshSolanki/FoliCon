using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays.Designer;
using Newtonsoft.Json;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayPackageLoader"/> — opening a local package for editing
/// without modifying it.
/// </summary>
public class OverlayPackageLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly OverlayPackageLoader _loader = new();

    public OverlayPackageLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconLoaderTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Load_ValidPackage_ReturnsPopulatedDocument()
    {
        var path = WritePackage(CreateValidDefinition());

        var result = _loader.Load(path);

        Assert.True(result.Succeeded);
        Assert.Equal("test-overlay", result.Document.Id);
        Assert.Equal("Test Overlay", result.Document.DisplayName);
        Assert.True(result.Validation.IsValid);
    }

    [Fact]
    public void Load_SetsAssetFolderToThePackageDirectory()
    {
        var path = WritePackage(CreateValidDefinition());

        var result = _loader.Load(path);

        Assert.Equal(Path.GetFullPath(_tempDir), Path.GetFullPath(result.Document!.AssetFolderPath));
    }

    [Fact]
    public void Load_DoesNotModifyTheSourceFolder()
    {
        var path = WritePackage(CreateValidDefinition());
        var before = Directory.GetFiles(_tempDir).OrderBy(f => f).ToArray();
        var contentBefore = File.ReadAllText(path);

        _loader.Load(path);

        Assert.Equal(before, Directory.GetFiles(_tempDir).OrderBy(f => f).ToArray());
        Assert.Equal(contentBefore, File.ReadAllText(path));
    }

    [Fact]
    public void Load_MissingFile_FailsWithReason()
    {
        var result = _loader.Load(Path.Combine(_tempDir, "nope.json"));

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.FailureReason);
    }

    [Fact]
    public void Load_MalformedJson_FailsWithReason()
    {
        var path = Path.Combine(_tempDir, "overlay.json");
        File.WriteAllText(path, "{ this is not json");

        var result = _loader.Load(path);

        Assert.False(result.Succeeded);
        Assert.Contains("not valid JSON", result.FailureReason);
    }

    [Fact]
    public void Load_UnsupportedSchemaVersion_FailsWithUpgradeMessage()
    {
        var definition = CreateValidDefinition();
        definition.SchemaVersion = 99;
        var path = WritePackage(definition);

        var result = _loader.Load(path);

        Assert.False(result.Succeeded);
        Assert.Contains("Update FoliCon", result.FailureReason);
    }

    [Fact]
    public void Load_PackageWithValidationErrors_StillLoadsForRepair()
    {
        // The author opened a broken package precisely to fix it; refusing to load is unhelpful.
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = "missing.png";
        var path = WritePackage(definition, createBaseImage: false);

        var result = _loader.Load(path);

        Assert.True(result.Succeeded);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, e => e.Field == "baseLayer.imagePath");
    }

    [Fact]
    public void Load_WithSiblingManifest_PreservesCreationDate()
    {
        var created = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var path = WritePackage(CreateValidDefinition());
        File.WriteAllText(
            Path.Combine(_tempDir, "manifest.json"),
            JsonConvert.SerializeObject(new OverlayManifest { Id = "test-overlay", CreatedAt = created }));

        var result = _loader.Load(path);

        Assert.Equal(created, result.Document!.CreatedAt);
    }

    [Fact]
    public void Load_WithoutManifest_LeavesCreationDateUnset()
    {
        var path = WritePackage(CreateValidDefinition());

        var result = _loader.Load(path);

        Assert.Null(result.Document!.CreatedAt);
    }

    [Fact]
    public void Load_RoundTripsThroughSnapshotWithoutDrift()
    {
        var original = CreateValidDefinition();
        var path = WritePackage(original);

        var snapshot = _loader.Load(path).Document!.CreateSnapshot();

        Assert.Equal(original.Poster.Margin, snapshot.Poster.Margin);
        Assert.Equal(original.RootMargin, snapshot.RootMargin);
        Assert.Equal(original.BaseLayer!.Margin, snapshot.BaseLayer!.Margin);
        Assert.Equal(original.LayerOrder, snapshot.LayerOrder);
    }

    private string WritePackage(PosterOverlayDefinition definition, bool createBaseImage = true)
    {
        var path = Path.Combine(_tempDir, "overlay.json");
        File.WriteAllText(path, JsonConvert.SerializeObject(definition, Formatting.Indented));

        if (createBaseImage)
        {
            File.WriteAllBytes(Path.Combine(_tempDir, "base.png"), new byte[100]);
        }

        return path;
    }

    private static PosterOverlayDefinition CreateValidDefinition() => new()
    {
        SchemaVersion = 1,
        Id = "test-overlay",
        DisplayName = "Test Overlay",
        Author = "Author",
        OverlayVersion = "1.0.0",
        DesignWidth = 265,
        DesignHeight = 256,
        RootMargin = "0,0,0,-11",
        LayerOrder = ["base", "poster", "rating"],
        BaseLayer = new LayerDefinition { ImagePath = "base.png", Margin = "30,14,48,15" },
        Poster = new PosterConfig { Margin = "31,42,50,19", ClipRadius = "0" },
        Rating = new RatingConfig { ShieldMargin = "160,97,6,5", TextMargin = "189,30,21,24", FontSize = 25 },
        Title = new TitleConfig { IsVisible = false, RotationOrigin = "0.5,0.5" }
    };
}
