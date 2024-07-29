using FoliCon.Models.Data.Wrapper;

namespace FoliCon.Modules.TMDB;

internal class TmdbDataTransformer(
    ref List<PickedListItem> listDataTable,
    ref List<ImageToDownload> imgDownloadList, TMDbClient client)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<PickedListItem> _listDataTable = listDataTable;
    private readonly List<ImageToDownload> _imgDownloadList = imgDownloadList;

    private const string SmallPosterBase = "https://image.tmdb.org/t/p/w200";

    public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
        SearchContainer<SearchCollection> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            items.Add(TransformDetailsIntoListItem(item.Name,
                null,
                null,
                item.Overview,
                item.PosterPath,
                item.Id));
        }

        return items;
    }

    public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
        Collection result)
    {
        return
        [
            TransformDetailsIntoListItem(
                result.Name,
                null,
                null,
                result.Overview,
                result.PosterPath,
                result.Id
            )
        ];
    }
    
    public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(Movie result)
    {
        return
        [
            TransformDetailsIntoListItem(
                result.Title,
                result.ReleaseDate,
                result.VoteAverage,
                result.Overview,
                result.PosterPath,
                result.Id
            )
        ];
    }

    public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
        SearchContainer<SearchMovie> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            items.Add(TransformDetailsIntoListItem(item.Title,
                item.ReleaseDate,
                item.VoteAverage,
                item.Overview,
                item.PosterPath,
                item.Id));
        }

        return items;
    }
    
    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(SearchContainer<SearchTv> result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var item in result.Results)
        {
            items.Add(TransformDetailsIntoListItem(item.Name,
                item.FirstAirDate,
                item.VoteAverage,
                item.Overview,
                item.PosterPath,
                item.Id));
        }

        return items;
    }

    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(TvShow result)
    {
        return
        [
            TransformDetailsIntoListItem(
                result.Name,
                result.FirstAirDate,
                result.VoteAverage,
                result.Overview,
                result.PosterPath,
                result.Id
            )
        ];
    }
    
    public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(
        SearchContainer<SearchBase> result)
    {
        Logger.Debug("Extracting Resource Details into List Item");
        var items = new ObservableCollection<ListItem>();

        foreach (var item in result.Results)
        {
            var mediaType = item.MediaType;

            Logger.Debug("Media Type: {MediaType}", mediaType);
            ListItem listItem;
            switch (mediaType)
            {
                case MediaType.Tv:
                {
                    var res = (SearchTv)item;
                    listItem = TransformDetailsIntoListItem(res.Name, res.FirstAirDate, res.VoteAverage, res.Overview,
                        res.PosterPath, res.Id);
                    break;
                }
                case MediaType.Movie:
                {
                    var res = (SearchMovie)item;
                    listItem = TransformDetailsIntoListItem(res.Title, res.ReleaseDate, res.VoteAverage, res.Overview,
                        res.PosterPath, res.Id);
                    break;
                }
                default: continue;
            }
            listItem.MediaType = mediaType;
            Logger.Trace("Adding {item}", listItem);
            items.Add(listItem);
            Logger.Info("Added {MediaName} to List", listItem.Title);
        }

        return items;
    }
    
    public static ListItem TransformDetailsIntoListItem(
        string mediaName, DateTime? date, double? ratingValue, string overview, string posterPath, int id)
    {
        var year = date != null ? date.Value.Year.ToString(CultureInfo.InvariantCulture) : string.Empty;
        var rating = ratingValue != null ? ratingValue.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;
        var poster = posterPath != null ? SmallPosterBase + posterPath : null;

        return new ListItem
        {
            Title = mediaName,
            Year = year,
            Rating = rating,
            Overview = overview,
            Poster = poster,
            Id = id.ToString()
        };
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
    public void ResultPicked(IResult result, string resultType, string fullFolderPath, string rating = "",
        bool isPickedById = false)
    {
        Logger.Debug("Preparing the Selected Result for Download And final List View");
    
        if (result.PosterPath == null)
        {
            Logger.Warn("No Poster Found, path - {Folder}", fullFolderPath);
            throw new InvalidDataException("NoPoster");
        }
    
        Logger.Debug("Rating: {Rating}, Result Type: {ResultType}", rating, resultType);

        rating = PrepareRating(resultType, rating, result);
    
        var folderName = Path.GetFileName(fullFolderPath);
        var localPosterPath = Path.Combine(fullFolderPath, $"{IconUtils.GetImageName()}.png");
        var posterUrl = GetPosterUrl(result.PosterPath, PosterSize.Original, client);
    
        var resultDetails = new SearchResultData
        {
            Result = result,
            ResultType = resultType,
            FullFolderPath = fullFolderPath,
            Rating = rating,
            IsPickedById = isPickedById,
            FolderName = folderName,
            LocalPosterPath = localPosterPath,
        };
        var type = PickResult(resultDetails, out var id);
    
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
    
    private static string PrepareRating(string resultType, string rating, dynamic result)
    {
        if (!string.IsNullOrWhiteSpace(rating) || resultType == MediaTypes.Collection)
        {
            return rating;
        }

        Logger.Debug("Rating is null or empty, getting rating from result");
        rating = result.VoteAverage.ToString(CultureInfo.InvariantCulture);
        Logger.Debug("Rating: {Rating}", rating);
        return rating;
    }
    
    private string PickResult(SearchResultData details, out int id)
    {
        id = 0;
        var mediaType = details.ResultType;
        switch (mediaType)
        {
            case MediaTypes.Tv:
            {
                var pickedResult = CastResult(details.IsPickedById ? typeof(TvShow) : typeof(SearchTv), details.Result);
                var year = pickedResult.FirstAirDate?.Year.ToString(CultureInfo.InvariantCulture) ?? "";
                AddToPickedList(pickedResult.Name, year, details.Rating, details.FullFolderPath, details.FolderName, details.LocalPosterPath);
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Movie:
            {
                var pickedResult = CastResult(details.IsPickedById ? typeof(Movie) : typeof(SearchMovie), details.Result);
                var year = pickedResult.ReleaseDate?.Year.ToString(CultureInfo.InvariantCulture) ?? "";
                AddToPickedList(pickedResult.Title, year, details.Rating, details.FullFolderPath, details.FolderName, details.LocalPosterPath);
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Collection:
            {
                var pickedResult = CastResult(details.IsPickedById ? typeof(Collection) : typeof(SearchCollection), details.Result);
                AddToPickedList(pickedResult.Name, null, details.Rating, details.FullFolderPath, details.FolderName, details.LocalPosterPath);
                id = pickedResult.Id;
                break;
            }
            case MediaTypes.Mtv:
                mediaType = SelectMtvType(details, out id);
                break;
            default: throw new InvalidDataException($"Invalid Result Type: {mediaType}");
        }

        return mediaType;
    }
    
    private string SelectMtvType(SearchResultData details, out int id)
    {
        var mediaType = details.Result.MediaType;
        id = 0;
        details.ResultType = mediaType switch
        {
            MediaType.Tv => MediaTypes.Tv,
            MediaType.Movie => MediaTypes.Movie,
            _ => mediaType
        };
        return PickResult(details, out id);
    }
    
    private static dynamic CastResult(Type targetType, dynamic result)
    {
        return Convert.ChangeType(result, targetType);
    }
    
    private void AddToPickedList(string title, string year, string rating, string fullFolderPath, string folderName, string localPosterPath)
    {
        FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, title, rating, fullFolderPath, folderName, year);
    }

    public static string GetPosterUrl(ImageData image,string posterSize, TMDbClient client)
    {
        return GetPosterUrl(image.FilePath, posterSize, client);
    }
    
    public static string GetPosterUrl(string posterPath, string posterSize, TMDbClient client)
    {
        return posterPath is null ? string.Empty : client.GetImageUrl(posterSize, posterPath).ToString();
    }
}