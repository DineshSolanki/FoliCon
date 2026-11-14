#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Represents user-configured overrides (Tier 3) for a specific theme.
/// Allows personal customization of accents, wallpapers, effects, and opacity
/// without mutating the original theme package.
/// </summary>
public sealed class UserThemeCustomization
{
    /// <summary>
    /// ID of the target theme this customization extends.
    /// </summary>
    [JsonPropertyName("themeId")]
    public string ThemeId { get; set; } = string.Empty;

    /// <summary>
    /// Custom accent hex color (#RRGGBB). Overrides primary and accent colors.
    /// </summary>
    [JsonPropertyName("customAccentColor")]
    public string? CustomAccentColor { get; set; }

    /// <summary>
    /// Absolute local file path to a custom wallpaper image chosen by user.
    /// </summary>
    [JsonPropertyName("customWallpaperPath")]
    public string? CustomWallpaperPath { get; set; }

    /// <summary>
    /// Whether atmospheric wallpaper is enabled for this theme. Default is true.
    /// </summary>
    [JsonPropertyName("isWallpaperEnabled")]
    public bool IsWallpaperEnabled { get; set; } = true;

    /// <summary>
    /// User multiplier for glow and bloom effects (0.0 = disabled, 1.0 = standard, 1.5 = boosted).
    /// </summary>
    [JsonPropertyName("glowMultiplier")]
    public double GlowMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Optional override for surface glassmorphism panel opacity (0.2 to 1.0).
    /// </summary>
    [JsonPropertyName("panelOpacity")]
    public double? PanelOpacity { get; set; }
}
