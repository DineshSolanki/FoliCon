﻿using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Modules.utils;
using NLog;
using Collection = TMDbLib.Objects.Collections.Collection;

namespace FoliCon.Modules.TMDB;

internal class TmdbDataTransformer(
    ref List<PickedListItem> listDataTable,
    ref List<ImageToDownload> imgDownloadList)
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<PickedListItem> _listDataTable = listDataTable;
    private readonly List<ImageToDownload> _imgDownloadList = imgDownloadList;

    private const string SmallPosterBase = "https://image.tmdb.org/t/p/w200";
    private const string PosterBase = "https://image.tmdb.org/t/p/original";

    public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
        SearchContainer<SearchCollection> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            var mediaName = item.Name;
            const string year = "";
            const string rating = "";
            var poster = Convert.ToString(item.PosterPath != null ? SmallPosterBase + item.PosterPath : null,
                CultureInfo.InvariantCulture);
            
            items.Add(new ListItem
            {
                Title = mediaName,
                Year = year,
                Rating = rating,
                Overview = item.Overview,
                Poster = poster,
                Id = item.Id.ToString()
            });
        }

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
        Collection result)
    {
        var items = new ObservableCollection<ListItem>();
        var mediaName = result.Name;
        const string year = "";
        const string rating = "";
        var poster = Convert.ToString(result.PosterPath != null ? SmallPosterBase + result.PosterPath : null, CultureInfo.InvariantCulture);
        items.Add(new ListItem
        {
            Title = mediaName,
            Year = year,
            Rating = rating,
            Overview = result.Overview,
            Poster = poster,
            Id = result.Id.ToString()
        });

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
        Movie result)
    {
        var items = new ObservableCollection<ListItem>();
        var mediaName = result.Title;
        var year = result.ReleaseDate != null ? result.ReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture) : "";
        var rating = result.VoteAverage.ToString(CultureInfo.CurrentCulture);
        var overview = result.Overview;
        var poster = result.PosterPath != null ? SmallPosterBase + result.PosterPath : null;
        items.Add(new ListItem
        {
            Title = mediaName,
            Year = year,
            Rating = rating,
            Overview = overview,
            Poster = poster,
            Id = result.Id.ToString()
        });

        return items;
    }

    public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
        SearchContainer<SearchMovie> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            var mediaName = item.Title;
            var year = item.ReleaseDate != null
                ? item.ReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture)
                : "";
            var rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
            var overview = item.Overview;
            var poster = item.PosterPath != null ? SmallPosterBase + item.PosterPath : null;
            items.Add(new ListItem
            {
                Title = mediaName,
                Year = year,
                Rating = rating,
                Overview = overview,
                Poster = poster,
                Id = item.Id.ToString()
            });
        }

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(SearchContainer<SearchTv> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            var mediaName = item.Name;
            var year = item.FirstAirDate != null ? item.FirstAirDate.Value.Year.ToString(CultureInfo.InvariantCulture) : "";
            var rating = item.VoteAverage.ToString(CultureInfo.CurrentCulture);
            var overview = item.Overview;
            var poster = item.PosterPath != null ? SmallPosterBase + item.PosterPath : null;
            items.Add(new ListItem
            {
                Title = mediaName,
                Year = year,
                Rating = rating,
                Overview = overview,
                Poster = poster,
                Id = item.Id.ToString()
            });
        }

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(TvShow result)
    {
        var items = new ObservableCollection<ListItem>();
        var mediaName = result.Name;
        var year = result.FirstAirDate != null
            ? result.FirstAirDate.Value.Year.ToString(CultureInfo.InvariantCulture)
            : "";
        var rating = result.VoteAverage.ToString(CultureInfo.CurrentCulture);
        var overview = result.Overview;
        var poster = result.PosterPath != null ? SmallPosterBase + result.PosterPath : null;
        items.Add(new ListItem
        {
            Title = mediaName,
            Year = year,
            Rating = rating,
            Overview = overview,
            Poster = poster,
            Id = result.Id.ToString()
        });

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(
        SearchContainer<SearchBase> result)
    {
        Logger.Debug("Extracting Resource Details into List Item");
        var items = new ObservableCollection<ListItem>();
        var mediaName = "";
        var year = "";
        var rating = "";
        var overview = "";
        string poster = null;
        var id = 0;
        foreach (var item in result.Results)
        {
            var mediaType = item.MediaType;

            Logger.Debug("Media Type: {MediaType}", mediaType);
            switch (mediaType)
            {
                case MediaType.Tv:
                {
                    var res = (SearchTv)item;
                    id = res.Id;
                    mediaName = res.Name;
                    year = res.FirstAirDate != null
                        ? res.FirstAirDate.Value.Year.ToString(CultureInfo.InvariantCulture)
                        : "";
                    rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                    overview = res.Overview;
                    poster = res.PosterPath != null ? SmallPosterBase + res.PosterPath : null;
                    break;
                }
                case MediaType.Movie:
                {
                    var res = (SearchMovie)item;
                    id = res.Id;
                    mediaName = res.Title;
                    year = res.ReleaseDate != null
                        ? res.ReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture)
                        : "";
                    rating = res.VoteAverage.ToString(CultureInfo.CurrentCulture);
                    overview = res.Overview;
                    poster = res.PosterPath != null ? SmallPosterBase + res.PosterPath : null;
                    break;
                }
            }

            Logger.Trace(
                "Media Name: {MediaName}, Year: {Year}, Rating: {Rating}, Overview:{Pverview}, PosterPath: {Poster}",
                mediaName, year, rating, overview, poster);
            items.Add(new ListItem
            {
                Title = mediaName,
                Year = year,
                Rating = rating,
                Overview = overview,
                Poster = poster,
                Id = id.ToString(),
                MediaType = mediaType
            });
            Logger.Info("Added {MediaName} to List", mediaName);
        }

        return items;
    }
    
    /// <summary>
    /// Prepares the Selected Result for Download And final List View
    /// </summary>
    /// <param name="result">Search Response</param>
    /// <param name="resultType">Type of search Response.</param>
    /// <param name="fullFolderPath">Full Path to the current Media Folder</param>
    /// <param name="rating">Rating for media</param>
    /// <param name="isPickedById"> identifies if Title was picked by media ID.</param>
    /// TODO: Merge parameter response and resultType.
    public void ResultPicked(dynamic result, string resultType, string fullFolderPath, string rating = "",bool isPickedById = false)
    {
        Logger.Debug("Preparing the Selected Result for Download And final List View");
        if (result.PosterPath == null)
        {
            Logger.Warn("No Poster Found, path - {Folder}", fullFolderPath );
            throw new InvalidDataException("NoPoster");
        }
        
        Logger.Debug("Rating: {Rating}, Result Type: {ResultType}", rating, resultType);
        var id = 0;
        var type = resultType;
        if (string.IsNullOrWhiteSpace(rating) && resultType != MediaTypes.Collection)
        {
            Logger.Debug("Rating is null or empty, getting rating from result");
            rating = result.VoteAverage.ToString(CultureInfo.InvariantCulture);
            Logger.Debug("Rating: {Rating}", rating);
        }

        var folderName = Path.GetFileName(fullFolderPath);
        var localPosterPath = $@"{fullFolderPath}\{IconUtils.GetImageName()}.png";

        string posterUrl = string.Concat(PosterBase, result.PosterPath.Replace("https://image.tmdb.org/t/p/w500",""));

        switch (resultType)
        {
            case MediaTypes.Tv:
            {
                dynamic pickedResult = isPickedById ? (TvShow)result : (SearchTv)result;
                var year = pickedResult.FirstAirDate != null ? pickedResult.FirstAirDate.Year.ToString(CultureInfo.InvariantCulture) : "";
                FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name,
                    rating, fullFolderPath, folderName,
                    year);
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Movie:
            {
                dynamic pickedResult = isPickedById ? (Movie)result : (SearchMovie)result;
                var year = pickedResult.ReleaseDate != null ? pickedResult.ReleaseDate.Year.ToString(CultureInfo.InvariantCulture) : "";
                FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Title,
                    rating, fullFolderPath, folderName, year);
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Collection:
            {
                dynamic pickedResult = isPickedById ? (Collection)result : (SearchCollection)result;
                FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name, rating, fullFolderPath,
                    folderName, "");
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Mtv:
            {
                MediaType mediaType = result.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                    {
                        type = MediaTypes.Tv;
                        dynamic pickedResult = isPickedById ? (TvShow)result : (SearchTv)result;
                        var year = pickedResult.FirstAirDate != null
                            ? pickedResult.FirstAirDate.Year.ToString(CultureInfo.InvariantCulture)
                            : "";
                        FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Name,
                            rating, fullFolderPath, folderName,
                            year);
                        id = pickedResult.Id;
                        break;
                    }
                    case MediaType.Movie:
                    {
                        type = MediaTypes.Movie;
                        dynamic pickedResult = isPickedById ? (Movie)result : (SearchMovie)result;
                        var year = pickedResult.ReleaseDate != null
                            ? pickedResult.ReleaseDate.Year.ToString(CultureInfo.InvariantCulture)
                            : "";
                        FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, pickedResult.Title,
                            rating, fullFolderPath, folderName,
                            year);
                        id = pickedResult.Id;
                        break;
                    }
                }

                break;
            }
        }

        if (!isPickedById && id != 0)
        {
            FileUtils.SaveMediaInfo(id, type, fullFolderPath);
        }
        var tempImage = new ImageToDownload
        {
            LocalPath = localPosterPath,
            RemotePath = new Uri(posterUrl)
        };
        _imgDownloadList.Add(tempImage);
    }
}