namespace FoliCon.Modules.TMDB;

/// <summary>
/// TMDB service configuration constants.
/// TMDB uses a BYOK (Bring Your Own Key) model — users obtain their own API key
/// from the TMDB developer portal.
/// </summary>
internal static class TmdbAppConfig
{
    /// <summary>
    /// Portal URL where users obtain their TMDB API key.
    /// </summary>
    public const string apiKeyPortalUrl = "https://www.themoviedb.org/settings/api"; // NOSONAR — intentional external portal URL
}
