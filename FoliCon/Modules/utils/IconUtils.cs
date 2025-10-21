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
    public static async Task<int> MakeIco(string iconMode, string selectedFolder, List<PickedListItem> pickedListDataTable,
         bool isRatingVisible, bool isMockupVisible, IProgress<ProgressBarData> progressCallback)
     {
         Logger.Debug(
             "Creating Icons from PNG, Icon Mode: {IconMode}, Selected Folder: {SelectedFolder}, isRatingVisible: {IsRatingVisible}, isMockupVisible: {IsMockupVisible}",
             iconMode, selectedFolder, isRatingVisible, isMockupVisible);
         var iconProcessedCount = 0;
         var ratingVisibility = isRatingVisible ? "visible" : "hidden";
         var mockupVisibility = isMockupVisible ? "visible" : "hidden";
         var max = pickedListDataTable.Count;
         var extractionProgress = new ProgressBarData(0, max, Lang.CreatingIconWithCount.Format(0, max));
         progressCallback.Report(extractionProgress);
         
         var lockObj = new object();
         var iconOverlay = GlobalVariables.IconOverlayType();
         await Parallel.ForEachAsync(pickedListDataTable, async (item, _) =>
         {
             var parent = Directory.GetParent(item.Folder);
             var parentFolder = parent != null ? parent.FullName : selectedFolder;
             var folderName = item.FolderName;
             var targetFile = $@"{parentFolder}\{folderName}\{ImageName}.ico";
             var pngFilePath = $@"{parentFolder}\{folderName}\{ImageName}.png";
             
             if (FileUtils.FileExists(pngFilePath) && !FileUtils.FileExists(targetFile))
             {
                 var rating = item.Rating;
                 var mediaTitle = item.Title;
                 var iconProperties = new IconProperties(iconMode, pngFilePath, rating, ratingVisibility, mockupVisibility, mediaTitle);
                 await BuildFolderIco(iconProperties, iconOverlay);
                 
                 lock (lockObj)
                 {
                     iconProcessedCount += 1;
                 }
                 
                 Logger.Info("Icon Created for Folder: {Folder}", folderName);
                 Logger.Debug("Deleting PNG File: {PngFilePath}", pngFilePath);
                 
                 File.Delete(pngFilePath); //<--IO Exception here
             }
             
             if (FileUtils.FileExists(targetFile))
             {
                 FileUtils.HideFile(targetFile);
                 FileUtils.SetFolderIcon($"{ImageName}.ico", $@"{parentFolder}\{folderName}");
             }
             
             lock (lockObj)
             {
                 extractionProgress.Value = iconProcessedCount;
                 extractionProgress.Text = Lang.CreatingIconWithCount.Format(iconProcessedCount, max);
                 progressCallback.Report(extractionProgress);
             }
         });
     
         extractionProgress.Text = Lang.RefreshingFolder;
         progressCallback.Report(extractionProgress);
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
    private static async Task BuildFolderIco(IconProperties iconProperties, IconOverlay iconOverlay)
    {
        Logger.Debug("Converting From PNG to ICO, {IconProperties}", iconProperties);
        var filmFolderPath = iconProperties.FilmFolderPath;
        if (!FileUtils.FileExists(filmFolderPath))
        {
            Logger.Warn("PNG File Not Found: {FilmFolderPath}", filmFolderPath);
            return;
        }

        var ratingVisibility = string.IsNullOrEmpty(iconProperties.Rating) ? "Hidden" : iconProperties.RatingVisibility;
        var rating = iconProperties.Rating;
        if (!string.IsNullOrEmpty(iconProperties.Rating) && iconProperties.Rating != "10" && !iconProperties.Rating.Contains('.'))
        {
            rating = $"{iconProperties.Rating}.0";
        }

        Bitmap icon;
        if (iconProperties.IconMode == "Professional")
        {
            icon = new ProIcon(filmFolderPath).RenderToBitmap();
        }
        else
        {
            var mockupVisibility = iconProperties.MockupVisibility;
            var mediaTitle = iconProperties.MediaTitle;
            using var task = iconOverlay switch
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
                IconOverlay.Windows11 => StaTask.Start(() =>
                    new PosterIconWindows11(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap()),
                _ => StaTask.Start(() =>
                    new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                        .RenderToBitmap())
            };
            icon = await task;
        }
        Logger.Info("Converting PNG to ICO for Folder: {FilmFolderPath}", filmFolderPath);
        PngToIcoService.Convert(icon, filmFolderPath.Replace("png", "ico"));
        icon.Dispose();
        Logger.Debug("Icon Created for Folder: {Folder}", filmFolderPath);
    }
}
