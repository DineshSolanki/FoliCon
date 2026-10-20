#nullable enable
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Memento snapshot of the application theme state prior to a transient live preview.
/// Enables guaranteed rollback on cancellation or unexpected window closing.
/// </summary>
public sealed class ThemeStateSnapshot
{
    public ThemeManifest? ActiveThemeManifest { get; init; }
    public string? ActiveThemeId { get; init; }
    public FoliconThemes CurrentTheme { get; init; }
    public bool UsingSystemTheme { get; init; }
}
