namespace FoliCon.Modules;

public class IgdbClass
{
    private readonly DataTable _listDataTable;
    private readonly IGDBClient _serviceClient;
    private readonly List<ImageToDownload> _imgDownloadList;

    public IGDBClient GetClient()
    {
        return _serviceClient;
    }
    /// <summary>
    /// IGDB Helper Class for Working with IGDB API efficiently for thi project.
    /// </summary>
    /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
    /// <param name="serviceClient">Initialized IGDB/Twitch Client</param>
    /// <param name="imgDownloadList">List that stores all the images to download.</param>
    public IgdbClass(ref DataTable listDataTable, ref IGDBClient serviceClient,
        ref List<ImageToDownload> imgDownloadList)
    {
        _listDataTable = listDataTable ?? throw new ArgumentNullException(nameof(listDataTable));
        _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
        _imgDownloadList = imgDownloadList ?? throw new ArgumentNullException(nameof(imgDownloadList));
    }

    /// <summary>
    /// Searches IGDB for a specified query.
    /// </summary>
    /// <param name="query">Title to search</param>
    /// <returns>Returns Search result with its Media Type</returns>
    public async Task<ResultResponse> SearchGameAsync(string query)
    {
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"search \"{query}\"; fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*;");

        var response = new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = r
        };
        return response;
    }

    public async Task<GameVideo[]> GetGameVideo(string id)
    {
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<GameVideo>(IGDBClient.Endpoints.GameVideos,
            $"fields game,name,video_id; where game = {id};");
        return r;
    }
    public async Task<ResultResponse> SearchGameByIdAsync(string id)
    {
        Contract.Assert(_serviceClient != null);
        var r = await _serviceClient.QueryAsync<Game>(IGDBClient.Endpoints.Games,
            $"fields artworks.image_id, name,first_release_date,total_rating,summary,cover.*; where id = {id};");
        var response = new ResultResponse
        {
            MediaType = MediaTypes.Game,
            Result = r
        };
        return response;
    }

    public async Task<Artwork[]> GetArtworksByGameIdAsync(string id)
    {
        var r = await _serviceClient.QueryAsync<Artwork>(IGDBClient.Endpoints.Artworks,
            $"fields image_id; where game = {id};");
        return r;
    }
    public static ObservableCollection<ListItem> ExtractGameDetailsIntoListItem(Game[] result)
    {
        var items = new ObservableCollection<ListItem>();
        foreach (var (mediaName, year, overview, poster,id) in from item in result
                 let mediaName = item.Name
                 let id = item.Id
                 let year = item.FirstReleaseDate != null ? item.FirstReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture) : ""
                 let overview = item.Summary
                 let poster = item.Cover != null
                     ? "https://" + ImageHelper.GetImageUrl(item.Cover.Value.ImageId, ImageSize.HD720)[2..]
                     : null
                 select (mediaName, year, overview, poster,id))
        {
            items.Add(new ListItem(mediaName, year, "", overview, poster,"",id.ToString()));
        }

        return items;
    }

    public void ResultPicked(Game result, string fullFolderPath, string rating = "")
    {
        if (result.Cover == null)
        {
            throw new InvalidDataException("NoPoster");
        }

        var folderName = Path.GetFileName(fullFolderPath);
        var localPosterPath = fullFolderPath + @"\" + folderName + ".png";
        var year = result.FirstReleaseDate != null ? result.FirstReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture) : "";
        var posterUrl = ImageHelper.GetImageUrl(result.Cover.Value.ImageId, ImageSize.HD720);
        Util.AddToPickedListDataTable(_listDataTable, localPosterPath, result.Name, rating, fullFolderPath, folderName,
            year);
        if (result.Id != null) Util.SaveMediaInfo((int)result.Id, "Game", fullFolderPath);
        var tempImage = new ImageToDownload
        {
            LocalPath = localPosterPath,
            RemotePath = new Uri("https://" + posterUrl[2..])
        };
        _imgDownloadList.Add(tempImage);
    }
}