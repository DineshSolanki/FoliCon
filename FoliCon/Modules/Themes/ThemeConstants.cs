#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FoliCon.Modules.Themes;

/// <summary>
/// Constants and regular expressions for the FoliCon community theme system.
/// </summary>
public static class ThemeConstants
{
    /// <summary>
    /// Subfolder under %AppData%/FoliCon where user-installed themes are stored.
    /// </summary>
    public const string themesFolder = "Themes";

    /// <summary>
    /// File extension for packaged theme archives.
    /// </summary>
    public const string themePackageExtension = ".folicontheme";

    /// <summary>
    /// File name of the declarative JSON theme manifest.
    /// </summary>
    public const string themeJsonFileName = "theme.json";

    /// <summary>
    /// File name of the optional XAML resource dictionary package.
    /// </summary>
    public const string themeXamlFileName = "Theme.xaml";

    /// <summary>
    /// File name of the preview image within a theme folder.
    /// </summary>
    public const string previewImageFileName = "preview.png";

    /// <summary>
    /// Master theme catalog file name in the community repository.
    /// </summary>
    public const string catalogFileName = "catalog.json";

    /// <summary>
    /// Default GitHub repository base URL for the community theme catalog.
    /// </summary>
    public const string defaultRepoThemesUrl = "https://raw.githubusercontent.com/DineshSolanki/FoliCon-Overlays/main/themes";

    /// <summary>
    /// Maximum allowed theme package size in bytes (10 MB).
    /// </summary>
    public const long maxThemePackageSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Regex verifying valid hex color strings: #RGB, #ARGB, #RRGGBB, #AARRGGBB.
    /// </summary>
    public static readonly Regex HexColorRegex = new(
        @"^#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Regex verifying theme IDs: lower-case alphanumeric with hyphens (e.g. "cyberpunk-2077").
    /// </summary>
    public static readonly Regex ThemeIdRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Set of built-in theme IDs that cannot be deleted or uninstalled.
    /// </summary>
    public static readonly HashSet<string> BuiltInThemeIds = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "light",
        "dark",
        "system",
        "the-butterfly",
        "tokyo-neon",
        "mica-dark-obsidian",
        "nordic-pine",
        "cinema-35mm"
    };
}
