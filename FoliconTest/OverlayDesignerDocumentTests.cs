using FoliCon.Models.Data;
using FoliCon.Modules.Overlays.Designer;
using Thickness = System.Windows.Thickness;
using Rect = System.Windows.Rect;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayDesignerDocument"/> — typed edit state and its projection
/// back onto the canonical <see cref="PosterOverlayDefinition"/> schema model.
/// </summary>
public class OverlayDesignerDocumentTests
{
    #region Snapshot mapping

    [Fact]
    public void CreateSnapshot_MapsMetadata()
    {
        var document = new OverlayDesignerDocument
        {
            Id = "my-overlay",
            DisplayName = "My Overlay",
            Author = "Author",
            Description = "Description",
            OverlayVersion = "2.1.0"
        };
        document.Tags.AddRange(["a", "b"]);

        var snapshot = document.CreateSnapshot();

        Assert.Equal("my-overlay", snapshot.Id);
        Assert.Equal("My Overlay", snapshot.DisplayName);
        Assert.Equal("Author", snapshot.Author);
        Assert.Equal("Description", snapshot.Description);
        Assert.Equal("2.1.0", snapshot.OverlayVersion);
        Assert.Equal(["a", "b"], snapshot.Tags);
    }

    [Fact]
    public void CreateSnapshot_ConvertsTypedThicknessToSchemaStrings()
    {
        var document = new OverlayDesignerDocument
        {
            PosterMargin = new Thickness(31, 42, 50, 19),
            RootMargin = new Thickness(0, 0, 0, -11)
        };

        var snapshot = document.CreateSnapshot();

        Assert.Equal("31,42,50,19", snapshot.Poster.Margin);
        Assert.Equal("0,0,0,-11", snapshot.RootMargin);
    }

    [Fact]
    public void CreateSnapshot_AbsentLayers_AreNull()
    {
        var document = new OverlayDesignerDocument { HasBaseLayer = false, HasFrontLayer = false };

        var snapshot = document.CreateSnapshot();

        Assert.Null(snapshot.BaseLayer);
        Assert.Null(snapshot.FrontLayer);
    }

    [Fact]
    public void CreateSnapshot_PresentLayers_CarryPathAndMargin()
    {
        var document = new OverlayDesignerDocument
        {
            HasBaseLayer = true,
            BaseLayerImagePath = "base.png",
            BaseLayerMargin = new Thickness(30, 14, 48, 15)
        };

        var snapshot = document.CreateSnapshot();

        Assert.NotNull(snapshot.BaseLayer);
        Assert.Equal("base.png", snapshot.BaseLayer.ImagePath);
        Assert.Equal("30,14,48,15", snapshot.BaseLayer.Margin);
    }

    [Fact]
    public void CreateSnapshot_LayerOrder_UsesSchemaKeys()
    {
        var document = new OverlayDesignerDocument();
        document.LayerOrder.Clear();
        document.LayerOrder.AddRange([OverlayElementKind.Base, OverlayElementKind.Poster, OverlayElementKind.Rating]);

        var snapshot = document.CreateSnapshot();

        Assert.Equal(["base", "poster", "rating"], snapshot.LayerOrder);
    }

    [Fact]
    public void CreateSnapshot_MarksDocumentAsCommunityOverlay()
    {
        // A designer-authored overlay is never built-in, even when cloned from one.
        var document = new OverlayDesignerDocument { Id = "clone-of-liaher" };

        Assert.False(document.CreateSnapshot().IsBuiltIn);
    }

    [Fact]
    public void CreateSnapshot_SetsOverlayFolderPathForAssetResolution()
    {
        var document = new OverlayDesignerDocument { AssetFolderPath = @"C:\work\my-overlay" };

        Assert.Equal(@"C:\work\my-overlay", document.CreateSnapshot().OverlayFolderPath);
    }

    [Fact]
    public void CreateSnapshot_EmptyAssetFolder_LeavesFolderPathNull()
    {
        var document = new OverlayDesignerDocument { AssetFolderPath = string.Empty };

        Assert.Null(document.CreateSnapshot().OverlayFolderPath);
    }

