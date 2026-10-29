#nullable enable
using System.Security;
using System.Windows.Markup;
using System.Windows.Media.Effects;
using System.Xml;
using FoliCon.Models.Themes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Point = System.Windows.Point;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Hybrid theme parser capable of parsing declarative JSON manifests,
/// synthesizing frozen WPF ResourceDictionaries, sandboxing XAML packages,
/// and composably layering user customizations.
/// </summary>
public class HybridThemeParser : IThemeParser
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ThemeValidationResult Validate(string json)
    {
        var result = new ThemeValidationResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            result.Errors.Add("Theme manifest JSON is empty or whitespace.");
            return result;
        }

        ThemeManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ThemeManifest>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Invalid JSON syntax: {ex.Message}");
            return result;
        }

        if (manifest == null)
        {
            result.Errors.Add("Failed to deserialize theme manifest.");
            return result;
        }

        // Validate ID
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            result.Errors.Add("Theme manifest must contain an 'id'.");
        }
        else if (!ThemeConstants.ThemeIdRegex.IsMatch(manifest.Id))
        {
            result.Errors.Add($"Theme ID '{manifest.Id}' is invalid. IDs must use lowercase alphanumeric characters and hyphens only (e.g. 'cyberpunk-2077').");
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            result.Errors.Add("Theme manifest must contain a 'name'.");
        }

        // Validate BaseTheme
        if (!string.Equals(manifest.BaseTheme, "Dark", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(manifest.BaseTheme, "Light", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Unrecognized baseTheme '{manifest.BaseTheme}'. Defaulting to 'Dark'.");
        }

        // Validate Mandatory Colors
        ValidateColorToken(result, "colors.background", manifest.Colors?.Background, isRequired: true);
        ValidateColorToken(result, "colors.cardSurface", manifest.Colors?.CardSurface, isRequired: true);
        ValidateColorToken(result, "colors.primary", manifest.Colors?.Primary, isRequired: true);
        ValidateColorToken(result, "colors.border", manifest.Colors?.Border, isRequired: true);
        ValidateColorToken(result, "colors.primaryText", manifest.Colors?.PrimaryText, isRequired: true);

        // Validate Optional Colors
        if (manifest.Colors != null)
        {
            ValidateColorToken(result, "colors.elevatedSurface", manifest.Colors.ElevatedSurface, isRequired: false);
            ValidateColorToken(result, "colors.primaryHover", manifest.Colors.PrimaryHover, isRequired: false);
            ValidateColorToken(result, "colors.primaryPressed", manifest.Colors.PrimaryPressed, isRequired: false);
            ValidateColorToken(result, "colors.accent", manifest.Colors.Accent, isRequired: false);
            ValidateColorToken(result, "colors.secondaryText", manifest.Colors.SecondaryText, isRequired: false);
            ValidateColorToken(result, "colors.thirdText", manifest.Colors.ThirdText, isRequired: false);
            ValidateColorToken(result, "colors.success", manifest.Colors.Success, isRequired: false);
            ValidateColorToken(result, "colors.warning", manifest.Colors.Warning, isRequired: false);
            ValidateColorToken(result, "colors.error", manifest.Colors.Error, isRequired: false);
        }

        // Validate Materials & Gradients
        if (manifest.Materials != null)
        {
            if (manifest.Materials.BackdropOpacity is < 0.0 or > 1.0)
            {
                result.Warnings.Add("materials.backdropOpacity must be between 0.0 and 1.0.");
            }

            if (!string.IsNullOrEmpty(manifest.Materials.BackdropColor))
            {
                ValidateColorToken(result, "materials.backdropColor", manifest.Materials.BackdropColor, isRequired: false);
            }

            ValidateGradient(result, "materials.heroCardGradient", manifest.Materials.HeroCardGradient);
            ValidateGradient(result, "materials.heroCardBorder", manifest.Materials.HeroCardBorder);
            ValidateGradient(result, "materials.primaryActionGradient", manifest.Materials.PrimaryActionGradient);
            ValidateGradient(result, "materials.headerGradient", manifest.Materials.HeaderGradient);
            ValidateGradient(result, "materials.cardBorderGradient", manifest.Materials.CardBorderGradient);
            ValidateGradient(result, "materials.folderBadgeGradient", manifest.Materials.FolderBadgeGradient);
        }

        // Validate Effects
        if (manifest.Effects != null)
        {
            if (manifest.Effects.GlowIntensity is < 0.0 or > 2.0)
            {
                result.Warnings.Add("effects.glowIntensity should be between 0.0 and 2.0.");
            }
            if (manifest.Effects.PanelOpacity is < 0.0 or > 1.0)
            {
                result.Warnings.Add("effects.panelOpacity should be between 0.0 and 1.0.");
            }
            if (manifest.Effects.BloomRadius < 0.0)
            {
                result.Warnings.Add("effects.bloomRadius should be non-negative.");
            }
        }

        // Validate Shapes
        if (manifest.Shapes != null)
        {
            if (manifest.Shapes.PanelRadius < 0.0 || manifest.Shapes.ButtonRadius < 0.0 || manifest.Shapes.CardRadius < 0.0)
            {
                result.Warnings.Add("shapes corner radii should be non-negative.");
            }
        }

        // Validate Assets
        if (manifest.Assets != null)
        {
            ValidateAssetPath(result, "assets.wallpaper", manifest.Assets.Wallpaper);
            ValidateAssetPath(result, "assets.movieCard", manifest.Assets.MovieCard);
            ValidateAssetPath(result, "assets.tvCard", manifest.Assets.TvCard);
            ValidateAssetPath(result, "assets.gameCard", manifest.Assets.GameCard);
            ValidateAssetPath(result, "assets.autoCard", manifest.Assets.AutoCard);
        }

        // Validate XamlPackageFile path safety
        if (string.IsNullOrWhiteSpace(manifest.XamlPackageFile))
        {
            return result;
        }

        if (manifest.XamlPackageFile.Contains("..") || Path.IsPathRooted(manifest.XamlPackageFile))
        {
            result.Errors.Add("xamlPackageFile must be a relative file name within the package root and cannot contain path traversal sequences ('..').");
        }

        return result;
    }

    private static void ValidateAssetPath(ThemeValidationResult result, string propertyName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase)) return;

        if (path.Contains(".."))
        {
            result.Errors.Add($"{propertyName} cannot contain path traversal sequences ('..').");
        }
    }

    public ThemeManifest Parse(string json)
    {
        var validation = Validate(json);
        if (!validation.IsValid)
        {
            throw new InvalidDataException($"Theme manifest validation failed:\n{string.Join("\n", validation.Errors)}");
        }

        var manifest = JsonSerializer.Deserialize<ThemeManifest>(json, JsonOptions)!;
        return manifest;
    }

    public ResourceDictionary SynthesizeResourceDictionary(ThemeManifest manifest, UserThemeCustomization? customization = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var colors = manifest.Colors ?? new ThemeColorPalette();
        var materials = manifest.Materials;
        var effects = manifest.Effects;
        var shapes = manifest.Shapes;
        var assets = manifest.Assets;

        var dict = new ResourceDictionary();

        // 1. Resolve Color Primitives (Layer 2 Theme Pack + Layer 3 User Overrides)
        var bgColor = ParseColor(colors.Background, Color.FromRgb(0x14, 0x14, 0x14));
        var cardColor = ParseColor(colors.CardSurface, Color.FromRgb(0x1E, 0x1E, 0x1E));
        var elevatedColor = ParseColor(colors.ElevatedSurface, AdjustBrightness(cardColor, 1.15f));

        var primaryColor = !string.IsNullOrEmpty(customization?.CustomAccentColor)
            ? ParseColor(customization.CustomAccentColor, Color.FromRgb(0x3B, 0x82, 0xF6))
            : ParseColor(colors.Primary, Color.FromRgb(0x3B, 0x82, 0xF6));

        var primaryDark = ParseColor(colors.PrimaryPressed, AdjustBrightness(primaryColor, 0.8f));
        var primaryLight = ParseColor(colors.PrimaryHover, AdjustBrightness(primaryColor, 1.2f));

        var accentColor = !string.IsNullOrEmpty(customization?.CustomAccentColor)
            ? primaryColor
            : ParseColor(colors.Accent, primaryLight);

        var borderColor = ParseColor(colors.Border, Color.FromRgb(0x38, 0x38, 0x38));
        var primaryTextColor = ParseColor(colors.PrimaryText, Color.FromRgb(0xF8, 0xFA, 0xFC));
        var secondaryTextColor = ParseColor(colors.SecondaryText, WithAlpha(primaryTextColor, 0xB3)); // ~70%
        var thirdTextColor = ParseColor(colors.ThirdText, WithAlpha(primaryTextColor, 0x80)); // ~50%

        var successColor = ParseColor(colors.Success, Color.FromRgb(0x10, 0xB9, 0x81));
        var warningColor = ParseColor(colors.Warning, Color.FromRgb(0xF5, 0x9E, 0x0B));
        var dangerColor = ParseColor(colors.Error, Color.FromRgb(0xEF, 0x44, 0x44));

        // 2. Put Colors into ResourceDictionary
        dict["BackgroundColor"] = bgColor;
        dict["PrimaryColor"] = primaryColor;
        dict["DarkPrimaryColor"] = primaryDark;
        dict["LightPrimaryColor"] = primaryLight;
        dict["AccentColor"] = accentColor;
        dict["CardSurfaceColor"] = cardColor;
        dict["ElevatedSurfaceColor"] = elevatedColor;
        dict["BorderColor"] = borderColor;
        dict["PrimaryTextColor"] = primaryTextColor;
        dict["SecondaryTextColor"] = secondaryTextColor;
        dict["ThirdTextColor"] = thirdTextColor;
        dict["SuccessColor"] = successColor;
        dict["WarningColor"] = warningColor;
        dict["DangerColor"] = dangerColor;

        // HandyControl Regional Color Keys
        dict["RegionColor"] = bgColor;
        dict["SecondaryRegionColor"] = cardColor;
        dict["ThirdRegionColor"] = elevatedColor;

        // 3. Put Frozen SolidColorBrushes
        dict["PrimaryBrush"] = CreateFrozenBrush(primaryColor);
        dict["DarkPrimaryBrush"] = CreateFrozenBrush(primaryDark);
        dict["LightPrimaryBrush"] = CreateFrozenBrush(primaryLight);
        dict["AccentBrush"] = CreateFrozenBrush(accentColor);

        dict["RegionBrush"] = CreateFrozenBrush(bgColor);
        dict["SecondaryRegionBrush"] = CreateFrozenBrush(cardColor);
        dict["ThirdRegionBrush"] = CreateFrozenBrush(elevatedColor);
        dict["ThirdlyRegionBrush"] = CreateFrozenBrush(elevatedColor);

        dict["BorderBrush"] = CreateFrozenBrush(borderColor);

        dict["PrimaryTextBrush"] = CreateFrozenBrush(primaryTextColor);
        dict["SecondaryTextBrush"] = CreateFrozenBrush(secondaryTextColor);
        dict["ThirdTextBrush"] = CreateFrozenBrush(thirdTextColor);
        dict["ThirdlyTextBrush"] = CreateFrozenBrush(thirdTextColor);

        dict["SuccessBrush"] = CreateFrozenBrush(successColor);
        dict["WarningBrush"] = CreateFrozenBrush(warningColor);
        dict["DangerBrush"] = CreateFrozenBrush(dangerColor);

        dict["DefaultBrush"] = CreateFrozenBrush(cardColor);
        dict["DarkDefaultBrush"] = CreateFrozenBrush(bgColor);

        // 4. Synthesize WindowBackdropBrush (Mica Wallpaper Translucent Tint)
        var backdropTint = !string.IsNullOrEmpty(materials?.BackdropColor)
            ? ParseColor(materials.BackdropColor, bgColor)
            : bgColor;

        var opacity = customization?.PanelOpacity
            ?? effects?.PanelOpacity
            ?? materials?.BackdropOpacity
            ?? 0.85;

        var backdropAlpha = (byte)Math.Clamp((int)Math.Round(opacity * 255.0), 0, 255);
        dict["WindowBackdropBrush"] = CreateFrozenBrush(WithAlpha(backdropTint, backdropAlpha));

        // 5. Synthesize Gradients
        dict["HeroCardBackgroundBrush"] = CreateGradientBrush(
            materials?.HeroCardGradient,
            start: new Point(0, 0),
            end: new Point(1, 1),
            fallbackStops: [
                new GradientStop(AdjustBrightness(cardColor, 1.1f), 0.0),
                new GradientStop(cardColor, 1.0)
            ]);

        dict["HeroCardBorderBrush"] = CreateGradientBrush(
            materials?.HeroCardBorder,
            start: new Point(0, 0),
            end: new Point(0, 1),
            fallbackStops: [
                new GradientStop(AdjustBrightness(borderColor, 1.4f), 0.0),
                new GradientStop(borderColor, 1.0)
            ]);

        dict["PrimaryActionGradientBrush"] = CreateGradientBrush(
            materials?.PrimaryActionGradient,
            start: new Point(0, 0),
            end: new Point(1, 1),
            fallbackStops: [
                new GradientStop(primaryColor, 0.0),
                new GradientStop(primaryDark, 1.0)
            ]);

        dict["HeaderGradientBrush"] = CreateGradientBrush(
            materials?.HeaderGradient,
            start: new Point(0, 0),
            end: new Point(0, 1),
            fallbackStops: [
                new GradientStop(AdjustBrightness(bgColor, 1.2f), 0.0),
                new GradientStop(bgColor, 1.0)
            ]);

        dict["CardBorderGradientBrush"] = CreateGradientBrush(
            materials?.CardBorderGradient,
            start: new Point(0, 0),
            end: new Point(0, 1),
            fallbackStops: [
                new GradientStop(AdjustBrightness(borderColor, 1.25f), 0.0),
                new GradientStop(borderColor, 1.0)
            ]);

        dict["FolderBadgeGradientBrush"] = CreateGradientBrush(
            materials?.FolderBadgeGradient,
            start: new Point(0, 0),
            end: new Point(1, 1),
            fallbackStops: [
                new GradientStop(primaryLight, 0.0),
                new GradientStop(primaryColor, 1.0)
            ]);

        // 6. Synthesize Effects (Layer 4 & Layer 6)
        var glowScalar = (effects?.GlowIntensity ?? 0.8) * (customization?.GlowMultiplier ?? 1.0);
        var bloomRadius = effects?.BloomRadius ?? 24.0;
        var shadowDepth = effects?.ShadowDepth ?? 0.0;

        if (glowScalar > 0.01)
        {
            var themeGlow = new DropShadowEffect
            {
                Color = primaryColor,
                BlurRadius = Math.Max(0.0, bloomRadius * glowScalar),
                ShadowDepth = shadowDepth,
                Opacity = Math.Clamp(glowScalar, 0.0, 1.0)
            };
            themeGlow.Freeze();
            dict["ThemeGlowEffect"] = themeGlow;

            var ctaGlow = new DropShadowEffect
            {
                Color = primaryLight,
                BlurRadius = Math.Max(0.0, (bloomRadius + 6.0) * glowScalar),
                ShadowDepth = shadowDepth,
                Opacity = Math.Clamp(glowScalar * 1.1, 0.0, 1.0)
            };
            ctaGlow.Freeze();
            dict["ThemeCtaGlowEffect"] = ctaGlow;

            var activeCardGlow = new DropShadowEffect
            {
                Color = primaryColor,
                BlurRadius = Math.Max(0.0, (bloomRadius - 4.0) * glowScalar),
                ShadowDepth = shadowDepth,
                Opacity = Math.Clamp(glowScalar * 0.9, 0.0, 1.0)
            };
            activeCardGlow.Freeze();
            dict["ActiveCardGlowEffect"] = activeCardGlow;
        }
        else
        {
            // Crisp zero glow for Corporate/Minimal themes
            var zeroGlow = new DropShadowEffect { BlurRadius = 0, ShadowDepth = 0, Opacity = 0 };
            zeroGlow.Freeze();
            dict["ThemeGlowEffect"] = zeroGlow;
            dict["ThemeCtaGlowEffect"] = zeroGlow;
            dict["ActiveCardGlowEffect"] = zeroGlow;
        }

        // 7. Synthesize Shapes (Layer 5)
        var panelRadius = shapes?.PanelRadius ?? 10.0;
        var buttonRadius = shapes?.ButtonRadius ?? 8.0;
        var cardRadius = shapes?.CardRadius ?? 10.0;
        dict["PanelCornerRadius"] = new CornerRadius(panelRadius);
        dict["ButtonCornerRadius"] = new CornerRadius(buttonRadius);
        dict["CardCornerRadius"] = new CornerRadius(cardRadius);
        dict["ControlCornerRadius"] = new CornerRadius(buttonRadius);

        // 8. Synthesize Living Assets & Wallpapers (Layer 2)
        Brush wallpaperBrush = Brushes.Transparent;
        if (customization != null && !customization.IsWallpaperEnabled)
        {
            wallpaperBrush = Brushes.Transparent;
        }
        else if (customization != null && !string.IsNullOrEmpty(customization.CustomWallpaperPath) && File.Exists(customization.CustomWallpaperPath))
        {
            var customBrush = LoadImageBrushFromFile(customization.CustomWallpaperPath);
            if (customBrush != null) wallpaperBrush = customBrush;
        }
        else
        {
            var wpCandidate = assets?.Wallpaper ?? assets?.Backdrop;
            if (string.IsNullOrEmpty(wpCandidate) && !string.IsNullOrEmpty(manifest.PackageDirectory))
            {
                wpCandidate = ResolveStandardAssetFile(manifest.PackageDirectory, "backdrop", "wallpaper");
            }
            if (!string.IsNullOrEmpty(wpCandidate))
            {
                var manifestBrush = ResolveAssetBrush(wpCandidate, manifest.PackageDirectory);
                if (manifestBrush != null) wallpaperBrush = manifestBrush;
            }
        }
        dict["ThemeWallpaperBrush"] = wallpaperBrush;


        var movieCandidate = assets?.MovieCard;
        if (string.IsNullOrEmpty(movieCandidate) && !string.IsNullOrEmpty(manifest.PackageDirectory))
        {
            movieCandidate = ResolveStandardAssetFile(manifest.PackageDirectory, "card_movie", "movie");
        }
        dict["ThemeCardMovieBrush"] = ResolveAssetBrush(movieCandidate, manifest.PackageDirectory) ?? Brushes.Transparent;

        var tvCandidate = assets?.TvCard;
        if (string.IsNullOrEmpty(tvCandidate) && !string.IsNullOrEmpty(manifest.PackageDirectory))
        {
            tvCandidate = ResolveStandardAssetFile(manifest.PackageDirectory, "card_tv", "tv");
        }
        dict["ThemeCardTvBrush"] = ResolveAssetBrush(tvCandidate, manifest.PackageDirectory) ?? Brushes.Transparent;

        var gameCandidate = assets?.GameCard;
        if (string.IsNullOrEmpty(gameCandidate) && !string.IsNullOrEmpty(manifest.PackageDirectory))
        {
            gameCandidate = ResolveStandardAssetFile(manifest.PackageDirectory, "card_game", "game");
        }
        dict["ThemeCardGameBrush"] = ResolveAssetBrush(gameCandidate, manifest.PackageDirectory) ?? Brushes.Transparent;

        var autoCandidate = assets?.AutoCard;
        if (string.IsNullOrEmpty(autoCandidate) && !string.IsNullOrEmpty(manifest.PackageDirectory))
        {
            autoCandidate = ResolveStandardAssetFile(manifest.PackageDirectory, "card_auto", "auto");
        }
        dict["ThemeCardAutoBrush"] = ResolveAssetBrush(autoCandidate, manifest.PackageDirectory) ?? Brushes.Transparent;

        // Vignette
        if (effects?.VignetteEnabled != false && wallpaperBrush != Brushes.Transparent)
        {
            var vignette = new RadialGradientBrush
            {
                Center = new Point(0.5, 0.4),
                GradientOrigin = new Point(0.5, 0.4),
                RadiusX = 0.9,
                RadiusY = 0.9
            };
            vignette.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, bgColor.R, bgColor.G, bgColor.B), 0.0));
            vignette.GradientStops.Add(new GradientStop(Color.FromArgb(0x20, bgColor.R, bgColor.G, bgColor.B), 0.55));
            vignette.GradientStops.Add(new GradientStop(Color.FromArgb(0x80, bgColor.R, bgColor.G, bgColor.B), 1.0));
            vignette.Freeze();
            dict["ThemeVignetteBrush"] = vignette;
        }
        else
        {
            dict["ThemeVignetteBrush"] = Brushes.Transparent;
        }


        // 9. Synthesize Branding Vector Geometry
        if (!string.IsNullOrWhiteSpace(assets?.BrandingGeometry))
        {
            try
            {
                var geom = Geometry.Parse(assets.BrandingGeometry);
                geom.Freeze();
                dict["ThemeBrandingGeometry"] = geom;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to parse theme branding geometry '{Geometry}'", assets.BrandingGeometry);
            }
        }

        // 9b. Synthesize Empty Queue Vector Geometry (Theme-specific or Neutral Folder)
        if (!string.IsNullOrWhiteSpace(assets?.EmptyQueueGeometry))
        {
            try
            {
                var geom = Geometry.Parse(assets.EmptyQueueGeometry);
                geom.Freeze();
                dict["ThemeEmptyQueueGeometry"] = geom;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to parse theme empty queue geometry '{Geometry}'", assets.EmptyQueueGeometry);
            }
        }
        else
        {
            try
            {
                var defaultGeom = Geometry.Parse("M20 6h-8l-2-2H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2zm0 12H4V8h16v10z");
                defaultGeom.Freeze();
                dict["ThemeEmptyQueueGeometry"] = defaultGeom;
            }
            catch
            {
                // ignored
            }
        }

        return dict;
    }

    public static string? ResolveStandardAssetFile(string packageDirectory, params string[] baseNames)
    {
        if (string.IsNullOrWhiteSpace(packageDirectory) || !Directory.Exists(packageDirectory))
        {
            return null;
        }

        string[] extensions = [".png", ".jpg", ".jpeg", ".webp"];
        foreach (var baseName in baseNames)
        {
            foreach (var ext in extensions)
            {
                var candidate = baseName + ext;
                var fullPath = Path.Combine(packageDirectory, candidate);
                if (File.Exists(fullPath))
                {
                    return candidate;
                }
            }
        }
        return null;
    }

    private static Brush? ResolveAssetBrush(string? assetPathOrUri, string? packageDirectory)
    {
        if (string.IsNullOrWhiteSpace(assetPathOrUri)) return null;

        try
        {
            if (assetPathOrUri.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(assetPathOrUri, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                var brush = new ImageBrush(bi) { Stretch = Stretch.UniformToFill };
                brush.Freeze();
                return brush;
            }

            var fullPath = !string.IsNullOrEmpty(packageDirectory)
                ? Path.Combine(packageDirectory, assetPathOrUri)
                : assetPathOrUri;

            if (File.Exists(fullPath))
            {
                return LoadImageBrushFromFile(fullPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to resolve theme asset brush from '{Path}'", assetPathOrUri);
        }

        return null;
    }

    private static ImageBrush? LoadImageBrushFromFile(string filePath)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(filePath, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();

            var brush = new ImageBrush(bi) { Stretch = Stretch.UniformToFill };
            brush.Freeze();
            return brush;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to load image brush from file '{FilePath}'", filePath);
            return null;
        }
    }

    private static readonly HashSet<string> AllowedXamlElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "ResourceDictionary",
        "ResourceDictionary.MergedDictionaries",
        "SolidColorBrush",
        "LinearGradientBrush",
        "RadialGradientBrush",
        "GradientStop",
        "GradientStopCollection",
        "Color"
    };

    private static readonly HashSet<string> AllowedNamespaces =
    [
        "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
        "http://schemas.microsoft.com/winfx/2006/xaml"
    ];

    public ResourceDictionary LoadPackageResourceDictionary(string xamlFilePath)
    {
        if (!File.Exists(xamlFilePath))
        {
            throw new FileNotFoundException($"XAML theme package file not found: {xamlFilePath}", xamlFilePath);
        }

        var fileInfo = new FileInfo(xamlFilePath);
        if (fileInfo.Length > ThemeConstants.maxThemePackageSizeBytes)
        {
            throw new InvalidOperationException($"XAML theme file exceeds maximum allowed size ({ThemeConstants.maxThemePackageSizeBytes} bytes).");
        }

        // Hardened XML Allowlist Sandbox (neutralizes XML entity attacks, SSRF, and arbitrary class instantiation)
        var readerSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };

        using (var stream = File.OpenRead(xamlFilePath))
        using (var xmlReader = XmlReader.Create(stream, readerSettings))
        {
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    var localName = xmlReader.LocalName;
                    if (!AllowedXamlElements.Contains(localName))
                    {
                        throw new SecurityException(
                            $"Theme XAML contains prohibited element '<{localName}>'. Only declarative palette resources are permitted.");
                    }

                    if (!string.IsNullOrEmpty(xmlReader.NamespaceURI) && !AllowedNamespaces.Contains(xmlReader.NamespaceURI))
                    {
                        throw new SecurityException(
                            $"Theme XAML contains unauthorized namespace '{xmlReader.NamespaceURI}'. Only standard WPF presentation namespaces are permitted.");
                    }

                    if (!xmlReader.HasAttributes)
                    {
                        continue;
                    }
                    while (xmlReader.MoveToNextAttribute())
                    {
                        var attrName = xmlReader.Name;
                        var attrValue = xmlReader.Value;

                        if (attrName.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase) || attrName.Equals("xmlns", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!AllowedNamespaces.Contains(attrValue))
                            {
                                throw new SecurityException(
                                    $"Theme XAML contains unauthorized xmlns declaration '{attrName}=\"{attrValue}\"'.");
                            }
                            continue;
                        }

                        if (attrName.Equals("x:Class", StringComparison.OrdinalIgnoreCase) ||
                            attrName.Equals("x:Subclass", StringComparison.OrdinalIgnoreCase) ||
                            attrName.Equals("Source", StringComparison.OrdinalIgnoreCase) ||
                            attrName.Equals("UriSource", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new SecurityException(
                                $"Theme XAML contains unauthorized attribute '{attrName}'.");
                        }

                        if (attrValue.Contains("://") || attrValue.StartsWith(@"\\"))
                        {
                            throw new SecurityException(
                                $"Theme XAML contains forbidden network or UNC path reference: '{attrValue}'.");
                        }
                    }
                    xmlReader.MoveToElement();
                }
            }
        }

        using var safeStream = File.OpenRead(xamlFilePath);
        using var safeXmlReader = XmlReader.Create(safeStream, readerSettings);
        var parsed = XamlReader.Load(safeXmlReader);

        if (parsed is not ResourceDictionary dict)
        {
            throw new InvalidDataException("Root element of custom XAML theme package must be a <ResourceDictionary>.");
        }

        // Freeze all Freezable objects for cross-thread access
        foreach (var key in dict.Keys)
        {
            if (dict[key] is Freezable freezable && freezable.CanFreeze && !freezable.IsFrozen)
            {
                freezable.Freeze();
            }
        }

        return dict;
    }

    private static void ValidateColorToken(ThemeValidationResult result, string propertyName, string? value, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (isRequired)
            {
                result.Errors.Add($"Required color '{propertyName}' is missing.");
            }
            return;
        }

        if (!ThemeConstants.HexColorRegex.IsMatch(value))
        {
            result.Errors.Add($"Color '{propertyName}' has invalid hex format '{value}'. Expected #RGB, #RRGGBB, or #AARRGGBB.");
        }
    }

    private static void ValidateGradient(ThemeValidationResult result, string propertyName, GradientDefinition? gradient)
    {
        if (gradient == null) return;

        if (gradient.StartPoint is not { Length: 2 })
        {
            result.Warnings.Add($"{propertyName}.startPoint should be an array of [x, y].");
        }

        if (gradient.EndPoint is not { Length: 2 })
        {
            result.Warnings.Add($"{propertyName}.endPoint should be an array of [x, y].");
        }

        for (var i = 0; i < gradient.Stops.Count; i++)
        {
            var stop = gradient.Stops[i];
            if (!string.IsNullOrWhiteSpace(stop.Color) && !ThemeConstants.HexColorRegex.IsMatch(stop.Color))
            {
                result.Errors.Add($"{propertyName}.stops[{i}].color has invalid hex format '{stop.Color}'.");
            }
        }
    }

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;

        try
        {
            var cleanHex = hex.Trim();
            if (cleanHex.StartsWith('#'))
            {
                cleanHex = cleanHex[1..];
            }

            return cleanHex.Length switch
            {
                3 => Color.FromRgb(
                    (byte)(Convert.ToByte(cleanHex[0].ToString(), 16) * 17),
                    (byte)(Convert.ToByte(cleanHex[1].ToString(), 16) * 17),
                    (byte)(Convert.ToByte(cleanHex[2].ToString(), 16) * 17)),
                4 => Color.FromArgb(
                    (byte)(Convert.ToByte(cleanHex[0].ToString(), 16) * 17),
                    (byte)(Convert.ToByte(cleanHex[1].ToString(), 16) * 17),
                    (byte)(Convert.ToByte(cleanHex[2].ToString(), 16) * 17),
                    (byte)(Convert.ToByte(cleanHex[3].ToString(), 16) * 17)),
                6 => Color.FromRgb(
                    Convert.ToByte(cleanHex[..2], 16),
                    Convert.ToByte(cleanHex.Substring(2, 2), 16),
                    Convert.ToByte(cleanHex.Substring(4, 2), 16)),
                8 => Color.FromArgb(
                    Convert.ToByte(cleanHex[..2], 16),
                    Convert.ToByte(cleanHex.Substring(2, 2), 16),
                    Convert.ToByte(cleanHex.Substring(4, 2), 16),
                    Convert.ToByte(cleanHex.Substring(6, 2), 16)),
                _ => fallback
            };
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to parse color hex '{Hex}'. Using fallback", hex);
            return fallback;
        }
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush CreateGradientBrush(
        GradientDefinition? definition,
        Point start,
        Point end,
        GradientStop[] fallbackStops)
    {
        LinearGradientBrush brush;

        if (definition != null && definition.Stops.Count > 0)
        {
            var startPt = (definition.StartPoint is { Length: 2 })
                ? new Point(definition.StartPoint[0], definition.StartPoint[1])
                : start;

            var endPt = (definition.EndPoint is { Length: 2 })
                ? new Point(definition.EndPoint[0], definition.EndPoint[1])
                : end;

            brush = new LinearGradientBrush { StartPoint = startPt, EndPoint = endPt };
            foreach (var stopDef in definition.Stops)
            {
                var color = ParseColor(stopDef.Color, Colors.Transparent);
                brush.GradientStops.Add(new GradientStop(color, stopDef.Offset));
            }
        }
        else
        {
            brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
            foreach (var stop in fallbackStops)
            {
                brush.GradientStops.Add(stop);
            }
        }

        brush.Freeze();
        return brush;
    }

    private static Color WithAlpha(Color color, byte alpha) => Color.FromArgb(alpha, color.R, color.G, color.B);

    private static Color AdjustBrightness(Color color, float factor)
    {
        var r = (byte)Math.Clamp((int)Math.Round(color.R * factor), 0, 255);
        var g = (byte)Math.Clamp((int)Math.Round(color.G * factor), 0, 255);
        var b = (byte)Math.Clamp((int)Math.Round(color.B * factor), 0, 255);
        return Color.FromArgb(color.A, r, g, b);
    }
}
