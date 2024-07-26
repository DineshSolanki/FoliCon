using System.Windows.Documents;
using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using NLog;
using TMDbLib.Objects.Find;
using Collection = TMDbLib.Objects.Collections.Collection;

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
            { MediaTypes.Movie, async id => await _serviceClient.GetMovieAsync(id) },
            { MediaTypes.Collection, async id => await _serviceClient.GetCollectionAsync(id) },
            { MediaTypes.Tv, async id => await _serviceClient.GetTvShowAsync(id) }
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

        if (!_mediaTypeHandlers.TryGetValue(mediaType, out var handler))
        {
            throw new ArgumentException($"Invalid media type: {mediaType}");
        }

        var r = await handler(id);

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
        var (r, mediaType) = searchMode switch
        {
            MediaTypes.Movie => await SearchMoviesAsync(query),
            MediaTypes.Tv => await SearchTvShowAsync(query),
            MediaTypes.Mtv => await SearchMultiAsync(query),
            _ => (null, "")
        };
        return new ResultResponse
        {
            Result = r,
            MediaType = mediaType
        };
    }

    private async Task<(object Result, string MediaType)> SearchMoviesAsync(string query)
    {
        object r;
        string mediaType;
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
        return (r, mediaType);
    }

    private async Task<(object Result, string MediaType)> SearchTvShowAsync(string query)
    {
        var r = await _serviceClient.SearchTvShowAsync(query);
        const string mediaType = MediaTypes.Tv;
        return (r, mediaType);
    }

    private async Task<(object Result, string MediaType)> SearchMultiAsync(string query)
    {
        var r = await _serviceClient.SearchMultiAsync(query);
        const string mediaType = MediaTypes.Mtv;
        return (r, mediaType);
    }

    public async Task<ResultResponse> SearchWithParamsAsync(ParsedTitle parsedTitle, string searchMode)
    {
        Logger.Debug("Searching for {ParsedTitle} in {SearchMode}", parsedTitle, searchMode);

        var mediaType = "";
        object? searchResult = null;

        switch (searchMode)
        {
            case MediaTypes.Movie:
                searchResult = parsedTitle.Title.ToLower(CultureInfo.InvariantCulture).Contains("collection")
                    ? await SearchCollection(parsedTitle)
                    : await SearchMovie(parsedTitle);
                mediaType = parsedTitle.Title.ToLower(CultureInfo.InvariantCulture).Contains("collection")
                    ? MediaTypes.Collection
                    : MediaTypes.Movie;
                break;
            case MediaTypes.Tv:
                searchResult = await SearchTvShow(parsedTitle);
                mediaType = MediaTypes.Tv;
                break;
            case MediaTypes.Mtv:
                searchResult = await SearchMulti(parsedTitle);
                mediaType = MediaTypes.Mtv;
                break;
        }

        return new ResultResponse
        {
            Result = searchResult,
            MediaType = mediaType
        };
    }

    private async Task<object> SearchMovie(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => parsedTitle.Year != 0
                ? await _serviceClient.SearchMovieAsync(query:query, year:parsedTitle.Year)
                : await _serviceClient.SearchMovieAsync(query),
            IdType.Tvdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb,
                parsedTitle.Id)),
            IdType.Tmdb => GetMovieSearchContainer(await _serviceClient.GetMovieAsync(Convert.ToInt32(parsedTitle.Id))),
            IdType.Imdb => await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id),
            _ => parsedTitle.Year != 0
                ? await _serviceClient.SearchMovieAsync(query, parsedTitle.Year)
                : await _serviceClient.SearchMovieAsync(query)
        };
    }

    private async Task<object> SearchCollection(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => await _serviceClient.SearchCollectionAsync(query),
            IdType.Tvdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb,
                parsedTitle.Id)),
            IdType.Tmdb => GetCollectionSearchContainer(
                await _serviceClient.GetCollectionAsync(Convert.ToInt32(parsedTitle.Id))),
            IdType.Imdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb,
                parsedTitle.Id)),
            _ => await _serviceClient.SearchCollectionAsync(query)
        };
    }

    private async Task<object> SearchTvShow(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => parsedTitle.Year != 0
                ? await _serviceClient.SearchTvShowAsync(query:query, firstAirDateYear:parsedTitle.Year)
                : await _serviceClient.SearchTvShowAsync(query),
            IdType.Tvdb =>
                GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
            IdType.Tmdb => GetTvSearchContainer(await _serviceClient.GetTvShowAsync(Convert.ToInt32(parsedTitle.Id))),
            IdType.Imdb =>
                GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
            _ => parsedTitle.Year != 0
                ? await _serviceClient.SearchTvShowAsync(query, parsedTitle.Year)
                : await _serviceClient.SearchTvShowAsync(query)
        };
    }

    private async Task<object> SearchMulti(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => parsedTitle.Year != 0
                ? await _serviceClient.SearchMultiAsync(query:query, year: parsedTitle.Year)
                : await _serviceClient.SearchMultiAsync(query),
            IdType.Tvdb => GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb,
                parsedTitle.Id)),
            IdType.Imdb => GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb,
                parsedTitle.Id)),
            _ => parsedTitle.Year != 0
                ? await _serviceClient.SearchMultiAsync(query:query, year:parsedTitle.Year)
                : await _serviceClient.SearchMultiAsync(query)
        };
    }
    
    private static SearchContainer<SearchMovie> GetMovieSearchContainer(FindContainer findContainer)
    {
        return new SearchContainer<SearchMovie>
        {
            TotalResults = findContainer.MovieResults.Count,
            Results = findContainer.MovieResults
        };
    }
    
    private static SearchContainer<SearchMovie> GetMovieSearchContainer(Movie movie)
    {
        return new SearchContainer<SearchMovie>
        {
            TotalResults = 1,
            Results = movie is null ? [] : [ConvertMovieToSearchMovie(movie)]
        };
    }
    
    private static SearchContainer<SearchCollection> GetCollectionSearchContainer(Collection collection)
    {
        return new SearchContainer<SearchCollection>
        {
            TotalResults = 1,
            Results = collection is null ? [] : [ConvertCollectionToSearchCollection(collection)]
        };
    }
    
    private static SearchContainer<SearchTv> GetTvSearchContainer(FindContainer findContainer)
    {
        return new SearchContainer<SearchTv>
        {
            TotalResults = findContainer.TvResults.Count,
            Results = findContainer.TvResults
        };
    }
    
    private static SearchContainer<SearchTv> GetTvSearchContainer(TvShow tvShow)
    {
        return new SearchContainer<SearchTv>
        {
            TotalResults = 1,
            Results = tvShow is null ? [] : [ConvertTvShowToSearchTv(tvShow)]
        };
    }
    
    private static SearchContainer<dynamic> GetMultiSearchContainer(FindContainer findContainer)
    {
        dynamic result;
        if (findContainer.MovieResults.Count != 0)
        {
            result = findContainer.MovieResults;
        }
        else if (findContainer.TvResults.Count != 0)
        {
            result = findContainer.TvResults;
        }
        else
        {
            result = new List();
        }
        return new SearchContainer<dynamic>
        {
            TotalResults = findContainer.MovieResults.Count + findContainer.TvResults.Count,
            Results = result
        };
    }
    
    private static SearchTv ConvertTvShowToSearchTv(TvShow tvShow)
    {
        return new SearchTv
        {
            Id = tvShow.Id,
            Name = tvShow.Name,
            FirstAirDate = tvShow.FirstAirDate,
            Overview = tvShow.Overview,
            PosterPath = tvShow.PosterPath,
            VoteAverage = tvShow.VoteAverage
        };
    }
    
    private static SearchMovie ConvertMovieToSearchMovie(Movie movie)
    {
        return new SearchMovie
        {
            Id = movie.Id,
            Title = movie.Title,
            ReleaseDate = movie.ReleaseDate,
            Overview = movie.Overview,
            PosterPath = movie.PosterPath,
            VoteAverage = movie.VoteAverage
        };
    }
    
    private static SearchCollection ConvertCollectionToSearchCollection(Collection collection)
    {
        return new SearchCollection
        {
            Id = collection.Id,
            Name = collection.Name,
            PosterPath = collection.PosterPath,
            BackdropPath = collection.BackdropPath
        };
    }
 }