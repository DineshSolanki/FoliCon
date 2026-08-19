using System.Text.Json;
using FoliCon.Models.Data;

namespace FoliconTest;

/// <summary>
/// Unit tests for <see cref="OverlayManifest"/> data model.
/// </summary>
public class OverlayManifestTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var manifest = new OverlayManifest();

        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal(string.Empty, manifest.Id);
        Assert.Equal(string.Empty, manifest.DisplayName);
        Assert.Equal(string.Empty, manifest.Author);
        Assert.Equal(string.Empty, manifest.Description);
        Assert.Equal("1.0.0", manifest.OverlayVersion);
        Assert.Empty(manifest.Tags);
        Assert.Equal(string.Empty, manifest.PreviewImage);
        Assert.Empty(manifest.Assets);
        Assert.Empty(manifest.Sha256);
        Assert.Equal(0, manifest.SizeBytes);
    }

    [Fact]
    public void DeserializeFromJson_RoundTrips()
    {
        var json = """
        {
            "schemaVersion": 1,
            "id": "neon-glow",
            "displayName": "Neon Glow",
            "author": "TestAuthor",
            "description": "Cyberpunk-style neon overlay",
            "overlayVersion": "2.1.0",
            "tags": ["neon", "cyberpunk", "modern"],
            "previewImage": "preview.png",
            "assets": ["overlay.json", "base.png", "front.png", "preview.png"],
            "sha256": {
                "overlay.json": "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
                "base.png": "b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3"
            },
            "sizeBytes": 245000,
            "createdAt": "2026-07-01T00:00:00Z",
            "updatedAt": "2026-07-15T00:00:00Z"
        }
        """;

        var manifest = JsonSerializer.Deserialize<OverlayManifest>(json, JsonOptions);

        Assert.NotNull(manifest);
        Assert.Equal(1, manifest.SchemaVersion);
        Assert.Equal("neon-glow", manifest.Id);
        Assert.Equal("Neon Glow", manifest.DisplayName);
        Assert.Equal("TestAuthor", manifest.Author);
        Assert.Equal("Cyberpunk-style neon overlay", manifest.Description);
        Assert.Equal("2.1.0", manifest.OverlayVersion);
        Assert.Equal(3, manifest.Tags.Length);
        Assert.Contains("neon", manifest.Tags);
        Assert.Contains("cyberpunk", manifest.Tags);
        Assert.Equal("preview.png", manifest.PreviewImage);
        Assert.Equal(4, manifest.Assets.Length);
        Assert.Contains("overlay.json", manifest.Assets);
        Assert.Equal(2, manifest.Sha256.Count);
        Assert.True(manifest.Sha256.ContainsKey("overlay.json"));
        Assert.Equal(245000, manifest.SizeBytes);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), manifest.CreatedAt);
    }

    [Fact]
    public void SerializeToJson_ContainsAllFields()
    {
        var manifest = new OverlayManifest
        {
            Id = "test-overlay",
            DisplayName = "Test",
            Author = "Author",
            OverlayVersion = "1.0.0",
            Tags = ["test"],
            Assets = ["overlay.json"],
            SizeBytes = 1000
        };

        var json = JsonSerializer.Serialize(manifest, JsonOptions);

        // Verify the JSON contains expected camelCase field names
        Assert.Contains("\"id\":\"test-overlay\"", json);
        Assert.Contains("\"displayName\":\"Test\"", json);

        var deserialized = JsonSerializer.Deserialize<OverlayManifest>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("test-overlay", deserialized.Id);
        Assert.Equal("Test", deserialized.DisplayName);
        Assert.Single(deserialized.Tags);
        Assert.Single(deserialized.Assets);
    }

    [Fact]
    public void ToCatalogEntry_MapsFieldsCorrectly()
    {
        var manifest = new OverlayManifest
        {
            Id = "neon-glow",
            DisplayName = "Neon Glow",
            Author = "TestAuthor",
            Description = "A neon overlay",
            OverlayVersion = "1.2.0",
            Tags = ["neon"],
            PreviewImage = "preview.png",
            SizeBytes = 200000,
            Sha256 = new Dictionary<string, string>
            {
                ["overlay.json"] = "abc123",
                ["base.png"] = "def456"
            },
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var entry = manifest.ToCatalogEntry("https://example.com/overlays");

        Assert.Equal("neon-glow", entry.Id);
        Assert.Equal("Neon Glow", entry.DisplayName);
        Assert.Equal("TestAuthor", entry.Author);
        Assert.Equal("A neon overlay", entry.Description);
        Assert.Equal("1.2.0", entry.OverlayVersion);
        Assert.Single(entry.Tags);
        Assert.Equal("https://example.com/overlays/neon-glow/preview.png", entry.PreviewUrl);
        Assert.Equal("https://example.com/overlays", entry.OverlayBaseUrl);
        Assert.Equal("neon-glow", entry.OverlayPath);
        Assert.Equal(200000, entry.SizeBytes);
        Assert.Equal("abc123", entry.Sha256); // overlay.json hash
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), entry.CreatedAt);
    }

    [Fact]
    public void ToCatalogEntry_WhenNoOverlayJsonHash_ReturnsEmpty()
    {
        var manifest = new OverlayManifest
        {
            Id = "test",
            Sha256 = new Dictionary<string, string> { ["base.png"] = "abc" }
        };

        var entry = manifest.ToCatalogEntry("https://example.com");

        Assert.Equal(string.Empty, entry.Sha256);
    }

    [Fact]
    public void Deserialize_EmptyJson_HasDefaults()
    {
        const string json = "{}";
        var manifest = JsonSerializer.Deserialize<OverlayManifest>(json, JsonOptions);

        Assert.NotNull(manifest);
        Assert.Equal(1, manifest.SchemaVersion); // default value in class
        Assert.Equal(string.Empty, manifest.Id);
        Assert.Empty(manifest.Tags);
        Assert.Empty(manifest.Assets);
    }
}
