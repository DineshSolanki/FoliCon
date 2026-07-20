namespace FoliCon.Modules.Overlays.Internal;

/// <summary>
/// Validates overlay.json files and overlay package folders.
/// </summary>
[Localizable(false)]
public static partial class OverlayValidator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Validates an overlay definition and its assets in the given folder.
    /// Returns a list of validation errors (empty if valid).
    /// </summary>
    public static List<string> Validate(string overlayFolder, PosterOverlayDefinition definition)
    {
        var errors = new List<string>();

        // Schema version check
        if (definition.SchemaVersion > OverlayConstants.AppSupportedSchemaVersion)
        {
            errors.Add($"Overlay '{definition.Id}' requires schema v{definition.SchemaVersion}, " +
                        $"app supports v{OverlayConstants.AppSupportedSchemaVersion}. Skipping.");
            return errors; // Don't continue — app doesn't understand this schema
        }

        ValidateMetadata(definition, errors);
        ValidateLayers(overlayFolder, definition, errors);
        ValidatePosterConfig(overlayFolder, definition, errors);
        ValidateRatingConfig(definition, errors);
        ValidateTitleConfig(definition, errors);

        // Validate total package size
        ValidatePackageSize(overlayFolder, errors);

        return errors;
    }

    private static void ValidateMetadata(PosterOverlayDefinition definition, List<string> errors)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            errors.Add("Overlay 'id' is required.");
        }
        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            errors.Add("Overlay 'displayName' is required.");
        }

        // ID safety
        if (!string.IsNullOrWhiteSpace(definition.Id) && !IsValidId(definition.Id))
        {
            errors.Add($"Overlay 'id' '{definition.Id}' contains invalid characters. Use lowercase alphanumeric and hyphens.");
        }
    }

    private static void ValidateLayers(string overlayFolder, PosterOverlayDefinition definition, List<string> errors)
    {
        // Validate base layer
        if (definition.BaseLayer != null)
        {
            ValidateLayer(overlayFolder, definition.BaseLayer, "baseLayer", errors);
        }

        // Validate front layer
        if (definition.FrontLayer != null)
        {
            ValidateLayer(overlayFolder, definition.FrontLayer, "frontLayer", errors);
        }
    }

    private static void ValidatePosterConfig(string overlayFolder, PosterOverlayDefinition definition, List<string> errors)
    {
        // Validate poster config
        ValidateMargin(definition.Poster.Margin, "poster.margin", errors);
        if (definition.Poster.OpacityMaskPath == null) return;
        var maskPath = Path.Combine(overlayFolder, definition.Poster.OpacityMaskPath);
        if (!File.Exists(maskPath))
        {
            errors.Add($"poster.opacityMaskPath '{definition.Poster.OpacityMaskPath}' file not found.");
        }
        else
        {
            ValidateImageSize(maskPath, "poster.opacityMask", errors);
        }
    }

    private static void ValidateRatingConfig(PosterOverlayDefinition definition, List<string> errors)
    {
        // Validate rating config
        ValidateMargin(definition.Rating.ShieldMargin, "rating.shieldMargin", errors);
        ValidateMargin(definition.Rating.TextMargin, "rating.textMargin", errors);
    }

    private static void ValidateTitleConfig(PosterOverlayDefinition definition, List<string> errors)
    {
        // Validate title config
        if (definition.Title.IsVisible)
        {
            ValidateMargin(definition.Title.Margin, "title.margin", errors);
        }

        // Validate rotation origin
        if (!string.IsNullOrWhiteSpace(definition.Title.RotationOrigin))
        {
            ValidateRotationOrigin(definition.Title.RotationOrigin, errors);
        }
    }

    private static void ValidateLayer(string overlayFolder, LayerDefinition layer, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(layer.ImagePath))
        {
            errors.Add($"{prefix}.imagePath is required when layer is defined.");
            return;
        }

        var imagePath = Path.Combine(overlayFolder, layer.ImagePath);
        if (!File.Exists(imagePath))
        {
            errors.Add($"{prefix}.imagePath '{layer.ImagePath}' file not found.");
            return;
        }

        ValidateImageSize(imagePath, prefix, errors);
        ValidateMargin(layer.Margin, $"{prefix}.margin", errors);
    }

    private static void ValidateImageSize(string imagePath, string prefix, List<string> errors)
    {
        try
        {
            var fileInfo = new FileInfo(imagePath);
            if (fileInfo.Length > OverlayConstants.MaxImageSizeBytes)
            {
                errors.Add($"{prefix} image '{imagePath}' exceeds maximum size " +
                            $"({fileInfo.Length / 1024.0 / 1024.0:F1} MB > {OverlayConstants.MaxImageSizeBytes / 1024.0 / 1024.0:F0} MB).");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to check image size for {ImagePath}", imagePath);
        }
    }

    private static void ValidateMargin(string margin, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(margin))
        {
            return;
        }

        var parts = margin.Split(',');
        if (parts.Length < 1 || parts.Length > 4)
        {
            errors.Add($"{field} '{margin}' is not a valid Thickness string. Expected 1-4 numeric values.");
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"{field} contains non-numeric value '{part.Trim()}'.");
            }
        }
    }

    private static void ValidateRotationOrigin(string origin, List<string> errors)
    {
        var parts = origin.Split(',');
        if (parts.Length != 2)
        {
            errors.Add($"title.rotationOrigin '{origin}' must be 'x,y' with exactly 2 values.");
            return;
        }

        foreach (var part in parts)
        {
            if (!double.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var val) || val < 0.0 || val > 1.0)
            {
                errors.Add($"title.rotationOrigin value '{part.Trim()}' must be between 0.0 and 1.0.");
            }
        }
    }

    private static void ValidatePackageSize(string overlayFolder, List<string> errors)
    {
        try
        {
            var totalSize = Directory.GetFiles(overlayFolder, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
            if (totalSize > OverlayConstants.MaxOverlayPackageSizeBytes)
            {
                errors.Add($"Overlay folder exceeds maximum total size " +
                            $"({totalSize / 1024.0 / 1024.0:F1} MB > {OverlayConstants.MaxOverlayPackageSizeBytes / 1024.0 / 1024.0:F0} MB).");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to calculate package size for {OverlayFolder}", overlayFolder);
        }
    }

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$")]
    private static partial Regex IdRegex();

    private static bool IsValidId(string id) => IdRegex().IsMatch(id);
}
