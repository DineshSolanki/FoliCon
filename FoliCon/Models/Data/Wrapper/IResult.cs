namespace FoliCon.Models.Data.Wrapper;
[Localizable(false)]
public interface IResult
{
    string Title { get; }
    DateTime? ReleaseDate { get; }
    double? Rating { get; }
    string Summary { get; }
    string PosterUrl { get; }
    
    int TotalResults { get; }
    
    IResult GetFirst();
    T GetFirst<T>() where T : IResult => (T)GetFirst();

    IResult GetAt(int index) => throw new NotImplementedException($"GetAt is not supported for type {GetType()}");
    T GetAt<T>(int index) => (T)GetAt(index);
    
    Type GetOriginalType() => GetType();

}


public class GameWrapper(Game game) : IResult
{
    public string Title => game.Name;
    public DateTime? ReleaseDate => game.FirstReleaseDate?.DateTime;
    public double? Rating => game.TotalRating;
    public string Summary => game.Summary;
    public string PosterUrl => IgdbDataTransformer.GetPosterUrl(game.Cover.Value.ImageId, ImageSize.HD720);
    public int TotalResults => game is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(Game);
}

public class GamesWrapper(Game[] games) : IResult
{
    public string Title => games[0].Name;
    public DateTime? ReleaseDate => games[0].FirstReleaseDate?.DateTime;
    public double? Rating => games[0].TotalRating;
    public string Summary => games[0].Summary;
    public string PosterUrl => IgdbDataTransformer.GetPosterUrl(games[0].Cover.Value.ImageId, ImageSize.HD720);
    public int TotalResults => games.Length;
    public IResult GetFirst() => new GameWrapper(games[0]);
    public IResult GetAt(int index) => new GameWrapper(games[index]);
    public Type GetOriginalType() => typeof(Game[]);
}
public class MovieWrapper(Movie movie, TMDbClient tmDbClient) : IResult
{
    public string Title => movie.Title;
    public DateTime? ReleaseDate => movie.ReleaseDate;
    public double? Rating => movie.VoteAverage;
    public string Summary => movie.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(movie.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => movie is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(Movie);
}

public class TvShowWrapper(TvShow tvShow, TMDbClient tmDbClient) : IResult
{
    public string Title => tvShow.Name;
    public DateTime? ReleaseDate => tvShow.FirstAirDate;
    public double? Rating => tvShow.VoteAverage;
    public string Summary => tvShow.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(tvShow.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => tvShow is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(TvShow);
}

public class CollectionWrapper(Collection collection, TMDbClient tmDbClient) : IResult
{
    public string Title => collection.Name;
    public DateTime? ReleaseDate => null;
    public double? Rating => null;  
    public string Summary => collection.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(collection.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => collection is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(Collection);
}

public class SearchCollectionWrapper(SearchCollection searchCollection, TMDbClient tmDbClient) : IResult
{
    public string Title => searchCollection.Name;
    public DateTime? ReleaseDate => null;
    public double? Rating => null;
    public string Summary => searchCollection.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(searchCollection.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => searchCollection is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(SearchCollection);
}

public class SearchMovieWrapper(SearchMovie searchMovie, TMDbClient tmDbClient) : IResult
{
    public string Title => searchMovie.Title;
    public DateTime? ReleaseDate => searchMovie.ReleaseDate;
    public double? Rating => searchMovie.VoteAverage;
    public string Summary => searchMovie.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(searchMovie.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => searchMovie is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(SearchMovie);
}

public class SearchTvShowWrapper(SearchTv searchTvShow, TMDbClient tmDbClient) : IResult
{
    public string Title => searchTvShow.Name;
    public DateTime? ReleaseDate => searchTvShow.FirstAirDate;
    public double? Rating => searchTvShow.VoteAverage;
    public string Summary => searchTvShow.Overview;
    public string PosterUrl => TmdbDataTransformer.GetPosterUrl(searchTvShow.PosterPath, PosterSize.W500, tmDbClient);
    public int TotalResults => searchTvShow is not null ? 1 : 0;
    public IResult GetFirst() => this;
    public Type GetOriginalType() => typeof(SearchTv);
}

public class SearchContainerWrapper<T>(SearchContainer<T> searchContainer, TMDbClient tmDbClient) : IResult
{
    public string Title => null;
    public DateTime? ReleaseDate => null;
    public double? Rating => null;
    public string Summary => null;
    public string PosterUrl => null;
    public int TotalResults => searchContainer.TotalResults;
    public IResult GetFirst() => searchContainer.Results[0] switch
    {
        Game game => new GameWrapper(game),
        Movie movie => new MovieWrapper(movie, tmDbClient),
        TvShow tvShow => new TvShowWrapper(tvShow, tmDbClient),
        Collection collection => new CollectionWrapper(collection, tmDbClient),
        SearchCollection searchCollection => new SearchCollectionWrapper(searchCollection, tmDbClient),
        SearchMovie searchMovie => new SearchMovieWrapper(searchMovie, tmDbClient),
        SearchTv searchTvShow => new SearchTvShowWrapper(searchTvShow, tmDbClient),
        _ => throw new ArgumentException("Unknown Result Type")
    };

    public IResult GetAt(int index) => searchContainer.Results[index] switch
    {
        Game game => new GameWrapper(game),
        Movie movie => new MovieWrapper(movie, tmDbClient),
        TvShow tvShow => new TvShowWrapper(tvShow, tmDbClient),
        Collection collection => new CollectionWrapper(collection, tmDbClient),
        SearchCollection searchCollection => new SearchCollectionWrapper(searchCollection, tmDbClient),
        SearchMovie searchMovie => new SearchMovieWrapper(searchMovie, tmDbClient),
        SearchTv searchTvShow => new SearchTvShowWrapper(searchTvShow, tmDbClient),
        _ => throw new ArgumentException("Unknown Result Type")
    };
    public Type GetOriginalType() => typeof(SearchContainer<T>);
}