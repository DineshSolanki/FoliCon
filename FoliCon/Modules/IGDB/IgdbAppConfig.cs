namespace FoliCon.Modules.IGDB;

/// <summary>
/// IGDB service configuration constants.
/// IGDB uses a BYOK (Bring Your Own Key) model — users register a Twitch application
/// to obtain Client ID and Client Secret for IGDB API access.
/// </summary>
internal static class IgdbAppConfig
{
    /// <summary>
    /// Portal URL where users register a Twitch application to get IGDB credentials.
    /// </summary>
    public const string credentialsPortalUrl = "https://dev.twitch.tv/console/apps"; // NOSONAR — intentional external portal URL
}