    [Fact]
    public void CreateSnapshot_CalledTwice_ProducesIndependentInstances()
    {
        var document = new OverlayDesignerDocument { Id = "test" };

        var first = document.CreateSnapshot();
        var second = document.CreateSnapshot();

        Assert.NotSame(first, second);
        Assert.NotSame(first.Poster, second.Poster);
    }

    #endregion

    #region Loading from a definition

    [Fact]
    public void FromDefinition_RoundTripsThroughSnapshot()
    {
        var original = CreateFullDefinition();

        var document = OverlayDesignerDocument.FromDefinition(original, @"C:\overlays\test");
        var snapshot = document.CreateSnapshot();

        Assert.Equal(original.Id, snapshot.Id);
        Assert.Equal(original.DisplayName, snapshot.DisplayName);
        Assert.Equal(original.Poster.Margin, snapshot.Poster.Margin);
        Assert.Equal(original.Rating.ShieldMargin, snapshot.Rating.ShieldMargin);
        Assert.Equal(original.Rating.FontFamily, snapshot.Rating.FontFamily);
        Assert.Equal(original.Title.RotationAngle, snapshot.Title.RotationAngle);
        Assert.Equal(original.LayerOrder, snapshot.LayerOrder);
        Assert.Equal(original.BaseLayer!.ImagePath, snapshot.BaseLayer!.ImagePath);
    }

    [Fact]
    public void FromDefinition_DoesNotMutateSourceDefinition()
    {
        var original = CreateFullDefinition();
        var originalMargin = original.Poster.Margin;
        var originalTagCount = original.Tags.Length;

        var document = OverlayDesignerDocument.FromDefinition(original, @"C:\overlays\test");
        document.PosterMargin = new Thickness(999);
        document.Tags.Add("new-tag");
        document.Id = "changed";

        Assert.Equal(originalMargin, original.Poster.Margin);
        Assert.Equal(originalTagCount, original.Tags.Length);
        Assert.Equal("test-overlay", original.Id);
    }

    [Fact]
    public void FromDefinition_NullLayerOrder_FallsBackToDefault()
    {
        var definition = CreateFullDefinition();
        definition.LayerOrder = null;

        var document = OverlayDesignerDocument.FromDefinition(definition, @"C:\x");

        Assert.Equal(OverlayElementKinds.DefaultOrder, document.LayerOrder);
    }

    [Fact]
    public void FromDefinition_UnknownLayerKeys_AreDropped()
    {
        var definition = CreateFullDefinition();
        definition.LayerOrder = ["base", "bogus", "poster"];

        var document = OverlayDesignerDocument.FromDefinition(definition, @"C:\x");

        Assert.Equal([OverlayElementKind.Base, OverlayElementKind.Poster], document.LayerOrder);
    }

    [Fact]
    public void FromDefinition_DuplicateLayerKeys_AreDeduplicated()
    {
        var definition = CreateFullDefinition();
        definition.LayerOrder = ["poster", "poster", "rating"];

        var document = OverlayDesignerDocument.FromDefinition(definition, @"C:\x");

        Assert.Equal([OverlayElementKind.Poster, OverlayElementKind.Rating], document.LayerOrder);
    }

