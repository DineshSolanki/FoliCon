#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines living theme asset references such as wallpapers, banners, card viewports, and branding geometry.
/// </summary>
public sealed class ThemeAssetsTokens
{
    /// <summary>
    /// Relative path or pack URI to 1080p atmospheric wallpaper backdrop.
    /// </summary>
    [JsonPropertyName("wallpaper")]
    public string? Wallpaper { get; set; }

    /// <summary>
    /// Alias for wallpaper.
    /// </summary>
    [JsonPropertyName("backdrop")]
    public string? Backdrop { get => Wallpaper; set => Wallpaper ??= value; }

    /// <summary>
    /// Optional preview image relative file name.
    /// </summary>
    [JsonPropertyName("preview")]
    public string? Preview { get; set; }

    /// <summary>
    /// Relative path or pack URI to Movie card illustrated viewport artwork.
    /// </summary>
    [JsonPropertyName("movieCard")]
    public string? MovieCard { get; set; }

    /// <summary>
    /// Relative path or pack URI to TV card illustrated viewport artwork.
    /// </summary>
    [JsonPropertyName("tvCard")]
    public string? TvCard { get; set; }

    /// <summary>
    /// Relative path or pack URI to Game card illustrated viewport artwork.
    /// </summary>
    [JsonPropertyName("gameCard")]
    public string? GameCard { get; set; }

    /// <summary>
    /// Relative path or pack URI to Auto card illustrated viewport artwork.
    /// </summary>
    [JsonPropertyName("autoCard")]
    public string? AutoCard { get; set; }

    /// <summary>
    /// SVG Path Data string representing the theme's signature branding vector glyph.
    /// </summary>
    [JsonPropertyName("brandingGeometry")]
    public string? BrandingGeometry { get; set; }

    /// <summary>
    /// SVG Path Data string representing the theme's empty queue artwork glyph.
    /// When omitted, falls back to the neutral folder geometry.
    /// </summary>
    [JsonPropertyName("emptyQueueGeometry")]
    public string? EmptyQueueGeometry { get; set; }
}
