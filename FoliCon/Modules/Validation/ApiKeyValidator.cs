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

    /// <summary>
    /// Validates a TMDB API key by calling GetConfigAsync.
    /// </summary>
    public static async Task<ApiKeyValidationResult> ValidateTmdbKeyAsync(string key) =>
        await ValidateTmdbKeyAsync(key, CancellationToken.None);

    public static async Task<ApiKeyValidationResult> ValidateTmdbKeyAsync(string key,
        CancellationToken ct)
    {
        Logger.Debug("Validating TMDB API key...");

        if (string.IsNullOrWhiteSpace(key))
        {
            return ApiKeyValidationResult.Fail(Lang.ApiKeyCannotBeEmpty);
        }

        try
        {
            using var client = new TMDbClient(key);
            await client.GetConfigAsync();
            Logger.Info("TMDB API key validated successfully.");
            return ApiKeyValidationResult.Success();
        }
        catch (OperationCanceledException ex)
        {
            Logger.Warn(ex, "TMDB validation timed out.");
            return ApiKeyValidationResult.NetworkError(Lang.RequestTimedOut);
        }
        catch (HttpRequestException ex) when (IsUnauthorized(ex))
        {
            Logger.Warn(ex, "TMDB API key is invalid: {Message}", ex.Message);
            return ApiKeyValidationResult.Fail(Lang.InvalidApiKey);
        }
        catch (HttpRequestException ex)
        {
            Logger.Warn(ex, "TMDB validation network error: {Message}", ex.Message);
            return ApiKeyValidationResult.NetworkError(string.Format(Lang.CouldNotReachService, "TMDB", ex.Message));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error validating TMDB key.");
            return ApiKeyValidationResult.Fail(string.Format(Lang.UnexpectedError, ex.Message));
        }
    }

    /// <summary>
    /// Validates IGDB/Twitch credentials by running a minimal game query.
    /// </summary>
    public static async Task<ApiKeyValidationResult> ValidateIgdbCredentialsAsync(string clientId,
        string clientSecret) =>
        await ValidateIgdbCredentialsAsync(clientId, clientSecret, CancellationToken.None);

    public static async Task<ApiKeyValidationResult> ValidateIgdbCredentialsAsync(string clientId,
        string clientSecret, CancellationToken ct)
    {
        Logger.Debug("Validating IGDB/Twitch credentials...");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return ApiKeyValidationResult.Fail(Lang.ClientIdAndSecretCannotBeEmpty);
        }

        try
        {
            var store = new IgdbJotTrackerStore();
            var client = new IGDBClient(clientId, clientSecret, store);
            // Minimal query to force Twitch token fetch and API call
            await client.QueryAsync<Game>(IGDBClient.Endpoints.Games,
                "fields name; limit 1;");
            Logger.Info("IGDB credentials validated successfully.");
            return ApiKeyValidationResult.Success();
        }
        catch (OperationCanceledException ex)
        {
            Logger.Warn(ex, "IGDB validation timed out.");
            return ApiKeyValidationResult.NetworkError(Lang.RequestTimedOut);
        }
        catch (HttpRequestException ex) when (IsUnauthorized(ex))
        {
            Logger.Warn(ex, "IGDB credentials are invalid: {Message}", ex.Message);
            return ApiKeyValidationResult.Fail(Lang.InvalidClientIdOrSecret);
        }
        catch (HttpRequestException ex)
        {
            Logger.Warn(ex, "IGDB validation network error: {Message}", ex.Message);
            return ApiKeyValidationResult.NetworkError(string.Format(Lang.CouldNotReachService, "IGDB/Twitch", ex.Message));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Unexpected error validating IGDB credentials.");
            return ApiKeyValidationResult.Fail(string.Format(Lang.UnexpectedError, ex.Message));
        }
    }

    private static bool IsUnauthorized(HttpRequestException ex)
    {
        return ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden;
    }
}
