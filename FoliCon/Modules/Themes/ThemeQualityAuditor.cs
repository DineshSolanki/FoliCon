#nullable enable
using System;
using System.Collections.Generic;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Detailed evaluation result for a community theme candidate against quality, accessibility,
/// contrast, and aesthetic guidelines.
/// </summary>
public sealed class ThemeQualityReport
{
    public string ThemeId { get; init; } = string.Empty;
    public string ThemeName { get; init; } = string.Empty;

    public bool IsQualified => Errors.Count == 0 && ContrastPassed;

    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Info { get; } = [];

    // Contrast Check Metrics
    public bool ContrastPassed { get; set; } = true;
    public double PrimaryTextContrastRatio { get; set; }
    public double SecondaryTextContrastRatio { get; set; }
    public double PrimaryButtonTextContrastRatio { get; set; }
    public double CardToBackgroundContrastRatio { get; set; }
}

/// <summary>
/// Evaluates community theme submissions against WCAG contrast, structure, and quality guidelines.
/// </summary>
public static class ThemeQualityAuditor
{
    // WCAG AA minimum contrast ratio for normal text is 4.5:1
    public const double MinNormalTextContrast = 4.5;
    // WCAG AA minimum contrast ratio for large text / UI elements is 3.0:1
    public const double MinLargeTextContrast = 3.0;

