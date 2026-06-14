using FoliCon.Models.Api;
using FoliCon.Models.Configs;

namespace FoliCon.Modules.Validation;

/// <summary>
/// Validates API keys by making lightweight authenticated calls to each service.
/// DeviantArt is no longer validated here — its OAuth flow is self-validating
/// (if AuthorizeAsync() succeeds, the token is valid by definition).
/// </summary>
public static class ApiKeyValidator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly HttpClient ValidationHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// Validates a TMDB API key by calling GetConfigAsync.
    /// </summary>
    public static async Task<ApiKeyValidationResult> ValidateTmdbKeyAsync(string key,
        CancellationToken ct = default)
    {
        Logger.Debug("Validating TMDB API key...");

        if (string.IsNullOrWhiteSpace(key))
            return ApiKeyValidationResult.Fail("API key cannot be empty.");

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            var linkedCt = cts.Token;

            using var client = new TMDbClient(key);
            await client.GetConfigAsync();
            Logger.Info("TMDB API key validated successfully.");
            return ApiKeyValidationResult.Success();
        }
        catch (OperationCanceledException)
        {
            Logger.Warn("TMDB validation timed out.");
            return ApiKeyValidationResult.NetworkError("Request timed out. Check your internet connection.");
        }
        catch (HttpRequestException ex) when (IsUnauthorized(ex))
        {
            Logger.Warn("TMDB API key is invalid: {Message}", ex.Message);
            return ApiKeyValidationResult.Fail("Invalid API key.");
        }
        catch (HttpRequestException ex)
        {
            Logger.Warn("TMDB validation network error: {Message}", ex.Message);
            return ApiKeyValidationResult.NetworkError($"Could not reach TMDB: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error validating TMDB key.");
            return ApiKeyValidationResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates IGDB/Twitch credentials by running a minimal game query.
    /// </summary>
    public static async Task<ApiKeyValidationResult> ValidateIgdbCredentialsAsync(string clientId,
        string clientSecret, CancellationToken ct = default)
    {
        Logger.Debug("Validating IGDB/Twitch credentials...");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return ApiKeyValidationResult.Fail("Client ID and Client Secret cannot be empty.");

        try
        {
            var store = new IgdbJotTrackerStore();
            var client = new IGDBClient(clientId, clientSecret, store);
            // Minimal query to force Twitch token fetch and API call
            var games = await client.QueryAsync<Game>(IGDBClient.Endpoints.Games,
                "fields name; limit 1;");
            Logger.Info("IGDB credentials validated successfully.");
            return ApiKeyValidationResult.Success();
        }
        catch (OperationCanceledException)
        {
            Logger.Warn("IGDB validation timed out.");
            return ApiKeyValidationResult.NetworkError("Request timed out. Check your internet connection.");
        }
        catch (HttpRequestException ex) when (IsUnauthorized(ex))
        {
            Logger.Warn("IGDB credentials are invalid: {Message}", ex.Message);
            return ApiKeyValidationResult.Fail("Invalid Client ID or Client Secret.");
        }
        catch (HttpRequestException ex)
        {
            Logger.Warn("IGDB validation network error: {Message}", ex.Message);
            return ApiKeyValidationResult.NetworkError($"Could not reach IGDB/Twitch: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error validating IGDB credentials.");
            return ApiKeyValidationResult.Fail($"Unexpected error: {ex.Message}");
        }
    }

    private static bool IsUnauthorized(HttpRequestException ex)
    {
        return ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden;
    }
}
