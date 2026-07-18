namespace FoliCon.Models.Data;

/// <summary>
/// Defines a poster icon overlay package — the JSON schema for overlay.json files.
/// </summary>
[Localizable(false)]
public class PosterOverlayDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OverlayVersion { get; set; } = "1.0.0";
    public string[] Tags { get; set; } = [];
    public bool IsBuiltIn { get; set; }

    // Canvas compatibility — defaults preserve existing compiled overlay coordinates
    public double DesignWidth { get; set; } = 265;
    public double DesignHeight { get; set; } = 256;
    public string RootMargin { get; set; } = "0,0,0,-11";
    public double RenderWidth { get; set; } = 256;
    public double RenderHeight { get; set; } = 256;

    // Layer definitions
    public LayerDefinition? BaseLayer { get; set; }
    public LayerDefinition? FrontLayer { get; set; }

    /// <summary>
    /// Explicit z-order of children in the root Grid.
    /// Valid values: "base", "poster", "front", "rating", "title".
    /// If null, defaults to ["base","poster","front","rating","title"].
    /// Must match the original compiled XAML child order exactly.
    /// </summary>
    public string[]? LayerOrder { get; set; }

    // Poster image configuration
    public PosterConfig Poster { get; set; } = new();

    // Rating badge configuration
    public RatingConfig Rating { get; set; } = new();

    // Title configuration (optional)
    public TitleConfig Title { get; set; } = new();
}
