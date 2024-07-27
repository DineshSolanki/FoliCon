using FoliCon.Modules.Media;

namespace FoliCon.Modules.utils;

[Localizable(false)]
public static class IconUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string ImageName = "folicon";

    public static string GetImageName()
    {
        return ImageName;
    }
    
    /// <summary>
    /// Creates Icons from PNG
    /// </summary>
    public static int MakeIco(string iconMode, string selectedFolder, List<PickedListItem> pickedListDataTable,
        bool isRatingVisible, bool isMockupVisible)
    {
        Logger.Debug(
            "Creating Icons from PNG, Icon Mode: {IconMode}, Selected Folder: {SelectedFolder}, isRatingVisible: {IsRatingVisible}, isMockupVisible: {IsMockupVisible}",
            iconMode, selectedFolder, isRatingVisible, isMockupVisible);
        
        var iconProcessedCount = 0;
        var ratingVisibility = isRatingVisible ? "visible" : "hidden";
        var mockupVisibility = isMockupVisible ? "visible" : "hidden";

        foreach (var item in pickedListDataTable)
        {
            var parent = Directory.GetParent(item.Folder);
            var parentFolder = parent != null
                ? parent.FullName
                : selectedFolder;
            var folderName = item.FolderName;
            var targetFile = $@"{parentFolder}\{folderName}\{ImageName}.ico";
            var pngFilePath = $@"{parentFolder}\{folderName}\{ImageName}.png";
            if (File.Exists(pngFilePath) && !File.Exists(targetFile))
            {
                var rating = item.Rating;
                var mediaTitle = item.Title;
                
                BuildFolderIco(iconMode, pngFilePath, rating, ratingVisibility, 
                    mockupVisibility, mediaTitle);
                iconProcessedCount += 1;

                Logger.Info("Icon Created for Folder: {Folder}", folderName);
                Logger.Debug("Deleting PNG File: {PngFilePath}", pngFilePath);

                File.Delete(pngFilePath); //<--IO Exception here
            }
            if (!File.Exists(targetFile))
            {
                continue;
            }

            FileUtils.HideFile(targetFile);
            FileUtils.SetFolderIcon($"{ImageName}.ico", $@"{parentFolder}\{folderName}");
        }

        FileUtils.ApplyChanges(selectedFolder);
        SHChangeNotify(SHCNE.SHCNE_UPDATEITEM, SHCNF.SHCNF_PATHW, selectedFolder);
        return iconProcessedCount;
    }

    /// <summary>
    /// Converts From PNG to ICO
    /// </summary>
    /// <param name="iconMode">Icon Mode to generate Icon.</param>
    /// <param name="filmFolderPath"> Path where to save and where PNG is Downloaded</param>
    /// <param name="rating"> if Wants to Include rating on Icon</param>
    /// <param name="ratingVisibility">Show rating or NOT</param>
    /// <param name="mockupVisibility">Is Cover Mockup visible. </param>
    /// <param name="mediaTitle">Title of the media.</param>
    private static void BuildFolderIco(string iconMode, string filmFolderPath, string rating,
        string ratingVisibility, string mockupVisibility, string mediaTitle)
    {
        Logger.Debug("Converting From PNG to ICO, Icon Mode: {IconMode}, Film Folder Path: {FilmFolderPath}," +
                          " Rating: {Rating}, Rating Visibility: {RatingVisibility}, Mockup Visibility: {MockupVisibility}," +
                          " Media Title: {MediaTitle}",
            iconMode, filmFolderPath, rating, ratingVisibility, mockupVisibility, mediaTitle);
        
        if (!File.Exists(filmFolderPath))
        {
            Logger.Warn("PNG File Not Found: {FilmFolderPath}", filmFolderPath);
            return;
        }

        ratingVisibility = string.IsNullOrEmpty(rating) ? "Hidden" : ratingVisibility;
        if (!string.IsNullOrEmpty(rating) && rating != "10")
        {
            rating = !rating.Contains('.') ? $"{rating}.0" : rating;
        }

        Bitmap icon;
        if (iconMode == "Professional")
        {
            icon = new ProIcon(filmFolderPath).RenderToBitmap();
        }
        else
        {
            using var task = GlobalVariables.IconOverlayType() switch
            {
                IconOverlay.Legacy => StaTask.Start(() =>
                    new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap()),
                IconOverlay.Alternate => StaTask.Start(() =>
                    new PosterIconAlt(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap()),
                IconOverlay.Liaher => StaTask.Start(() =>
                    new PosterIconLiaher(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap()),
                IconOverlay.Faelpessoal => StaTask.Start(() => new PosterIconFaelpessoal(new PosterIcon(
                    filmFolderPath, rating,
                    ratingVisibility, mockupVisibility, mediaTitle)).RenderToBitmap()),
                IconOverlay.FaelpessoalHorizontal => StaTask.Start(() => new PosterIconFaelpessoalHorizontal(
                    new PosterIcon(
                        filmFolderPath, rating,
                        ratingVisibility, mockupVisibility, mediaTitle)).RenderToBitmap()),
                _ => StaTask.Start(() =>
                    new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap())
            };
            task.Wait();
            icon = task.Result;
        }
        Logger.Info("Converting PNG to ICO for Folder: {FilmFolderPath}", filmFolderPath);
        PngToIcoService.Convert(icon, filmFolderPath.Replace("png", "ico"));
        icon.Dispose();
        Logger.Debug("Icon Created for Folder: {Folder}", filmFolderPath);
    }
}
