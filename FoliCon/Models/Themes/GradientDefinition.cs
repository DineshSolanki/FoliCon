#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Defines a single color stop in a linear gradient.
/// </summary>
public sealed class GradientStopDefinition
{
    [JsonPropertyName("offset")]
    public double Offset { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";
}

/// <summary>
/// Defines a 2D linear gradient with normalized start/end points and stops.
/// </summary>
public sealed class GradientDefinition
{
    [JsonPropertyName("startPoint")]
    public double[] StartPoint { get; set; } = [0.0, 0.0];

    [JsonPropertyName("endPoint")]
    public double[] EndPoint { get; set; } = [1.0, 1.0];

    [JsonPropertyName("stops")]
    public List<GradientStopDefinition> Stops { get; set; } = [];
}
