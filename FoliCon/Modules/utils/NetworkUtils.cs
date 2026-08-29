using Polly;
using Polly.Retry;

namespace FoliCon.Modules.utils;

[Localizable(false)]
public static class NetworkUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly ResiliencePipeline DownloadRetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
            OnRetry = args =>
            {
                Logger.Warn(args.Outcome.Exception, "Retry {Attempt} for image download failed.", args.AttemptNumber);
                return ValueTask.CompletedTask;
            }
        })
        .Build();
    /// <summary>
    /// Checks if Web is accessible from This System
    /// </summary>
    /// <returns> Returns true if Web is accessible</returns>
    public static bool IsNetworkAvailable()
    {
        Logger.ForDebugEvent().Message("Network Availability Check Started").Log();
        const string host = "8.8.8.8"; // NOSONAR — Google Public DNS, used only for ICMP reachability check
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
    /// Downloads an image from any URL and saves to local path.
    /// Throws <see cref="HttpRequestException"/> if the download fails after retries
    /// or the server returns a non-success status code.
    /// </summary>
    /// <param name="url"> The URL of Image to Download</param>
    /// <param name="saveFileName">The Local Path Of Downloaded Image</param>
    public static async Task DownloadImageFromUrlAsync(Uri url, string saveFileName)
    {
        await DownloadRetryPipeline.ExecuteAsync(
            async _ => await DownloadAndSaveImageAsync(url, saveFileName));
    }

    private static async Task DownloadAndSaveImageAsync(Uri url, string saveFileName)
    {
        Logger.Info($"Downloading Image from URL: {url}");
        using var response = await Services.HttpC.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var fs = new FileStream(saveFileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        Logger.Info("Saving Image to Path: {Path}", saveFileName);
        await response.Content.CopyToAsync(fs);
    }
}