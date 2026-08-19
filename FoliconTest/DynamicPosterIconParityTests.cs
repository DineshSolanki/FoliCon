using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.utils;
using FoliCon.Views;
using PosterIcon = FoliCon.Models.Data.PosterIcon;

namespace FoliconTest;

/// <summary>
/// Golden-image parity tests: verify that DynamicPosterIcon renders all 6 built-in
/// overlay definitions successfully to 256×256 bitmaps.
///
/// Full pixel-level parity with compiled XAML views requires the FoliCon application
/// context (pack URI resolution via Application.Current) and is verified manually.
/// These tests confirm the dynamic renderer handles all overlay configurations
/// without errors.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class DynamicPosterIconParityTests
{
    private static readonly string[] OverlayIds =
        ["legacy", "alternate", "liaher", "faelpessoal", "faelpessoal-horizontal", "windows11"];

    [Fact]
    public async Task AllOverlays_DynamicPosterIcon_RendersSuccessfully()
    {
        var provider = new OverlayProvider();
        var posterIcon = new PosterIcon();

        try
        {
            foreach (var overlayId in OverlayIds)
            {
                var definition = provider.GetOverlayById(overlayId);
                Assert.NotNull(definition);

                var bitmap = await StaRenderer.Default.EnqueueRender(() =>
                {
                    var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                    return dynamicIcon.RenderToBitmap();
                });

                Assert.NotNull(bitmap);
                Assert.Equal(256, bitmap.Width);
                Assert.Equal(256, bitmap.Height);
                bitmap.Dispose();
            }
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("alternate")]
    [InlineData("liaher")]
    [InlineData("faelpessoal")]
    [InlineData("faelpessoal-horizontal")]
    [InlineData("windows11")]
    public async Task Overlay_RendersToCorrectDimensions(string overlayId)
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById(overlayId);
        Assert.NotNull(definition);

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            Assert.Equal(256, bitmap.Width);
            Assert.Equal(256, bitmap.Height);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Fact]
    public async Task Overlay_WithClipRadius_RendersWithoutError()
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById("liaher");
        Assert.NotNull(definition);
        Assert.NotEqual("0", definition.Poster.ClipRadius);

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            Assert.Equal(256, bitmap.Width);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Fact]
    public async Task Overlay_WithOpacityMask_RendersWithoutError()
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById("windows11");
        Assert.NotNull(definition);
        Assert.False(string.IsNullOrEmpty(definition.Poster.OpacityMaskPath));

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            Assert.Equal(256, bitmap.Width);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Fact]
    public async Task Overlay_WithRotatedTitle_RendersWithoutError()
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById("faelpessoal");
        Assert.NotNull(definition);
        Assert.True(definition.Title.IsVisible);
        Assert.True(definition.Title.RotationAngle > 0);

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            Assert.Equal(256, bitmap.Width);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Fact]
    public async Task Overlay_NullBaseLayer_RendersWithoutError()
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById("legacy");
        Assert.NotNull(definition);
        Assert.Null(definition.BaseLayer);

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            Assert.Equal(256, bitmap.Width);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }

    [Fact]
    public async Task Overlay_Windows11_NullFrontLayer_WithOpacityMask_RendersWithoutError()
    {
        var provider = new OverlayProvider();
        var definition = provider.GetOverlayById("windows11");
        Assert.NotNull(definition);
        Assert.Null(definition.FrontLayer);
        Assert.False(string.IsNullOrEmpty(definition.Poster.OpacityMaskPath));

        var posterIcon = new PosterIcon();
        try
        {
            var bitmap = await StaRenderer.Default.EnqueueRender(() =>
            {
                var dynamicIcon = new DynamicPosterIcon(definition, posterIcon);
                return dynamicIcon.RenderToBitmap();
            });

            Assert.NotNull(bitmap);
            bitmap.Dispose();
        }
        finally
        {
            posterIcon.Dispose();
        }
    }
}
