#nullable enable
using FoliCon.Modules.Overlays.Designer;
using Size = System.Windows.Size;
using Rect = System.Windows.Rect;
using Point = System.Windows.Point;
using Thickness = System.Windows.Thickness;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayGeometry"/> — the single conversion point between schema
/// margin strings and the typed bounds the designer canvas manipulates.
/// </summary>
public class OverlayDesignerGeometryTests
{
    #region Thickness parsing and formatting

    [Theory]
    [InlineData("10", 10, 10, 10, 10)]
    [InlineData("10,20", 10, 20, 10, 20)]
    [InlineData("10,20,30", 10, 20, 30, 20)]
    [InlineData("10,20,30,40", 10, 20, 30, 40)]
    public void ParseThickness_HandlesAllShorthandForms(string input, double l, double t, double r, double b)
    {
        var result = OverlayGeometry.ParseThickness(input);

        Assert.Equal(l, result.Left);
        Assert.Equal(t, result.Top);
        Assert.Equal(r, result.Right);
        Assert.Equal(b, result.Bottom);
    }

    [Fact]
    public void ParseThickness_NegativeValues_ArePreserved()
    {
        // Built-in overlays rely on negative margins for parity; they must survive round-tripping.
        var result = OverlayGeometry.ParseThickness("-8,0,0,10");

        Assert.Equal(-8, result.Left);
        Assert.Equal(10, result.Bottom);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseThickness_EmptyInput_ReturnsZero(string? input)
    {
        var result = OverlayGeometry.ParseThickness(input);

        Assert.Equal(new Thickness(0), result);
    }

    [Fact]
    public void ParseThickness_NonNumericSegment_BecomesZero()
    {
        // Matches DynamicPosterIcon's parsing so the canvas shows what actually renders.
        var result = OverlayGeometry.ParseThickness("10,abc,30,40");

        Assert.Equal(10, result.Left);
        Assert.Equal(0, result.Top);
        Assert.Equal(30, result.Right);
    }

    [Fact]
    public void FormatThickness_AlwaysEmitsFourInvariantValues()
    {
        var result = OverlayGeometry.FormatThickness(new Thickness(1, 2, 3, 4));

        Assert.Equal("1,2,3,4", result);
    }

    [Fact]
    public void FormatThickness_NegativeAndFractional_RoundTripsThroughParse()
    {
        var original = new Thickness(-8.5, 0, 16.25, -11);

        var reparsed = OverlayGeometry.ParseThickness(OverlayGeometry.FormatThickness(original));

        Assert.Equal(original, reparsed);
    }

    [Fact]
    public void FormatThickness_IsCultureInvariant()
    {
        // A comma decimal separator would corrupt the schema's comma-delimited format.
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var result = OverlayGeometry.FormatThickness(new Thickness(1.5, 2, 3, 4));

            Assert.Equal("1.5,2,3,4", result);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    #endregion

    #region Layout surface

    [Fact]
    public void GetLayoutSurface_NoRootMargin_EqualsDesignSize()
    {
        var surface = OverlayGeometry.GetLayoutSurface(256, 256, "0,0,0,0");

        Assert.Equal(new Size(256, 256), surface);
    }

    [Fact]
    public void GetLayoutSurface_LegacyBuiltInSurface_ExpandsByNegativeRootMargin()
    {
        // The six built-ins declare 265x256 with rootMargin "0,0,0,-11", which grows the
        // content box to 265x267. Getting this wrong shifts every element on the canvas.
        var surface = OverlayGeometry.GetLayoutSurface(265, 256, "0,0,0,-11");

        Assert.Equal(265, surface.Width);
        Assert.Equal(267, surface.Height);
    }

    [Fact]
    public void GetLayoutSurface_PositiveRootMargin_ShrinksSurface()
    {
        var surface = OverlayGeometry.GetLayoutSurface(256, 256, "8,8,8,8");

        Assert.Equal(240, surface.Width);
        Assert.Equal(240, surface.Height);
    }

    [Fact]
    public void GetLayoutSurface_OversizedMargin_ClampsToZero()
    {
        var surface = OverlayGeometry.GetLayoutSurface(100, 100, "200,200,0,0");

        Assert.Equal(0, surface.Width);
        Assert.Equal(0, surface.Height);
    }

    #endregion

    #region Margin and bounds conversion

    [Fact]
    public void MarginToBounds_InsetsOnAllFourSides()
    {
        var bounds = OverlayGeometry.MarginToBounds(new Thickness(10, 20, 30, 40), new Size(256, 256));

        Assert.Equal(10, bounds.X);
        Assert.Equal(20, bounds.Y);
        Assert.Equal(216, bounds.Width);  // 256 - 10 - 30
        Assert.Equal(196, bounds.Height); // 256 - 20 - 40
    }

    [Fact]
    public void BoundsToMargin_IsInverseOfMarginToBounds()
    {
        var surface = new Size(265, 267);
        var original = new Thickness(34, 10, 43, 21);

        var bounds = OverlayGeometry.MarginToBounds(original, surface);
        var roundTripped = OverlayGeometry.BoundsToMargin(bounds, surface);

        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void BoundsToMargin_NegativeMargins_RoundTripOnLegacySurface()
    {
        // liaher's base layer uses "-8,0,0,10" on the 265x267 legacy surface.
        var surface = OverlayGeometry.GetLayoutSurface(265, 256, "0,0,0,-11");
        var original = new Thickness(-8, 0, 0, 10);

        var bounds = OverlayGeometry.MarginToBounds(original, surface);
        var roundTripped = OverlayGeometry.BoundsToMargin(bounds, surface);

        Assert.Equal(original, roundTripped);
        Assert.Equal(-8, bounds.X);
    }

    [Fact]
    public void MarginToBounds_OverlappingMargins_ClampSizeToZero()
    {
        var bounds = OverlayGeometry.MarginToBounds(new Thickness(200, 200, 200, 200), new Size(256, 256));

        Assert.Equal(0, bounds.Width);
        Assert.Equal(0, bounds.Height);
    }

    #endregion

    #region Precision aids

    [Fact]
    public void SnapToPixels_RoundsAllComponents()
    {
        var snapped = OverlayGeometry.SnapToPixels(new Rect(10.4, 20.6, 100.7, 99.49));

        Assert.Equal(10, snapped.X);
        Assert.Equal(21, snapped.Y);
        Assert.Equal(101, snapped.Width);
        Assert.Equal(99, snapped.Height);
    }

    [Fact]
    public void SnapToPixels_AlreadyIntegral_IsUnchanged()
    {
        var bounds = new Rect(31, 42, 184, 195);

        Assert.Equal(bounds, OverlayGeometry.SnapToPixels(bounds));
    }

    [Fact]
    public void Nudge_MovesWithoutResizing()
    {
        var nudged = OverlayGeometry.Nudge(new Rect(10, 20, 100, 50), 1, -1);

        Assert.Equal(11, nudged.X);
        Assert.Equal(19, nudged.Y);
        Assert.Equal(100, nudged.Width);
        Assert.Equal(50, nudged.Height);
    }

    #endregion

    #region Zoom independence

    [Fact]
    public void CanvasToDesign_DividesByZoom()
    {
        var design = OverlayGeometry.CanvasToDesign(new Point(200, 100), zoom: 2);

        Assert.Equal(new Point(100, 50), design);
    }

    [Fact]
    public void DesignToCanvas_MultipliesByZoom()
    {
        var canvas = OverlayGeometry.DesignToCanvas(new Rect(10, 20, 30, 40), zoom: 4);

        Assert.Equal(new Rect(40, 80, 120, 160), canvas);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ZoomRoundTrip_PreservesDesignCoordinates(double zoom)
    {
        // Zoom is a display concern only — exported coordinates must never depend on it.
        var original = new Rect(31, 42, 184, 195);

        var canvas = OverlayGeometry.DesignToCanvas(original, zoom);
        var origin = OverlayGeometry.CanvasToDesign(new Point(canvas.X, canvas.Y), zoom);
        var size = OverlayGeometry.CanvasToDesign(new Point(canvas.Width, canvas.Height), zoom);
        var backToDesign = new Rect(origin.X, origin.Y, size.X, size.Y);

        Assert.Equal(original, backToDesign);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void ZoomedGesture_ProducesTheSameMarginAtEveryZoomLevel(double zoom)
    {
        // Dragging an element to the same on-screen spot must yield identical exported
        // margins whether the author is zoomed in or out.
        var surface = new Size(265, 267);
        var canvasDropPoint = new Point(60 * zoom, 80 * zoom);

        var designPoint = OverlayGeometry.CanvasToDesign(canvasDropPoint, zoom);
        var margin = OverlayGeometry.BoundsToMargin(
            new Rect(designPoint.X, designPoint.Y, 100, 100), surface);

        Assert.Equal("60,80,105,87", OverlayGeometry.FormatThickness(margin));
    }

    [Fact]
    public void CanvasToDesign_ZeroZoom_ReturnsInputUnchanged()
    {
        var point = new Point(50, 60);

        Assert.Equal(point, OverlayGeometry.CanvasToDesign(point, zoom: 0));
    }

    #endregion
}
