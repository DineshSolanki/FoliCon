#nullable enable
using FoliCon.Modules.TMDB;
using TMDbLib.Objects.Collections;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

namespace FoliconTest;

public class TmdbServiceContainerTests
{
    [Fact]
    public void GetMovieSearchContainer_WhenMovieIsNull_ReturnsZeroTotalResultsAndEmptyList()
    {
        var container = TmdbService.GetMovieSearchContainer((Movie?)null);

        Assert.Equal(0, container.TotalResults);
        Assert.NotNull(container.Results);
        Assert.Empty(container.Results);
    }

    [Fact]
    public void GetMovieSearchContainer_WhenMovieIsNotNull_ReturnsOneTotalResultAndConvertedItem()
    {
        var movie = new Movie
        {
            Id = 3021,
            Title = "1408",
            ReleaseDate = new DateTime(2007, 6, 22),
            VoteAverage = 6.6,
            Overview = "A man who specializes in debunking paranormal occurrences..."
        };

        var container = TmdbService.GetMovieSearchContainer(movie);

        Assert.Equal(1, container.TotalResults);
        Assert.NotNull(container.Results);
        var single = Assert.Single(container.Results);
        Assert.Equal(3021, single.Id);
        Assert.Equal("1408", single.Title);
    }

    [Fact]
    public void GetCollectionSearchContainer_WhenCollectionIsNull_ReturnsZeroTotalResultsAndEmptyList()
    {
        var container = TmdbService.GetCollectionSearchContainer((Collection?)null);

        Assert.Equal(0, container.TotalResults);
        Assert.NotNull(container.Results);
        Assert.Empty(container.Results);
    }

    [Fact]
    public void GetTvSearchContainer_WhenTvShowIsNull_ReturnsZeroTotalResultsAndEmptyList()
    {
        var container = TmdbService.GetTvSearchContainer((TvShow?)null);

        Assert.Equal(0, container.TotalResults);
        Assert.NotNull(container.Results);
        Assert.Empty(container.Results);
    }
}
