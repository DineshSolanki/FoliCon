#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;
using SysJsonSerializer = System.Text.Json.JsonSerializer;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Default implementation of <see cref="IThemeRepositoryService"/> managing catalog querying,
/// safe package downloads, SHA256 integrity verification, and update checking.
/// </summary>
public class ThemeRepositoryService : IThemeRepositoryService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IThemeStorageService _storageService;
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(1);

    private ThemeCatalog? _cachedCatalog;
    private DateTime _cacheTimestamp = DateTime.MinValue;
    private readonly Dictionary<string, string> _availableUpdates = new(StringComparer.OrdinalIgnoreCase);

    public string BaseUrl { get; }

    public ThemeRepositoryService(
        IThemeStorageService storageService,
        HttpClient? httpClient = null,
        string? baseUrl = null,
        string? cacheDirectory = null)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _httpClient = httpClient ?? Services.HttpC;
        BaseUrl = (baseUrl ?? ThemeConstants.defaultRepoThemesUrl).TrimEnd('/');

        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FoliCon",
            "ThemeCache");

        try
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not create theme cache directory: {Path}", _cacheDirectory);
        }
    }

    public async Task<ThemeCatalog> FetchCatalogAsync(CancellationToken ct = default)
    {
        // 1. In-memory cache
        if (_cachedCatalog != null && DateTime.UtcNow - _cacheTimestamp < _cacheTtl)
        {
            Logger.Debug("Returning in-memory cached theme catalog.");
            SyncAvailableUpdates(_cachedCatalog);
            return _cachedCatalog;
        }

        var catalogFile = Path.Combine(_cacheDirectory, ThemeConstants.catalogFileName);

        // 2. Disk cache if fresh
        if (File.Exists(catalogFile))
        {
            var lastWrite = File.GetLastWriteTimeUtc(catalogFile);
            if (DateTime.UtcNow - lastWrite < _cacheTtl)
            {
                try
                {
                    var cachedJson = await File.ReadAllTextAsync(catalogFile, ct);
                    var catalog = SysJsonSerializer.Deserialize<ThemeCatalog>(cachedJson);
                    if (catalog != null && catalog.Themes.Count > 0)
                    {
                        _cachedCatalog = catalog;
                        _cacheTimestamp = lastWrite;
                        Logger.Debug("Loaded theme catalog from disk cache ({Count} themes)", catalog.Themes.Count);
                        SyncAvailableUpdates(_cachedCatalog);
                        return _cachedCatalog;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to read cached theme catalog, fetching fresh.");
                }
            }
        }

        // 3. Network fetch
        return await FetchCatalogFromNetworkAsync(catalogFile, ct);
    }

    private async Task<ThemeCatalog> FetchCatalogFromNetworkAsync(string catalogFile, CancellationToken ct)
    {
        var url = $"{BaseUrl}/catalog.json";
        Logger.Info("Fetching theme catalog from {Url}", url);

        try
        {
            var json = await _httpClient.GetStringAsync(url, ct);
            var catalog = SysJsonSerializer.Deserialize<ThemeCatalog>(json);

            if (catalog == null || catalog.Themes.Count == 0)
            {
                Logger.Warn("Fetched theme catalog is empty or null.");
                return _cachedCatalog ?? new ThemeCatalog();
            }

            _cachedCatalog = catalog;
            _cacheTimestamp = DateTime.UtcNow;

            try
            {
                await File.WriteAllTextAsync(catalogFile, json, ct);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to write theme catalog to disk cache.");
            }

            SyncAvailableUpdates(_cachedCatalog);
            return catalog;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Network request failed for theme catalog. Attempting fallback to stale disk cache.");

            if (File.Exists(catalogFile))
            {
                try
                {
                    var staleJson = await File.ReadAllTextAsync(catalogFile, ct);
                    _cachedCatalog = SysJsonSerializer.Deserialize<ThemeCatalog>(staleJson);
                    if (_cachedCatalog != null)
                    {
                        SyncAvailableUpdates(_cachedCatalog);
                        return _cachedCatalog;
                    }
                }
                catch (Exception cacheEx)
                {
                    Logger.Warn(cacheEx, "Failed to deserialize stale theme catalog.");
                }
            }

            return _cachedCatalog ?? new ThemeCatalog();
        }
    }

    public async Task<ThemeManifest> FetchManifestAsync(string themeId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            throw new ArgumentException("Theme ID cannot be empty.", nameof(themeId));
        }

        var catalog = await FetchCatalogAsync(ct);
        var entry = catalog.Themes.Find(t => string.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase));

        var manifestUrl = !string.IsNullOrEmpty(entry?.ManifestUrl)
            ? ResolveUrl(entry.ManifestUrl)
            : $"{BaseUrl}/{themeId}/{ThemeConstants.themeJsonFileName}";

        Logger.Info("Fetching theme manifest for '{Id}' from {Url}", themeId, manifestUrl);
        var json = await _httpClient.GetStringAsync(manifestUrl, ct);

        var manifest = SysJsonSerializer.Deserialize<ThemeManifest>(json);
        return manifest ?? throw new InvalidOperationException($"Failed to deserialize theme manifest for '{themeId}'.");
    }

    public async Task<ThemeManifest> InstallThemeAsync(
        ThemeCatalogEntry entry,
        IProgress<(int Percent, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (ThemeConstants.BuiltInThemeIds.Contains(entry.Id))
        {
            throw new InvalidOperationException($"Cannot overwrite built-in theme '{entry.Id}'.");
        }

        Logger.Info("Installing theme '{Id}' v{Version}", entry.Id, entry.Version);
        progress?.Report((10, $"Downloading theme {entry.Name}..."));

        // Case A: Package archive (.folicontheme)
        if (!string.IsNullOrEmpty(entry.DownloadUrl))
        {
            var downloadUrl = ResolveUrl(entry.DownloadUrl);
            var packageBytes = await DownloadBytesSafelyAsync(downloadUrl, ThemeConstants.maxThemePackageSizeBytes, ct);

            // Verify SHA256 integrity
            if (!string.IsNullOrEmpty(entry.Sha256))
            {
                progress?.Report((60, "Verifying package integrity..."));
                VerifySha256(packageBytes, entry.Sha256, entry.Id);
            }

            progress?.Report((80, "Extracting theme package..."));
            using var stream = new MemoryStream(packageBytes);
            var installedManifest = await _storageService.InstallThemeFromPackageAsync(stream, ct);

            _availableUpdates.Remove(entry.Id);
            progress?.Report((100, $"Successfully installed {entry.Name}."));
            return installedManifest;
        }

        // Case B: Declarative manifest + preview image
        progress?.Report((30, "Fetching theme manifest..."));
        var manifestUrl = !string.IsNullOrEmpty(entry.ManifestUrl)
            ? ResolveUrl(entry.ManifestUrl)
            : $"{BaseUrl}/{entry.Id}/{ThemeConstants.themeJsonFileName}";

        var manifestJson = await _httpClient.GetStringAsync(manifestUrl, ct);

        byte[]? previewBytes = null;
        if (!string.IsNullOrEmpty(entry.PreviewUrl))
        {
            try
            {
                progress?.Report((50, "Fetching preview image..."));
                var previewUrl = ResolveUrl(entry.PreviewUrl);
                previewBytes = await DownloadBytesSafelyAsync(previewUrl, 2 * 1024 * 1024, ct);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Optional preview image download failed for {Id}", entry.Id);
            }
        }

        progress?.Report((80, "Installing declarative theme..."));
        var manifest = await _storageService.InstallThemeFromJsonAsync(manifestJson, previewBytes, ct);

        _availableUpdates.Remove(entry.Id);
        progress?.Report((100, $"Successfully installed {entry.Name}."));
        return manifest;
    }

    public async Task<ThemeManifest> UpdateThemeAsync(
        string themeId,
        IProgress<(int Percent, string Status)>? progress = null,
        CancellationToken ct = default)
    {
        var catalog = await FetchCatalogAsync(ct);
        var entry = catalog.Themes.Find(t => string.Equals(t.Id, themeId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Theme '{themeId}' not found in community catalog.");

        return await InstallThemeAsync(entry, progress, ct);
    }

    public bool IsUpdateAvailable(string themeId) => _availableUpdates.ContainsKey(themeId);

    public string? GetAvailableVersion(string themeId)
    {
        _availableUpdates.TryGetValue(themeId, out var version);
        return version;
    }

    public void InvalidateCache()
    {
        _cachedCatalog = null;
        _cacheTimestamp = DateTime.MinValue;
        try
        {
            var catalogFile = Path.Combine(_cacheDirectory, ThemeConstants.catalogFileName);
            if (File.Exists(catalogFile))
            {
                File.Delete(catalogFile);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to delete theme catalog cache file.");
        }
    }

    private void SyncAvailableUpdates(ThemeCatalog catalog)
    {
        _availableUpdates.Clear();
        foreach (var entry in catalog.Themes)
        {
            if (ThemeConstants.BuiltInThemeIds.Contains(entry.Id))
            {
                continue;
            }

            var installed = _storageService.GetThemeById(entry.Id);
            if (installed != null)
            {
                if (Version.TryParse(entry.Version, out var remoteVer) &&
                    Version.TryParse(installed.Version, out var localVer))
                {
                    if (remoteVer > localVer)
                    {
                        _availableUpdates[entry.Id] = entry.Version;
                    }
                }
                else if (!string.Equals(entry.Version, installed.Version, StringComparison.OrdinalIgnoreCase))
                {
                    _availableUpdates[entry.Id] = entry.Version;
                }
            }
        }
    }

    private string ResolveUrl(string relativeOrAbsolute)
    {
        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return relativeOrAbsolute;
        }

        return $"{BaseUrl}/{relativeOrAbsolute.TrimStart('/')}";
    }

    private async Task<byte[]> DownloadBytesSafelyAsync(string url, long maxBytes, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw new InvalidOperationException($"Resource at '{url}' exceeds maximum allowed size of {maxBytes} bytes.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length > maxBytes)
        {
            throw new InvalidOperationException($"Downloaded resource at '{url}' exceeded limit ({bytes.Length} > {maxBytes}).");
        }

        return bytes;
    }

    private static void VerifySha256(byte[] data, string expectedHex, string resourceId)
    {
        var hashBytes = SHA256.HashData(data);
        var actualHex = Convert.ToHexStringLower(hashBytes);

        if (!string.Equals(actualHex, expectedHex.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SHA256 checksum mismatch for theme '{resourceId}'. Expected: {expectedHex}, Actual: {actualHex}");
        }
    }
}
