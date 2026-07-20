using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays.Internal;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="OverlayValidator"/>.
/// </summary>
public class OverlayValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public OverlayValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconTest_{Guid.NewGuid():N}");
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
    public void Validate_ValidDefinition_ReturnsNoErrors()
    {
        // Arrange
        var definition = CreateValidDefinition();
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        // Act
        var errors = OverlayValidator.Validate(_tempDir, definition);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MissingId_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.Id = "";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("id"));
    }

    [Fact]
    public void Validate_MissingDisplayName_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.DisplayName = "";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("displayName"));
    }

    [Fact]
    public void Validate_InvalidIdFormat_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.Id = "Invalid_ID!";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("invalid characters"));
    }

    [Fact]
    public void Validate_ValidIdWithHyphens_ReturnsNoIdError()
    {
        var definition = CreateValidDefinition();
        definition.Id = "my-cool-overlay";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.DoesNotContain(errors, e => e.Contains("id"));
    }

    [Fact]
    public void Validate_SchemaVersionTooHigh_ReturnsErrorImmediately()
    {
        var definition = CreateValidDefinition();
        definition.SchemaVersion = 999;

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Single(errors);
        Assert.Contains("schema v999", errors[0]);
    }

    [Fact]
    public void Validate_MissingBaseLayerImage_ReturnsError()
    {
        var definition = CreateValidDefinition();
        // Don't create base.png
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("baseLayer") && e.Contains("not found"));
    }

    [Fact]
    public void Validate_MissingFrontLayerImage_ReturnsError()
    {
        var definition = CreateValidDefinition();
        CreateDummyImage("base.png");
        // Don't create front.png

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("frontLayer") && e.Contains("not found"));
    }

    [Fact]
    public void Validate_NegativeMargins_AreAllowed()
    {
        var definition = CreateValidDefinition();
        definition.BaseLayer.Margin = "-8,0,0,10";
        definition.FrontLayer.Margin = "-9,-18,16,2";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.DoesNotContain(errors, e => e.Contains("margin"));
    }

    [Fact]
    public void Validate_NonNumericMargin_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.BaseLayer.Margin = "abc,0,0,0";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("non-numeric"));
    }

    [Fact]
    public void Validate_InvalidRotationOrigin_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.Title.IsVisible = true;
        definition.Title.RotationOrigin = "0.5";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("rotationOrigin"));
    }

    [Fact]
    public void Validate_RotationOriginOutOfRange_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.Title.IsVisible = true;
        definition.Title.RotationOrigin = "0.5,1.5";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("rotationOrigin"));
    }

    [Fact]
    public void Validate_ImageExceedsMaxSize_ReturnsError()
    {
        var definition = CreateValidDefinition();
        CreateDummyImage("base.png", sizeBytes: 3 * 1024 * 1024); // 3MB > 2MB limit
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("exceeds maximum size"));
    }

    [Fact]
    public void Validate_NullBaseLayer_NoBaseLayerError()
    {
        var definition = CreateValidDefinition();
        definition.BaseLayer = null;
        CreateDummyImage("front.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.DoesNotContain(errors, e => e.Contains("baseLayer"));
    }

    [Fact]
    public void Validate_NullFrontLayer_NoFrontLayerError()
    {
        var definition = CreateValidDefinition();
        definition.FrontLayer = null;
        CreateDummyImage("base.png");

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.DoesNotContain(errors, e => e.Contains("frontLayer"));
    }

    [Fact]
    public void Validate_MissingOpacityMaskFile_ReturnsError()
    {
        var definition = CreateValidDefinition();
        definition.Poster.OpacityMaskPath = "mask.png";
        CreateDummyImage("base.png");
        CreateDummyImage("front.png");
        // Don't create mask.png

        var errors = OverlayValidator.Validate(_tempDir, definition);

        Assert.Contains(errors, e => e.Contains("opacityMask") && e.Contains("not found"));
    }

    private static PosterOverlayDefinition CreateValidDefinition()
    {
        return new PosterOverlayDefinition
        {
            Id = "test-overlay",
            DisplayName = "Test Overlay",
            Author = "Test",
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
                RotationAngle = 0,
                RotationOrigin = "0.5,0.5",
                FontFamily = "Arial",
                Foreground = "White"
            }
        };
    }

    private void CreateDummyImage(string fileName, int sizeBytes = 100)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(path, new byte[sizeBytes]);
    }
}
