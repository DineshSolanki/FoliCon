namespace FoliCon.Models.Data;

/// <summary>
/// Represents the auto-generated catalog.json from the FoliCon-Overlays repository.
/// </summary>
[Localizable(false)]
public class OverlayCatalog
{
    public int SchemaVersion { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<OverlayCatalogEntry> Overlays { get; set; } = [];
}

/// <summary>
/// A single overlay entry in the catalog.
/// </summary>
[Localizable(false)]
public class OverlayCatalogEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OverlayVersion { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public string PreviewUrl { get; set; } = string.Empty;
    public string OverlayBaseUrl { get; set; } = string.Empty;
    public string OverlayPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