    /// <summary>
    /// Audits a theme manifest and returns a comprehensive qualification report.
    /// </summary>
    public static ThemeQualityReport Audit(ThemeManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var report = new ThemeQualityReport
        {
            ThemeId = manifest.Id,
            ThemeName = manifest.Name
        };

        // 1. Structural Checks
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            report.Errors.Add("Theme ID is missing.");
        }
        else if (!ThemeConstants.ThemeIdRegex.IsMatch(manifest.Id))
        {
            report.Errors.Add($"Theme ID '{manifest.Id}' does not conform to kebab-case standard (^[a-z0-9]+(?:-[a-z0-9]+)*$).");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            report.Errors.Add("Theme display name is missing.");
        }
        else if (manifest.Name.Length < 3 || manifest.Name.Length > 50)
        {
            report.Warnings.Add("Theme name should be between 3 and 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Description))
        {
            report.Warnings.Add("Theme description is missing or empty.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Author) || manifest.Author.Equals("Community", StringComparison.OrdinalIgnoreCase))
        {
            report.Warnings.Add("Author name is missing or set to generic 'Community'.");
        }

        if (!Version.TryParse(manifest.Version, out _))
        {
            report.Errors.Add($"Version '{manifest.Version}' is not a valid SemVer (e.g. 1.0.0).");
        }

        if (ThemeConstants.BuiltInThemeIds.Contains(manifest.Id))
        {
            report.Errors.Add($"Theme ID '{manifest.Id}' collides with an official built-in theme.");
        }

        if (manifest.Colors == null)
        {
            report.Errors.Add("Theme color palette is missing.");
            report.ContrastPassed = false;
            return report;
        }

        // 2. Color Parsing & Contrast Verification
        var bgOk = TryParseRgb(manifest.Colors.Background, out var bgRgb);
        var cardOk = TryParseRgb(manifest.Colors.CardSurface, out var cardRgb);
        var primaryOk = TryParseRgb(manifest.Colors.Primary, out var primaryRgb);
        var primaryTextOk = TryParseRgb(manifest.Colors.PrimaryText, out var primaryTextRgb);
        var secondaryTextOk = TryParseRgb(manifest.Colors.SecondaryText ?? manifest.Colors.PrimaryText, out var secondaryTextRgb);

        if (!bgOk) report.Errors.Add("Background color is invalid or missing.");
        if (!cardOk) report.Errors.Add("CardSurface color is invalid or missing.");
        if (!primaryOk) report.Errors.Add("Primary accent color is invalid or missing.");
        if (!primaryTextOk) report.Errors.Add("PrimaryText color is invalid or missing.");

        if (bgOk && primaryTextOk)
        {
            report.PrimaryTextContrastRatio = CalculateContrastRatio(bgRgb, primaryTextRgb);
            if (report.PrimaryTextContrastRatio < MinNormalTextContrast)
            {
                report.Errors.Add(
                    $"PrimaryText contrast against Background is {report.PrimaryTextContrastRatio:F2}:1 (WCAG AA requires ≥ {MinNormalTextContrast}:1).");
                report.ContrastPassed = false;
            }
        }

        if (cardOk && primaryTextOk)
        {
            var cardTextRatio = CalculateContrastRatio(cardRgb, primaryTextRgb);
            if (cardTextRatio < MinNormalTextContrast)
            {
                report.Errors.Add(
                    $"PrimaryText contrast against CardSurface is {cardTextRatio:F2}:1 (WCAG AA requires ≥ {MinNormalTextContrast}:1).");
                report.ContrastPassed = false;
            }
        }

        if (cardOk && secondaryTextOk)
        {
            report.SecondaryTextContrastRatio = CalculateContrastRatio(cardRgb, secondaryTextRgb);
            if (report.SecondaryTextContrastRatio < MinLargeTextContrast)
            {
                report.Warnings.Add(
                    $"SecondaryText contrast against CardSurface is {report.SecondaryTextContrastRatio:F2}:1 (recommended ≥ {MinLargeTextContrast}:1).");
            }
        }

        if (primaryOk && primaryTextOk)
        {
            report.PrimaryButtonTextContrastRatio = CalculateContrastRatio(primaryRgb, primaryTextRgb);
            report.Info.Add($"Action button text contrast ratio: {report.PrimaryButtonTextContrastRatio:F2}:1.");
        }

        if (bgOk && cardOk)
        {
            report.CardToBackgroundContrastRatio = CalculateContrastRatio(bgRgb, cardRgb);
            if (Math.Abs(report.CardToBackgroundContrastRatio - 1.0) < 0.05)
            {
                report.Warnings.Add("CardSurface and Background colors are almost identical; cards may lack depth separation.");
            }
        }

        return report;
    }

    /// <summary>
    /// Calculates the WCAG 2.1 relative luminance-based contrast ratio between two colors (ranging from 1.0 to 21.0).
    /// </summary>
    public static double CalculateContrastRatio((byte R, byte G, byte B) c1, (byte R, byte G, byte B) c2)
    {
        var l1 = RelativeLuminance(c1);
        var l2 = RelativeLuminance(c2);

        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);

        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance((byte R, byte G, byte B) color)
    {
        double r = TransformComponent(color.R);
        double g = TransformComponent(color.G);
        double b = TransformComponent(color.B);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static double TransformComponent(byte value)
    {
        var s = value / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static bool TryParseRgb(string? hex, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        var clean = hex.Trim().TrimStart('#');
        if (clean.Length == 3)
        {
            try
            {
                var r = Convert.ToByte(new string(clean[0], 2), 16);
                var g = Convert.ToByte(new string(clean[1], 2), 16);
                var b = Convert.ToByte(new string(clean[2], 2), 16);
                rgb = (r, g, b);
                return true;
            }
            catch { return false; }
        }

        if (clean.Length == 6)
        {
            try
            {
                var r = Convert.ToByte(clean[..2], 16);
                var g = Convert.ToByte(clean.Substring(2, 2), 16);
                var b = Convert.ToByte(clean.Substring(4, 2), 16);
                rgb = (r, g, b);
                return true;
            }
            catch { return false; }
        }

        if (clean.Length == 8)
        {
            try
            {
                // #AARRGGBB -> ignore Alpha for canvas contrast calculation
                var r = Convert.ToByte(clean.Substring(2, 2), 16);
                var g = Convert.ToByte(clean.Substring(4, 2), 16);
                var b = Convert.ToByte(clean.Substring(6, 2), 16);
                rgb = (r, g, b);
                return true;
            }
            catch { return false; }
        }

        return false;
    }
}
