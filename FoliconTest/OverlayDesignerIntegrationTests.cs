#nullable enable
using System.Drawing;
using System.IO;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;
using FoliCon.Views;
using Newtonsoft.Json;
using PosterIcon = FoliCon.Models.Data.PosterIcon;
using Thickness = System.Windows.Thickness;

namespace FoliconTest;

/// <summary>
/// End-to-end tests for the Step 1 designer foundation against real collaborators:
/// the actual <see cref="OverlayProvider"/> (loading built-in overlays from embedded
/// resources) and the production <see cref="DynamicPosterIcon"/> renderer on the STA thread.
///
/// The unit suites use stub providers and never render. These tests prove the pieces
/// actually work together: a cloned template produces a package that validates, renders,
/// and reloads without drift.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public sealed class OverlayDesignerIntegrationTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly OverlayProvider _provider = new();
    private readonly OverlayTemplateProvider _templates;
    private readonly OverlayPackageLoader _loader = new();
    private readonly string _workDir;

    public OverlayDesignerIntegrationTests()
    {
        _templates = new OverlayTemplateProvider(_provider);
        _workDir = Path.Combine(Path.GetTempPath(), $"FoliconIntegration_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
    }

    public void Dispose()
    {
        _host.Dispose();
        if (Directory.Exists(_workDir))
        {
            Directory.Delete(_workDir, true);
        }
        GC.SuppressFinalize(this);
    }

    #region Full authoring round trip

    [Theory]
    [InlineData("legacy")]
    [InlineData("alternate")]
    [InlineData("liaher")]
    [InlineData("faelpessoal")]
    [InlineData("faelpessoal-horizontal")]
    [InlineData("windows11")]
    public void CloneBuiltIn_ProducesPackageThatValidatesAndRenders(string builtInId)
    {
        // The complete "New from template" path against the real provider: clone the
        // built-in, extract its embedded assets, validate the result as a store package,
        // and render it through the production pipeline.
        var template = GetTemplate(builtInId);
        var folder = Path.Combine(_workDir, $"clone-{builtInId}");

        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(template, folder, $"my-{builtInId}", $"My {builtInId}", "Integration Test"));

        var snapshot = document.CreateSnapshot();

        var validation = FoliCon.Modules.Overlays.Internal.OverlayValidator.ValidateDetailed(folder, snapshot);
        Assert.True(validation.IsValid,
            $"Clone of '{builtInId}' failed validation: {string.Join("; ", validation.Errors.Select(e => $"{e.Field}: {e.Message}"))}");

        // Every asset must be a real file in the package folder. A clone that still points at
        // a pack URI renders fine inside FoliCon but ships broken — the validator skips pack
        // paths and the renderer resolves them from the assembly, so nothing else catches this.
        AssertAllAssetsAreSelfContained(document, folder, builtInId);

        using var bitmap = RenderToBitmap(snapshot);
        Assert.Equal(256, bitmap.Width);
        Assert.Equal(256, bitmap.Height);
        Assert.True(HasVisibleContent(bitmap), $"Clone of '{builtInId}' rendered a blank image.");
    }

    [Theory]
    [InlineData("liaher")]
    [InlineData("windows11")]
    [InlineData("legacy")]
    public void ClonedPackage_IsSelfContained_WhenFoliConResourcesAreUnavailable(string builtInId)
    {
        // Simulates the overlay running on another user's machine: copy the package
        // somewhere isolated and confirm every declared asset resolves from its own files.
        var folder = Path.Combine(_workDir, $"portable-{builtInId}");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate(builtInId), folder, $"portable-{builtInId}", "Portable", "Test"));

        var elsewhere = Path.Combine(_workDir, $"relocated-{builtInId}");
        Directory.CreateDirectory(elsewhere);
        foreach (var file in Directory.GetFiles(folder))
        {
            File.Copy(file, Path.Combine(elsewhere, Path.GetFileName(file)));
        }

        var assets = document.GetReferencedAssets().ToList();
        Assert.NotEmpty(assets);

        foreach (var asset in assets)
        {
            Assert.True(File.Exists(Path.Combine(elsewhere, asset)),
                $"Relocated clone of '{builtInId}' is missing asset '{asset}'.");
        }
    }

    [Theory]
    [InlineData("liaher")]
    [InlineData("windows11")]
    [InlineData("faelpessoal")]
    public void ClonedOverlay_RendersIdenticallyToItsTemplate(string builtInId)
    {
        // Cloning only changes identity and asset locations, never geometry. If extraction
        // or path rewriting were wrong, the clone would render differently from the original.
        var original = _provider.GetOverlayById(builtInId);
        Assert.NotNull(original);

        var folder = Path.Combine(_workDir, $"parity-{builtInId}");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate(builtInId), folder, $"copy-{builtInId}", "Copy", "Test"));

        using var originalBitmap = RenderToBitmap(original);
        using var cloneBitmap = RenderToBitmap(document.CreateSnapshot());

        var differingPixels = CountDifferingPixels(originalBitmap, cloneBitmap);
        Assert.True(differingPixels == 0,
            $"Clone of '{builtInId}' differs from its template in {differingPixels} pixels.");
    }

    [Fact]
    public void SaveClonedPackage_ThenReload_PreservesEveryGeometryValue()
    {
        // Write a cloned package to disk the way the exporter will, then reopen it.
        // Any drift in margin formatting or parsing shows up as a changed snapshot.
        var folder = Path.Combine(_workDir, "roundtrip");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"), folder, "roundtrip-test", "Round Trip", "Test"));

        var beforeSave = document.CreateSnapshot();
        var jsonPath = Path.Combine(folder, "overlay.json");
        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(beforeSave, Formatting.Indented));

        var reloaded = _loader.Load(jsonPath);
        Assert.True(reloaded.Succeeded, reloaded.FailureReason);

        var afterReload = reloaded.Document.CreateSnapshot();

        Assert.Equal(beforeSave.RootMargin, afterReload.RootMargin);
        Assert.Equal(beforeSave.Poster.Margin, afterReload.Poster.Margin);
        Assert.Equal(beforeSave.Poster.ClipRect, afterReload.Poster.ClipRect);
        Assert.Equal(beforeSave.BaseLayer?.Margin, afterReload.BaseLayer?.Margin);
        Assert.Equal(beforeSave.FrontLayer?.Margin, afterReload.FrontLayer?.Margin);
        Assert.Equal(beforeSave.Rating.ShieldMargin, afterReload.Rating.ShieldMargin);
        Assert.Equal(beforeSave.LayerOrder, afterReload.LayerOrder);
        Assert.Equal(beforeSave.DesignWidth, afterReload.DesignWidth);
    }

    [Fact]
    public void ReloadedPackage_RendersIdenticallyToTheInMemoryDocument()
    {
        var folder = Path.Combine(_workDir, "render-roundtrip");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"), folder, "render-test", "Render Test", "Test"));

        var jsonPath = Path.Combine(folder, "overlay.json");
        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(document.CreateSnapshot(), Formatting.Indented));

        using var beforeBitmap = RenderToBitmap(document.CreateSnapshot());
        using var afterBitmap = RenderToBitmap(_loader.Load(jsonPath).Document!.CreateSnapshot());

        Assert.Equal(0, CountDifferingPixels(beforeBitmap, afterBitmap));
    }

    #endregion

    #region Editing through the command stack

    [Fact]
    public void EditThroughHistory_ChangesRenderedOutput_AndUndoRestoresItExactly()
    {
        // Proves the whole loop: an edit command mutates the document, the change is
        // visible in the rendered bitmap, and undo restores it pixel-for-pixel.
        var folder = Path.Combine(_workDir, "edit-cycle");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"), folder, "edit-test", "Edit Test", "Test"));
        var history = new OverlayEditHistory(document);

        using var beforeBitmap = RenderToBitmap(document.CreateSnapshot());

        history.Execute(new ElementBoundsCommand(
            OverlayElementKind.Poster,
            document.PosterMargin,
            new Thickness(60, 60, 60, 60),
            "Move poster"));

        using var afterEditBitmap = RenderToBitmap(document.CreateSnapshot());
        Assert.True(CountDifferingPixels(beforeBitmap, afterEditBitmap) > 0,
            "Moving the poster did not change the rendered output.");

        history.Undo();

        using var afterUndoBitmap = RenderToBitmap(document.CreateSnapshot());
        Assert.Equal(0, CountDifferingPixels(beforeBitmap, afterUndoBitmap));
    }

    [Fact]
    public void CanvasGestureAndNumericEdit_ProduceTheSameMargin()
    {
        // Acceptance criterion: "Canvas gestures and numeric fields produce the same
        // PosterOverlayDefinition values." A drag at 2x zoom and a typed margin must agree.
        var folder = Path.Combine(_workDir, "gesture-parity");
        var viaGesture = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"), folder, "gesture-test", "Gesture", "Test"));
        var viaNumeric = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"),
                Path.Combine(_workDir, "numeric-parity"), "numeric-test", "Numeric", "Test"));

        // Gesture: author drags the poster to (40, 50) on a 2x-zoomed canvas and sizes it 150x160.
        const double zoom = 2;
        var canvasBounds = new System.Windows.Rect(40 * zoom, 50 * zoom, 150 * zoom, 160 * zoom);
        var origin = OverlayGeometry.CanvasToDesign(new System.Windows.Point(canvasBounds.X, canvasBounds.Y), zoom);
        var size = OverlayGeometry.CanvasToDesign(new System.Windows.Point(canvasBounds.Width, canvasBounds.Height), zoom);
        viaGesture.SetElementBounds(OverlayElementKind.Poster,
            OverlayGeometry.SnapToPixels(new System.Windows.Rect(origin.X, origin.Y, size.X, size.Y)));

        // Numeric: author types the equivalent bounds directly.
        viaNumeric.SetElementBounds(OverlayElementKind.Poster, new System.Windows.Rect(40, 50, 150, 160));

        Assert.Equal(
            viaNumeric.CreateSnapshot().Poster.Margin,
            viaGesture.CreateSnapshot().Poster.Margin);
    }

    [Fact]
    public void NudgingByOnePixel_ShiftsTheRenderedPoster()
    {
        var folder = Path.Combine(_workDir, "nudge");
        var document = WpfTestHost.Invoke(() =>
            _templates.CreateFromTemplate(GetTemplate("liaher"), folder, "nudge-test", "Nudge", "Test"));

        using var beforeBitmap = RenderToBitmap(document.CreateSnapshot());

        var bounds = document.GetElementBounds(OverlayElementKind.Poster);
        document.SetElementBounds(OverlayElementKind.Poster, OverlayGeometry.Nudge(bounds, 1, 0));

        using var afterBitmap = RenderToBitmap(document.CreateSnapshot());

        Assert.True(CountDifferingPixels(beforeBitmap, afterBitmap) > 0,
            "A 1px nudge produced no visible change.");
    }

    #endregion

    #region Title layer

    [Fact]
    public void EnablingTheTitle_MakesItRender_EvenWhenLayerOrderOmitsIt()
    {
        // Most templates predate the title and leave "title" out of layerOrder. The renderer
        // only draws listed layers, so the title was built and then silently dropped.
        var definition = _provider.GetOverlayById("liaher")!;
        Assert.DoesNotContain("title", definition.LayerOrder!);

        var withoutTitle = CloneDefinition(definition);
        withoutTitle.Title.IsVisible = false;

        var withTitle = CloneDefinition(definition);
        withTitle.Title.IsVisible = true;
        withTitle.Title.Foreground = "Red";
        withTitle.Title.Margin = "20,20,20,20";

        using var before = RenderToBitmap(withoutTitle);
        using var after = RenderToBitmap(withTitle);

        Assert.True(CountDifferingPixels(before, after) > 0,
            "Enabling the title produced no visible change — it is still being dropped.");
    }

    [Fact]
    public void LayerOrder_StillControlsZOrderForListedLayers()
    {
        // The append-missing-layers fallback must not disturb the order of listed layers,
        // or built-in overlays would drift.
        var definition = _provider.GetOverlayById("liaher")!;

        var normal = CloneDefinition(definition);

        var reordered = CloneDefinition(definition);
        reordered.LayerOrder = ["poster", "base", "front", "rating"];

        using var normalBitmap = RenderToBitmap(normal);
        using var reorderedBitmap = RenderToBitmap(reordered);

        Assert.True(CountDifferingPixels(normalBitmap, reorderedBitmap) > 0,
            "Changing layerOrder had no effect, so z-order is no longer being honoured.");
    }

    #endregion

    #region Provider integration

    [Fact]
    public void RealProvider_ExposesEveryBuiltInAsATemplate()
    {
        var templateIds = _templates.GetTemplates().Select(t => t.Id).ToList();

        foreach (var builtIn in new[] { "legacy", "alternate", "liaher", "faelpessoal", "faelpessoal-horizontal", "windows11" })
        {
            Assert.Contains(builtIn, templateIds);
        }
    }

    [Fact]
    public void RealProvider_RejectsBuiltInIdsForNewOverlays()
    {
        Assert.False(_templates.IsIdAvailable("liaher"));
        Assert.True(_templates.IsIdAvailable("definitely-not-taken-xyz"));
    }

    [Fact]
    public void CloningDoesNotRegisterTheNewOverlayWithTheProvider()
    {
        // Designing is not installing; the clone must stay out of the user's overlay list
        // until they explicitly install or export it.
        var before = _provider.GetAllOverlays().Count;

        WpfTestHost.Invoke(() => _templates.CreateFromTemplate(
            GetTemplate("liaher"), Path.Combine(_workDir, "unregistered"), "not-installed", "Not Installed", "Test"));

        Assert.Equal(before, _provider.GetAllOverlays().Count);
        Assert.Null(_provider.GetOverlayById("not-installed"));
    }

    [Fact]
    public void CloningLeavesTheBuiltInDefinitionUntouched()
    {
        var liaher = _provider.GetOverlayById("liaher");
        Assert.NotNull(liaher);
        var originalMargin = liaher.Poster.Margin;
        var originalImagePath = liaher.BaseLayer?.ImagePath;

        var document = WpfTestHost.Invoke(() => _templates.CreateFromTemplate(
            GetTemplate("liaher"), Path.Combine(_workDir, "immutability"), "immutable-test", "Test", "Test"));
        document.PosterMargin = new Thickness(999);

        Assert.Equal(originalMargin, _provider.GetOverlayById("liaher")!.Poster.Margin);
        Assert.Equal(originalImagePath, _provider.GetOverlayById("liaher")!.BaseLayer?.ImagePath);
        Assert.True(_provider.GetOverlayById("liaher")!.IsBuiltIn);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Deep-copies a definition so a test can vary one field without mutating the
    /// provider's shared instance.
    /// </summary>
    private static FoliCon.Models.Data.PosterOverlayDefinition CloneDefinition(
        FoliCon.Models.Data.PosterOverlayDefinition source) =>
        OverlayDesignerDocument.FromDefinition(source, source.OverlayFolderPath ?? string.Empty)
            .CreateSnapshot();

    private OverlayTemplate GetTemplate(string id) =>
        _templates.GetTemplates().First(t => t.Id == id);

    private static Bitmap RenderToBitmap(FoliCon.Models.Data.PosterOverlayDefinition definition) =>
        WpfTestHost.Invoke(() =>
        {
            var posterIcon = new PosterIcon();
            return new DynamicPosterIcon(definition, posterIcon).RenderToBitmap();
        });

    /// <summary>
    /// Asserts the document references no pack URIs and that every referenced asset exists
    /// as a real file in the package folder.
    /// </summary>
    private static void AssertAllAssetsAreSelfContained(OverlayDesignerDocument document, string folder, string context)
    {
        var packPaths = new List<string>();

        if (document.HasBaseLayer && document.BaseLayerImagePath.StartsWith('/'))
        {
            packPaths.Add($"baseLayer: {document.BaseLayerImagePath}");
        }

        if (document.HasFrontLayer && document.FrontLayerImagePath.StartsWith('/'))
        {
            packPaths.Add($"frontLayer: {document.FrontLayerImagePath}");
        }

        if (document.PosterOpacityMaskPath?.StartsWith('/') == true)
        {
            packPaths.Add($"opacityMask: {document.PosterOpacityMaskPath}");
        }

        Assert.True(packPaths.Count == 0,
            $"Clone of '{context}' still references embedded resources instead of extracted files: {string.Join(", ", packPaths)}");

        foreach (var asset in document.GetReferencedAssets())
        {
            Assert.True(File.Exists(Path.Combine(folder, asset)),
                $"Clone of '{context}' declares asset '{asset}' but no such file exists in the package.");
        }
    }

    /// <summary>A render is blank when every pixel is fully transparent.</summary>
    private static bool HasVisibleContent(Bitmap bitmap)
    {
        for (var y = 0; y < bitmap.Height; y += 4)
        {
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                if (bitmap.GetPixel(x, y).A > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int CountDifferingPixels(Bitmap a, Bitmap b)
    {
        if (a.Width != b.Width || a.Height != b.Height)
        {
            return int.MaxValue;
        }

        var count = 0;
        for (var y = 0; y < a.Height; y++)
        {
            for (var x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                {
                    count++;
                }
            }
        }

        return count;
    }

    #endregion
}
