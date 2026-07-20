using System.Security.Cryptography;

#nullable enable
namespace FoliCon.Modules.Overlays;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Manages the overlay repository: catalog fetching with ETag caching,
/// atomic install/update/uninstall, and SHA256 integrity verification.
/// </summary>
[Localizable(false)]
public class OverlayRepositoryService : IOverlayRepositoryService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [SuppressMessage("Sonar", "S1075:URIs should not be hardcoded", Justification = "This is the default repository base URL.")]
    private const string defaultBaseUrl = OverlayConstants.defaultBaseUrl;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IOverlayProvider _overlayProvider;
    private readonly string _cacheDir;
    private readonly string _userOverlaysDir;

    // In-memory cache
    private OverlayCatalog? _cachedCatalog;
    private DateTime _cacheTimestamp;

    // Tracks which overlays have updates available
    private readonly Dictionary<string, string> _availableUpdates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new OverlayRepositoryService.
    /// Base URL resolution order:
    /// 1. Explicit baseUrl parameter
    /// 2. FOLICON_OVERLAY_REPO_URL environment variable
    /// 3. Default GitHub URL
    ///
    /// Supports file:// URIs for local testing (e.g. "file:///E:/FoliCon-Overlays").
    /// </summary>
    public OverlayRepositoryService(IOverlayProvider overlayProvider,
        string? userOverlaysDir = null, string? cacheDir = null, string? baseUrl = null)
    {
        _overlayProvider = overlayProvider;
        _cacheDir = cacheDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FoliCon", OverlayConstants.cacheFolder);
        _userOverlaysDir = userOverlaysDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FoliCon", OverlayConstants.overlaysFolder);
        BaseUrl = baseUrl
            ?? Environment.GetEnvironmentVariable("FOLICON_OVERLAY_REPO_URL")
            ?? ReadLocalOverrideFile()
            ?? defaultBaseUrl;
        Logger.Info("OverlayRepositoryService initialized with base URL: {BaseUrl}", BaseUrl);
    }

    /// <summary>
    /// Reads a local override URL from a .overlay-repo-url file in the app directory.
    /// Useful for local development — just create this file with a single line containing the URL.
    /// </summary>
    private static string? ReadLocalOverrideFile()
    {
        try
        {
            var overrideFile = Path.Combine(AppContext.BaseDirectory, ".overlay-repo-url");
            if (File.Exists(overrideFile))
            {
                var url = File.ReadAllText(overrideFile).Trim();
                if (!string.IsNullOrEmpty(url))
                {
                    Logger.Info("Using local override URL from {File}: {Url}", overrideFile, url);
                    return url;
                }
            }
        }
        catch { /* best effort */ }
        return null;
    }

    public string BaseUrl { get; }

    public Task<OverlayCatalog> FetchCatalogAsync() => FetchCatalogAsync(default);
    public async Task<OverlayCatalog> FetchCatalogAsync(CancellationToken ct)
    {
        // Return in-memory cache if fresh
        if (_cachedCatalog != null && DateTime.UtcNow - _cacheTimestamp < CacheTtl)
        {
            Logger.Debug("Returning in-memory cached catalog");
            return _cachedCatalog;
        }

        // Try disk cache
        var catalogPath = Path.Combine(_cacheDir, OverlayConstants.catalogFileName);
        if (!File.Exists(catalogPath))
        {
            return await FetchCatalogFromNetworkAsync(catalogPath, ct);
        }

        var lastWrite = File.GetLastWriteTimeUtc(catalogPath);
        if (DateTime.UtcNow - lastWrite >= CacheTtl)
        {
            return await FetchCatalogFromNetworkAsync(catalogPath, ct);
        }

        try
        {
            var cachedJson = await File.ReadAllTextAsync(catalogPath, ct);
            _cachedCatalog = JsonConvert.DeserializeObject<OverlayCatalog>(cachedJson);
            if (_cachedCatalog != null)
            {
                _cacheTimestamp = lastWrite;
                Logger.Debug("Loaded catalog from disk cache ({Count} overlays)", _cachedCatalog.Overlays.Count);
                return _cachedCatalog;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to read cached catalog, fetching fresh");
        }

        // Fetch from network
        return await FetchCatalogFromNetworkAsync(catalogPath, ct);
    }

    private async Task<OverlayCatalog> FetchCatalogFromNetworkAsync(string catalogPath, CancellationToken ct)
    {
        var url = $"{BaseUrl}/catalog.json";
        Logger.Info("Fetching catalog from {Url}", url);

        try
        {
            var json = await Services.HttpC.GetStringAsync(url, ct);
            var catalog = JsonConvert.DeserializeObject<OverlayCatalog>(json);

            if (catalog == null || catalog.Overlays.Count == 0)
            {
                Logger.Warn("Fetched catalog is null or empty");
                return _cachedCatalog ?? new OverlayCatalog();
            }

            // Cache to disk
            Directory.CreateDirectory(_cacheDir);
            await File.WriteAllTextAsync(catalogPath, json, ct);

            _cachedCatalog = catalog;
            _cacheTimestamp = DateTime.UtcNow;
            Logger.Info("Fetched and cached catalog with {Count} overlays", catalog.Overlays.Count);

            // Check for available updates
            CheckForUpdates(catalog);

            return catalog;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to fetch catalog from {Url}", url);

            // Fall back to stale disk cache
            if (!File.Exists(catalogPath))
            {
                return new OverlayCatalog();
            }

            try
            {
                var staleJson = await File.ReadAllTextAsync(catalogPath, ct);
                _cachedCatalog = JsonConvert.DeserializeObject<OverlayCatalog>(staleJson);
                if (_cachedCatalog != null)
                {
                    Logger.Info("Using stale disk cache ({Count} overlays)", _cachedCatalog.Overlays.Count);
                    return _cachedCatalog;
                }
            }
            catch (Exception cacheEx)
            {
                Logger.Warn(cacheEx, "Failed to read stale cache");
            }

            return new OverlayCatalog();
        }
    }

    public Task<OverlayManifest> FetchManifestAsync(string overlayId) => FetchManifestAsync(overlayId, default);
    public async Task<OverlayManifest> FetchManifestAsync(string overlayId, CancellationToken ct)
    {
        var url = $"{BaseUrl}/overlays/{overlayId}/manifest.json";
        Logger.Info("Fetching manifest for '{Id}' from {Url}", overlayId, url);

        var json = await Services.HttpC.GetStringAsync(url, ct);
        var manifest = JsonConvert.DeserializeObject<OverlayManifest>(json);

        return manifest ?? throw new InvalidOperationException($"Failed to deserialize manifest for '{overlayId}'");
    }

    public Task InstallOverlayAsync(OverlayCatalogEntry entry) => InstallOverlayAsync(entry, null, default);
    public Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress) => InstallOverlayAsync(entry, progress, default);
    public async Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress, CancellationToken ct)
    {
        Logger.Info("Installing overlay '{Id}' v{Version}", entry.Id, entry.OverlayVersion);

        // Prevent installing over built-in overlays
        if (OverlayConstants.BuiltInOverlayIds.Contains(entry.Id, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Cannot install '{entry.Id}': it is a built-in overlay.");
        }

        var tmpDir = Path.Combine(_userOverlaysDir, $"{entry.Id}.tmp");
        var finalDir = Path.Combine(_userOverlaysDir, entry.Id);

        try
        {
            // Clean up any previous failed install
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, true);
            }

            Directory.CreateDirectory(tmpDir);

            // Fetch manifest to get asset list and hashes
            progress?.Report((10, "Fetching manifest..."));
            var manifest = await FetchManifestAsync(entry.Id, ct);

            // Download all assets
            try
            {
                await DownloadAssetsAsync(entry.Id, manifest, tmpDir, progress, ct);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download assets for overlay '{entry.Id}'", ex);
            }

            // Validate overlay.json schema
            progress?.Report((85, "Validating..."));
            await ValidateInstalledOverlayAsync(tmpDir, ct);

            // Atomic rename: tmp → final
            progress?.Report((95, "Installing..."));
            if (Directory.Exists(finalDir))
            {
                Directory.Delete(finalDir, true);
            }

            Directory.Move(tmpDir, finalDir);

            // Refresh overlay provider
            _overlayProvider.Refresh();

            // Clear update marker
            _availableUpdates.Remove(entry.Id);

            progress?.Report((100, "Installed."));
            Logger.Info("Successfully installed overlay '{Id}' v{Version}", entry.Id, entry.OverlayVersion);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}'", entry.Id);
            // Clean up temp directory
            if (Directory.Exists(tmpDir))
            {
                try { Directory.Delete(tmpDir, true); } catch { /* best effort */ }
            }
            throw;
        }
    }

    private async Task DownloadAssetsAsync(string overlayId, OverlayManifest manifest, string targetDir, IProgress<(int Percent, string Status)>? progress, CancellationToken ct)
    {
        var totalAssets = manifest.Assets.Length;
        for (var i = 0; i < totalAssets; i++)
        {
            var asset = manifest.Assets[i];
            var percent = 10 + (int)((i + 1) / (double)totalAssets * 70);
            progress?.Report((percent, $"Downloading {asset}..."));

            var assetUrl = $"{BaseUrl}/overlays/{overlayId}/{asset}";
            var bytes = await Services.HttpC.GetByteArrayAsync(assetUrl, ct);

            VerifyAsset(asset, bytes, manifest);

            var assetPath = Path.Combine(targetDir, asset);
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
            await File.WriteAllBytesAsync(assetPath, bytes, ct);
        }
    }

    private static void VerifyAsset(string asset, byte[] bytes, OverlayManifest manifest)
    {
        // Size check
        if (bytes.Length > OverlayConstants.maxImageSizeBytes && asset != OverlayConstants.overlayJsonFileName)
        {
            throw new InvalidOperationException(
                $"Asset '{asset}' exceeds {OverlayConstants.maxImageSizeBytes / 1024 / 1024}MB limit.");
        }

        // SHA256 verification
        if (manifest.Sha256.TryGetValue(asset, out var expectedHash))
        {
            var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"SHA256 mismatch for '{asset}': expected {expectedHash}, got {actualHash}");
            }
        }
    }

    private static async Task ValidateInstalledOverlayAsync(string tmpDir, CancellationToken ct)
    {
        var overlayJsonPath = Path.Combine(tmpDir, OverlayConstants.overlayJsonFileName);
        var definitionJson = await File.ReadAllTextAsync(overlayJsonPath, ct);
        var definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(definitionJson);
        if (definition == null)
        {
            throw new InvalidOperationException("overlay.json deserialized to null");
        }

        var errors = Internal.OverlayValidator.Validate(tmpDir, definition);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Validation failed: {string.Join("; ", errors)}");
        }
    }

    public Task UpdateOverlayAsync(string overlayId) => UpdateOverlayAsync(overlayId, null, default);
    public Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress) => UpdateOverlayAsync(overlayId, progress, default);
    public async Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress, CancellationToken ct)
    {
        Logger.Info("Updating overlay '{Id}'", overlayId);

        var catalog = await FetchCatalogAsync(ct);
        var entry = catalog.Overlays.FirstOrDefault(o =>
            string.Equals(o.Id, overlayId, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new InvalidOperationException($"Overlay '{overlayId}' not found in catalog.");
        }

        var finalDir = Path.Combine(_userOverlaysDir, overlayId);
        var backupDir = Path.Combine(_userOverlaysDir, $"{overlayId}_previous");

        // Backup current version
        if (Directory.Exists(finalDir))
        {
            progress?.Report((5, "Backing up current version..."));
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
            }

            Directory.Move(finalDir, backupDir);
        }

        try
        {
            // Install new version
            await InstallOverlayAsync(entry, progress, ct);

            // Remove backup on success
            if (Directory.Exists(backupDir))
            {
                Directory.Delete(backupDir, true);
            }

            Logger.Info("Successfully updated overlay '{Id}'", overlayId);
        }
        catch (Exception ex)
        {
            // Rollback: restore backup
            Logger.Warn(ex, "Update failed for '{Id}', rolling back", overlayId);
            if (Directory.Exists(finalDir))
            {
                Directory.Delete(finalDir, true);
            }

            if (Directory.Exists(backupDir))
            {
                Directory.Move(backupDir, finalDir);
            }

            throw new InvalidOperationException($"Failed to update overlay '{overlayId}'", ex);
        }
    }

    public Task UninstallOverlayAsync(string overlayId)
    {
        Logger.Info("Uninstalling overlay '{Id}'", overlayId);

        var overlayDir = Path.Combine(_userOverlaysDir, overlayId);
        if (Directory.Exists(overlayDir))
        {
            Directory.Delete(overlayDir, true);
        }

        // Remove backup too
        var backupDir = Path.Combine(_userOverlaysDir, $"{overlayId}_previous");
        if (Directory.Exists(backupDir))
        {
            Directory.Delete(backupDir, true);
        }

        _overlayProvider.Refresh();
        _availableUpdates.Remove(overlayId);

        Logger.Info("Successfully uninstalled overlay '{Id}'", overlayId);
        return Task.CompletedTask;
    }

    public bool IsOverlayInstalled(string overlayId)
    {
        if (OverlayConstants.BuiltInOverlayIds.Contains(overlayId, StringComparer.OrdinalIgnoreCase))
        {
            return false; // built-in is not "installed" (user-installed)
        }

        var overlayDir = Path.Combine(_userOverlaysDir, overlayId);
        return Directory.Exists(overlayDir) &&
               File.Exists(Path.Combine(overlayDir, OverlayConstants.overlayJsonFileName));
    }

    public bool IsUpdateAvailable(string overlayId) => _availableUpdates.ContainsKey(overlayId);

    public string? GetInstalledVersion(string overlayId)
    {
        var overlay = _overlayProvider.GetOverlayById(overlayId);
        if (overlay != null && !overlay.IsBuiltIn)
        {
            return overlay.OverlayVersion;
        }

        // Try reading from disk
        var overlayJsonPath = Path.Combine(_userOverlaysDir, overlayId, OverlayConstants.overlayJsonFileName);
        if (!File.Exists(overlayJsonPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(overlayJsonPath);
            var definition = JsonConvert.DeserializeObject<PosterOverlayDefinition>(json);
            return definition?.OverlayVersion;
        }
        catch { return null; }
    }

    public void MarkUpdateAvailable(string overlayId, string availableVersion) => _availableUpdates[overlayId] = availableVersion;

    public void InvalidateCache()
    {
        _cachedCatalog = null;
        _cacheTimestamp = DateTime.MinValue;
        _availableUpdates.Clear();

        var catalogPath = Path.Combine(_cacheDir, OverlayConstants.catalogFileName);
        if (File.Exists(catalogPath))
        {
            try { File.Delete(catalogPath); } catch { /* best effort */ }
        }

        Logger.Info("Catalog cache invalidated");
    }

    private void CheckForUpdates(OverlayCatalog catalog)
    {
        _availableUpdates.Clear();

        var installedOverlays = _overlayProvider.GetUserOverlays();
        foreach (var installed in installedOverlays)
        {
            var catalogEntry = catalog.Overlays.FirstOrDefault(o =>
                string.Equals(o.Id, installed.Id, StringComparison.OrdinalIgnoreCase));

            if (catalogEntry == null)
            {
                continue;
            }

            if (!OverlayConstants.TryCompareVersions(catalogEntry.OverlayVersion, installed.OverlayVersion,
                    out var isNewer) || !isNewer)
            {
                continue;
            }

            _availableUpdates[installed.Id] = catalogEntry.OverlayVersion;
            Logger.Info("Update available for '{Id}': {Installed} → {Available}",
                installed.Id, installed.OverlayVersion, catalogEntry.OverlayVersion);
        }
    }

}
