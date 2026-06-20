using Polly;
using Polly.Retry;

namespace FoliCon.Modules.DeviantArt;

[Localizable(false)]
public class DArt : BindableBase, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private bool _disposed;

    private const string DeviantArtApiBase = "https://www.deviantart.com/api/v1/oauth2";

    private static readonly JsonSerializerSettings SerializerSettings = new() { NullValueHandling = NullValueHandling.Ignore };

    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(500),
            UseJitter = true,
            OnRetry = args =>
            {
                var attempt = args.AttemptNumber + 1;
                Logger.Warn($"DeviantArt API request attempt {attempt} failed: {args.Outcome.Exception?.Message}. Retrying...");
                return ValueTask.CompletedTask;
            },
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
                ex.InnerException is IOException ||
                ex.InnerException is System.Net.Sockets.SocketException ||
                ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        })
        .Build();

    private string ClientAccessToken
    {
        get;
        set => SetProperty(ref field, value);
    }

    private string RefreshToken
    {
        get;
        set => SetProperty(ref field, value);
    }

    private DateTime TokenExpiresAt
    {
        get;
        set => SetProperty(ref field, value);
    }

    private DArt(string accessToken, string refreshToken, int expiresIn)
    {
        Services.Tracker.Configure<DArt>()
            .Property(p => p.ClientAccessToken)
            .Property(p => p.RefreshToken)
            .Property(p => p.TokenExpiresAt)
            .PersistOn(nameof(PropertyChanged));
        // Track FIRST — this restores any persisted values from a previous session.
        // Then overwrite with the fresh values passed in, so they always take precedence.
        Services.Tracker.Track(this);
        ClientAccessToken = accessToken;
        RefreshToken = refreshToken;
        // Subtract 60 seconds to account for clock skew and network delays.
        TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60);
        Logger.Info("DeviantArt client initialized with existing token (expires at {ExpiresAt})", TokenExpiresAt);
    }

    /// <summary>
    /// Creates a DArt instance using stored tokens from config.
    /// Use this for normal app startup when the user has already authorized.
    /// </summary>
    public static async Task<DArt> GetInstanceAsync()
    {
        var accessToken = Services.Settings.DeviantArtAccessToken;
        var refreshToken = Services.Settings.DeviantArtRefreshToken;
        var expiresAt = Services.Settings.DeviantArtTokenExpiresAt;

        if (string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(refreshToken))
        {
            throw new LocalizedException(
                "No DeviantArt tokens found. User needs to authorize via the Setup Wizard.",
                Lang.DeviantArtNoTokens);
        }

        var dArt = new DArt(accessToken ?? "", refreshToken ?? "", 0);
        // EnsureKind is Utc — System.Text.Json deserializes DateTime as DateTimeKind.Unspecified,
        // which causes the comparison with DateTime.UtcNow to produce wrong results.
        dArt.TokenExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc);

        // Ensure we have a valid token
        await dArt.GetClientAccessTokenAsync();
        return dArt;
    }

    /// <summary>
    /// Initiates the full OAuth2 PKCE authorization flow.
    /// Opens the browser, waits for the user to authorize, and returns a new DArt instance.
    /// Also persists the tokens to AppConfig.
    /// </summary>
    public static async Task<DArt> AuthorizeAsync()
    {
        Logger.Info("Starting DeviantArt authorization flow");
        var result = await OAuthCallbackListener.AuthorizeAsync();

        // Persist tokens to config
        Services.Settings.DeviantArtAccessToken = result.AccessToken;
        Services.Settings.DeviantArtRefreshToken = result.RefreshToken;
        Services.Settings.DeviantArtTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);
        Services.Settings.Save();

        return new DArt(result.AccessToken, result.RefreshToken, result.ExpiresIn);
    }

    /// <summary>
    /// Obtains an access token using client_credentials grant (user's own Client ID + Secret).
    /// Returns a new DArt instance and persists the token.
    /// </summary>
    public static async Task<DArt> AuthorizeWithCredentialsAsync(string clientId, string clientSecret)
    {
        Logger.Info("Starting DeviantArt client_credentials flow");
        var result = await OAuthCallbackListener.ClientCredentialsAsync(clientId, clientSecret);

        // Persist to config — no refresh token for client_credentials
        Services.Settings.DeviantArtAccessToken = result.AccessToken;
        Services.Settings.DeviantArtRefreshToken = "";
        Services.Settings.DeviantArtTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);
        Services.Settings.Save();

        return new DArt(result.AccessToken, "", result.ExpiresIn);
    }

    private async Task GetClientAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(ClientAccessToken) && DateTime.UtcNow < TokenExpiresAt)
        {
            Logger.Debug("Using cached DeviantArt token (expires at {ExpiresAt}, Kind={Kind})", TokenExpiresAt, TokenExpiresAt.Kind);
            return;
        }

        Logger.Debug("Token expired or not available (now={Now}, expiresAt={ExpiresAt}, Kind={Kind}), refreshing",
            DateTime.UtcNow, TokenExpiresAt, TokenExpiresAt.Kind);
        await RefreshAccessTokenAsync();
    }

    public static async Task<bool> IsTokenValidAsync(string clientAccessToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{DeviantArtApiBase}/placebo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", clientAccessToken);
            using var response = await Services.HttpC.SendAsync(request);
            var jsonData = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);
            return tokenResponse?.Status == "success";
        }
        catch
        {
            return false;
        }
    }

    private async Task RefreshAccessTokenAsync()
    {
        // client_credentials mode: no refresh token — re-authenticate with stored credentials
        var customClientId = Services.Settings.DeviantArtClientId;
        if (string.IsNullOrEmpty(RefreshToken) && !string.IsNullOrEmpty(customClientId))
        {
            Logger.Debug("Re-authenticating DeviantArt via client_credentials (no refresh token)");
            try
            {
                var customClientSecret = Services.Settings.DeviantArtClientSecret;
                if (string.IsNullOrEmpty(customClientSecret))
                {
                    throw new LocalizedException(
                        "DeviantArt Client Secret is missing. Please reconfigure via the Setup Wizard.",
                        Lang.DeviantArtClientSecretMissing);
                }

                var result = await OAuthCallbackListener.ClientCredentialsAsync(customClientId, customClientSecret);

                ClientAccessToken = result.AccessToken;
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);

                Services.Settings.DeviantArtAccessToken = result.AccessToken;
                Services.Settings.DeviantArtTokenExpiresAt = TokenExpiresAt;
                Services.Settings.Save();

                Logger.Info("DeviantArt re-authenticated via client_credentials (expires at {ExpiresAt})", TokenExpiresAt);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to re-authenticate DeviantArt via client_credentials.");
                Services.Settings.DeviantArtAccessToken = "";
                Services.Settings.DeviantArtTokenExpiresAt = DateTime.MinValue;
                Services.Settings.Save();
                throw;
            }
            return;
        }

        // OAuth PKCE mode: refresh with refresh token
        if (string.IsNullOrEmpty(RefreshToken))
        {
            throw new LocalizedException(
                "No refresh token available. User needs to re-authorize via the Setup Wizard.",
                Lang.DeviantArtNoRefreshToken);
        }

        Logger.Debug("Refreshing DeviantArt access token");
        try
        {
            // Include client_secret if the user configured custom credentials.
            var clientSecret = Services.Settings.DeviantArtClientSecret;
            var result = await OAuthCallbackListener.RefreshTokenAsync(RefreshToken, clientSecret);

            ClientAccessToken = result.AccessToken;
            RefreshToken = result.RefreshToken;
            // Subtract 60 seconds to account for clock skew and network delays.
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn - 60);

            // Persist updated tokens to config
            Services.Settings.DeviantArtAccessToken = result.AccessToken;
            Services.Settings.DeviantArtRefreshToken = result.RefreshToken;
            Services.Settings.DeviantArtTokenExpiresAt = TokenExpiresAt;
            Services.Settings.Save();

            Logger.Info("DeviantArt token refreshed successfully (expires at {ExpiresAt})", TokenExpiresAt);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to refresh DeviantArt token. User may need to re-authorize.");
            // Clear invalid tokens so the app knows to re-authorize
            Services.Settings.DeviantArtAccessToken = "";
            Services.Settings.DeviantArtRefreshToken = "";
            Services.Settings.DeviantArtTokenExpiresAt = DateTime.MinValue;
            Services.Settings.Save();
            throw;
        }
    }

    public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
    {
        await GetClientAccessTokenAsync();

        var jsonData = await ExecuteWithRetryAsync(
            () => CreateAuthenticatedRequest($"{DeviantArtApiBase}/browse/home?offset={offset}&q={Uri.EscapeDataString($"{query} folder icon")}&limit=20"),
            "Browse");

        var result = JsonConvert.DeserializeObject<DArtBrowseResult>(jsonData, SerializerSettings);

        return result;
    }

    /// <summary>
    /// Watches a DeviantArt user to unlock watcher-gated deviations.
    /// Requires the "watch" OAuth scope
    /// </summary>
    public async Task<bool> WatchAsync(string username)
    {
        await GetClientAccessTokenAsync();
        try
        {
            var jsonData = await ExecuteWithRetryAsync(
                () =>
                {
                    var request = CreateAuthenticatedRequest(
                        $"{DeviantArtApiBase}/user/friends/watch/{Uri.EscapeDataString(username)}",
                        HttpMethod.Post);
                    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["watch[friend]"] = "1",
                        ["watch[deviations]"] = "1",
                        ["watch[journals]"] = "1",
                        ["watch[forum_threads]"] = "0",
                        ["watch[critiques]"] = "0",
                        ["watch[scraps]"] = "0",
                        ["watch[activity]"] = "1",
                        ["watch[collections]"] = "0"
                    });
                    return request;
                },
                "Watch");
            var response = JsonConvert.DeserializeObject<DArtSuccessResponse>(jsonData);
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to watch DeviantArt user {Username}", username);
            return false;
        }
    }

    /// <summary>
    /// Unwatches a DeviantArt user after downloading a watcher-gated deviation.
    /// Non-critical: failures are logged but not propagated.
    /// </summary>
    public async Task UnwatchAsync(string username)
    {
        await GetClientAccessTokenAsync();
        try
        {
            await ExecuteWithRetryAsync(
                () => CreateAuthenticatedRequest($"{DeviantArtApiBase}/user/friends/unwatch/{Uri.EscapeDataString(username)}"),
                "Unwatch");
            Logger.Info("Successfully unwatched DeviantArt user {Username}", username);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to unwatch DeviantArt user {Username} after download", username);
        }
    }

    /// <summary>
    /// Downloads a file from the DeviantArt API.
    /// </summary>
    /// <param name="deviationId">The ID of the deviation.</param>
    /// <returns>The DArtDownloadResponse object containing the download details.</returns>
    public async Task<DArtDownloadResponse> Download(string deviationId)
    {
        await GetClientAccessTokenAsync();
        var dArtDownloadResponse = await GetDArtDownloadResponseAsync(deviationId);
        await TryExtraction(deviationId, dArtDownloadResponse, CancellationToken.None, new Progress<ProgressBarData>(_ => { }));

        return dArtDownloadResponse;
    }

    private static async Task<DArtDownloadResponse> TryExtraction(string deviationId,
        DArtDownloadResponse dArtDownloadResponse, CancellationToken cancellationToken,
        IProgress<ProgressBarData> progressCallback)
    {
        progressCallback.Report(new ProgressBarData(0, 1, LangProvider.Instance.Downloading));
        var targetDirectoryPath = FileUtils.CreateDirectoryInFoliConTemp(deviationId);
        dArtDownloadResponse.LocalDownloadPath = targetDirectoryPath;
        var downloadResponse = await Services.HttpC.GetAsync(dArtDownloadResponse.Src, cancellationToken);

        if (FileUtils.IsCompressedArchive(dArtDownloadResponse.Filename))
        {
            await ProcessCompressedFiles(downloadResponse, targetDirectoryPath,cancellationToken, progressCallback);
        }
        else
        {
            await FileStreamToDestination(downloadResponse, targetDirectoryPath, dArtDownloadResponse.Filename);
        }

        FileUtils.DeleteDirectoryIfEmpty(targetDirectoryPath);
        return dArtDownloadResponse;
    }

    public async Task<DArtDownloadResponse> ExtractDeviation(string deviationId,
        DArtDownloadResponse dArtDownloadResponse, CancellationToken cancellationToken,
        IProgress<ProgressBarData> progressCallback)
    {
        await GetClientAccessTokenAsync();
        return await TryExtraction(deviationId, dArtDownloadResponse, cancellationToken, progressCallback);

    }
    public async Task<DArtDownloadResponse> GetDArtDownloadResponseAsync(string deviationId)
    {
        var jsonData = await ExecuteWithRetryAsync(
            () => CreateAuthenticatedRequest($"{DeviantArtApiBase}/deviation/download/{Uri.EscapeDataString(deviationId)}"),
            "Download");
        return JsonConvert.DeserializeObject<DArtDownloadResponse>(jsonData);
    }

    private static async Task ProcessCompressedFiles(HttpResponseMessage downloadResponse, string targetDirectoryPath,
        CancellationToken cancellationToken,
        IProgress<ProgressBarData> progressCallback)
    {
        await using var stream = await downloadResponse.Content.ReadAsStreamAsync(cancellationToken);
        stream.ExtractPngAndIcoToDirectory(targetDirectoryPath, cancellationToken, progressCallback);
    }

    private static async Task FileStreamToDestination(HttpResponseMessage downloadResponse, string targetDirectoryPath,
        string filename)
    {
        await using var fileStream = await downloadResponse.Content.ReadAsStreamAsync();
        await using var file = File.Create(Path.Combine(targetDirectoryPath, filename));
        await fileStream.CopyToAsync(file);
    }

    /// <summary>
    /// Creates an HttpRequestMessage with the current ClientAccessToken in the Authorization header.
    /// Must be called inside the retry lambda so it picks up a refreshed token.
    /// </summary>
    private HttpRequestMessage CreateAuthenticatedRequest(string url) => CreateAuthenticatedRequest(url, HttpMethod.Get);

    private HttpRequestMessage CreateAuthenticatedRequest(string url, HttpMethod method)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ClientAccessToken);
        return request;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Cleanup resources if needed in the future
        }
        _disposed = true;
    }

    private async Task<string> ExecuteWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        string requestType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RetryPipeline.ExecuteAsync(async ct =>
            {
                // Build request inside the lambda so it reads the current ClientAccessToken
                // (which may have been refreshed by a previous 401 handler)
                using var request = requestFactory();
                using var response = await Services.HttpC.SendAsync(request, ct);

                // Handle 401 Unauthorized: refresh token, then throw to exit the pipeline.
                // The catch block below retries with a fresh request from requestFactory().
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Logger.Warn($"DeviantArt API {requestType} returned 401, attempting token refresh");
                    response.Dispose();

                    if (string.IsNullOrEmpty(RefreshToken))
                    {
                        throw new LocalizedException(
                            "DeviantArt access expired. Please re-authorize via the Setup Wizard.",
                            Lang.DeviantArtAccessExpired);
                    }

                    await RefreshAccessTokenAsync();
                    Logger.Info($"DeviantArt token refreshed, retrying {requestType} request");
                    throw new DeviantArtTokenExpiredException();

                }

                if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync(ct);
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Logger.Warn("DeviantArt API {RequestType} returned {StatusCode}: {ErrorBody}",
                    requestType, (int)response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync(ct);
            }, cancellationToken);
        }
        catch (DeviantArtTokenExpiredException)
        {
            // Token was refreshed — retry with a fresh request that uses the new token
            Logger.Info($"Retrying DeviantArt API {requestType} request after token refresh");
            using var request = requestFactory();
            using var response = await Services.HttpC.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync(cancellationToken);
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Logger.Warn("DeviantArt API {RequestType} returned {StatusCode}: {ErrorBody}",
                requestType, (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DeviantArtTokenExpiredException)
        {
            Logger.Error(ex, $"DeviantArt API {requestType} request failed after all retry attempts");
            throw new LocalizedException(
                "Failed to connect to DeviantArt API. Please check your network connection and firewall settings.",
                Lang.DeviantArtConnectionFailed,
                ex);
        }
    }
}
