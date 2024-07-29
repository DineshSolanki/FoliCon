using System.Windows.Documents;
using FoliCon.Models.Data.Wrapper;
using TMDbLib.Objects.Find;

namespace FoliCon.Modules.TMDB;

[Localizable(false)]
internal class TmdbService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
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
            Result = ResultWrapperFactory.CreateWrapper(r, mediaType, _serviceClient),
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

        IResult r;
        string mediaType;
        switch (searchMode)
        {
            case MediaTypes.Movie:
                (r, mediaType) = await SearchMoviesAsync(query);
                break;
            case MediaTypes.Tv:
                (r, mediaType) = await SearchTvShowAsync(query);
                break;
            case MediaTypes.Mtv:
                (r, mediaType) = await SearchMultiAsync(query);
                break;
            default:
                (r, mediaType) = (null, "");
                break;
        }

        return new ResultResponse
        {
            Result = r,
            MediaType = mediaType
        };
    }

    private async Task<(IResult Result, string MediaType)> SearchMoviesAsync(string query)
    {
        IResult r;
        string mediaType;
        if (query.ToLower(CultureInfo.InvariantCulture).Contains("collection"))
        {
            var result = await _serviceClient.SearchCollectionAsync(query);
            r = new SearchContainerWrapper<SearchCollection>(result, _serviceClient);
            mediaType = MediaTypes.Collection;
        }
        else
        {
            var result = await _serviceClient.SearchMovieAsync(query);
            r = new SearchContainerWrapper<SearchMovie>(result, _serviceClient);
            mediaType = MediaTypes.Movie;
        }
        return (r, mediaType);
    }

    private async Task<(SearchContainerWrapper<SearchTv> Result, string MediaType)> SearchTvShowAsync(string query)
    {
        var r = await _serviceClient.SearchTvShowAsync(query);
        const string mediaType = MediaTypes.Tv;
        return (new SearchContainerWrapper<SearchTv>(r, _serviceClient), mediaType);
    }

    private async Task<(SearchContainerWrapper<SearchBase> Result, string MediaType)> SearchMultiAsync(string query)
    {
        var r = await _serviceClient.SearchMultiAsync(query);
        const string mediaType = MediaTypes.Mtv;
        return (new SearchContainerWrapper<SearchBase>(r, _serviceClient), mediaType);
    }

    public async Task<ResultResponse> SearchWithParamsAsync(ParsedTitle parsedTitle, string searchMode)
    {
        Logger.Debug("Searching for {ParsedTitle} in {SearchMode}", parsedTitle, searchMode);

        string mediaType;
        IResult searchResult;

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
            default: throw new InvalidDataException($"Invalid search mode: {searchMode}");
        }

        return new ResultResponse
        {
            Result = searchResult,
            MediaType = mediaType
        };
    }

    private async Task<IResult> SearchMovie(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        var searchContainer =  parsedTitle.IdType switch
        {
            IdType.None => await _serviceClient.SearchMovieAsync(query:query, year:parsedTitle.Year),
            IdType.Tvdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb,
                parsedTitle.Id)),
            IdType.Tmdb => GetMovieSearchContainer(await _serviceClient.GetMovieAsync(Convert.ToInt32(parsedTitle.Id))),
            IdType.Imdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
            _ => await _serviceClient.SearchMovieAsync(query, parsedTitle.Year)
        };
        return new SearchContainerWrapper<SearchMovie>(searchContainer, _serviceClient);
    }

    private async Task<IResult> SearchCollection(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => new SearchContainerWrapper<SearchCollection>(
                await _serviceClient.SearchCollectionAsync(query), _serviceClient),
            IdType.Tvdb => new SearchContainerWrapper<SearchMovie>(
                GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
                _serviceClient),
            IdType.Tmdb => new SearchContainerWrapper<SearchCollection>(
                GetCollectionSearchContainer(await _serviceClient.GetCollectionAsync(Convert.ToInt32(parsedTitle.Id))),
                _serviceClient),
            IdType.Imdb => new SearchContainerWrapper<SearchMovie>(
                GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
                _serviceClient),
            _ => new SearchContainerWrapper<SearchCollection>(await _serviceClient.SearchCollectionAsync(query),
                _serviceClient)
        };
    }

    private async Task<SearchContainerWrapper<SearchTv>> SearchTvShow(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        var searchContainer = parsedTitle.IdType switch
        {
            IdType.None => await _serviceClient.SearchTvShowAsync(query: query, firstAirDateYear: parsedTitle.Year),
            IdType.Tvdb =>
                GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
            IdType.Tmdb => GetTvSearchContainer(await _serviceClient.GetTvShowAsync(Convert.ToInt32(parsedTitle.Id))),
            IdType.Imdb =>
                GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
            _ => await _serviceClient.SearchTvShowAsync(query, parsedTitle.Year)
        };
        return new SearchContainerWrapper<SearchTv>(searchContainer, _serviceClient);
    }

    private async Task<IResult> SearchMulti(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        return parsedTitle.IdType switch
        {
            IdType.None => new SearchContainerWrapper<SearchBase>(await _serviceClient.SearchMultiAsync(query:query, year: parsedTitle.Year),_serviceClient),
            IdType.Tvdb => new SearchContainerWrapper<dynamic>(GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb,
                parsedTitle.Id)), _serviceClient),
            IdType.Imdb => new SearchContainerWrapper<dynamic>(GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb,
                parsedTitle.Id)), _serviceClient),
            _ => new SearchContainerWrapper<SearchBase>(await _serviceClient.SearchMultiAsync(query:query, year:parsedTitle.Year),_serviceClient)
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