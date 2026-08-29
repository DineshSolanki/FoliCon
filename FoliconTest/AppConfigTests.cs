using System.IO;
using System.Text.Json;
using FoliCon.Modules.Configuration;

namespace FoliconTest;

public class AppConfigTests : IDisposable
{
    private readonly string _root;

    public AppConfigTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"FoliconConfig_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void JsonSerializerOptions_ReturnsFreshMutableInstances()
    {
        var config = new AppConfig();

        var first = config.JsonSerializerOptions;
        var second = config.JsonSerializerOptions;

        Assert.NotSame(first, second);
        first.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        second.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    [Fact]
    public void Save_WritesEncryptedSecretValues()
    {
        var config = new AppConfig
        {
            FileName = Path.Combine(_root, "FoliConConfigTest.json"),
            DeviantArtAccessToken = "access-token",
            DeviantArtRefreshToken = "refresh-token",
            DeviantArtClientId = "client-id",
            DeviantArtClientSecret = "client-secret",
            TmdbKey = "tmdb-key",
            IgdbClientId = "igdb-client-id",
            IgdbClientSecret = "igdb-client-secret",
            OnboardingCompleted = true,
            ContextEntryName = "Test Context Entry",
        };

        config.Save();
        config.Save();

        var json = File.ReadAllText(config.FileName);
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement;

        AssertEncrypted(properties, nameof(AppConfig.DeviantArtAccessToken), "access-token");
        AssertEncrypted(properties, nameof(AppConfig.DeviantArtRefreshToken), "refresh-token");
        AssertEncrypted(properties, nameof(AppConfig.DeviantArtClientId), "client-id");
        AssertEncrypted(properties, nameof(AppConfig.DeviantArtClientSecret), "client-secret");
        AssertEncrypted(properties, nameof(AppConfig.TmdbKey), "tmdb-key");
        AssertEncrypted(properties, nameof(AppConfig.IgdbClientId), "igdb-client-id");
        AssertEncrypted(properties, nameof(AppConfig.IgdbClientSecret), "igdb-client-secret");
        Assert.Equal("Test Context Entry", properties.GetProperty(nameof(AppConfig.ContextEntryName)).GetString());
    }

    private static void AssertEncrypted(JsonElement properties, string propertyName, string plaintext)
    {
        var storedValue = properties.GetProperty(propertyName).GetString();

        Assert.NotNull(storedValue);
        Assert.NotEqual(plaintext, storedValue);

        var encryptedBytes = Convert.FromBase64String(storedValue);
        var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
            encryptedBytes,
            null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

        Assert.Equal(plaintext, System.Text.Encoding.UTF8.GetString(decryptedBytes));
    }

    [Fact]
    public void Save_WhenDirectoryDoesNotExist_CreatesParentDirectoryAndSavesFile()
    {
        var nestedDir = Path.Combine(_root, "nested", "subfolder");
        var filePath = Path.Combine(nestedDir, "Config.json");

        var config = new AppConfig
        {
            FileName = filePath,
            ContextEntryName = "Nested Save Test"
        };

        Assert.False(Directory.Exists(nestedDir));
        config.Save();
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(filePath));
        Assert.False(Directory.Exists(filePath));
    }
}
