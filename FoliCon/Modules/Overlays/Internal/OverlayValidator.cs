#nullable enable
namespace FoliCon.Modules.Overlays.Internal;

/// <summary>
/// Validates overlay.json files and overlay package folders.
/// </summary>
[Localizable(false)]
public static partial class OverlayValidator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string[] AllowedAssetExtensions = [".png"];

    private static readonly string[] KnownLayerKeys = ["base", "poster", "front", "rating", "title"];

    /// <summary>
    /// Validates an overlay definition and its assets in the given folder.
    /// Returns a list of validation errors (empty if valid).
    /// </summary>
    /// <remarks>
    /// Compatibility entry point for the provider and repository service. New callers that
    /// need warnings or field identity should use <see cref="ValidateDetailed"/>.
    /// </remarks>
    public static List<string> Validate(string overlayFolder, PosterOverlayDefinition definition) =>
        ValidateDetailed(overlayFolder, definition).ToErrorMessages();

    /// <summary>
    /// Validates an overlay definition and its assets, returning structured errors and warnings
    /// with the schema field each finding belongs to.
    /// </summary>
    public static OverlayValidationResult ValidateDetailed(string overlayFolder, PosterOverlayDefinition definition)
    {
        var result = new OverlayValidationResult();

        // Schema version check — don't continue, the app doesn't understand this schema.
        if (definition.SchemaVersion > OverlayConstants.appSupportedSchemaVersion)
        {
            result.AddError("schemaVersion",
                $"Overlay '{definition.Id}' requires schema v{definition.SchemaVersion}, " +
                $"app supports v{OverlayConstants.appSupportedSchemaVersion}. Skipping.");
            return result;
        }

        ValidateMetadata(definition, result);
        ValidateLayers(overlayFolder, definition, result);
        ValidatePosterConfig(overlayFolder, definition, result);
        ValidateRatingConfig(definition, result);
        ValidateTitleConfig(definition, result);
        ValidateLayerOrder(definition, result);
        ValidatePackageSize(overlayFolder, result);
        ValidateAssetFilenameCollisions(overlayFolder, result);

        return result;
    }

    private static void ValidateMetadata(PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            result.AddError("id", "Overlay 'id' is required.");
        }
        else if (!IsValidId(definition.Id))
        {
            result.AddError("id",
                $"Overlay 'id' '{definition.Id}' contains invalid characters. Use lowercase alphanumeric and hyphens.");
        }

        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            result.AddError("displayName", "Overlay 'displayName' is required.");
        }

        if (string.IsNullOrWhiteSpace(definition.Author))
        {
            result.AddWarning("author", "Overlay 'author' is empty. Store submissions should name their author.");
        }

        if (!string.IsNullOrWhiteSpace(definition.OverlayVersion) && !Version.TryParse(definition.OverlayVersion, out _))
        {
            result.AddError("overlayVersion",
                $"Overlay 'overlayVersion' '{definition.OverlayVersion}' is not a valid version (expected e.g. '1.0.0').");
        }
    }

    private static void ValidateLayers(string overlayFolder, PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        if (definition.BaseLayer != null)
        {
            ValidateLayer(overlayFolder, definition.BaseLayer, "baseLayer", result);
        }

        if (definition.FrontLayer != null)
        {
            ValidateLayer(overlayFolder, definition.FrontLayer, "frontLayer", result);
        }
    }

    private static void ValidatePosterConfig(string overlayFolder, PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        ValidateMargin(definition.Poster.Margin, "poster.margin", result);

        if (definition.Poster.OpacityMaskPath == null)
        {
            return;
        }

        ValidateAssetReference(overlayFolder, definition.Poster.OpacityMaskPath, "poster.opacityMaskPath", result);
    }

    private static void ValidateRatingConfig(PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        ValidateMargin(definition.Rating.ShieldMargin, "rating.shieldMargin", result);
        ValidateMargin(definition.Rating.TextMargin, "rating.textMargin", result);

        if (definition.Rating.FontSize <= 0)
        {
            result.AddError("rating.fontSize", $"rating.fontSize must be greater than 0 (got {definition.Rating.FontSize}).");
        }
    }

    private static void ValidateTitleConfig(PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        if (definition.Title.IsVisible)
        {
            ValidateMargin(definition.Title.Margin, "title.margin", result);
        }

        if (!string.IsNullOrWhiteSpace(definition.Title.RotationOrigin))
        {
            ValidateRotationOrigin(definition.Title.RotationOrigin, result);
        }
    }

    private static void ValidateLayerOrder(PosterOverlayDefinition definition, OverlayValidationResult result)
    {
        if (definition.LayerOrder is not { Length: > 0 })
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in definition.LayerOrder)
        {
            if (!KnownLayerKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                result.AddError("layerOrder",
                    $"layerOrder contains unknown layer '{key}'. Valid values: {string.Join(", ", KnownLayerKeys)}.");
                continue;
            }

            if (!seen.Add(key))
            {
                result.AddError("layerOrder", $"layerOrder lists '{key}' more than once.");
            }
        }

        if (!seen.Contains("poster"))
        {
            result.AddWarning("layerOrder", "layerOrder does not include 'poster'; the media poster will not be drawn.");
        }
    }

    private static void ValidateLayer(string overlayFolder, LayerDefinition layer, string prefix, OverlayValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(layer.ImagePath))
        {
            result.AddError($"{prefix}.imagePath", $"{prefix}.imagePath is required when layer is defined.");
            return;
        }

        ValidateAssetReference(overlayFolder, layer.ImagePath, $"{prefix}.imagePath", result);
        ValidateMargin(layer.Margin, $"{prefix}.margin", result);
    }

    /// <summary>
    /// Validates that an asset reference is a safe relative path inside the overlay folder,
    /// points at an allowed image format, exists, and fits the per-image size limit.
    /// Built-in overlays use absolute pack-style paths (leading '/'), which are skipped.
    /// </summary>
    private static void ValidateAssetReference(string overlayFolder, string assetPath, string field, OverlayValidationResult result)
    {
        // Built-in overlays reference embedded resources with a leading slash; those are
        // resolved by DynamicPosterIcon against pack URIs and never touch the overlay folder.
        if (assetPath.StartsWith('/'))
        {
            return;
        }

        if (!IsSafeRelativePath(overlayFolder, assetPath))
        {
            result.AddError(field,
                $"{field} '{assetPath}' must be a relative path inside the overlay folder (no absolute paths, no '..').");
            return;
        }

        var extension = Path.GetExtension(assetPath);
        if (!AllowedAssetExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            result.AddError(field, $"{field} '{assetPath}' must be a PNG file.");
            return;
        }

        var fullPath = Path.Combine(overlayFolder, assetPath);
        if (!File.Exists(fullPath))
        {
            result.AddError(field, $"{field} '{assetPath}' file not found.");
            return;
        }

        ValidateImageSize(fullPath, field, result);
    }

    /// <summary>
    /// Rejects absolute paths, rooted paths, and any path that escapes the overlay folder
    /// after canonicalization.
    /// </summary>
    private static bool IsSafeRelativePath(string overlayFolder, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        try
        {
            var folderFull = Path.GetFullPath(overlayFolder);
            var assetFull = Path.GetFullPath(Path.Combine(folderFull, relativePath));

            // Compare with a trailing separator so "C:\Overlays\ab" cannot pass as inside "C:\Overlays\a".
            var folderPrefix = folderFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;

            return assetFull.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// Flags filenames that differ only by case. These work on Windows but collide when the
    /// package is checked into a case-sensitive repository or unpacked on Linux CI.
    /// </summary>
    private static void ValidateAssetFilenameCollisions(string overlayFolder, OverlayValidationResult result)
    {
        try
        {
            if (!Directory.Exists(overlayFolder))
            {
                return;
            }

            var byLowerName = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(overlayFolder, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(overlayFolder, file);
                var key = relative.ToLowerInvariant();

                if (byLowerName.TryGetValue(key, out var existing))
                {
                    result.AddError("assets",
                        $"Files '{existing}' and '{relative}' differ only by case and will collide on case-sensitive filesystems.");
                    continue;
                }

                byLowerName[key] = relative;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Failed to check filename collisions for {OverlayFolder}", overlayFolder);
        }
    }

    private static void ValidateImageSize(string imagePath, string field, OverlayValidationResult result)
    {
        try
        {
            var fileInfo = new FileInfo(imagePath);
            if (fileInfo.Length > OverlayConstants.maxImageSizeBytes)
            {
                result.AddError(field,
                    $"{field} image '{Path.GetFileName(imagePath)}' exceeds maximum size " +
                    $"({fileInfo.Length / 1024.0 / 1024.0:F1} MB > {OverlayConstants.maxImageSizeBytes / 1024.0 / 1024.0:F0} MB).");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Failed to check image size for {ImagePath}", imagePath);
        }
    }

    private static void ValidateMargin(string margin, string field, OverlayValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(margin))
        {
            return;
        }

        var parts = margin.Split(',');
        if (parts.Length is < 1 or > 4)
        {
            result.AddError(field, $"{field} '{margin}' is not a valid Thickness string. Expected 1-4 numeric values.");
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var value))
            {
                result.AddError(field, $"{field} contains non-numeric value '{part.Trim()}'.");
            }
            else if (Math.Abs(value) > OverlayConstants.maxAbsoluteMarginValue)
            {
                result.AddWarning(field,
                    $"{field} value {value:0.##} is far outside the canvas and may render nothing visible.");
            }
        }
    }

    private static void ValidateRotationOrigin(string origin, OverlayValidationResult result)
    {
        var parts = origin.Split(',');
        if (parts.Length != 2)
        {
            result.AddError("title.rotationOrigin", $"title.rotationOrigin '{origin}' must be 'x,y' with exactly 2 values.");
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var val) || val is < 0.0 or > 1.0)
            {
                result.AddError("title.rotationOrigin", $"title.rotationOrigin value '{part.Trim()}' must be between 0.0 and 1.0.");
            }
        }
    }

    private static void ValidatePackageSize(string overlayFolder, OverlayValidationResult result)
    {
        try
        {
            if (!Directory.Exists(overlayFolder))
            {
                return;
            }

            var totalSize = Directory.GetFiles(overlayFolder, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
            if (totalSize > OverlayConstants.maxOverlayPackageSizeBytes)
            {
                result.AddError("assets",
                    $"Overlay folder exceeds maximum total size " +
                    $"({totalSize / 1024.0 / 1024.0:F1} MB > {OverlayConstants.maxOverlayPackageSizeBytes / 1024.0 / 1024.0:F0} MB).");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Failed to calculate package size for {OverlayFolder}", overlayFolder);
        }
    }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$")]
    private static partial Regex IdRegex();

    private static bool IsValidId(string id) => IdRegex().IsMatch(id);
}
