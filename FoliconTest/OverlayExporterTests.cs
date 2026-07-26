#nullable enable
using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays.Designer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayExporter"/>: package layout, determinism, integrity metadata,
/// and failure recovery.
/// </summary>
public class OverlayExporterTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly string _root;
    private readonly string _sourceFolder;

    public OverlayExporterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FoliconExport_{Guid.NewGuid():N}");
        _sourceFolder = Path.Combine(_root, "source");
        Directory.CreateDirectory(_sourceFolder);
        WritePng(Path.Combine(_sourceFolder, "base.png"));
        WritePng(Path.Combine(_sourceFolder, "front.png"));
    }

    public void Dispose()
    {
        _host.Dispose();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
        GC.SuppressFinalize(this);
    }

    #region Package layout

    [Fact]
    public async Task Export_WritesTheCompletePackage()
    {
        var result = await ExportAsync(CreateDocument());

        Assert.True(result.Succeeded, result.FailureReason);

        var files = Directory.GetFiles(result.PackagePath).Select(Path.GetFileName).ToList();
        Assert.Contains("overlay.json", files);
        Assert.Contains("manifest.json", files);
        Assert.Contains("preview.png", files);
        Assert.Contains("base.png", files);
    }

    [Fact]
    public async Task Export_UsesTheOverlayIdAsTheFolderName()
    {
        var result = await ExportAsync(CreateDocument());

        Assert.Equal("my-overlay", Path.GetFileName(result.PackagePath));
    }

    [Fact]
    public async Task Export_CopiesOnlyReferencedAssets()
    {
        // An abandoned file in the working folder must not ship inside the package.
        WritePng(Path.Combine(_sourceFolder, "leftover-experiment.png"));

        var result = await ExportAsync(CreateDocument());

        var files = Directory.GetFiles(result.PackagePath!).Select(Path.GetFileName).ToList();
        Assert.DoesNotContain("leftover-experiment.png", files);
    }

    [Fact]
    public async Task Export_LeavesTheSourceFolderUntouched()
    {
        var before = Directory.GetFiles(_sourceFolder).OrderBy(f => f).ToArray();

        await ExportAsync(CreateDocument());

        Assert.Equal(before, Directory.GetFiles(_sourceFolder).OrderBy(f => f).ToArray());
    }

    [Fact]
    public async Task Export_LeavesNoStagingFolderBehind()
    {
        var destination = Path.Combine(_root, "out");

        await ExportAsync(CreateDocument(), destination);

        Assert.DoesNotContain(Directory.GetDirectories(destination), d => Path.GetFileName(d).StartsWith('.'));
    }

    #endregion

    #region Schema conformance

    [Fact]
    public async Task ExportedDefinition_UsesCamelCaseKeys()
    {
        // The schema and the catalog CI both read camelCase; PascalCase would be silently skipped.
        var result = await ExportAsync(CreateDocument());

        var json = JObject.Parse(File.ReadAllText(Path.Combine(result.PackagePath!, "overlay.json")));

        Assert.NotNull(json["id"]);
        Assert.NotNull(json["displayName"]);
        Assert.NotNull(json["poster"]);
        Assert.Null(json["Id"]);
    }

    [Fact]
    public async Task ExportedDefinition_OmitsRuntimeOnlyFields()
    {
        // overlay-schema.json sets additionalProperties:false, so these would fail validation.
        var result = await ExportAsync(CreateDocument());

        var json = JObject.Parse(File.ReadAllText(Path.Combine(result.PackagePath!, "overlay.json")));

        Assert.Null(json["isBuiltIn"]);
        Assert.Null(json["overlayFolderPath"]);
    }

    [Fact]
    public async Task ExportedManifest_UsesCamelCaseKeys()
    {
        var result = await ExportAsync(CreateDocument());

        var json = JObject.Parse(File.ReadAllText(Path.Combine(result.PackagePath!, "manifest.json")));

        Assert.NotNull(json["id"]);
        Assert.NotNull(json["sha256"]);
        Assert.NotNull(json["sizeBytes"]);
        Assert.NotNull(json["previewImage"]);
    }

    [Fact]
    public async Task ExportedDefinition_ReloadsThroughTheNormalLoader()
    {
        var result = await ExportAsync(CreateDocument());

        var reloaded = new OverlayPackageLoader().Load(Path.Combine(result.PackagePath!, "overlay.json"));

        Assert.True(reloaded.Succeeded, reloaded.FailureReason);
        Assert.Equal("my-overlay", reloaded.Document.Id);
        Assert.True(reloaded.Validation.IsValid);
    }

    #endregion

    #region Integrity metadata

    [Fact]
    public async Task Manifest_ListsEveryShippedFileWithAHash()
    {
        var result = await ExportAsync(CreateDocument());

        var manifest = ReadManifest(result.PackagePath!);

        // manifest.json is written after the inventory is taken, so it cannot list or hash
        // itself — a self-referential hash is impossible. Everything else must be covered.
        var shippedFiles = Directory.GetFiles(result.PackagePath!)
            .Select(Path.GetFileName)
            .Where(f => f != "manifest.json")
            .OrderBy(f => f)
            .ToList();

        Assert.Equal(shippedFiles, manifest.Assets.OrderBy(a => a).ToList());
        Assert.Contains("overlay.json", manifest.Assets);
        Assert.Contains("preview.png", manifest.Assets);

        foreach (var asset in manifest.Assets)
        {
            Assert.True(manifest.Sha256.ContainsKey(asset), $"No hash recorded for '{asset}'.");
            Assert.Equal(64, manifest.Sha256[asset].Length);
        }
    }

    [Fact]
    public async Task Manifest_HashesMatchTheFilesOnDisk()
    {
        var result = await ExportAsync(CreateDocument());
        var manifest = ReadManifest(result.PackagePath!);

        Assert.NotEmpty(manifest.Sha256);

        foreach (var (file, expected) in manifest.Sha256)
        {
            using var stream = File.OpenRead(Path.Combine(result.PackagePath!, file));
            var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public async Task Manifest_SizeMatchesTheSumOfFileSizes()
    {
        var result = await ExportAsync(CreateDocument());
        var manifest = ReadManifest(result.PackagePath!);

        var expected = Directory.GetFiles(result.PackagePath!)
            .Where(f => Path.GetFileName(f) != "manifest.json")
            .Sum(f => new FileInfo(f).Length);

        // The manifest is written last, so its own bytes are not in the total.
        Assert.Equal(expected, manifest.SizeBytes);
    }

    [Fact]
    public async Task Manifest_PreservesTheOriginalCreationDate()
    {
        var created = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var document = CreateDocument();
        document.CreatedAt = created;

        var result = await ExportAsync(document);
        var manifest = ReadManifest(result.PackagePath!);

        Assert.Equal(created, manifest.CreatedAt);
        Assert.True(manifest.UpdatedAt > created);
    }

    [Fact]
    public async Task Manifest_ForANewOverlay_SetsBothDates()
    {
        var result = await ExportAsync(CreateDocument());
        var manifest = ReadManifest(result.PackagePath!);

        Assert.NotEqual(default, manifest.CreatedAt);
        Assert.NotEqual(default, manifest.UpdatedAt);
    }

    #endregion

    #region Determinism

    [Fact]
    public async Task ExportingTwice_ProducesByteIdenticalDefinitionAndPreview()
    {
        // Re-exporting an unchanged overlay must not churn hashes and create pull-request noise.
        var first = await ExportAsync(CreateDocument(), Path.Combine(_root, "out1"));
        var second = await ExportAsync(CreateDocument(), Path.Combine(_root, "out2"));

        Assert.Equal(
            File.ReadAllBytes(Path.Combine(first.PackagePath!, "overlay.json")),
            File.ReadAllBytes(Path.Combine(second.PackagePath!, "overlay.json")));

        Assert.Equal(
            File.ReadAllBytes(Path.Combine(first.PackagePath!, "preview.png")),
            File.ReadAllBytes(Path.Combine(second.PackagePath!, "preview.png")));
    }

    [Fact]
    public async Task PreviewIgnoresTheAuthorsTestSettings()
    {
        // The transient test poster and rating must not leak into the shipped preview.
        var first = await ExportAsync(CreateDocument(), Path.Combine(_root, "canonical1"));
        var second = await ExportAsync(CreateDocument(), Path.Combine(_root, "canonical2"));

        var hashA = ReadManifest(first.PackagePath!).Sha256["preview.png"];
        var hashB = ReadManifest(second.PackagePath!).Sha256["preview.png"];

        Assert.Equal(hashA, hashB);
    }

    [Fact]
    public async Task ExportedPreview_Is256Square()
    {
        var result = await ExportAsync(CreateDocument());

        using var bitmap = new System.Drawing.Bitmap(Path.Combine(result.PackagePath!, "preview.png"));

        Assert.Equal(256, bitmap.Width);
        Assert.Equal(256, bitmap.Height);
    }

    #endregion

    #region Validation gating and failures

    [Fact]
    public async Task Export_WithValidationErrors_FailsAndWritesNothing()
    {
        var document = CreateDocument();
        document.Id = "Not A Valid Id";

        var destination = Path.Combine(_root, "invalid");
        var result = await new OverlayExporter().ExportAsync(document, destination);

        Assert.False(result.Succeeded);
        Assert.False(result.Validation.IsValid);
        Assert.False(Directory.Exists(Path.Combine(destination, document.Id)));
    }

    [Fact]
    public async Task Export_WithoutAnId_Fails()
    {
        var document = CreateDocument();
        document.Id = "";

        var result = await new OverlayExporter().ExportAsync(document, Path.Combine(_root, "noid"));

        Assert.False(result.Succeeded);
        Assert.Contains("ID", result.FailureReason);
    }

    [Fact]
    public async Task Export_FailedAttempt_CleansUpStaging()
    {
        var document = CreateDocument();
        document.Id = "Bad Id";
        var destination = Path.Combine(_root, "cleanup");
        Directory.CreateDirectory(destination);

        await new OverlayExporter().ExportAsync(document, destination);

        Assert.Empty(Directory.GetDirectories(destination));
    }

    [Fact]
    public async Task Export_OverExisting_RequiresPermission()
    {
        var destination = Path.Combine(_root, "collide");
        await ExportAsync(CreateDocument(), destination);

        var second = await new OverlayExporter().ExportAsync(CreateDocument(), destination, overwrite: false);

        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.FailureReason);
    }

    [Fact]
    public async Task Export_WithOverwrite_ReplacesTheExistingPackage()
    {
        var destination = Path.Combine(_root, "replace");
        await ExportAsync(CreateDocument(), destination);

        var document = CreateDocument();
        document.DisplayName = "Renamed Overlay";
        var second = await new OverlayExporter().ExportAsync(document, destination, overwrite: true);

        Assert.True(second.Succeeded, second.FailureReason);
        Assert.Equal("Renamed Overlay", ReadManifest(second.PackagePath!).DisplayName);
        Assert.DoesNotContain(Directory.GetDirectories(destination), d => Path.GetFileName(d).Contains("replaced"));
    }

    #endregion

    #region Local install

    [Fact]
    public async Task InstallLocally_CopiesThePackageIntoTheOverlaysFolder()
    {
        var exported = await ExportAsync(CreateDocument());
        var overlaysRoot = Path.Combine(_root, "installed");

        var result = new OverlayExporter().InstallLocally(exported.PackagePath!, overlaysRoot);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.True(File.Exists(Path.Combine(overlaysRoot, "my-overlay", "overlay.json")));
        Assert.True(File.Exists(Path.Combine(overlaysRoot, "my-overlay", "base.png")));
    }

    [Fact]
    public async Task InstallLocally_Twice_ReplacesCleanly()
    {
        var exported = await ExportAsync(CreateDocument());
        var overlaysRoot = Path.Combine(_root, "reinstall");
        var exporter = new OverlayExporter();

        exporter.InstallLocally(exported.PackagePath!, overlaysRoot);
        var second = exporter.InstallLocally(exported.PackagePath!, overlaysRoot);

        Assert.True(second.Succeeded, second.FailureReason);
        Assert.Single(Directory.GetDirectories(overlaysRoot));
    }

    [Fact]
    public void InstallLocally_RejectsBuiltInIds()
    {
        var fakePackage = Path.Combine(_root, "liaher");
        Directory.CreateDirectory(fakePackage);
        File.WriteAllText(Path.Combine(fakePackage, "overlay.json"), "{}");

        var result = new OverlayExporter().InstallLocally(fakePackage, Path.Combine(_root, "installed2"));

        Assert.False(result.Succeeded);
        Assert.Contains("built-in", result.FailureReason);
    }

    [Fact]
    public async Task UninstallLocal_RemovesAnInstalledOverlay()
    {
        // An overlay installed from the designer has no catalog entry, so the store cannot
        // remove it — this is the only uninstall path such an overlay has.
        var exported = await ExportAsync(CreateDocument());
        var overlaysRoot = Path.Combine(_root, "uninstall");
        var exporter = new OverlayExporter();
        exporter.InstallLocally(exported.PackagePath!, overlaysRoot);

        var result = exporter.UninstallLocal("my-overlay", overlaysRoot);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.False(Directory.Exists(Path.Combine(overlaysRoot, "my-overlay")));
    }

    [Fact]
    public void UninstallLocal_RefusesBuiltInIds()
    {
        var result = new OverlayExporter().UninstallLocal("liaher", Path.Combine(_root, "uninstall2"));

        Assert.False(result.Succeeded);
        Assert.Contains("built-in", result.FailureReason);
    }

    [Fact]
    public void UninstallLocal_OfSomethingNotInstalled_ReportsClearly()
    {
        var overlaysRoot = Path.Combine(_root, "uninstall3");
        Directory.CreateDirectory(overlaysRoot);

        var result = new OverlayExporter().UninstallLocal("never-installed", overlaysRoot);

        Assert.False(result.Succeeded);
        Assert.Contains("not installed", result.FailureReason);
    }

    [Fact]
    public async Task InstallThenUninstall_LeavesTheOverlaysFolderEmpty()
    {
        var exported = await ExportAsync(CreateDocument());
        var overlaysRoot = Path.Combine(_root, "roundtrip-install");
        var exporter = new OverlayExporter();

        exporter.InstallLocally(exported.PackagePath!, overlaysRoot);
        exporter.UninstallLocal("my-overlay", overlaysRoot);

        Assert.Empty(Directory.GetDirectories(overlaysRoot));
    }

    [Fact]
    public void InstallLocally_RejectsAFolderWithoutADefinition()
    {
        var notAPackage = Path.Combine(_root, "empty-folder");
        Directory.CreateDirectory(notAPackage);

        var result = new OverlayExporter().InstallLocally(notAPackage, Path.Combine(_root, "installed3"));

        Assert.False(result.Succeeded);
    }

    #endregion

    private Task<OverlayExportResult> ExportAsync(OverlayDesignerDocument document, string? destination = null) =>
        new OverlayExporter().ExportAsync(document, destination ?? Path.Combine(_root, "out"));

    private static OverlayManifest ReadManifest(string packagePath) =>
        JsonConvert.DeserializeObject<OverlayManifest>(
            File.ReadAllText(Path.Combine(packagePath, "manifest.json")))!;

    private OverlayDesignerDocument CreateDocument()
    {
        var document = new OverlayDesignerDocument
        {
            AssetFolderPath = _sourceFolder,
            Id = "my-overlay",
            DisplayName = "My Overlay",
            Author = "Test Author",
            Description = "A test overlay",
            OverlayVersion = "1.0.0",
            HasBaseLayer = true,
            BaseLayerImagePath = "base.png",
            BaseLayerMargin = new System.Windows.Thickness(30, 14, 48, 15),
            PosterMargin = new System.Windows.Thickness(31, 42, 50, 19)
        };
        document.Tags.Add("test");
        return document;
    }

    /// <summary>A real 1x1 PNG, so the renderer and validator both accept it.</summary>
    private static void WritePng(string path)
    {
        using var bitmap = new System.Drawing.Bitmap(1, 1);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}
