#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines visual effect tokens including glow intensity, blur radius, and shadows.
/// </summary>
public sealed class ThemeEffectsTokens
{
    /// <summary>
    /// Overall glow intensity scalar (0.0 to 1.5). 0.0 disables glows completely.
    /// </summary>
    [JsonPropertyName("glowIntensity")]
    public double GlowIntensity { get; set; } = 0.8;

    /// <summary>
    /// Surface and card glassmorphism panel opacity ratio (0.1 to 1.0).
    /// </summary>
    [JsonPropertyName("panelOpacity")]
    public double PanelOpacity { get; set; } = 0.85;

    /// <summary>
    /// Bloom blur radius in dips for glowing halos.
    /// </summary>
    [JsonPropertyName("bloomRadius")]
    public double BloomRadius { get; set; } = 24.0;

    /// <summary>
    /// Drop shadow depth distance in dips.
    /// </summary>
    [JsonPropertyName("shadowDepth")]
    public double ShadowDepth { get; set; } = 0.0;

    /// <summary>
    /// Whether edge vignette darkening is rendered over wallpapers.
    /// </summary>
    [JsonPropertyName("vignetteEnabled")]
    public bool VignetteEnabled { get; set; } = true;
}
