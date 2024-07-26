using FoliCon.Models.Api;
using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.Extension;
using FoliCon.Modules.utils;
using Microsoft.Extensions.Caching.Memory;

namespace FoliCon.Modules.DeviantArt;

public class DArt : BindableBase
{
    private string _clientAccessToken;
    private string _clientSecret;
    private string _clientId;
    
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private static readonly JsonSerializerSettings SerializerSettings = new() { NullValueHandling = NullValueHandling.Ignore };
    
    public string ClientId
    {
        get => _clientId;
        set => SetProperty(ref _clientId, value);
    }

    public string ClientSecret
    {
        get => _clientSecret;
        set => SetProperty(ref _clientSecret, value);
    }

    public string ClientAccessToken
    {
        get => _clientAccessToken;
        set => SetProperty(ref _clientAccessToken, value);
    }

    private DArt(string clientSecret, string clientId)
    {
        Services.Tracker.Configure<DArt>()
            .Property(p => p.ClientAccessToken)
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
        if (!_cache.TryGetValue("DArtToken", out string cachedToken))
        {
            cachedToken = await GenerateNewAccessToken();
        }

        ClientAccessToken = cachedToken;
    }

    public static async Task<bool> IsTokenValidAsync(string clientAccessToken)
    {
        var url = GetPlaceboApiUrl(clientAccessToken);
        using var response = await Services.HttpC.GetAsync(new Uri(url));
        var jsonData = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        return tokenResponse?.Status == "success";
    }

    private async Task<string> GenerateNewAccessToken()
    {
        var url = GetTokenApiUrl();
        using var response = await Services.HttpC.GetAsync(new Uri(url));
        var jsonData = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

        if (tokenResponse == null)
        {
            return string.Empty;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        _cache.Set("DArtToken", tokenResponse.AccessToken, cacheEntryOptions);

        return tokenResponse.AccessToken;

    }

    public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
    {
        await GetClientAccessTokenAsync();

        var url = GetBrowseApiUrl(query, offset);
        using var response = await Services.HttpC.GetAsync(new Uri(url));
        var jsonData = await response.Content.ReadAsStringAsync();
        
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
        await TryExtraction(deviationId, dArtDownloadResponse, CancellationToken.None, new Progress<ProgressInfo>(_ => { }));

        return dArtDownloadResponse;
    }

    private static async Task<DArtDownloadResponse> TryExtraction(string deviationId,
        DArtDownloadResponse dArtDownloadResponse, CancellationToken cancellationToken,
        IProgress<ProgressInfo> progressCallback)
    {
        progressCallback.Report(new ProgressInfo(0, 1, LangProvider.Instance.Downloading));
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
        IProgress<ProgressInfo> progressCallback)
    {
        await GetClientAccessTokenAsync();
        return await TryExtraction(deviationId, dArtDownloadResponse, cancellationToken, progressCallback);

    }
    public async Task<DArtDownloadResponse> GetDArtDownloadResponseAsync(string deviationId)
    {
        var url = GetDownloadApiUrl(deviationId);
        using var response = await Services.HttpC.GetAsync(new Uri(url));
        var jsonData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<DArtDownloadResponse>(jsonData);
    }

    private static async Task ProcessCompressedFiles(HttpResponseMessage downloadResponse, string targetDirectoryPath,
        CancellationToken cancellationToken,
        IProgress<ProgressInfo> progressCallback)
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
    
    private static string GetPlaceboApiUrl(string clientAccessToken)
    {
        return $"https://www.deviantart.com/api/v1/oauth2/placebo?access_token={clientAccessToken}";
    }

    private string GetTokenApiUrl()
    {
        return $"https://www.deviantart.com/oauth2/token?client_id={ClientId}&client_secret={ClientSecret}&grant_type=client_credentials";
    }

    private string GetBrowseApiUrl(string query, int offset)
    {
        return $"https://www.deviantart.com/api/v1/oauth2/browse/newest?timerange=alltime&offset={offset}&q={query} folder icon&limit=20&access_token={ClientAccessToken}";
    }
    
    private string GetDownloadApiUrl(string deviationId)
    {
        return $"https://www.deviantart.com/api/v1/oauth2/deviation/download/{deviationId}?access_token={ClientAccessToken}";
    }
}