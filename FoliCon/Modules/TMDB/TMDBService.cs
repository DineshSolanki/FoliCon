using NLog;

namespace FoliCon.Modules.TMDB;

internal class TmdbService
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly TMDbClient _serviceClient;
    private readonly Dictionary<string, Func<int, Task<object>>> _mediaTypeHandlers;

    public TmdbService(TMDbClient serviceClient)
    {
        _serviceClient = serviceClient;
        _ = _serviceClient.GetConfigAsync().Result;
        _mediaTypeHandlers = new Dictionary<string, Func<int, Task<object>>>
        {
            { MediaTypes.Movie, async (id) => await _serviceClient.GetMovieAsync(id) },
            { MediaTypes.Collection, async (id) => await _serviceClient.GetCollectionAsync(id) },
            { MediaTypes.Tv, async (id) => await _serviceClient.GetTvShowAsync(id) }
        };
    }

    public TMDbClient GetClient() => _serviceClient;

    public Task<ImagesWithId> SearchMovieImages(int tvId)
    {
        return _serviceClient.GetMovieImagesAsync(tvId);
    }

    public async Task<ResultResponse> SearchByIdAsync(int id, string mediaType)
    {
        Logger.Info("Searching for {Id} in {MediaType}", id, mediaType);

        if (!_mediaTypeHandlers.ContainsKey(mediaType))
        {
            throw new ArgumentException($"Invalid media type: {mediaType}");
        }

        var r = await _mediaTypeHandlers[mediaType](id);

        return new ResultResponse
        {
            Result = r,
            MediaType = mediaType
        };
    }

    public Task<ImagesWithId> SearchTvImages(int tvId)
    {
        return _serviceClient.GetTvShowImagesAsync(tvId);
    }

    public Task<ImagesWithId> SearchCollectionImages(int collectionId)
    {
        return _serviceClient.GetCollectionImagesAsync(collectionId);
    }

    /// <summary>
    /// Searches TMDB for a query in Specified search mode
    /// </summary>
    /// <param name="query">Title to search</param>
    /// <param name="searchMode">Search Mode such as Movie,TV</param>
    /// <returns>Returns Search result with its Media Type</returns>
    public async Task<ResultResponse> SearchAsync(string query, string searchMode)
    {
        Logger.Info("Searching for {Query} in {SearchMode}", query, searchMode);
        object r = null;
        var mediaType = "";
        if (searchMode == MediaTypes.Movie)
        {
            if (query.ToLower(CultureInfo.InvariantCulture).Contains("collection"))
            {
                r = await _serviceClient.SearchCollectionAsync(query);
                mediaType = MediaTypes.Collection;
            }
            else
            {
                r = await _serviceClient.SearchMovieAsync(query);
                mediaType = MediaTypes.Movie;
            }
        }
        else if (searchMode == MediaTypes.Tv)
        {
            r = await _serviceClient.SearchTvShowAsync(query);
            mediaType = MediaTypes.Tv;
        }
        else if (searchMode == MediaTypes.Mtv)
        {
            r = await _serviceClient.SearchMultiAsync(query);
            mediaType = MediaTypes.Mtv;
        }

        var response = new ResultResponse
        {
            Result = r,
            MediaType = mediaType
        };
        return response;
    }
}