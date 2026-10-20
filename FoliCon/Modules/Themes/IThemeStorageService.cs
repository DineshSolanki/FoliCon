#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Service managing local theme storage, discovery, packaging, and uninstallation.
/// </summary>
public interface IThemeStorageService
{
    /// <summary>
    /// Gets the root folder path where community/user themes are stored (%AppData%/FoliCon/Themes).
    /// </summary>
    string ThemesDirectory { get; }

    /// <summary>
    /// Returns all available themes: built-in official themes first, followed by user-installed community themes.
    /// </summary>
    IReadOnlyList<ThemeManifest> GetInstalledThemes();

    /// <summary>
    /// Returns only user-installed community themes.
    /// </summary>
    IReadOnlyList<ThemeManifest> GetUserThemes();

    /// <summary>
    /// Finds a theme manifest by its unique ID. Returns null if not installed or recognized.
    /// </summary>
    ThemeManifest? GetThemeById(string themeId);

    /// <summary>
    /// Returns true if a theme with the given ID is installed (built-in or user-installed).
    /// </summary>
    bool IsThemeInstalled(string themeId);

    /// <summary>
    /// Gets the absolute directory path where the theme files reside.
    /// </summary>
    string GetThemeFolderPath(string themeId);

    /// <summary>
    /// Installs a theme from raw JSON manifest content and optional preview image bytes.
    /// </summary>
    Task<ThemeManifest> InstallThemeFromJsonAsync(string json, byte[]? previewImageBytes = null, CancellationToken ct = default);

    /// <summary>
    /// Installs a theme from a .folicontheme ZIP package stream, enforcing zip-slip prevention and manifest verification.
    /// </summary>
    Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default);

    /// <summary>
    /// Exports an installed theme directory to a .folicontheme zip package archive.
    /// </summary>
    Task<string> ExportThemePackageAsync(string themeId, string destinationZipPath, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user-installed theme. Returns false if the theme is built-in or does not exist.
    /// </summary>
    bool DeleteTheme(string themeId);

    /// <summary>
    /// Reloads theme manifests from disk.
    /// </summary>
    void Refresh();
}
