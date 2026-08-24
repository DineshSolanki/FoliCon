#nullable enable
using JsonException = Newtonsoft.Json.JsonException;

namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// A saved work-in-progress overlay.
/// </summary>
public sealed class OverlayDraftInfo(string draftId, string displayName, string folderPath, DateTime savedAt)
{
    /// <summary>Folder name under the drafts root. Derived from the overlay ID.</summary>
    public string DraftId { get; } = draftId;

    public string DisplayName { get; } = displayName;

    public string FolderPath { get; } = folderPath;

    public DateTime SavedAt { get; } = savedAt;
}

/// <summary>
/// Persists in-progress designer work under <c>%AppData%/FoliCon/OverlayDrafts/{id}</c>.
///
/// A draft is a normal overlay folder — <c>overlay.json</c> plus its assets — so it can be
/// reopened by the same loader that reads finished packages, and so a half-finished draft is
/// never a special format that only the designer understands.
///
/// Saves are atomic: the draft is written to a temporary folder and swapped into place only
/// once complete, so interrupting a save cannot corrupt the previous one.
///
/// Command history is deliberately not persisted; reopening a draft starts a fresh undo stack
/// with the saved state as its clean baseline.
/// </summary>
public class OverlayDraftStore(string? draftsRoot = null)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly string _draftsRoot = draftsRoot ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FoliCon", OverlayConstants.draftsFolder);

    public string DraftsRoot => _draftsRoot;

    /// <summary>
    /// Saves <paramref name="document"/> as a draft and returns the folder it now lives in.
    /// The document's asset folder is repointed at the saved copy, so subsequent edits and
    /// exports read the draft's own assets rather than the original source folder.
    /// </summary>
    public virtual string Save(OverlayDesignerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new InvalidOperationException(Lang.OverlayDraftIdRequired);
        }

        var finalPath = Path.Combine(_draftsRoot, document.Id);
        var stagingPath = Path.Combine(_draftsRoot, $".{document.Id}.draft-tmp");

        Directory.CreateDirectory(_draftsRoot);
        SafeDelete(stagingPath);
        Directory.CreateDirectory(stagingPath);

        try
        {
            CopyReferencedAssets(document, stagingPath);

            var definition = document.CreateSnapshot();
            definition.OverlayFolderPath = null;
            File.WriteAllText(
                Path.Combine(stagingPath, OverlayConstants.overlayJsonFileName),
                OverlayPackageSerializer.SerializeDefinition(definition));

            Commit(stagingPath, finalPath);

            // Future edits and exports should read the draft's copies, not the original source.
            document.AssetFolderPath = finalPath;

            Logger.Info("Saved draft '{Id}' to {Path}", document.Id, finalPath);
            return finalPath;
        }
        catch
        {
            SafeDelete(stagingPath);
            throw;
        }
    }

    /// <summary>
    /// Lists saved drafts, newest first. Unreadable drafts are skipped rather than failing
    /// the whole listing — one corrupt folder should not hide the others.
    /// </summary>
    public virtual IReadOnlyList<OverlayDraftInfo> List()
    {
        if (!Directory.Exists(_draftsRoot))
        {
            return [];
        }

        var drafts = new List<OverlayDraftInfo>();

        foreach (var folder in Directory.GetDirectories(_draftsRoot))
        {
            var name = Path.GetFileName(folder);

            // Interrupted saves leave these behind.
            if (name.StartsWith('.'))
            {
                continue;
            }

            var jsonPath = Path.Combine(folder, OverlayConstants.overlayJsonFileName);
            if (!File.Exists(jsonPath))
            {
                continue;
            }

            try
            {
                var definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(File.ReadAllText(jsonPath));
                drafts.Add(new OverlayDraftInfo(
                    name,
                    string.IsNullOrWhiteSpace(definition?.DisplayName) ? name : definition.DisplayName,
                    folder,
                    File.GetLastWriteTimeUtc(jsonPath)));
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                Logger.Warn(ex, "Skipping unreadable draft at {Folder}", folder);
            }
        }

        return [.. drafts.OrderByDescending(d => d.SavedAt)];
    }

    /// <summary>True when a draft folder exists for this overlay ID.</summary>
    public virtual bool Exists(string draftId) =>
        !string.IsNullOrWhiteSpace(draftId)
        && File.Exists(Path.Combine(_draftsRoot, draftId, OverlayConstants.overlayJsonFileName));

    /// <summary>Path to a draft's <c>overlay.json</c>, for handing to the package loader.</summary>
    public virtual string GetDraftDefinitionPath(string draftId) =>
        Path.Combine(_draftsRoot, draftId, OverlayConstants.overlayJsonFileName);

    public virtual void Delete(string draftId)
    {
        if (string.IsNullOrWhiteSpace(draftId))
        {
            return;
        }

        SafeDelete(Path.Combine(_draftsRoot, draftId));
        Logger.Info("Deleted draft '{Id}'", draftId);
    }

    private static void CopyReferencedAssets(OverlayDesignerDocument document, string stagingPath) =>
        Internal.OverlayPackageIo.CopyReferencedAssets(document, stagingPath, requireAll: false);

    /// <summary>Swaps the staged draft into place, keeping the previous copy until it succeeds.</summary>
    private static void Commit(string stagingPath, string finalPath) =>
        Internal.OverlayPackageIo.Commit(stagingPath, finalPath);

    private static void SafeDelete(string path) => Internal.OverlayPackageIo.SafeDelete(path);
}
