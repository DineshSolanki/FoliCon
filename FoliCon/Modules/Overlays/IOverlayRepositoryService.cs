#nullable enable
namespace FoliCon.Modules.Overlays;

/// <summary>
/// Manages the overlay repository: catalog fetching, install, update, and uninstall
/// of community overlays from the FoliCon-Overlays GitHub repository.
/// </summary>
public interface IOverlayRepositoryService
{
    /// <summary>
    /// Fetches the catalog from the remote repository. Uses cached version if fresh (24h TTL).
    /// </summary>
    Task<OverlayCatalog> FetchCatalogAsync();
    Task<OverlayCatalog> FetchCatalogAsync(CancellationToken ct);

    /// <summary>
    /// Fetches the manifest for a specific overlay from the repository.
    /// </summary>
    Task<OverlayManifest> FetchManifestAsync(string overlayId);
    Task<OverlayManifest> FetchManifestAsync(string overlayId, CancellationToken ct);

    /// <summary>
    /// Downloads and installs an overlay. Validates schema, SHA256, and sizes before committing.
    /// </summary>
    Task InstallOverlayAsync(OverlayCatalogEntry entry);
    Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress);
    Task InstallOverlayAsync(OverlayCatalogEntry entry, IProgress<(int Percent, string Status)>? progress, CancellationToken ct);

    /// <summary>
    /// Updates an installed overlay to the version specified in the catalog.
    /// Backs up the current version before replacing.
    /// </summary>
    Task UpdateOverlayAsync(string overlayId);
    Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress);
    Task UpdateOverlayAsync(string overlayId, IProgress<(int Percent, string Status)>? progress, CancellationToken ct);

    /// <summary>
    /// Uninstalls an overlay. If it was the active overlay, resets to the default.
    /// </summary>
    Task UninstallOverlayAsync(string overlayId);

    /// <summary>
    /// Checks if an overlay is installed (user-installed, not built-in).
    /// </summary>
    bool IsOverlayInstalled(string overlayId);

    /// <summary>
    /// Checks if an update is available for an installed overlay.
    /// </summary>
    bool IsUpdateAvailable(string overlayId);

    /// <summary>
    /// Gets the installed version of an overlay, or null if not installed.
    /// </summary>
    string? GetInstalledVersion(string overlayId);

    /// <summary>
    /// The base URL used for fetching catalog and assets.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Clears the catalog cache, forcing a fresh fetch on next call.
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// Marks an overlay as having an available update.
    /// </summary>
    void MarkUpdateAvailable(string overlayId, string availableVersion);

    /// <summary>
    /// Recomputes update availability for all installed overlays against the given catalog,
    /// replacing any previously marked updates. The single source of truth for version
    /// comparison; also invoked automatically after every fresh catalog fetch.
    /// </summary>
    void SyncAvailableUpdates(OverlayCatalog catalog);
}
