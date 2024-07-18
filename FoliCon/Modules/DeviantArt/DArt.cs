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

    public DArt(string clientSecret, string clientId)
    {
        Services.Tracker.Configure<DArt>()
            .Property(p => p.ClientAccessToken)
            .PersistOn(nameof(PropertyChanged));
        ClientSecret = clientSecret;
        ClientId = clientId;
        Services.Tracker.Track(this);
        GetClientAccessTokenAsync();
    }

    public async void GetClientAccessTokenAsync()
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

        if (tokenResponse == null) return string.Empty;
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(tokenResponse.ExpiresIn));
        _cache.Set("DArtToken", tokenResponse.AccessToken, cacheEntryOptions);

        return tokenResponse.AccessToken;

    }

    public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
    {
        GetClientAccessTokenAsync();

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
        GetClientAccessTokenAsync();
        var dArtDownloadResponse = await GetDArtDownloadResponseAsync(deviationId);
        await TryExtraction(deviationId, dArtDownloadResponse, new Progress<ExtractionProgress>(_ => { }));

        return dArtDownloadResponse;
    }

    private async Task<DArtDownloadResponse> TryExtraction(string deviationId,
        DArtDownloadResponse dArtDownloadResponse, IProgress<ExtractionProgress> progressCallback)
    {
        var targetDirectoryPath = FileUtils.CreateDirectoryInFoliConTemp(deviationId);
        dArtDownloadResponse.LocalDownloadPath = targetDirectoryPath;
        var downloadResponse = await Services.HttpC.GetAsync(dArtDownloadResponse.Src);
        
        if (FileUtils.IsCompressedArchive(dArtDownloadResponse.Filename))
        {
            await ProcessCompressedFiles(downloadResponse, targetDirectoryPath, progressCallback);
        }
        else
        {
            await FileStreamToDestination(downloadResponse, targetDirectoryPath, dArtDownloadResponse.Filename);
        }

        FileUtils.DeleteDirectoryIfEmpty(targetDirectoryPath);
        return dArtDownloadResponse;
    }

    public async Task<DArtDownloadResponse> ExtractDeviation(string deviationId,
        DArtDownloadResponse dArtDownloadResponse, IProgress<ExtractionProgress> progressCallback)
    {
        GetClientAccessTokenAsync();
        return await TryExtraction(deviationId, dArtDownloadResponse, progressCallback);

    }
    public async Task<DArtDownloadResponse> GetDArtDownloadResponseAsync(string deviationId)
    {
        var url = GetDownloadApiUrl(deviationId);
        using var response = await Services.HttpC.GetAsync(new Uri(url));
        var jsonData = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<DArtDownloadResponse>(jsonData);
    }

    private async Task ProcessCompressedFiles(HttpResponseMessage downloadResponse, string targetDirectoryPath,
        IProgress<ExtractionProgress> progressCallback)
    {
        await using var stream = await downloadResponse.Content.ReadAsStreamAsync();
        stream.ExtractPngAndIcoToDirectory(targetDirectoryPath, progressCallback);
    }

    private async Task FileStreamToDestination(HttpResponseMessage downloadResponse, string targetDirectoryPath,
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