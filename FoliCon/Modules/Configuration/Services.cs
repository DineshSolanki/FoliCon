namespace FoliCon.Modules.Configuration;

public static class Services
{
    public static readonly Tracker Tracker = new();
    public static readonly HttpClient HttpC = CreateHttpClient();
    public static readonly AppConfig Settings = GlobalDataHelper.Load<AppConfig>();

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                
                // Enable ALPN (Application-Layer Protocol Negotiation) like browsers do
                ApplicationProtocols =
                [
                    System.Net.Security.SslApplicationProtocol.Http2,
                    System.Net.Security.SslApplicationProtocol.Http11
                ],
                RemoteCertificateValidationCallback = (_, _, _, errors) => errors == System.Net.Security.SslPolicyErrors.None
            },
            
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 10,
            
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | 
                                    System.Net.DecompressionMethods.Deflate | 
                                    System.Net.DecompressionMethods.Brotli,
            
            ConnectTimeout = TimeSpan.FromSeconds(15),
            ResponseDrainTimeout = TimeSpan.FromSeconds(5),
            Expect100ContinueTimeout = TimeSpan.Zero,
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(60),
            DefaultRequestVersion = new Version(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", 
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", 
            "en-US,en;q=0.9");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", 
            "gzip, deflate, br, zstd");
        client.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
        
        return client;
    }
}