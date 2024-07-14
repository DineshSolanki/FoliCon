using FoliCon.Models.Api;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.utils;
using Microsoft.Extensions.Caching.Memory;
using SharpCompress.Common;
using SharpCompress.Readers;

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
}