namespace FoliCon.Modules.Overlays;

/// <summary>
/// Constants for the overlay plugin system.
/// </summary>
[Localizable(false)]
internal static class OverlayConstants
{
    /// <summary>
    /// Subfolder under %AppData% where user-installed overlays are stored.
    /// </summary>
    public const string overlaysFolder = "Overlays";

    /// <summary>
    /// Subfolder under %LocalAppData% for catalog cache.
    /// </summary>
    public const string cacheFolder = "OverlayCache";

    /// <summary>
    /// Maximum size in bytes for a single image asset (2 MB).
    /// </summary>
    public const long maxImageSizeBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Maximum total size in bytes for an overlay package folder (5 MB).
    /// </summary>
    public const long maxOverlayPackageSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Maximum supported schema version by this app version.
    /// </summary>
    public const int appSupportedSchemaVersion = 1;

    /// <summary>
    /// Built-in overlay IDs that cannot be overridden by community overlays.
    /// </summary>
    public static readonly HashSet<string> BuiltInOverlayIds =
    [
        "legacy", "alternate", "liaher", "faelpessoal", "faelpessoal-horizontal", "windows11"
    ];

    /// <summary>
    /// Default overlay ID when no selection is found or active overlay is invalid.
    /// </summary>
    public const string defaultOverlayId = "liaher";

    [SuppressMessage("Sonar", "S1075:URIs should not be hardcoded", Justification = "This is the default repository base URL.")]
    public const string defaultBaseUrl = "https://raw.githubusercontent.com/DineshSolanki/FoliCon-Overlays/master";

    /// <summary>
    /// JSON file name for the overlay definition within an overlay folder.
    /// </summary>
    public const string overlayJsonFileName = "overlay.json";

    /// <summary>
    /// JSON file name for the catalog.
    /// </summary>
    public const string catalogFileName = "catalog.json";

    /// <summary>
    /// Compares two version strings. Returns true if parsing succeeded;
    /// <paramref name="aIsNewer"/> is true when <paramref name="a"/> is greater than <paramref name="b"/>.
    /// </summary>
    internal static bool TryCompareVersions(string a, string b, out bool aIsNewer)
    {
        aIsNewer = false;
        if (!Version.TryParse(a, out var versionA) || !Version.TryParse(b, out var versionB))
        {
            return false;
        }
        aIsNewer = versionA > versionB;
        return true;
    }
}
