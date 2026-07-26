#nullable enable
using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;

namespace FoliconTest;

/// <summary>
/// Tests for the first-run template picker: every offered template must be distinguishable
/// in the list and must produce its own distinct overlay when chosen.
/// </summary>
public class OverlayTemplatePickerTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly OverlayProvider _provider = new();
    private readonly OverlayTemplateProvider _templates;
    private readonly string _workDir;

    public OverlayTemplatePickerTests()
    {
        _templates = new OverlayTemplateProvider(_provider);
        _workDir = Path.Combine(Path.GetTempPath(), $"FoliconPicker_{Guid.NewGuid():N}");
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

    [Fact]
    public void EveryTemplate_HasANonEmptyDisplayName()
    {
        // A blank name renders as an empty card the author cannot identify.
        foreach (var template in _templates.GetTemplates())
        {
            Assert.False(string.IsNullOrWhiteSpace(template.DisplayName),
                $"Template '{template.Id}' has no display name.");
        }
    }

    [Fact]
    public void EveryTemplate_HasANonEmptyDescription()
    {
        foreach (var template in _templates.GetTemplates())
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Description),
                $"Template '{template.Id}' has no description.");
        }
    }

    [Fact]
    public void TemplateDisplayNames_AreDistinct()
    {
        var names = _templates.GetTemplates().Select(t => t.DisplayName).ToList();

        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void EachTemplate_CarriesItsOwnDefinition()
    {
        // If the picker handed every card the same definition, choosing any of them
        // would silently produce the same overlay.
        var templates = _templates.GetTemplates();

        foreach (var template in templates)
        {
            Assert.Equal(template.Id, template.Definition.Id);
        }

        var definitions = templates.Select(t => t.Definition).ToList();
        Assert.Equal(definitions.Count, definitions.Distinct().Count());
    }

    [Fact]
    public void ChoosingDifferentTemplates_ProducesDifferentGeometry()
    {
        // The concrete symptom to rule out: every card producing the same overlay.
        var results = new Dictionary<string, string>();

        foreach (var template in _templates.GetTemplates())
        {
            var folder = Path.Combine(_workDir, $"pick-{template.Id}");
            var document = _host.Invoke(() => _templates.CreateFromTemplate(
                template, folder, $"copy-{template.Id}", $"Copy of {template.DisplayName}", "Test"));

            var snapshot = document.CreateSnapshot();
            results[template.Id] =
                $"{snapshot.Poster.Margin}|{snapshot.RootMargin}|{string.Join(",", snapshot.LayerOrder ?? [])}";
        }

        // Built-ins genuinely differ in layout, so the fingerprints must not all collapse to one.
        Assert.True(results.Values.Distinct().Count() > 1,
            $"Every template produced identical geometry: {results.Values.First()}");
    }

    [Fact]
    public void ChoosingATemplate_CarriesThatTemplatesLayerConfiguration()
    {
        var templates = _templates.GetTemplates();

        // windows11 uses an opacity mask and no front layer; liaher uses both layers and a clip.
        var windows11 = templates.FirstOrDefault(t => t.Id == "windows11");
        var liaher = templates.FirstOrDefault(t => t.Id == "liaher");

        Assert.NotNull(windows11);
        Assert.NotNull(liaher);

        var fromWindows11 = _host.Invoke(() => _templates.CreateFromTemplate(
            windows11, Path.Combine(_workDir, "w11"), "copy-w11", "Copy W11", "Test"));
        var fromLiaher = _host.Invoke(() => _templates.CreateFromTemplate(
            liaher, Path.Combine(_workDir, "lia"), "copy-lia", "Copy Liaher", "Test"));

        Assert.False(fromWindows11.HasFrontLayer);
        Assert.NotNull(fromWindows11.PosterOpacityMaskPath);

        Assert.True(fromLiaher.HasFrontLayer);
        Assert.Null(fromLiaher.PosterOpacityMaskPath);
    }

    [Fact]
    public async Task EveryTemplate_RendersADistinguishableThumbnail()
    {
        // The picker is only usable if each card shows what its overlay looks like.
        using var renderer = new OverlayDesignerPreviewRenderer();
        var context = new OverlayPreviewContext { Rating = "8.4" };
        var fingerprints = new Dictionary<string, string>();

        foreach (var template in _templates.GetTemplates())
        {
            var image = await renderer.RenderNowAsync(template.Definition, context);

            Assert.True(image != null, $"Template '{template.Id}' produced no thumbnail.");
            Assert.Equal(256, image.PixelWidth);
            Assert.True(image.IsFrozen);

            fingerprints[template.Id] = Fingerprint(image);
        }

        // Identical thumbnails would leave the author picking blind even with artwork shown.
        Assert.True(fingerprints.Values.Distinct().Count() > 1,
            "Every template rendered the same thumbnail.");
    }

    private static string Fingerprint(System.Windows.Media.Imaging.BitmapSource source)
    {
        var stride = source.PixelWidth * 4;
        var buffer = new byte[stride * source.PixelHeight];
        source.CopyPixels(buffer, stride, 0);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(buffer));
    }

    [Fact]
    public void ChoosingATemplate_NamesTheNewOverlayAfterIt()
    {
        var template = _templates.GetTemplates().First(t => t.Id == "legacy");

        var document = _host.Invoke(() => _templates.CreateFromTemplate(
            template, Path.Combine(_workDir, "named"), "my-legacy", $"My {template.DisplayName}", "Test"));

        Assert.Equal("My Legacy", document.DisplayName);
        Assert.Equal("my-legacy", document.Id);
    }
}
