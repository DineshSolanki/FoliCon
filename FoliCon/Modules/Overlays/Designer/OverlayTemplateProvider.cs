#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// A template the designer can start a new overlay from.
/// </summary>
[Localizable(false)]
public sealed class OverlayTemplate(string id, string displayName, string description, PosterOverlayDefinition definition)
{
    /// <summary>Template identity (the source overlay's ID), not the new overlay's ID.</summary>
    public string Id { get; } = id;

    public string DisplayName { get; } = displayName;

    public string Description { get; } = description;

    /// <summary>The source definition. Cloned, never mutated.</summary>
    public PosterOverlayDefinition Definition { get; } = definition;
}

/// <summary>
/// Provides starting points for new overlays and materializes them into an editable
/// working folder.
///
/// Built-in overlays reference their images as pack URIs (<c>/Resources/poster_mockups/...</c>),
/// which are compiled into the assembly and have no file on disk. Cloning one therefore has to
/// extract each referenced image into the working folder and rewrite the path to a plain
/// relative filename, otherwise the clone would validate and export with unresolvable assets.
/// </summary>
[Localizable(false)]
public class OverlayTemplateProvider(IOverlayProvider overlayProvider)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IOverlayProvider _overlayProvider = overlayProvider
        ?? throw new ArgumentNullException(nameof(overlayProvider));

    /// <summary>
    /// Templates offered in the first-run picker: every built-in overlay, plus any
    /// installed community overlay the author can use as a starting point.
    /// </summary>
    public virtual IReadOnlyList<OverlayTemplate> GetTemplates() =>
        [.. _overlayProvider.GetAllOverlays()
            .Select(o => new OverlayTemplate(
                o.Id,
                o.DisplayName,
                string.IsNullOrWhiteSpace(o.Description)
                    ? (o.IsBuiltIn ? "Built-in overlay" : "Installed overlay")
                    : o.Description,
                o))];

    /// <summary>
    /// Clones a template into <paramref name="workingFolder"/> and returns a document ready
    /// for editing: assets extracted to loose files, image paths rewritten to relative names,
    /// and fresh identity applied.
    /// </summary>
    /// <param name="template">Template to clone.</param>
    /// <param name="workingFolder">Destination folder. Created if absent; must be writable.</param>
    /// <param name="newId">ID for the new overlay. Must not collide with a built-in or installed overlay.</param>
    /// <param name="newDisplayName">Display name for the new overlay.</param>
    /// <param name="author">Author name for the new overlay.</param>
    public virtual OverlayDesignerDocument CreateFromTemplate(
        OverlayTemplate template,
        string workingFolder,
        string newId,
        string newDisplayName,
        string author)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(newId);

        if (!IsIdAvailable(newId))
        {
            throw new InvalidOperationException(
                $"Overlay ID '{newId}' is already in use by a built-in or installed overlay.");
        }

        Directory.CreateDirectory(workingFolder);

        var document = OverlayDesignerDocument.FromDefinition(template.Definition, workingFolder);

        document.Id = newId;
        document.DisplayName = string.IsNullOrWhiteSpace(newDisplayName) ? newId : newDisplayName;
        document.Author = author ?? string.Empty;
        document.OverlayVersion = "1.0.0";
        document.CreatedAt = null;

        // Templates describe themselves; the clone is a different overlay.
        document.Description = string.Empty;
        document.Tags.Clear();

        MaterializeAssets(template, document, workingFolder);

        Logger.Info("Created overlay '{NewId}' from template '{TemplateId}' in {Folder}",
            newId, template.Id, workingFolder);

        return document;
    }

    /// <summary>
    /// True when no built-in or installed overlay already claims this ID.
    /// </summary>
    public virtual bool IsIdAvailable(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return !OverlayConstants.BuiltInOverlayIds.Contains(id, StringComparer.OrdinalIgnoreCase)
               && _overlayProvider.GetOverlayById(id) == null;
    }

    /// <summary>
    /// Suggests an available ID derived from a display name, appending a numeric suffix on collision.
    /// </summary>
    public virtual string SuggestId(string displayName)
    {
        var slug = Slugify(displayName);
        if (string.IsNullOrEmpty(slug))
        {
            slug = "my-overlay";
        }

        if (IsIdAvailable(slug))
        {
            return slug;
        }

        for (var suffix = 2; suffix < 1000; suffix++)
        {
            var candidate = $"{slug}-{suffix}";
            if (IsIdAvailable(candidate))
            {
                return candidate;
            }
        }

        return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
    }

    /// <summary>
    /// Copies or extracts every asset the template references into the working folder and
    /// rewrites the document's paths to relative filenames.
    /// </summary>
    private static void MaterializeAssets(OverlayTemplate template, OverlayDesignerDocument document, string workingFolder)
    {
        var sourceFolder = template.Definition.OverlayFolderPath;

        if (document.HasBaseLayer)
        {
            document.BaseLayerImagePath = MaterializeAsset(
                document.BaseLayerImagePath, sourceFolder, workingFolder, "base.png");
        }

        if (document.HasFrontLayer)
        {
            document.FrontLayerImagePath = MaterializeAsset(
                document.FrontLayerImagePath, sourceFolder, workingFolder, "front.png");
        }

        if (!string.IsNullOrWhiteSpace(document.PosterOpacityMaskPath))
        {
            document.PosterOpacityMaskPath = MaterializeAsset(
                document.PosterOpacityMaskPath, sourceFolder, workingFolder, "mask.png");
        }
    }

    /// <summary>
    /// Resolves one asset reference to a file inside the working folder.
    /// Pack-URI references are extracted from the assembly; relative references are copied
    /// from the template's own folder.
    /// </summary>
    /// <returns>The relative filename to store in the document.</returns>
    private static string MaterializeAsset(
        string assetPath,
        string? sourceFolder,
        string workingFolder,
        string preferredFileName)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return string.Empty;
        }

        var targetName = ResolveAvailableFileName(workingFolder, preferredFileName);
        var targetPath = Path.Combine(workingFolder, targetName);

        try
        {
            if (assetPath.StartsWith('/'))
            {
                ExtractPackResource(assetPath, targetPath);
            }
            else if (!string.IsNullOrWhiteSpace(sourceFolder))
            {
                var sourcePath = Path.Combine(sourceFolder, assetPath);
                if (!File.Exists(sourcePath))
                {
                    Logger.Warn("Template asset '{Asset}' not found at {Path}; leaving reference unresolved.",
                        assetPath, sourcePath);
                    return assetPath;
                }

                File.Copy(sourcePath, targetPath, overwrite: true);
            }
            else
            {
                Logger.Warn("Template asset '{Asset}' has no source folder; leaving reference unresolved.", assetPath);
                return assetPath;
            }

            return targetName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Logger.Error(ex, "Failed to materialize template asset '{Asset}'", assetPath);
            return assetPath;
        }
    }

    /// <summary>
    /// Reads a compiled WPF resource (<c>/Resources/...</c>) out of the assembly and writes it to disk.
    /// </summary>
    private static void ExtractPackResource(string resourcePath, string targetPath)
    {
        var packUri = new Uri($"pack://application:,,,/FoliCon;component{resourcePath}", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(packUri)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourcePath}");

        using var resourceStream = streamInfo.Stream;
        using var fileStream = File.Create(targetPath);
        resourceStream.CopyTo(fileStream);
    }

    /// <summary>
    /// Returns <paramref name="preferredFileName"/>, or a numbered variant when that name is taken,
    /// so two layers pointing at different images never overwrite each other.
    /// </summary>
    private static string ResolveAvailableFileName(string folder, string preferredFileName)
    {
        if (!File.Exists(Path.Combine(folder, preferredFileName)))
        {
            return preferredFileName;
        }

        var stem = Path.GetFileNameWithoutExtension(preferredFileName);
        var extension = Path.GetExtension(preferredFileName);

        for (var suffix = 2; suffix < 100; suffix++)
        {
            var candidate = $"{stem}{suffix}{extension}";
            if (!File.Exists(Path.Combine(folder, candidate)))
            {
                return candidate;
            }
        }

        return $"{stem}-{Guid.NewGuid().ToString("N")[..6]}{extension}";
    }

    /// <summary>
    /// Converts a display name into a schema-legal ID: lowercase, alphanumeric, hyphen-separated.
    /// </summary>
    internal static string Slugify(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(displayName.Length);
        var lastWasHyphen = false;

        foreach (var ch in displayName.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
