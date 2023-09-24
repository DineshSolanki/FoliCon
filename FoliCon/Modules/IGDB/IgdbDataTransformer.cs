using FoliCon.Modules.utils;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.IGDB;

public class IgdbDataTransformer(ref DataTable listDataTable, ref List<ImageToDownload> imgDownloadList)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<ImageToDownload> _imgDownloadList =
        imgDownloadList ?? throw new ArgumentNullException(nameof(imgDownloadList));

    private readonly DataTable _listDataTable = listDataTable ?? throw new ArgumentNullException(nameof(listDataTable));

    public static ObservableCollection<ListItem> ExtractGameDetailsIntoListItem(IEnumerable<Game> games)
    {
        return new ObservableCollection<ListItem>(
            games.Select(MakeListItemFromGame));
    }

    private static ListItem MakeListItemFromGame(Game game)
    {
        const string placeholder = "";
        var (mediaName, year, overview, poster, id) = GetGameInfo(game);
        return new ListItem(mediaName, year, placeholder, overview, poster, placeholder, id.ToString());
    }

    private static (string mediaName, string year, string overview, string poster, long? id) GetGameInfo(Game game)
    {
        var mediaName = game.Name;
        var id = game.Id;
        var year = game.FirstReleaseDate != null
            ? game.FirstReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        var overview = game.Summary;
        var poster = game.Cover != null
            ? $"https://{ImageHelper.GetImageUrl(game.Cover.Value.ImageId, ImageSize.HD720)[2..]}"
            : null;
        return (mediaName, year, overview, poster, id);
    }

    public void ResultPicked(Game game, string fullFolderPath, string rating = "")
    {
        ValidateGamePoster(game);
        var folderName = Path.GetFileName(fullFolderPath);
        var localPosterPath = $@"{fullFolderPath}\{folderName}.png";
        HandleGamePosterPath(game, fullFolderPath, localPosterPath, rating, folderName);
    }

    private static void ValidateGamePoster(Game game)
    {
        if (game.Cover != null) return;
        Logger.Warn($"No Poster Found for {game.Name}");
        throw new InvalidDataException("NoPoster");
    }

    private void HandleGamePosterPath(Game game, string fullFolderPath, string localPosterPath,
        string rating, string folderName)
    {
        var year = game.FirstReleaseDate != null
            ? game.FirstReleaseDate.Value.Year.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        var posterUrl = ImageHelper.GetImageUrl(game.Cover.Value.ImageId, ImageSize.HD720);

        FileUtils.AddToPickedListDataTable(_listDataTable, localPosterPath, game.Name, rating, fullFolderPath,
            folderName, year);

        if (game.Id != null)
            FileUtils.SaveMediaInfo((int)game.Id, "Game", fullFolderPath);

        var temporaryImage = new ImageToDownload
        {
            LocalPath = localPosterPath,
            RemotePath = new Uri($"https://{posterUrl[2..]}")
        };
        _imgDownloadList.Add(temporaryImage);
    }
}