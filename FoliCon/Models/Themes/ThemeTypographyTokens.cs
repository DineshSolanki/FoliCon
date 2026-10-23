#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines typography preferences for custom themes.
/// </summary>
public sealed class ThemeTypographyTokens
{
    /// <summary>
    /// Optional font family name or comma-separated fallback chain.
    /// </summary>
    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    /// <summary>
    /// Relative scale multiplier for header typography (0.8 to 1.4). Default is 1.0.
    /// </summary>
    [JsonPropertyName("headerScale")]
    public double HeaderScale { get; set; } = 1.0;
}
