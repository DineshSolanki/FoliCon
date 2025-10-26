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

    private string ClientId
    {
        get;
        init => SetProperty(ref field, value);
    }

    private string ClientSecret
    {
        get;
        init => SetProperty(ref field, value);
    }

    private string ClientAccessToken
    {
        get;
        set => SetProperty(ref field, value);
    }

    private DateTime TokenExpiresAt
    {
        get;
        set => SetProperty(ref field, value);
    }

    private DArt(string clientSecret, string clientId)
    {
        Services.Tracker.Configure<DArt>()
            .Property(p => p.ClientAccessToken)
            .Property(p => p.TokenExpiresAt)
            .PersistOn(nameof(PropertyChanged));
        ClientSecret = clientSecret;
        ClientId = clientId;
        Services.Tracker.Track(this);
    }
    
    public static async Task<DArt> GetInstanceAsync(string clientSecret, string clientId)
    {
        DArt dArt = new(clientSecret, clientId);
        await dArt.GetClientAccessTokenAsync();
        return dArt;
    }

    private async Task GetClientAccessTokenAsync()
    {

        if (!string.IsNullOrEmpty(ClientAccessToken) && DateTime.UtcNow < TokenExpiresAt)
        {
            Logger.Debug("Using cached DeviantArt token (expires at {ExpiresAt})", TokenExpiresAt);
            return;
        }

        Logger.Debug("Token expired or not available, generating new token");
        await GenerateNewAccessToken();
    }

    public static async Task<bool> IsTokenValidAsync(string clientAccessToken)
    {
        var url = GetPlaceboApiUrl(clientAccessToken);
        var jsonData = await GetStringWithRetryAsync(new Uri(url));
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        return tokenResponse?.Status == "success";
    }

    private async Task GenerateNewAccessToken()
    {
        Logger.Debug("Requesting new DeviantArt OAuth token");
        var tokenUri = new Uri("https://www.deviantart.com/oauth2/token");
        var form = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["grant_type"] = "client_credentials"
        };
        var jsonData = await PostFormWithRetryAsync(tokenUri, form);
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to obtain DeviantArt access token.");
        }

        ClientAccessToken = tokenResponse.AccessToken;
        TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60);
        
        Logger.Info("DeviantArt token obtained successfully (expires at {ExpiresAt})", TokenExpiresAt);
    }

    public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
    {
        await GetClientAccessTokenAsync();

        var url = GetBrowseApiUrl(query, offset);
        var jsonData = await GetStringWithRetryAsync(new Uri(url));
        
        var result = JsonConvert.DeserializeObject<DArtBrowseResult>(jsonData, SerializerSettings);

        return result;        
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
        var url = GetDownloadApiUrl(deviationId);
        var jsonData = await GetStringWithRetryAsync(new Uri(url));
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
    
    private static string AppendAccessToken(string url, string token) =>
        $"{url}?access_token={Uri.EscapeDataString(token)}";
    
    private static string GetPlaceboApiUrl(string clientAccessToken) =>
        AppendAccessToken($"{DeviantArtApiBase}/placebo", clientAccessToken);

    private string GetBrowseApiUrl(string query, int offset)
    {
        var q = Uri.EscapeDataString($"{query} folder icon");
        return $"{DeviantArtApiBase}/browse/home?offset={offset}&q={q}&limit=20&access_token={Uri.EscapeDataString(ClientAccessToken)}";
    }
    
    private string GetDownloadApiUrl(string deviationId) =>
        AppendAccessToken($"{DeviantArtApiBase}/deviation/download/{Uri.EscapeDataString(deviationId)}", ClientAccessToken);
    
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

    private static async Task<string> ExecuteWithRetryAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> requestFunc, 
        string requestType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RetryPipeline.ExecuteAsync(async ct =>
            {
                using var response = await requestFunc(ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"DeviantArt API {requestType} request failed after all retry attempts");
            throw new InvalidOperationException(
                "Failed to connect to DeviantArt API. Please check your network connection and firewall settings.",
                ex);
        }
    }

    private static Task<string> GetStringWithRetryAsync(Uri uri, CancellationToken cancellationToken = default) =>
        ExecuteWithRetryAsync(ct => Services.HttpC.GetAsync(uri, ct), "GET", cancellationToken);

    private static Task<string> PostFormWithRetryAsync(Uri uri, IDictionary<string, string> formValues, CancellationToken cancellationToken = default) =>
        ExecuteWithRetryAsync(async ct =>
        {
            using var content = new FormUrlEncodedContent(formValues);
            return await Services.HttpC.PostAsync(uri, content, ct);
        }, "POST", cancellationToken);
}