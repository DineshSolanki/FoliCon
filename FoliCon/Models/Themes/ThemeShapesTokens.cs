#nullable enable
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines shape and corner radius tokens for cards, panels, and input controls.
/// </summary>
public sealed class ThemeShapesTokens
{
    [JsonPropertyName("panelRadius")]
    public double PanelRadius { get; set; } = 10.0;

    [JsonPropertyName("buttonRadius")]
    public double ButtonRadius { get; set; } = 8.0;

    [JsonPropertyName("cardRadius")]
    public double CardRadius { get; set; } = 10.0;
}
