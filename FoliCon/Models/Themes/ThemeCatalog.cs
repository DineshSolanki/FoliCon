#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Root model for the community themes catalog (themes/catalog.json).
/// </summary>
public sealed class ThemeCatalog
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("themes")]
    public List<ThemeCatalogEntry> Themes { get; set; } = [];
}

/// <summary>
/// A single theme entry in the community repository catalog.
/// </summary>
public sealed class ThemeCatalogEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Community";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("baseTheme")]
    public string BaseTheme { get; set; } = "Dark";

    [JsonPropertyName("minAppVersion")]
    public string MinAppVersion { get; set; } = "5.3.1";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("manifestUrl")]
    public string ManifestUrl { get; set; } = string.Empty;

    [JsonPropertyName("previewUrl")]
    public string PreviewUrl { get; set; } = string.Empty;

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("rating")]
    public double Rating { get; set; } = 5.0;

    /// <summary>
    /// Preview color palette swatches directly provided in catalog for fast display before download.
    /// </summary>
    [JsonPropertyName("palette")]
    public ThemeColorPalette? Palette { get; set; }
}
