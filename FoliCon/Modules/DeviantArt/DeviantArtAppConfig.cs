namespace FoliCon.Modules.DeviantArt;

/// <summary>
/// DeviantArt app configuration for OAuth2.
///
/// Two modes:
/// 1. Built-in (FoliCon's registered app) — PKCE, no client secret needed.
/// 2. Custom — user provides their own Client ID (and optionally Client Secret).
///
/// Only Built-in mode supports user.manage scope (opt-in via settings) for watcher-gated icons.
/// When enabled, FoliCon temporarily watches/unwatches artists during icon download.
///
/// Register/update at: https://www.deviantart.com/developers/apps
///
/// IMPORTANT: When changing any value here, also update docs/oauth-redirect.html
/// to match the CallbackPort.
/// </summary>
internal static class DeviantArtAppConfig
{
    // ── Built-in OAuth App Registration ──────────────────────────────────
    public const string clientId = "69659";
    public const string authorizeUrl = "https://www.deviantart.com/oauth2/authorize"; // NOSONAR — intentional DeviantArt API endpoint
    public const string tokenUrl = "https://www.deviantart.com/oauth2/token"; // NOSONAR — intentional DeviantArt API endpoint

    /// <summary>
    /// The OAuth scopes to request. Includes "user.manage" only when the user has opted in.
    /// Changing this requires re-authorization to get a token with the new scope.
    /// </summary>
    public static string Scope =>
        Services.Settings?.DeviantArtWatchEnabled == true ? "browse user.manage" : "browse";

    // ── Redirect Flow ────────────────────────────────────────────────────
    // DeviantArt doesn't support http://localhost redirect URIs, so we use a
    // hosted HTTPS page that reads ?code= from the URL and bounces back to localhost.
    // The hosted page URL (registered with DeviantArt):
    public const string redirectUri = "https://dineshsolanki.github.io/FoliCon/oauth-redirect.html"; // NOSONAR — intentional hosted redirect page
    // The localhost port our HttpListener listens on (must match docs/oauth-redirect.html):
    public const int callbackPort = 6818;
    // Derived — the full local listener URL (used by OAuthCallbackListener):
    public static readonly string LocalCallbackPrefix = $"http://localhost:{callbackPort}/";
}
