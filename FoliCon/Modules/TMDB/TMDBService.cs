using System.Windows.Documents;
using Polly;
using Polly.Retry;
using TMDbLib.Objects.Find;

#nullable enable

namespace FoliCon.Modules.TMDB;

[Localizable(false)]
internal class TmdbService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly TMDbClient _serviceClient;
    private readonly Dictionary<string, Func<int, Task<object?>>> _mediaTypeHandlers;

    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>(ex =>
                    ex.InnerException is IOException or System.Net.Sockets.SocketException ||
                    ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
                .Handle<IOException>()
                .Handle<System.Net.Sockets.SocketException>(),
            OnRetry = args =>
            {
                Logger.Warn(args.Outcome.Exception,
                    "GetConfigAsync attempt {Attempt} failed, retrying...", args.AttemptNumber + 1);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public TmdbService(TMDbClient serviceClient)
    {
        _serviceClient = serviceClient;
        // GetConfigAsync intermittently fails with transient SSL/connection-reset errors; retry before giving up.
        // GetAwaiter().GetResult() (not .Result) so the original HttpRequestException isn't wrapped in an
        // AggregateException, which would otherwise bypass the retry filter.
        RetryPipeline.Execute(_ => _serviceClient.GetConfigAsync().GetAwaiter().GetResult());
        _mediaTypeHandlers = new Dictionary<string, Func<int, Task<object?>>>
        {
            { MediaTypes.movie, async id => await _serviceClient.GetMovieAsync(id) },
            { MediaTypes.collection, async id => await _serviceClient.GetCollectionAsync(id) },
            { MediaTypes.tv, async id => await _serviceClient.GetTvShowAsync(id) }
        };
    }

    public TMDbClient GetClient() => _serviceClient;

    public Task<ImagesWithId?> SearchMovieImages(int tvId) => _serviceClient.GetMovieImagesAsync(tvId);

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

    public Task<ImagesWithId?> SearchTvImages(int tvId) => _serviceClient.GetTvShowImagesAsync(tvId);

    public Task<ImagesWithId?> SearchCollectionImages(int collectionId) => _serviceClient.GetCollectionImagesAsync(collectionId);

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
            MediaTypes.movie => await SearchMoviesAsync(query),
            MediaTypes.tv => await SearchTvShowAsync(query),
            MediaTypes.mtv => await SearchMultiAsync(query),
            _ => ((object?)null, "")
        };
        return new ResultResponse
        {
            Result = r,
            MediaType = mediaType
        };
    }

    private async Task<(object? Result, string MediaType)> SearchMoviesAsync(string query)
    {
        object? r;
        string mediaType;
        if (query.Contains("collection", StringComparison.CurrentCultureIgnoreCase))
        {
            r = await _serviceClient.SearchCollectionAsync(query);
            mediaType = MediaTypes.collection;
        }
        else
        {
            r = await _serviceClient.SearchMovieAsync(query);
            mediaType = MediaTypes.movie;
        }
        return (r, mediaType);
    }

    private async Task<(object? Result, string MediaType)> SearchTvShowAsync(string query)
    {
        var r = await _serviceClient.SearchTvShowAsync(query);
        const string mediaType = MediaTypes.tv;
        return (r, mediaType);
    }

    private async Task<(object? Result, string MediaType)> SearchMultiAsync(string query)
    {
        var r = await _serviceClient.SearchMultiAsync(query);
        const string mediaType = MediaTypes.mtv;
        return (r, mediaType);
    }

    public async Task<ResultResponse> SearchWithParamsAsync(ParsedTitle parsedTitle, string searchMode)
    {
        Logger.Debug("Searching for {ParsedTitle} in {SearchMode}", parsedTitle, searchMode);

        string mediaType;
        object? searchResult;

        switch (searchMode)
        {
            case MediaTypes.movie:
                searchResult = parsedTitle.Title.Contains("collection", StringComparison.OrdinalIgnoreCase)
                    ? await SearchCollection(parsedTitle)
                    : await SearchMovie(parsedTitle);
                mediaType = parsedTitle.Title.Contains("collection", StringComparison.OrdinalIgnoreCase)
                    ? MediaTypes.collection
                    : MediaTypes.movie;
                break;
            case MediaTypes.tv:
                searchResult = await SearchTvShow(parsedTitle);
                mediaType = MediaTypes.tv;
                break;
            case MediaTypes.mtv:
                searchResult = await SearchMulti(parsedTitle);
                mediaType = MediaTypes.mtv;
                break;
            default: throw new InvalidDataException($"Invalid search mode: {searchMode}");
        }

        return new ResultResponse
        {
            Result = searchResult,
            MediaType = mediaType
        };
    }

    private async Task<object?> SearchMovie(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        if (parsedTitle.IdType != IdType.None && parsedTitle.Id != "0")
        {
            SearchContainer<SearchMovie>? container = parsedTitle.IdType switch
            {
                IdType.Tvdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
                IdType.Tmdb => int.TryParse(parsedTitle.Id, out var tmdbId)
                    ? GetMovieSearchContainer(await _serviceClient.GetMovieAsync(tmdbId))
                    : null,
                IdType.Imdb => GetMovieSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
                _ => null
            };

            if (container is { Results.Count: > 0 })
            {
                return container;
            }

            Logger.Warn("Search by {IdType} ID {Id} yielded no movie results; falling back to title search for '{Title}'",
                parsedTitle.IdType, parsedTitle.Id, query);
        }

        return parsedTitle.Year != 0
            ? await _serviceClient.SearchMovieAsync(query: query, year: parsedTitle.Year)
            : await _serviceClient.SearchMovieAsync(query);
    }

    private async Task<object?> SearchCollection(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        if (parsedTitle.IdType == IdType.None || parsedTitle.Id == "0")
        {
            return await _serviceClient.SearchCollectionAsync(query);
        }

        var container = parsedTitle.IdType == IdType.Tmdb && int.TryParse(parsedTitle.Id, out var tmdbId)
            ? GetCollectionSearchContainer(await _serviceClient.GetCollectionAsync(tmdbId))
            : null;

        if (container is { Results.Count: > 0 })
        {
            return container;
        }

        Logger.Warn("Search by {IdType} ID {Id} yielded no collection results; falling back to title search for '{Title}'",
            parsedTitle.IdType, parsedTitle.Id, query);

        return await _serviceClient.SearchCollectionAsync(query);
    }

    private async Task<object?> SearchTvShow(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        SearchContainer<SearchTv>? result = null;

        if (parsedTitle.IdType != IdType.None && parsedTitle.Id != "0")
        {
            result = parsedTitle.IdType switch
            {
                IdType.Tvdb =>
                    GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
                IdType.Tmdb => int.TryParse(parsedTitle.Id, out var tmdbId)
                    ? GetTvSearchContainer(await _serviceClient.GetTvShowAsync(tmdbId))
                    : null,
                IdType.Imdb =>
                    GetTvSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
                _ => null
            };

            if (result is not { Results.Count: > 0 })
            {
                Logger.Warn("Search by {IdType} ID {Id} yielded no TV results; falling back to title search for '{Title}'",
                    parsedTitle.IdType, parsedTitle.Id, query);
                result = null;
            }
        }

        result ??= await SearchTvByTitleAsync(query, parsedTitle.Year);

        if (parsedTitle.Season != 0 && result is { Results.Count: > 0 })
        {
            await ApplySeasonPosterAsync(result, parsedTitle.Season);
        }

        return result;
    }

    private Task<SearchContainer<SearchTv>?> SearchTvByTitleAsync(string query, int year) =>
        year != 0
            ? _serviceClient.SearchTvShowAsync(query: query, firstAirDateYear: year)
            : _serviceClient.SearchTvShowAsync(query);

    private async Task ApplySeasonPosterAsync(SearchContainer<SearchTv> tvResults, int seasonNumber)
    {
        await Parallel.ForEachAsync(tvResults.Results,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (tv, _) =>
            {
                try
                {
                    Logger.Debug("Fetching season {Season} poster for show {ShowId} ({ShowName})", seasonNumber, tv.Id, tv.Name);
                    var season = await _serviceClient.GetTvSeasonAsync(tv.Id, seasonNumber, cancellationToken: _);
                    if (season == null)
                    {
                        Logger.Debug("Season {Season} not found for show {ShowId}", seasonNumber, tv.Id);
                        return;
                    }
                    if (season.PosterPath != null)
                    {
                        Logger.Debug("Applying season {Season} poster {PosterPath} for show {ShowId}", seasonNumber, season.PosterPath, tv.Id);
                        tv.PosterPath = season.PosterPath;
                    }
                    else
                    {
                        Logger.Debug("Season {Season} exists for show {ShowId} but has no poster", seasonNumber, tv.Id);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to fetch season {Season} poster for show {ShowId}", seasonNumber, tv.Id);
                }
            });
    }

    private async Task<object?> SearchMulti(ParsedTitle parsedTitle)
    {
        var query = parsedTitle.Title;
        if (parsedTitle.IdType == IdType.None || parsedTitle.Id == "0")
        {
            return parsedTitle.Year != 0
                ? await _serviceClient.SearchMultiAsync(query: query, year: parsedTitle.Year)
                : await _serviceClient.SearchMultiAsync(query);
        }

        var container = parsedTitle.IdType switch
        {
            IdType.Tvdb => GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.TvDb, parsedTitle.Id)),
            IdType.Imdb => GetMultiSearchContainer(await _serviceClient.FindAsync(FindExternalSource.Imdb, parsedTitle.Id)),
            _ => null
        };

        if (container is { TotalResults: > 0 })
        {
            return container;
        }

        Logger.Warn("Search by {IdType} ID {Id} yielded no multi results; falling back to title search for '{Title}'",
            parsedTitle.IdType, parsedTitle.Id, query);

        return parsedTitle.Year != 0
            ? await _serviceClient.SearchMultiAsync(query: query, year: parsedTitle.Year)
            : await _serviceClient.SearchMultiAsync(query);
    }

    private static SearchContainer<SearchMovie> GetMovieSearchContainer(FindContainer? findContainer)
    {
        return new SearchContainer<SearchMovie>
        {
            TotalResults = findContainer?.MovieResults?.Count ?? 0,
            Results = findContainer?.MovieResults ?? []
        };
    }

    internal static SearchContainer<SearchMovie> GetMovieSearchContainer(Movie? movie)
    {
        return new SearchContainer<SearchMovie>
        {
            TotalResults = movie is null ? 0 : 1,
            Results = movie is null ? [] : [ConvertMovieToSearchMovie(movie)]
        };
    }

    internal static SearchContainer<SearchCollection> GetCollectionSearchContainer(Collection? collection)
    {
        return new SearchContainer<SearchCollection>
        {
            TotalResults = collection is null ? 0 : 1,
            Results = collection is null ? [] : [ConvertCollectionToSearchCollection(collection)]
        };
    }

    private static SearchContainer<SearchTv> GetTvSearchContainer(FindContainer? findContainer)
    {
        return new SearchContainer<SearchTv>
        {
            TotalResults = findContainer?.TvResults?.Count ?? 0,
            Results = findContainer?.TvResults ?? []
        };
    }

    internal static SearchContainer<SearchTv> GetTvSearchContainer(TvShow? tvShow)
    {
        return new SearchContainer<SearchTv>
        {
            TotalResults = tvShow is null ? 0 : 1,
            Results = tvShow is null ? [] : [ConvertTvShowToSearchTv(tvShow)]
        };
    }

    private static SearchContainer<dynamic> GetMultiSearchContainer(FindContainer? findContainer)
    {
        var movieResults = findContainer?.MovieResults;
        var tvResults = findContainer?.TvResults;

        dynamic result;
        if (movieResults is { Count: > 0 })
        {
            result = movieResults;
        }
        else if (tvResults is { Count: > 0 })
        {
            result = tvResults;
        }
        else
        {
            result = new List();
        }
        return new SearchContainer<dynamic>
        {
            TotalResults = (movieResults?.Count ?? 0) + (tvResults?.Count ?? 0),
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
