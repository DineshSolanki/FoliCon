using Polly;

namespace FoliCon.Modules.utils;

[Localizable(false)]
public static class NetworkUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Checks if Web is accessible from This System
    /// </summary>
    /// <returns> Returns true if Web is accessible</returns>
    public static bool IsNetworkAvailable()
    {
        Logger.ForDebugEvent().Message("Network Availability Check Started").Log();
        const string host = "8.8.8.8";
        var result = false;
        using var p = new Ping();
        try
        {
            Logger.Debug("Pinging {Host}", host);
            var reply = p.Send(host, 5000, new byte[32], new PingOptions { DontFragment = true, Ttl = 32 });
            if (reply is { Status: IPStatus.Success })
            {
                result = true;
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("Error Occurred while checking Network Availability : {Message}", e.Message)
                .Exception(e).Log();
            // ignored
        }
        Logger.Debug("Network availability: {IsNetworkAvailable}", result);
        return result;
    }

    /// <summary>
    /// Async function That can Download image from any URL and save to local path
    /// </summary>
    /// <param name="url"> The URL of Image to Download</param>
    /// <param name="saveFileName">The Local Path Of Downloaded Image</param>
    public static async Task DownloadImageFromUrlAsync(Uri url, string saveFileName)
    {
        try
        {
            await ExecuteWithPoliciesAsync(async () => await DownloadAndSaveImageAsync(url, saveFileName));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to download the image from URL: {url}. All attempts have failed.");
        }
    }

    private static async Task ExecuteWithPoliciesAsync(Func<Task> action)
    {
        const int maxRetry = 2;

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, retryCount, _) =>
                {
                    Logger.Warn(exception, $"Retry {retryCount} for action failed.");
                });

        var fallbackPolicy = Policy
            .Handle<HttpRequestException>()
            .FallbackAsync(async _ =>
            {
                Logger.Error("Executing fallback policy.");
                throw new HttpRequestException($"Action failed after {maxRetry} retry attempts.");
            });

        await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(action);
    }

    private static async Task DownloadAndSaveImageAsync(Uri url, string saveFileName)
    {
        Logger.Info($"Downloading Image from URL: {url}");
        using var response = await Services.HttpC.GetAsync(url);
        await using var fs = new FileStream(saveFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        Logger.Info("Saving Image to Path: {Path}", saveFileName);
        await response.Content.CopyToAsync(fs);
    }
}