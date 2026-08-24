#nullable enable
namespace FoliCon.Modules.Overlays.Internal;

/// <summary>
/// Validates overlay.json files and overlay package folders.
/// </summary>
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
                string.Format(Lang.OverlayValidationSchemaTooNew,
                    definition.Id, definition.SchemaVersion, OverlayConstants.appSupportedSchemaVersion));
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
            result.AddError("id", Lang.OverlayValidationIdRequired);
        }
        else if (!IsValidId(definition.Id))
        {
            result.AddError("id",
                string.Format(Lang.OverlayValidationIdInvalidChars, definition.Id));
        }

        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            result.AddError("displayName", Lang.OverlayValidationDisplayNameRequired);
        }

        if (string.IsNullOrWhiteSpace(definition.Author))
        {
            result.AddWarning("author", Lang.OverlayValidationAuthorEmpty);
        }

        if (!string.IsNullOrWhiteSpace(definition.OverlayVersion) && !Version.TryParse(definition.OverlayVersion, out _))
        {
            result.AddError("overlayVersion",
                string.Format(Lang.OverlayValidationVersionInvalid, definition.OverlayVersion));
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
            result.AddError("rating.fontSize",
                string.Format(Lang.OverlayValidationRatingFontSizeInvalid, definition.Rating.FontSize));
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
                    string.Format(Lang.OverlayValidationLayerOrderUnknown,
                        key, string.Join(", ", KnownLayerKeys)));
                continue;
            }

            if (!seen.Add(key))
            {
                result.AddError("layerOrder", string.Format(Lang.OverlayValidationLayerOrderDuplicate, key));
            }
        }

        if (!seen.Contains("poster"))
        {
            result.AddWarning("layerOrder", Lang.OverlayValidationLayerOrderNoPoster);
        }
    }

    private static void ValidateLayer(string overlayFolder, LayerDefinition layer, string prefix, OverlayValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(layer.ImagePath))
        {
            result.AddError($"{prefix}.imagePath",
                string.Format(Lang.OverlayValidationLayerImagePathRequired, prefix));
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
                string.Format(Lang.OverlayValidationAssetPathNotRelative, field, assetPath));
            return;
        }

        var extension = Path.GetExtension(assetPath);
        if (!AllowedAssetExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            result.AddError(field, string.Format(Lang.OverlayValidationAssetNotPng, field, assetPath));
            return;
        }

        var fullPath = Path.Combine(overlayFolder, assetPath);
        if (!File.Exists(fullPath))
        {
            result.AddError(field, string.Format(Lang.OverlayValidationAssetNotFound, field, assetPath));
            return;
        }

        ValidateImageSize(fullPath, field, result);
    }

    /// <summary>
    /// Rejects absolute paths, rooted paths, and any path that escapes the overlay folder
    /// after canonicalization. Delegates to the shared containment check.
    /// </summary>
    private static bool IsSafeRelativePath(string overlayFolder, string relativePath) =>
        OverlayPackageIo.TryGetContainedPath(overlayFolder, relativePath, out _);

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
                        string.Format(Lang.OverlayValidationFilenameCaseCollision, existing, relative));
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
                    string.Format(Lang.OverlayValidationImageTooLarge,
                        field,
                        Path.GetFileName(imagePath),
                        fileInfo.Length / 1024.0 / 1024.0,
                        OverlayConstants.maxImageSizeBytes / 1024.0 / 1024.0));
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
            result.AddError(field, string.Format(Lang.OverlayValidationMarginInvalid, field, margin));
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var value))
            {
                result.AddError(field, string.Format(Lang.OverlayValidationMarginNonNumeric, field, part.Trim()));
            }
            else if (Math.Abs(value) > OverlayConstants.maxAbsoluteMarginValue)
            {
                result.AddWarning(field,
                    string.Format(Lang.OverlayValidationMarginOffCanvas, field, value));
            }
        }
    }

    private static void ValidateRotationOrigin(string origin, OverlayValidationResult result)
    {
        var parts = origin.Split(',');
        if (parts.Length != 2)
        {
            result.AddError("title.rotationOrigin",
                string.Format(Lang.OverlayValidationRotationOriginShape, origin));
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var val) || val is < 0.0 or > 1.0)
            {
                result.AddError("title.rotationOrigin",
                    string.Format(Lang.OverlayValidationRotationOriginRange, part.Trim()));
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
                    string.Format(Lang.OverlayValidationPackageTooLarge,
                        totalSize / 1024.0 / 1024.0,
                        OverlayConstants.maxOverlayPackageSizeBytes / 1024.0 / 1024.0));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Failed to calculate package size for {OverlayFolder}", overlayFolder);
        }
    }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$")]
    private static partial Regex IdRegex();

    /// <summary>
    /// Determines whether an overlay ID is a safe package-folder name.
    /// </summary>
    public static bool IsValidId(string id) => !string.IsNullOrWhiteSpace(id) && IdRegex().IsMatch(id);
}
