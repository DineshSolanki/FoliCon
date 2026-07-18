namespace FoliCon.Models.Enums;

/// <summary>
/// Legacy enum for built-in overlay types.
/// Replaced by string-based overlay IDs in the plugin system.
/// Use <see cref="FoliCon.Modules.Overlays.IOverlayProvider"/> and overlay ID strings instead.
/// </summary>
[Obsolete("Use string-based overlay IDs with IOverlayProvider instead. " +
           "This enum is retained only for backward-compatible Tracker persistence migration.")]
public enum IconOverlay
{
    Legacy,
    Alternate,
    Liaher,
    Faelpessoal,
    FaelpessoalHorizontal,
    Windows11
}
