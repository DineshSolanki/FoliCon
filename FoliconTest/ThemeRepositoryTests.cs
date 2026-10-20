#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using Xunit;

namespace FoliconTest;

public class ThemeRepositoryTests : IDisposable
{
    private readonly string _testBaseDir;
    private readonly string _userThemesDir;
    private readonly string _testCacheDir;
    private readonly FakeThemeStorageService _fakeStorageService;

    public ThemeRepositoryTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), "FoliCon_ThemeRepoTests_" + Guid.NewGuid().ToString("N"));
        _userThemesDir = Path.Combine(_testBaseDir, "Themes");
        _testCacheDir = Path.Combine(_testBaseDir, "ThemeCache");
        Directory.CreateDirectory(_userThemesDir);
        Directory.CreateDirectory(_testCacheDir);

        _fakeStorageService = new FakeThemeStorageService(_userThemesDir);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            if (Directory.Exists(_testBaseDir))
            {
                Directory.Delete(_testBaseDir, true);
            }
        }
        catch
        {
            // best-effort test cleanup
        }
    }

    private sealed class FakeThemeStorageService(string dir) : IThemeStorageService
    {
        public string ThemesDirectory { get; } = dir;
        public Dictionary<string, ThemeManifest> Installed = new(StringComparer.OrdinalIgnoreCase);
        public bool PackageInstallCalled { get; private set; }

        public IReadOnlyList<ThemeManifest> GetInstalledThemes() => new List<ThemeManifest>(Installed.Values);
        public IReadOnlyList<ThemeManifest> GetUserThemes() => new List<ThemeManifest>(Installed.Values);
        public ThemeManifest? GetThemeById(string themeId) => Installed.TryGetValue(themeId, out var m) ? m : null;
        public bool IsThemeInstalled(string themeId) => Installed.ContainsKey(themeId);
        public string GetThemeFolderPath(string themeId) => Path.Combine(ThemesDirectory, themeId);

        public Task<ThemeManifest> InstallThemeFromJsonAsync(string json, byte[]? previewImageBytes = null, CancellationToken ct = default)
        {
            var manifest = new ThemeManifest { Id = "from-json", Name = "From JSON" };
            Installed[manifest.Id] = manifest;
            return Task.FromResult(manifest);
        }

        public Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default)
        {
            PackageInstallCalled = true;
            var manifest = new ThemeManifest { Id = "cyberpunk-2077", Name = "Cyberpunk 2077", Version = "1.0.0" };
            Installed[manifest.Id] = manifest;
            return Task.FromResult(manifest);
        }

        public Task<string> ExportThemePackageAsync(string themeId, string destinationZipPath, CancellationToken ct = default)
            => Task.FromResult(destinationZipPath);

        public bool DeleteTheme(string themeId) => Installed.Remove(themeId);
        public void Refresh() { }
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handlerFunc(request));
        }
    }

    [Fact]
    public async Task FetchCatalogAsync_ValidJson_ReturnsCatalogAndCaches()
    {
        var catalogJson = """
        {
          "version": 1,
          "updatedAt": "2026-10-20T12:00:00Z",
          "themes": [
            {
              "id": "cyberpunk-2077",
              "name": "Cyberpunk 2077",
              "description": "High-voltage neon",
              "author": "JohnnySilver",
              "version": "1.2.0",
              "baseTheme": "Dark",
              "tags": ["Neon", "Cyberpunk"],
              "manifestUrl": "themes/cyberpunk-2077/theme.json",
              "previewUrl": "themes/cyberpunk-2077/preview.png",
              "downloadUrl": "themes/cyberpunk-2077/cyberpunk-2077.folicontheme",
              "sha256": "abcdef123456",
              "downloads": 100
            }
          ]
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.EndsWith("/catalog.json", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(catalogJson, Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new HttpClient(handler);
        var repo = new ThemeRepositoryService(_fakeStorageService, client, "https://mock.repo.folicon/themes", _testCacheDir);

        var catalog = await repo.FetchCatalogAsync();

        Assert.NotNull(catalog);
        Assert.Single(catalog.Themes);
        Assert.Equal("cyberpunk-2077", catalog.Themes[0].Id);
        Assert.Equal("Cyberpunk 2077", catalog.Themes[0].Name);
        Assert.Equal("1.2.0", catalog.Themes[0].Version);
    }

    [Fact]
    public async Task FetchCatalogAsync_DetectsAvailableUpdate_WhenInstalledVersionIsOlder()
    {
        var catalogJson = """
        {
          "version": 1,
          "themes": [
            {
              "id": "cyberpunk-2077",
              "name": "Cyberpunk 2077",
              "version": "2.0.0"
            }
          ]
        }
        """;

        _fakeStorageService.Installed["cyberpunk-2077"] = new ThemeManifest
        {
            Id = "cyberpunk-2077",
            Name = "Cyberpunk 2077",
            Version = "1.0.0"
        };

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(catalogJson, Encoding.UTF8, "application/json")
        });

        var client = new HttpClient(handler);
        var repo = new ThemeRepositoryService(_fakeStorageService, client, "https://mock.repo.folicon/themes", _testCacheDir);

        await repo.FetchCatalogAsync();

        Assert.True(repo.IsUpdateAvailable("cyberpunk-2077"));
        Assert.Equal("2.0.0", repo.GetAvailableVersion("cyberpunk-2077"));
    }

    [Fact]
    public async Task InstallThemeAsync_VerifiesSha256Checksum_ThrowsOnMismatch()
    {
        var dummyZipBytes = Encoding.UTF8.GetBytes("fake package content");

        var entryWithWrongHash = new ThemeCatalogEntry
        {
            Id = "cyberpunk-2077",
            Name = "Cyberpunk 2077",
            Version = "1.0.0",
            DownloadUrl = "themes/cyberpunk-2077/package.folicontheme",
            Sha256 = "0000000000000000000000000000000000000000000000000000000000000000"
        };

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(dummyZipBytes)
        });

        var client = new HttpClient(handler);
        var repo = new ThemeRepositoryService(_fakeStorageService, client, "https://mock.repo.folicon/themes", _testCacheDir);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.InstallThemeAsync(entryWithWrongHash));
        Assert.Contains("SHA256 checksum mismatch", ex.Message);
    }

    [Fact]
    public async Task InstallThemeAsync_WithValidSha256_DelegatesToStorageService()
    {
        var dummyZipBytes = Encoding.UTF8.GetBytes("valid package content");
        var actualHash = Convert.ToHexStringLower(SHA256.HashData(dummyZipBytes));

        var entry = new ThemeCatalogEntry
        {
            Id = "cyberpunk-2077",
            Name = "Cyberpunk 2077",
            Version = "1.0.0",
            DownloadUrl = "themes/cyberpunk-2077/package.folicontheme",
            Sha256 = actualHash
        };

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(dummyZipBytes)
        });

        var client = new HttpClient(handler);
        var repo = new ThemeRepositoryService(_fakeStorageService, client, "https://mock.repo.folicon/themes", _testCacheDir);

        var result = await repo.InstallThemeAsync(entry);

        Assert.NotNull(result);
        Assert.Equal("cyberpunk-2077", result.Id);
        Assert.True(_fakeStorageService.PackageInstallCalled);
    }

    [Fact]
    public async Task InstallThemeAsync_RejectsBuiltInThemes()
    {
        var repo = new ThemeRepositoryService(_fakeStorageService);
        var entry = new ThemeCatalogEntry { Id = "dark", Name = "Dark" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.InstallThemeAsync(entry));
        Assert.Contains("Cannot overwrite built-in theme", ex.Message);
    }
}
