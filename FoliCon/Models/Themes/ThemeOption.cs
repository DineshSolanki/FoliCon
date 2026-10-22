#nullable enable
using FoliCon.Models.Enums;
using Prism.Mvvm;

namespace FoliCon.Models.Themes;

/// <summary>
/// Represents a selectable theme option in the UI (both built-in and community/user-installed themes).
/// </summary>
public sealed class ThemeOption : BindableBase
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsBuiltIn { get; init; }
    public FoliconThemes? BuiltInTheme { get; init; }
    public ThemeManifest? Manifest { get; init; }

    public override string ToString() => DisplayName;
}
