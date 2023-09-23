using NLog;
using Collection = TMDbLib.Objects.Collections.Collection;
using Logger = NLog.Logger;

namespace FoliCon.Modules.TMDB;

public class Tmdb
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly TmdbDataTransformer _dataTransformer;
    private readonly TmdbService _tmdbService;

    public TMDbClient GetClient()
    {
        return _tmdbService?.GetClient();
    }

    /// <summary>
    /// TMDB Helper Class for Working with IGDB API efficiently for this project.
    /// </summary>
    /// <param name="serviceClient">Initialized TMDB Client</param>
    /// <param name="listDataTable">DataTable that stores all the Picked Results.</param>
    /// <param name="imgDownloadList">List that stores all the images to download.</param>
    public Tmdb(ref TMDbClient serviceClient, ref DataTable listDataTable,
        ref List<ImageToDownload> imgDownloadList)
    {
        Logger.Debug("Initializing TMDB Helper Class");
        _tmdbService = new TmdbService(serviceClient);
        _dataTransformer = new TmdbDataTransformer(ref listDataTable, ref imgDownloadList);
        Logger.Debug("Initialized TMDB Helper Class");
    }

    public Task<ImagesWithId> SearchMovieImages(int movieId)
    {
        return _tmdbService.SearchMovieImages(movieId);
    }

    public Task<ImagesWithId> SearchTvImages(int tvId)
    {
        return _tmdbService.SearchTvImages(tvId);
    }

    public Task<ImagesWithId> SearchCollectionImages(int collectionId)
    {
        return _tmdbService.SearchCollectionImages(collectionId);
    }


    public static ObservableCollection<ListItem> ExtractResourceDetailsIntoListItem(
        SearchContainer<SearchBase> result)
    {
        return TmdbDataTransformer.ExtractResourceDetailsIntoListItem(result);
    }

    public static ObservableCollection<ListItem> ExtractCollectionDetailsIntoListItem(
        Collection result)
    {
        return TmdbDataTransformer.ExtractCollectionDetailsIntoListItem(result);
    }

    public static ObservableCollection<ListItem> ExtractMoviesDetailsIntoListItem(
        dynamic result)
    {
        return TmdbDataTransformer.ExtractMoviesDetailsIntoListItem(result);
    }

    public static ObservableCollection<ListItem> ExtractTvDetailsIntoListItem(dynamic result)
    {
        return TmdbDataTransformer.ExtractTvDetailsIntoListItem(result);
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
    public void ResultPicked(dynamic result, string resultType, string fullFolderPath, string rating = "",
        bool isPickedById = false)
    {
        _dataTransformer.ResultPicked(result, resultType, fullFolderPath, rating, isPickedById);
    }

    /// <summary>
    /// Searches TMDB for a query in Specified search mode
    /// </summary>
    /// <param name="query">Title to search</param>
    /// <param name="searchMode">Search Mode such as Movie,TV</param>
    /// <returns>Returns Search result with its Media Type</returns>
    public Task<ResultResponse> SearchAsync(string query, string searchMode)
    {
        return _tmdbService.SearchAsync(query, searchMode);
    }

    /// <summary>
    /// Searches TMDB media by ID as per media Type.
    /// </summary>
    /// <returns>Returns Search result with its Media Type</returns>
    public Task<ResultResponse> SearchByIdAsync(int id, string mediaType)
    {
        return _tmdbService.SearchByIdAsync(id, mediaType);
    }
}