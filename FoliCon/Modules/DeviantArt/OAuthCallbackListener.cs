using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace FoliCon.Modules.DeviantArt;

/// <summary>
/// Handles the OAuth2 PKCE authorization code flow for DeviantArt.
///
/// Flow:
/// 1. Browser opens DeviantArt authorize URL with redirect_uri = hosted HTTPS page
/// 2. User authorizes in browser
/// 3. DeviantArt redirects to the HTTPS hosted page with ?code=... in the URL
/// 4. The hosted page's JS redirects to http://localhost:{port}/?code=...
/// 5. This listener captures the code from localhost
/// 6. We exchange the code for tokens via PKCE (code_verifier proves we started the flow)
///
/// All OAuth configuration (client ID, ports, URLs) lives in <see cref="DeviantArtAppConfig"/>.
/// </summary>
public static class OAuthCallbackListener
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Result of the OAuth authorization flow.
    /// </summary>
    public sealed class OAuthResult
    {
        public required string AccessToken { get; init; }
        public required string RefreshToken { get; init; }
        public required int ExpiresIn { get; init; }
    }

    /// <summary>
    /// Initiates the OAuth2 PKCE flow: opens the browser, listens for the callback,
    /// and exchanges the authorization code for tokens.
    /// </summary>
    /// <param name="timeoutMs">How long to wait for the user to authorize (default 2 minutes).</param>
    /// <returns>OAuth tokens if successful.</returns>
    /// <exception cref="TimeoutException">If the user doesn't authorize within the timeout.</exception>
    /// <exception cref="InvalidOperationException">If the authorization fails.</exception>
    public static async Task<OAuthResult> AuthorizeAsync() => await AuthorizeAsync(120_000, CancellationToken.None);

    public static async Task<OAuthResult> AuthorizeAsync(int timeoutMs, CancellationToken ct)
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        const string clientId = DeviantArtAppConfig.clientId;

        Logger.Info("Starting DeviantArt OAuth PKCE flow (client_id={ClientId}, redirect via {RedirectUri})", clientId, DeviantArtAppConfig.redirectUri);

        // Build the authorization URL — redirect_uri is the hosted HTTPS page
        var authorizeUrl = $"{DeviantArtAppConfig.authorizeUrl}" +
                           $"?response_type=code" +
                           $"&client_id={Uri.EscapeDataString(clientId)}" +
                           $"&redirect_uri={Uri.EscapeDataString(DeviantArtAppConfig.redirectUri)}" +
                           $"&scope={Uri.EscapeDataString(DeviantArtAppConfig.Scope)}" +
                           $"&code_challenge={codeChallenge}" +
                           $"&code_challenge_method=S256";

        // Start the localhost listener before opening the browser
        using var listener = new HttpListener();
        listener.Prefixes.Add(DeviantArtAppConfig.LocalCallbackPrefix);
        listener.Start();

        try
        {
            // Open the system browser
            ProcessUtils.StartProcess(authorizeUrl);
            Logger.Debug("Opened browser for DeviantArt authorization");

            // Wait for the callback — the hosted page redirects to localhost:{port}/?code=...
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), ct);
            var code = context.Request.QueryString["code"];
            var error = context.Request.QueryString["error"];

            // Respond to the browser with a "close this tab" page
            var responseHtml = string.IsNullOrEmpty(error)
                ? "<html><body style='font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh'><h2>&#x2705; Authorization successful! You can close this tab.</h2></body></html>"
                : $"<html><body style='font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh'><h2>&#x274C; Authorization failed: {WebUtility.HtmlEncode(error)}</h2></body></html>";

            var responseBytes = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = responseBytes.Length;
            await context.Response.OutputStream.WriteAsync(responseBytes, ct);
            context.Response.Close();

            if (!string.IsNullOrEmpty(error))
            {
                throw new LocalizedException(
                    $"DeviantArt authorization failed: {error}",
                    string.Format(Lang.DeviantArtAuthFailed, error));
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("DeviantArt authorization callback did not include a code parameter.");
            }

            Logger.Info("Received authorization code, exchanging for tokens");

            // Exchange the code for tokens
            return await ExchangeCodeForTokensAsync(code, codeVerifier);
        }
        catch (OperationCanceledException)
        {
            throw new LocalizedException(
                "DeviantArt authorization timed out. Please try again.",
                Lang.DeviantArtAuthTimedOut);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// Exchanges an authorization code for access/refresh tokens using PKCE.
    /// </summary>
    private static async Task<OAuthResult> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = DeviantArtAppConfig.clientId,
            ["code"] = code,
            ["redirect_uri"] = DeviantArtAppConfig.redirectUri,
            ["code_verifier"] = codeVerifier
        };

        var jsonData = await PostFormAsync(DeviantArtAppConfig.tokenUrl, form);
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain DeviantArt access token from authorization code.");
        }

        Logger.Info("DeviantArt tokens obtained successfully");
        return new OAuthResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken ?? "",
            ExpiresIn = tokenResponse.ExpiresIn
        };
    }

    /// <summary>
    /// Refreshes an expired access token using a refresh token.
    /// For non-public (custom) apps, <paramref name="clientSecret"/> must be provided.
    /// For the built-in public app, client_secret is not required by the OAuth2 spec.
    /// </summary>
    public static async Task<OAuthResult> RefreshTokenAsync(string refreshToken, string clientSecret)
    {
        Logger.Debug("Refreshing DeviantArt access token");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = DeviantArtAppConfig.clientId,
            ["refresh_token"] = refreshToken
        };

        if (!string.IsNullOrEmpty(clientSecret))
        {
            form["client_secret"] = clientSecret;
        }

        var jsonData = await PostFormAsync(DeviantArtAppConfig.tokenUrl, form);
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to refresh DeviantArt access token.");
        }

        Logger.Info("DeviantArt token refreshed successfully");
        return new OAuthResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken ?? refreshToken, // Some providers don't rotate refresh tokens
            ExpiresIn = tokenResponse.ExpiresIn
        };
    }

    /// <summary>
    /// Obtains an access token using client_credentials grant.
    /// Used when the user provides their own Client ID and Client Secret.
    /// Returns an access token only — no refresh token (must re-authenticate when expired).
    /// </summary>
    public static async Task<OAuthResult> ClientCredentialsAsync(string clientId, string clientSecret)
    {
        Logger.Debug("Requesting DeviantArt access token via client_credentials grant");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        var jsonData = await PostFormAsync(DeviantArtAppConfig.tokenUrl, form);
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            var error = tokenResponse?.ErrorDescription ?? "Client authentication failed.";
            throw new InvalidOperationException($"DeviantArt client_credentials failed: {error}");
        }

        Logger.Info("DeviantArt access token obtained via client_credentials (expires in {ExpiresIn}s)", tokenResponse.ExpiresIn);
        return new OAuthResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = "", // client_credentials does not return a refresh token
            ExpiresIn = tokenResponse.ExpiresIn
        };
    }

    /// <summary>
    /// Generates a PKCE code verifier — a cryptographically random Base64URL-encoded string.
    /// </summary>
    internal static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 32 bytes = 256 bits
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates a PKCE code challenge from a code verifier (SHA256 hash, Base64URL-encoded).
    /// </summary>
    internal static string GenerateCodeChallenge(string codeVerifier)
    {
        var verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(verifierBytes);
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Base64URL encoding (no padding, URL-safe characters).
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static async Task<string> PostFormAsync(string url, IDictionary<string, string> formValues)
    {
        using var content = new FormUrlEncodedContent(formValues);
        var response = await Services.HttpC.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        var errorBody = await response.Content.ReadAsStringAsync();
        Logger.Error("DeviantArt token endpoint returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
        response.EnsureSuccessStatusCode(); // throws with status code

        return await response.Content.ReadAsStringAsync();
    }
}
