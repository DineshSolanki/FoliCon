namespace FoliCon.Modules.IGDB;

public class IgdbClass
{
    private readonly IgdbDataTransformer _dataTransformer;
    private readonly IgdbService _igdbService;

    public IGDBClient GetClient()
    {
        return _igdbService.GetClient();
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
        _dataTransformer = new IgdbDataTransformer(ref listDataTable, ref imgDownloadList);
        _igdbService = new IgdbService(ref serviceClient);
    }

    /// <summary>
    /// Searches IGDB for a specified query.
    /// </summary>
    /// <param name="query">Title to search</param>
    /// <returns>Returns Search result with its Media Type</returns>
    public Task<ResultResponse> SearchGameAsync(string query)
    {
        return _igdbService.SearchGameAsync(query);
    }

    public Task<GameVideo[]> GetGameVideo(string id)
    {
        return _igdbService.GetGameVideo(id);
    }
    public Task<ResultResponse> SearchGameByIdAsync(string id)
    {
        return _igdbService.SearchGameByIdAsync(id);
    }

    public Task<Artwork[]> GetArtworksByGameIdAsync(string id)
    {
        return _igdbService.GetArtworksByGameIdAsync(id);
    }
    public static ObservableCollection<ListItem> ExtractGameDetailsIntoListItem(Game[] result)
    {
        return IgdbDataTransformer.ExtractGameDetailsIntoListItem(result);
    }

    public void ResultPicked(Game result, string fullFolderPath, string rating = "")
    {
        _dataTransformer.ResultPicked(result, fullFolderPath, rating);
    }
}