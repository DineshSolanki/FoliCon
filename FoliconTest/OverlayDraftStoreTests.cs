#nullable enable
using System.IO;
using FoliCon.Modules.Overlays.Designer;
using Thickness = System.Windows.Thickness;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayDraftStore"/>: atomic saves, asset copying, listing,
/// and corrupt-draft tolerance.
/// </summary>
public class OverlayDraftStoreTests : IDisposable
{
    private readonly string _root;
    private readonly string _sourceFolder;
    private readonly OverlayDraftStore _store;

    public OverlayDraftStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FoliconDrafts_{Guid.NewGuid():N}");
        _sourceFolder = Path.Combine(_root, "source");
        Directory.CreateDirectory(_sourceFolder);
        File.WriteAllBytes(Path.Combine(_sourceFolder, "base.png"), new byte[64]);

        _store = new OverlayDraftStore(Path.Combine(_root, "drafts"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
        GC.SuppressFinalize(this);
    }

    #region Saving

    [Fact]
    public void Save_WritesDefinitionAndAssets()
    {
        var path = _store.Save(CreateDocument());

        Assert.True(File.Exists(Path.Combine(path, "overlay.json")));
        Assert.True(File.Exists(Path.Combine(path, "base.png")));
    }

    [Fact]
    public void Save_RepointsTheDocumentAtTheDraftCopy()
    {
        // Later edits and exports must read the draft's own assets, not the original folder.
        var document = CreateDocument();

        var path = _store.Save(document);

        Assert.Equal(path, document.AssetFolderPath);
    }

    [Fact]
    public void Save_UsesTheOverlayIdAsTheFolderName()
    {
        var path = _store.Save(CreateDocument());

        Assert.Equal("draft-overlay", Path.GetFileName(path));
    }

    [Fact]
    public void Save_WithoutAnId_Throws()
    {
        var document = CreateDocument();
        document.Id = "";

        Assert.Throws<InvalidOperationException>(() => _store.Save(document));
    }

    [Fact]
    public void Save_Twice_ReplacesCleanlyWithoutLeftovers()
    {
        var document = CreateDocument();
        _store.Save(document);

        document.DisplayName = "Renamed";
        _store.Save(document);

        var draftFolders = Directory.GetDirectories(_store.DraftsRoot);
        Assert.Single(draftFolders);
        Assert.DoesNotContain(draftFolders, d => Path.GetFileName(d).StartsWith('.'));
    }

    [Fact]
    public void Save_OmitsMachineSpecificPathsFromTheDefinition()
    {
        // A saved draft is portable; an absolute asset path from this machine is not.
        var path = _store.Save(CreateDocument());

        var json = File.ReadAllText(Path.Combine(path, "overlay.json"));

        Assert.DoesNotContain(_sourceFolder, json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Save_TolerateseMissingAssets()
    {
        // A draft is allowed to be incomplete; export is where that becomes an error.
        var document = CreateDocument();
        document.BaseLayerImagePath = "never-created.png";

        var path = _store.Save(document);

        Assert.True(File.Exists(Path.Combine(path, "overlay.json")));
    }

    #endregion

    #region Round trip

    [Fact]
    public void SavedDraft_ReloadsThroughTheNormalLoader()
    {
        var path = _store.Save(CreateDocument());

        var reloaded = new OverlayPackageLoader().Load(Path.Combine(path, "overlay.json"));

        Assert.True(reloaded.Succeeded, reloaded.FailureReason);
        Assert.Equal("draft-overlay", reloaded.Document.Id);
    }

    [Fact]
    public void SavedDraft_PreservesGeometry()
    {
        var document = CreateDocument();
        document.PosterMargin = new Thickness(31, 42, 50, 19);
        document.RootMargin = new Thickness(0, 0, 0, -11);

        var path = _store.Save(document);
        var reloaded = new OverlayPackageLoader().Load(Path.Combine(path, "overlay.json")).Document!;

        Assert.Equal(document.PosterMargin, reloaded.PosterMargin);
        Assert.Equal(document.RootMargin, reloaded.RootMargin);
    }

    #endregion

    #region Listing

    [Fact]
    public void List_WithNoDraftsRoot_ReturnsEmpty()
    {
        var store = new OverlayDraftStore(Path.Combine(_root, "never-created"));

        Assert.Empty(store.List());
    }

    [Fact]
    public void List_ReturnsSavedDrafts()
    {
        _store.Save(CreateDocument("first", "First Overlay"));
        _store.Save(CreateDocument("second", "Second Overlay"));

        var drafts = _store.List();

        Assert.Equal(2, drafts.Count);
        Assert.Contains(drafts, d => d.DisplayName == "First Overlay");
        Assert.Contains(drafts, d => d.DisplayName == "Second Overlay");
    }

    [Fact]
    public void List_SkipsCorruptDraftsInsteadOfFailing()
    {
        // One unreadable folder must not hide every other draft.
        _store.Save(CreateDocument("good", "Good Draft"));

        var broken = Path.Combine(_store.DraftsRoot, "broken");
        Directory.CreateDirectory(broken);
        File.WriteAllText(Path.Combine(broken, "overlay.json"), "{ not json");

        var drafts = _store.List();

        Assert.Single(drafts);
        Assert.Equal("Good Draft", drafts[0].DisplayName);
    }

    [Fact]
    public void List_SkipsFoldersWithoutADefinition()
    {
        Directory.CreateDirectory(Path.Combine(_store.DraftsRoot, "not-a-draft"));

        Assert.Empty(_store.List());
    }

    [Fact]
    public void List_SkipsInterruptedSaveFolders()
    {
        _store.Save(CreateDocument());

        var leftover = Path.Combine(_store.DraftsRoot, ".crashed.draft-tmp");
        Directory.CreateDirectory(leftover);
        File.WriteAllText(Path.Combine(leftover, "overlay.json"), "{}");

        Assert.Single(_store.List());
    }

    #endregion

    #region Existence and deletion

    [Fact]
    public void Exists_ReflectsWhetherADraftWasSaved()
    {
        Assert.False(_store.Exists("draft-overlay"));

        _store.Save(CreateDocument());

        Assert.True(_store.Exists("draft-overlay"));
    }

    [Fact]
    public void Delete_RemovesTheDraft()
    {
        _store.Save(CreateDocument());

        _store.Delete("draft-overlay");

        Assert.False(_store.Exists("draft-overlay"));
    }

    [Fact]
    public void Delete_OfAMissingDraft_DoesNotThrow()
    {
        _store.Delete("never-existed");
    }

    [Fact]
    public void GetDraftDefinitionPath_PointsAtTheSavedFile()
    {
        _store.Save(CreateDocument());

        Assert.True(File.Exists(_store.GetDraftDefinitionPath("draft-overlay")));
    }

    #endregion

    private OverlayDesignerDocument CreateDocument(string id = "draft-overlay", string name = "Draft Overlay") =>
        new()
        {
            AssetFolderPath = _sourceFolder,
            Id = id,
            DisplayName = name,
            Author = "Test",
            OverlayVersion = "1.0.0",
            HasBaseLayer = true,
            BaseLayerImagePath = "base.png"
        };
}
