namespace FoliCon.Modules.DeviantArt;

/// <summary>
/// DeviantArt app configuration for OAuth2.
///
/// Two modes:
/// 1. Built-in (FoliCon's registered app) — PKCE, no client secret needed.
/// 2. Custom — user provides their own Client ID (and optionally Client Secret).
///
/// Register/update at: https://www.deviantart.com/developers/apps
/// App type: Public. Scopes: browse.
///
/// IMPORTANT: When changing any value here, also update docs/oauth-redirect.html
/// to match the CallbackPort.
/// </summary>
internal static class DeviantArtAppConfig
{
    // ── Built-in OAuth App Registration ──────────────────────────────────
    public const string ClientId = "69659";
    public const string Scope = "browse";
    public const string AuthorizeUrl = "https://www.deviantart.com/oauth2/authorize";
    public const string TokenUrl = "https://www.deviantart.com/oauth2/token";

    // ── Redirect Flow ────────────────────────────────────────────────────
    // DeviantArt doesn't support http://localhost redirect URIs, so we use a
    // hosted HTTPS page that reads ?code= from the URL and bounces back to localhost.
    // The hosted page URL (registered with DeviantArt):
    public const string RedirectUri = "https://dineshsolanki.github.io/FoliCon/oauth-redirect.html";
    // The localhost port our HttpListener listens on (must match docs/oauth-redirect.html):
    public const int CallbackPort = 6818;
    // Derived — the full local listener URL (used by OAuthCallbackListener):
    public static readonly string LocalCallbackPrefix = $"http://localhost:{CallbackPort}/";
}