    [Fact]
    public void FromDefinition_NullDefinition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OverlayDesignerDocument.FromDefinition(null!, @"C:\x"));
    }

    #endregion

    #region Element bounds

    [Fact]
    public void GetElementBounds_UsesLayoutSurfaceIncludingRootMargin()
    {
        var document = new OverlayDesignerDocument
        {
            DesignWidth = 265,
            DesignHeight = 256,
            RootMargin = new Thickness(0, 0, 0, -11),
            PosterMargin = new Thickness(34, 10, 43, 21)
        };

        var bounds = document.GetElementBounds(OverlayElementKind.Poster);

        // Surface is 265 x 267 once the negative root margin is applied.
        Assert.Equal(34, bounds.X);
        Assert.Equal(10, bounds.Y);
        Assert.Equal(188, bounds.Width);  // 265 - 34 - 43
        Assert.Equal(236, bounds.Height); // 267 - 10 - 21
    }

    [Fact]
    public void SetElementBounds_IsInverseOfGetElementBounds()
    {
        var document = new OverlayDesignerDocument
        {
            DesignWidth = 265,
            DesignHeight = 256,
            RootMargin = new Thickness(0, 0, 0, -11)
        };
        var target = new Rect(20, 30, 150, 180);

        document.SetElementBounds(OverlayElementKind.Front, target);

        Assert.Equal(target, document.GetElementBounds(OverlayElementKind.Front));
    }

    [Theory]
    [InlineData(OverlayElementKind.Base)]
    [InlineData(OverlayElementKind.Poster)]
    [InlineData(OverlayElementKind.Front)]
    [InlineData(OverlayElementKind.Rating)]
    [InlineData(OverlayElementKind.Title)]
    public void SetElementMargin_ThenGet_ReturnsSameValue(OverlayElementKind kind)
    {
        var document = new OverlayDesignerDocument();
        var margin = new Thickness(1, 2, 3, 4);

        document.SetElementMargin(kind, margin);

        Assert.Equal(margin, document.GetElementMargin(kind));
    }

    [Fact]
    public void MovingTheRatingBadge_CarriesTheNumberWithIt()
    {
        // The schema positions the shield and its number separately, but they are one badge
        // to the author — dragging it must not leave the number behind.
        var document = new OverlayDesignerDocument
        {
            RatingShieldMargin = new Thickness(160, 97, 6, 5),
            RatingTextMargin = new Thickness(189, 30, 21, 24)
        };

        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(150, 87, 16, 15));

        // Shield moved -10,-10; the number must move by the same delta.
        Assert.Equal(new Thickness(179, 20, 31, 34), document.RatingTextMargin);
    }

    [Fact]
    public void MovingTheRatingBadge_PreservesTheNumbersOffsetWithinIt()
    {
        var document = new OverlayDesignerDocument
        {
            RatingShieldMargin = new Thickness(100, 100, 0, 0),
            RatingTextMargin = new Thickness(129, 33, 0, 0)
        };
        var offsetX = document.RatingTextMargin.Left - document.RatingShieldMargin.Left;
        var offsetY = document.RatingTextMargin.Top - document.RatingShieldMargin.Top;

        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(40, 60, 0, 0));

        Assert.Equal(offsetX, document.RatingTextMargin.Left - document.RatingShieldMargin.Left);
        Assert.Equal(offsetY, document.RatingTextMargin.Top - document.RatingShieldMargin.Top);
    }

    [Fact]
    public void MovingTheRatingBadge_KeepsTheNumbersSize()
    {
        // Right/bottom must move opposite to left/top, or the text box would stretch.
        var document = new OverlayDesignerDocument
        {
            RatingShieldMargin = new Thickness(160, 97, 6, 5),
            RatingTextMargin = new Thickness(189, 30, 21, 24)
        };
        var surface = document.LayoutSurface;
        var widthBefore = surface.Width - document.RatingTextMargin.Left - document.RatingTextMargin.Right;

        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(100, 50, 66, 52));

        var widthAfter = surface.Width - document.RatingTextMargin.Left - document.RatingTextMargin.Right;
        Assert.Equal(widthBefore, widthAfter);
    }

    [Fact]
    public void ResizingTheRatingBadgeWithoutMoving_LeavesTheNumberAlone()
    {
        var document = new OverlayDesignerDocument
        {
            RatingShieldMargin = new Thickness(160, 97, 6, 5),
            RatingTextMargin = new Thickness(189, 30, 21, 24)
        };

        // Same left/top, different right/bottom: a resize, not a move.
        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(160, 97, 20, 20));

        Assert.Equal(new Thickness(189, 30, 21, 24), document.RatingTextMargin);
    }

    [Fact]
    public void MovingTheRatingBadgeAndBack_RestoresBothMargins()
    {
        // Undo replays the old margin through the same path, so the delta must reverse cleanly.
        var document = new OverlayDesignerDocument
        {
            RatingShieldMargin = new Thickness(160, 97, 6, 5),
            RatingTextMargin = new Thickness(189, 30, 21, 24)
        };

        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(40, 20, 126, 82));
        document.SetElementMargin(OverlayElementKind.Rating, new Thickness(160, 97, 6, 5));

        Assert.Equal(new Thickness(189, 30, 21, 24), document.RatingTextMargin);
    }

    [Fact]
    public void IsElementPresent_ReflectsLayerFlags()
    {
        var document = new OverlayDesignerDocument
        {
            HasBaseLayer = true,
            HasFrontLayer = false,
            TitleIsVisible = false
        };

        Assert.True(document.IsElementPresent(OverlayElementKind.Base));
        Assert.False(document.IsElementPresent(OverlayElementKind.Front));
        Assert.False(document.IsElementPresent(OverlayElementKind.Title));

        // Poster and rating always render.
        Assert.True(document.IsElementPresent(OverlayElementKind.Poster));
        Assert.True(document.IsElementPresent(OverlayElementKind.Rating));
    }

    #endregion

    #region Referenced assets

    [Fact]
    public void GetReferencedAssets_ReturnsOnlyPresentLayerImages()
    {
        var document = new OverlayDesignerDocument
        {
            HasBaseLayer = true,
            BaseLayerImagePath = "base.png",
            HasFrontLayer = false,
            FrontLayerImagePath = "front.png"
        };

        Assert.Equal(["base.png"], document.GetReferencedAssets());
    }

    [Fact]
    public void GetReferencedAssets_IncludesMaskAndFonts()
    {
        var document = new OverlayDesignerDocument
        {
            PosterOpacityMaskPath = "mask.png",
            RatingFontSource = "rating.ttf",
            TitleFontSource = "title.otf"
        };

        Assert.Equal(["mask.png", "rating.ttf", "title.otf"], document.GetReferencedAssets());
    }

    [Fact]
    public void GetReferencedAssets_ExcludesBuiltInPackPaths()
    {
        // Pack paths are compiled into the assembly; they are not package files to copy.
        var document = new OverlayDesignerDocument
        {
            HasBaseLayer = true,
            BaseLayerImagePath = "/Resources/poster_mockups/liaher/mockup liaher base.png"
        };

        Assert.Empty(document.GetReferencedAssets());
    }

    #endregion

    private static PosterOverlayDefinition CreateFullDefinition() => new()
    {
        SchemaVersion = 1,
        Id = "test-overlay",
        DisplayName = "Test Overlay",
        Author = "Author",
        Description = "Description",
        OverlayVersion = "1.0.0",
        Tags = ["tag1", "tag2"],
        DesignWidth = 265,
        DesignHeight = 256,
        RootMargin = "0,0,0,-11",
        RenderWidth = 256,
        RenderHeight = 256,
        LayerOrder = ["base", "poster", "front", "rating"],
        BaseLayer = new LayerDefinition { ImagePath = "base.png", Margin = "30,14,48,15" },
        FrontLayer = new LayerDefinition { ImagePath = "front.png", Margin = "16,14,35,15" },
        Poster = new PosterConfig { Margin = "31,42,50,19", ClipRadius = "8", ClipRect = "0,0,188,236" },
        Rating = new RatingConfig
        {
            ShieldMargin = "160,97,6,5",
            TextMargin = "189,30,21,24",
            FontSize = 25,
            FontFamily = "Castellar",
            TextWidth = 55,
            TextHeight = 46
        },
        Title = new TitleConfig
        {
            IsVisible = true,
            Margin = "190,14,-2,53",
            RotationAngle = 90,
            RotationOrigin = "0.5,0.5",
            FontFamily = "Cormorant",
            Container = "RatingGrid",
            GridRow = 1
        }
    };
}
