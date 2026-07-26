#nullable enable
using JsonException = Newtonsoft.Json.JsonException;

namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Outcome of opening a local overlay package for editing.
/// </summary>
[Localizable(false)]
public sealed class OverlayPackageLoadResult
{
    private OverlayPackageLoadResult(OverlayDesignerDocument? document, OverlayValidationResult validation, string? failureReason)
    {
        Document = document;
        Validation = validation;
        FailureReason = failureReason;
    }

    /// <summary>The loaded document, or null when the package could not be read at all.</summary>
    public OverlayDesignerDocument? Document { get; }

    /// <summary>
    /// Validation findings for the package as it exists on disk. A package with errors still
    /// loads — the author opened it to fix it — but cannot be exported until they are resolved.
    /// </summary>
    public OverlayValidationResult Validation { get; }

    /// <summary>Why loading failed outright (missing file, unreadable JSON, unsupported schema).</summary>
    public string? FailureReason { get; }

    [MemberNotNullWhen(true, nameof(Document))]
    public bool Succeeded => Document != null;

    public static OverlayPackageLoadResult Success(OverlayDesignerDocument document, OverlayValidationResult validation) =>
        new(document, validation, null);

    public static OverlayPackageLoadResult Failure(string reason) =>
        new(null, new OverlayValidationResult(), reason);
}

/// <summary>
/// Reads a local overlay package into an editable document.
///
/// Loading is strictly read-only: the source folder is never written to. The designer edits
/// the in-memory document and only touches disk on draft save or export.
/// </summary>
[Localizable(false)]
public class OverlayPackageLoader
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Loads the package containing <paramref name="overlayJsonPath"/>.
    /// </summary>
    public virtual OverlayPackageLoadResult Load(string overlayJsonPath)
    {
        if (string.IsNullOrWhiteSpace(overlayJsonPath) || !File.Exists(overlayJsonPath))
        {
            return OverlayPackageLoadResult.Failure($"Overlay file not found: {overlayJsonPath}");
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(overlayJsonPath));
        if (string.IsNullOrEmpty(folder))
        {
            return OverlayPackageLoadResult.Failure($"Could not determine the overlay folder for '{overlayJsonPath}'.");
        }

        PosterOverlayDefinition? definition;
        try
        {
            var json = File.ReadAllText(overlayJsonPath);
            definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(json);
        }
        catch (JsonException ex)
        {
            Logger.Warn(ex, "Malformed overlay.json at {Path}", overlayJsonPath);
            return OverlayPackageLoadResult.Failure($"'{Path.GetFileName(overlayJsonPath)}' is not valid JSON: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Could not read overlay.json at {Path}", overlayJsonPath);
            return OverlayPackageLoadResult.Failure($"Could not read '{overlayJsonPath}': {ex.Message}");
        }

        if (definition == null)
        {
            return OverlayPackageLoadResult.Failure($"'{Path.GetFileName(overlayJsonPath)}' did not contain an overlay definition.");
        }

        if (definition.SchemaVersion > OverlayConstants.appSupportedSchemaVersion)
        {
            return OverlayPackageLoadResult.Failure(
                $"This overlay requires schema v{definition.SchemaVersion}, but this version of FoliCon supports v{OverlayConstants.appSupportedSchemaVersion}. Update FoliCon to edit it.");
        }

        definition.OverlayFolderPath = folder;

        var document = OverlayDesignerDocument.FromDefinition(definition, folder);
        document.CreatedAt = ReadCreatedAt(folder);

        var validation = Internal.OverlayValidator.ValidateDetailed(folder, definition);

        Logger.Info("Loaded overlay package '{Id}' from {Folder} ({Errors} errors, {Warnings} warnings)",
            document.Id, folder, validation.ErrorCount, validation.WarningCount);

        return OverlayPackageLoadResult.Success(document, validation);
    }

    /// <summary>
    /// Reads the original creation date from a sibling manifest so re-exporting an existing
    /// package preserves it. Returns null when there is no manifest to read it from.
    /// </summary>
    private static DateTime? ReadCreatedAt(string folder)
    {
        var manifestPath = Path.Combine(folder, OverlayConstants.manifestFileName);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var manifest = JsonConvert.DeserializeObject<OverlayManifest>(File.ReadAllText(manifestPath));
            return manifest?.CreatedAt == default ? null : manifest?.CreatedAt;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Logger.Debug(ex, "Could not read manifest creation date from {Path}", manifestPath);
            return null;
        }
    }
}
