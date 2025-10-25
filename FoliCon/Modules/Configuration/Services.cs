namespace FoliCon.Modules.Configuration;

public static class Services
{
    public static readonly Tracker Tracker = new();
    // Configure a single shared HttpClient with explicit TLS 1.2 and sensible defaults
    public static readonly HttpClient HttpC = CreateHttpClient();
    public static readonly AppConfig Settings = GlobalDataHelper.Load<AppConfig>();

    private static HttpClient CreateHttpClient()
    {
        // Use SocketsHttpHandler to control TLS and connection behavior explicitly
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                // Force TLS 1.2 which is widely supported and avoids some TLS 1.3 middlebox issues
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            }
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestVersion = System.Net.HttpVersion.Version11
        };

        // Basic defaults to play nicely with CDNs/WAFs
        if (!client.DefaultRequestHeaders.UserAgent.Any())
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FoliCon");
        }
        // Do not set Accept globally; callers may download binary content
        return client;
    }
}