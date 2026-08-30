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

    [Fact]
    public void Save_DoesNotEncryptUnannotatedProperties_IncludingPatterns()
    {
        var config = new AppConfig
        {
            FileName = Path.Combine(_root, "PlaintextTest.json"),
            DeviantArtAccessToken = "secret-token",
            ContextEntryName = "Create icons with FoliCon",
            Patterns = [new FoliCon.Models.Data.Pattern("^[0-9]{1,2}x[0-9]{1,2}", false, true)]
        };

        config.Save();

        var json = File.ReadAllText(config.FileName);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Annotated secret must be encrypted
        AssertEncrypted(root, nameof(AppConfig.DeviantArtAccessToken), "secret-token");

        // Unannotated string properties must remain plaintext
        Assert.Equal("Create icons with FoliCon", root.GetProperty(nameof(AppConfig.ContextEntryName)).GetString());

        var patternElement = root.GetProperty(nameof(AppConfig.Patterns))[0];
        Assert.Equal("^[0-9]{1,2}x[0-9]{1,2}", patternElement.GetProperty("Regex").GetString());
    }

    [Fact]
    public void OnDeserialized_RecoversLegacyEncryptedNonSecretProperties()
    {
        // Simulate a corrupted legacy config file where ContextEntryName and Pattern Regex were encrypted with DPAPI
        var plainContextName = "Create icons with FoliCon";
        var plainRegex = "^[0-9]{1,2}x[0-9]{1,2}";

        var encContextBytes = System.Security.Cryptography.ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(plainContextName),
            null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);
        var encRegexBytes = System.Security.Cryptography.ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(plainRegex),
            null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

        var encContextB64 = Convert.ToBase64String(encContextBytes);
        var encRegexB64 = Convert.ToBase64String(encRegexBytes);

        var json = $$"""
        {
          "DeviantArtAccessToken": null,
          "ContextEntryName": "{{encContextB64}}",
          "Patterns": [
            {
              "Regex": "{{encRegexB64}}",
              "IsEnabled": false,
              "IsReadOnly": true
            }
          ]
        }
        """;

        var filePath = Path.Combine(_root, "CorruptedConfig.json");
        File.WriteAllText(filePath, json);

        var loaded = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(loaded);
        loaded.FileName = filePath;

        // Must be healed to plaintext
        Assert.Equal(plainContextName, loaded.ContextEntryName);
        Assert.Equal(plainRegex, loaded.Patterns[0].Regex);

        // When saved again, must be saved as plaintext
        loaded.Save();
        var reSavedJson = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(reSavedJson);
        var root = document.RootElement;

        Assert.Equal(plainContextName, root.GetProperty(nameof(AppConfig.ContextEntryName)).GetString());
        Assert.Equal(plainRegex, root.GetProperty(nameof(AppConfig.Patterns))[0].GetProperty("Regex").GetString());
    }
}
