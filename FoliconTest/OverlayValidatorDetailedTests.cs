using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Internal;

namespace FoliconTest;

/// <summary>
/// Tests for the structured validation surface added for the designer:
/// field-level identity, warnings, path safety, and layer-order rules.
/// The legacy <see cref="OverlayValidator.Validate"/> contract is covered by
/// <see cref="OverlayValidatorTests"/>.
/// </summary>
public class OverlayValidatorDetailedTests : IDisposable
{
    private readonly string _tempDir;

    public OverlayValidatorDetailedTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconDesignerTest_{Guid.NewGuid():N}");
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

    #region Structured results

    [Fact]
    public void ValidateDetailed_ValidPackage_IsValidWithNoErrors()
    {
        CreateImage("base.png");
        CreateImage("front.png");

        var result = OverlayValidator.ValidateDetailed(_tempDir, CreateValidDefinition());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateDetailed_CarriesTheOffendingFieldName()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Poster.Margin = "not-a-number";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "poster.margin");
    }

    [Fact]
    public void ValidateDetailed_MissingId_ReportsIdField()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Id = "";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "id");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_LegacyContract_ReturnsSameErrorsAsDetailed()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Id = "";

        var legacy = OverlayValidator.Validate(_tempDir, definition);
        var detailed = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Equal(detailed.ErrorCount, legacy.Count);
    }

    [Fact]
    public void Validate_LegacyContract_ExcludesWarnings()
    {
        // Warnings must not break existing provider/repository callers that treat any
        // returned string as a hard failure.
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Author = "";

        var legacy = OverlayValidator.Validate(_tempDir, definition);
        var detailed = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Empty(legacy);
        Assert.NotEmpty(detailed.Warnings);
    }

    #endregion

    #region Warnings

    [Fact]
    public void ValidateDetailed_EmptyAuthor_WarnsWithoutBlocking()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Author = "";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "author");
    }

    [Fact]
    public void ValidateDetailed_ExtremeMargin_WarnsWithoutBlocking()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Poster.Margin = "99999,0,0,0";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Field == "poster.margin");
    }

    [Fact]
    public void ValidateDetailed_NegativeMargins_AreAcceptedSilently()
    {
        // Built-in overlays depend on negative margins; they must not warn.
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.Margin = "-8,0,0,10";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Warnings, w => w.Field == "baseLayer.margin");
    }

    #endregion

    #region Path safety

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("../../etc/passwd.png")]
    [InlineData("sub/../../escape.png")]
    public void ValidateDetailed_PathTraversal_IsRejected(string maliciousPath)
    {
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = maliciousPath;

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "baseLayer.imagePath");
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateDetailed_AbsolutePath_IsRejected()
    {
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = @"C:\Windows\System32\evil.png";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "baseLayer.imagePath");
    }

    [Fact]
    public void ValidateDetailed_BuiltInPackPath_IsAllowed()
    {
        // Built-in overlays reference compiled resources with a leading slash.
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = "/Resources/poster_mockups/liaher/mockup liaher base.png";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.DoesNotContain(result.Errors, e => e.Field == "baseLayer.imagePath");
    }

    [Fact]
    public void ValidateDetailed_SubfolderAsset_IsAllowed()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "images"));
        CreateImage(Path.Combine("images", "base.png"));
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = "images/base.png";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.DoesNotContain(result.Errors, e => e.Field == "baseLayer.imagePath");
    }

    #endregion

    #region Asset format

    [Theory]
    [InlineData("base.jpg")]
    [InlineData("base.gif")]
    [InlineData("base.bmp")]
    [InlineData("base.svg")]
    public void ValidateDetailed_NonPngAsset_IsRejected(string fileName)
    {
        CreateImage(fileName);
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.BaseLayer!.ImagePath = fileName;

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "baseLayer.imagePath" && e.Message.Contains("PNG"));
    }

    [Fact]
    public void ValidateDetailed_OversizedImage_IsRejected()
    {
        CreateImage("base.png", 3 * 1024 * 1024);
        CreateImage("front.png");

        var result = OverlayValidator.ValidateDetailed(_tempDir, CreateValidDefinition());

        Assert.Contains(result.Errors, e => e.Field == "baseLayer.imagePath" && e.Message.Contains("maximum size"));
    }

    #endregion

    #region Layer order

    [Fact]
    public void ValidateDetailed_UnknownLayerKey_IsRejected()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.LayerOrder = ["base", "sparkles", "poster"];

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "layerOrder" && e.Message.Contains("sparkles"));
    }

    [Fact]
    public void ValidateDetailed_DuplicateLayerKey_IsRejected()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.LayerOrder = ["poster", "poster"];

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "layerOrder" && e.Message.Contains("more than once"));
    }

    [Fact]
    public void ValidateDetailed_LayerOrderWithoutPoster_Warns()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.LayerOrder = ["base", "front", "rating"];

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Warnings, w => w.Field == "layerOrder");
    }

    [Fact]
    public void ValidateDetailed_NullLayerOrder_IsAccepted()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.LayerOrder = null;

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.DoesNotContain(result.Issues, i => i.Field == "layerOrder");
    }

    #endregion

    #region Metadata rules

    [Fact]
    public void ValidateDetailed_InvalidVersionString_IsRejected()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.OverlayVersion = "v1-beta";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "overlayVersion");
    }

    [Fact]
    public void ValidateDetailed_NonPositiveFontSize_IsRejected()
    {
        CreateImage("base.png");
        CreateImage("front.png");
        var definition = CreateValidDefinition();
        definition.Rating.FontSize = 0;

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        Assert.Contains(result.Errors, e => e.Field == "rating.fontSize");
    }

    [Fact]
    public void ValidateDetailed_UnsupportedSchema_StopsAtSchemaError()
    {
        var definition = CreateValidDefinition();
        definition.SchemaVersion = 99;
        definition.Id = "";

        var result = OverlayValidator.ValidateDetailed(_tempDir, definition);

        // Bails immediately: the app can't reason about fields it doesn't understand.
        Assert.Single(result.Errors);
        Assert.Equal("schemaVersion", result.Errors.First().Field);
    }

    #endregion

    private void CreateImage(string relativePath, int sizeBytes = 100) =>
        File.WriteAllBytes(Path.Combine(_tempDir, relativePath), new byte[sizeBytes]);

    private static PosterOverlayDefinition CreateValidDefinition() => new()
    {
        Id = "test-overlay",
        DisplayName = "Test Overlay",
        Author = "Test Author",
        Description = "A test overlay",
        OverlayVersion = "1.0.0",
        Tags = ["test"],
        BaseLayer = new LayerDefinition { ImagePath = "base.png", Margin = "0,0,0,0" },
        FrontLayer = new LayerDefinition { ImagePath = "front.png", Margin = "0,0,0,0" },
        Poster = new PosterConfig { Margin = "10,10,10,10", ClipRadius = "0" },
        Rating = new RatingConfig
        {
            ShieldMargin = "100,50,10,10",
            TextMargin = "120,30,10,10",
            FontSize = 20,
            FontFamily = "Arial"
        },
        Title = new TitleConfig
        {
            IsVisible = false,
            Margin = "0,0,0,0",
            RotationOrigin = "0.5,0.5",
            FontFamily = "Arial",
            Foreground = "White"
        }
    };
}
