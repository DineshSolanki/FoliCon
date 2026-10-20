#nullable enable
using FoliCon.Models.Themes;

namespace FoliCon.Models.Themes;

/// <summary>
/// Represents a selectable theme option in theme dropdowns, supporting both built-in enum themes
/// and community user-installed themes with unified display names.
/// </summary>
public sealed class ThemeOptionItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public bool IsBuiltIn { get; }
    public ThemeManifest? Manifest { get; }

    public ThemeOptionItem(string id, string displayName, bool isBuiltIn, ThemeManifest? manifest = null)
    {
        Id = id;
        DisplayName = displayName;
        IsBuiltIn = isBuiltIn;
        Manifest = manifest;
    }

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) =>
        obj is ThemeOptionItem other && string.Equals(Id, other.Id, System.StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
}
