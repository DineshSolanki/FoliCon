#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines advanced material, gradient, and Windows 11 backdrop tokens.
/// </summary>
public sealed class ThemeMaterialTokens
{
    /// <summary>
    /// Translucent backdrop alpha ratio (0.1 to 1.0) allowing Windows 11 Mica blur to bleed through.
    /// Default is 0.85 (85% opacity).
    /// </summary>
    [JsonPropertyName("backdropOpacity")]
    public double BackdropOpacity { get; set; } = 0.85;

    /// <summary>
    /// Explicit backdrop tint color. Defaults to ThemeColorPalette.Background when omitted.
    /// </summary>
    [JsonPropertyName("backdropColor")]
    public string? BackdropColor { get; set; }

    [JsonPropertyName("heroCardGradient")]
    public GradientDefinition? HeroCardGradient { get; set; }

    [JsonPropertyName("heroCardBorder")]
    public GradientDefinition? HeroCardBorder { get; set; }

    [JsonPropertyName("primaryActionGradient")]
    public GradientDefinition? PrimaryActionGradient { get; set; }

    [JsonPropertyName("headerGradient")]
    public GradientDefinition? HeaderGradient { get; set; }

    [JsonPropertyName("cardBorderGradient")]
    public GradientDefinition? CardBorderGradient { get; set; }

    [JsonPropertyName("folderBadgeGradient")]
    public GradientDefinition? FolderBadgeGradient { get; set; }
}
