namespace FoliCon.Models.Data.Wrapper;

[Localizable(false)]
public static class ResultWrapperFactory
{
    public static IResult CreateWrapper(object result, string mediaType, TMDbClient tmDbClient)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "game" => new GameWrapper(result as Game),
            "movie" => new MovieWrapper(result as Movie, tmDbClient),
            "tv" => new MovieWrapper(result as Movie, tmDbClient),
            "tvshow" => new TvShowWrapper(result as TvShow, tmDbClient),
            "collection" => new CollectionWrapper(result as Collection, tmDbClient),
            _ => throw new ArgumentException("Unknown MediaType")
        };
    }
}