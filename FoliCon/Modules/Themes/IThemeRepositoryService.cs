#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Service managing community theme repository discovery, downloading, integrity verification,
/// and update checking from the FoliCon-Overlays repository (`themes/catalog.json`).
/// </summary>
public interface IThemeRepositoryService
{
    /// <summary>
    /// Base URL for the themes repository (defaults to https://raw.githubusercontent.com/DineshSolanki/FoliCon-Overlays/main/themes).
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Fetches the theme catalog. Returns in-memory or disk-cached copy if within cache TTL (1 hour).
    /// </summary>
    Task<ThemeCatalog> FetchCatalogAsync(CancellationToken ct = default);

    /// <summary>
    /// Fetches full ThemeManifest JSON for a specific theme from the remote repository.
    /// </summary>
    Task<ThemeManifest> FetchManifestAsync(string themeId, CancellationToken ct = default);

    /// <summary>
    /// Downloads and installs a theme package or declarative manifest from catalog entry.
    /// Verifies SHA256 integrity when a hash is provided.
    /// </summary>
    Task<ThemeManifest> InstallThemeAsync(
        ThemeCatalogEntry entry,
        IProgress<(int Percent, string Status)>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an installed theme to the version provided in the catalog.
    /// </summary>
    Task<ThemeManifest> UpdateThemeAsync(
        string themeId,
        IProgress<(int Percent, string Status)>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether an update is available for an installed theme against the fetched catalog.
    /// </summary>
    bool IsUpdateAvailable(string themeId);

    /// <summary>
    /// Gets the latest catalog version available for a given theme id.
    /// </summary>
    string? GetAvailableVersion(string themeId);

    /// <summary>
    /// Invalidates in-memory and disk cache, forcing a fresh catalog fetch.
    /// </summary>
    void InvalidateCache();
}
