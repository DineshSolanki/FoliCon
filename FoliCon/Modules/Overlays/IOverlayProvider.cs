using FoliCon.Models.Data;

namespace FoliCon.Modules.Overlays;

/// <summary>
/// Provides access to all available overlay definitions (built-in + user-installed).
/// This is the single source of truth for resolving overlay IDs into validated definitions.
/// </summary>
public interface IOverlayProvider
{
    /// <summary>
    /// Returns all available overlays: built-in first, then user-installed.
    /// </summary>
    IReadOnlyList<PosterOverlayDefinition> GetAllOverlays();

    /// <summary>
    /// Returns only user-installed overlays.
    /// </summary>
    IReadOnlyList<PosterOverlayDefinition> GetUserOverlays();

    /// <summary>
    /// Gets an overlay definition by its ID. Returns null if not found.
    /// </summary>
    PosterOverlayDefinition? GetOverlayById(string id);

    /// <summary>
    /// Resolves the active overlay ID to a validated definition.
    /// Falls back to the default built-in overlay if the ID is missing, corrupt, or not installed.
    /// </summary>
    PosterOverlayDefinition ResolveActiveOverlayOrDefault(string? activeOverlayId);

    /// <summary>
    /// Returns true if an overlay with the given ID is installed.
    /// </summary>
    bool IsOverlayInstalled(string id);

    /// <summary>
    /// Gets the full path to an overlay's folder.
    /// </summary>
    string GetOverlayFolderPath(string id);

    /// <summary>
    /// Reloads overlays from disk. Called after install/uninstall.
    /// </summary>
    void Refresh();
}
