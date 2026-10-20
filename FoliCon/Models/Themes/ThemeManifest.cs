#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FoliCon.Models.Themes;

/// <summary>
/// Root manifest for a FoliCon community or built-in theme.
/// Compatible with theme.json declarative schema and .folicontheme packages.
/// </summary>
public sealed class ThemeManifest
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

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

    /// <summary>
    /// Base application theme mode ("Dark" or "Light").
    /// </summary>
    [JsonPropertyName("baseTheme")]
    public string BaseTheme { get; set; } = "Dark";

    [JsonPropertyName("minAppVersion")]
    public string MinAppVersion { get; set; } = "5.3.1";

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("colors")]
    public ThemeColorPalette Colors { get; set; } = new();

    [JsonPropertyName("materials")]
    public ThemeMaterialTokens? Materials { get; set; }

    /// <summary>
    /// Optional relative path to a custom XAML resource dictionary within a .folicontheme package.
    /// </summary>
    [JsonPropertyName("xamlPackageFile")]
    public string? XamlPackageFile { get; set; }

    /// <summary>
    /// Optional relative path to preview image within the theme package.
    /// </summary>
    [JsonPropertyName("previewImageFile")]
    public string? PreviewImageFile { get; set; }

    /// <summary>
    /// Absolute local folder path containing the unpacked theme files. Null for built-in themes.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? PackageDirectory { get; set; }

    /// <summary>
    /// True if this theme is one of the compiled built-in themes.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// Returns true if BaseTheme represents a dark theme.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDark => !string.Equals(BaseTheme, "Light", System.StringComparison.OrdinalIgnoreCase);
}
