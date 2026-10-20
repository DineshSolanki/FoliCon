#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines the core color palette tokens for a FoliCon custom theme.
/// Colors are specified in standard hex notation (#RGB, #RRGGBB, #AARRGGBB).
/// </summary>
public sealed class ThemeColorPalette
{
    [JsonPropertyName("background")]
    public string Background { get; set; } = string.Empty;

    [JsonPropertyName("cardSurface")]
    public string CardSurface { get; set; } = string.Empty;

    [JsonPropertyName("elevatedSurface")]
    public string? ElevatedSurface { get; set; }

    [JsonPropertyName("primary")]
    public string Primary { get; set; } = string.Empty;

    [JsonPropertyName("primaryHover")]
    public string? PrimaryHover { get; set; }

    [JsonPropertyName("primaryPressed")]
    public string? PrimaryPressed { get; set; }

    [JsonPropertyName("accent")]
    public string? Accent { get; set; }

    [JsonPropertyName("border")]
    public string Border { get; set; } = string.Empty;

    [JsonPropertyName("primaryText")]
    public string PrimaryText { get; set; } = string.Empty;

    [JsonPropertyName("secondaryText")]
    public string? SecondaryText { get; set; }

    [JsonPropertyName("thirdText")]
    public string? ThirdText { get; set; }
}
