namespace FoliCon.Models.Data;

/// <summary>
/// Per-overlay manifest stored as manifest.json alongside the overlay files
/// in the FoliCon-Overlays GitHub repository. Contains metadata, asset inventory,
/// and per-file SHA256 integrity hashes for secure download validation.
/// </summary>
[Localizable(false)]
public class OverlayManifest
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    /// Unique overlay identifier (lowercase, hyphens allowed). Must match the folder name.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version string (e.g. "1.0.0"). Used for update detection.
    /// </summary>
    public string OverlayVersion { get; set; } = "1.0.0";

    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Relative path to the preview image within the overlay folder (e.g. "preview.png").
    /// </summary>
    public string PreviewImage { get; set; } = string.Empty;

    /// <summary>
    /// List of all files in the overlay package, including overlay.json and all assets.
    /// Used to enumerate what needs to be downloaded and validated.
    /// </summary>
    public string[] Assets { get; set; } = [];

    /// <summary>
    /// Per-file SHA256 hashes for integrity verification after download.
    /// Key is the filename (relative to the overlay folder), value is the hex-encoded hash.
    /// </summary>
    public Dictionary<string, string> Sha256 { get; set; } = new();

    /// <summary>
    /// Total download size in bytes (sum of all assets).
    /// </summary>
    public long SizeBytes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Convert this manifest to an OverlayCatalogEntry for use in the catalog.
    /// </summary>
    public OverlayCatalogEntry ToCatalogEntry(string overlayBaseUrl)
    {
        return new OverlayCatalogEntry
        {
            Id = Id,
            DisplayName = DisplayName,
            Author = Author,
            Description = Description,
            OverlayVersion = OverlayVersion,
            Tags = Tags,
            PreviewUrl = $"{overlayBaseUrl}/{Id}/{PreviewImage}",
            OverlayBaseUrl = overlayBaseUrl,
            OverlayPath = Id,
            SizeBytes = SizeBytes,
            Sha256 = Sha256.GetValueOrDefault("overlay.json", string.Empty),
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
