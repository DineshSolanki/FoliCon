namespace FoliCon.Modules.Configuration;

public static class Services
{
    public static readonly Tracker Tracker = new();
    public static readonly HttpClient HttpC = CreateHttpClient();
    public static readonly AppConfig Settings = GlobalDataHelper.Load<AppConfig>();

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient();
    }
}