using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.IGDB;
using FoliCon.Modules.TMDB;
using FoliCon.Modules.UI;
using NLog;
using Collection = TMDbLib.Objects.Collections.Collection;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class FileUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Determines whether a given string value ends with any string within a collection of file extensions.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="fileExtensions">An iterable collection of file extensions.</param>
    /// <returns>A boolean indicating whether the string value ends with any of the provided file extensions.
    /// Returns true if the value ends with any of the file extensions; false otherwise.</returns>
    private static bool EndsIn(string value, IEnumerable<string> fileExtensions)
    {
        return fileExtensions.Any(fileExtension => value.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsPngOrIco(string fileName) =>
        fileName != null && EndsIn(fileName, new[] { ".png", ".ico" });

    /// <summary>
    /// Deletes Icons (.ico and Desktop.ini files) from all subfolders of given path.
    /// </summary>
    /// <param name="folderPath">Path to delete Icons from</param>
    public static void DeleteIconsFromSubfolders(string folderPath)
    {
        Logger.Debug("Deleting Icons from Subfolders of: {FolderPath}", folderPath);
        DeleteIconsFromFolder(folderPath);
        foreach (var folder in Directory.EnumerateDirectories(folderPath))
        {
            DeleteIconsFromFolder(folder);
        }

        ProcessUtils.RefreshIconCache();
        Logger.Debug("Icons Deleted from Subfolders of: {FolderPath}", folderPath);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT, folderPath);
    }

    public static void DeleteIconsFromFolder(string folderPath)
    {
        Logger.Debug("Deleting Icons from: {FolderPath}", folderPath);
        
        var icoFile = Path.Combine(folderPath, $"{IconUtils.GetImageName()}.ico");
        var iniFile = Path.Combine(folderPath, "desktop.ini");
        try
        {
            if (File.Exists(icoFile))
                File.Delete(icoFile);
            else
                Logger.Debug("ICO File Not Found: {IcoFile}", icoFile);

            if (File.Exists(iniFile))
                File.Delete(iniFile);
            else
                Logger.Debug("INI File Not Found: {IniFile}", iniFile);

        }
        catch (UnauthorizedAccessException e)
        {
            Logger.ForErrorEvent().Message("DeleteIconsFromFolder: UnauthorizedAccessException Occurred. message: {Message}",
                    e.Message)
                .Exception(e).Log();
            HandleUnauthorizedAccessException(e,folderPath);
        }
        Logger.Debug("Icons Deleted from: {FolderPath}", folderPath);
    }

    public static void DeleteMediaInfoFromSubfolders(string folderPath)
    {
        Logger.Debug("Deleting MediaInfo from Subfolders of: {FolderPath}", folderPath);
        var icoFile = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        File.Delete(icoFile);
        foreach (var folder in Directory.EnumerateDirectories(folderPath))
        {
            icoFile = Path.Combine(folder, GlobalVariables.MediaInfoFile);
            File.Delete(icoFile);
        }
        Logger.Debug("MediaInfo Deleted from Subfolders of: {FolderPath}", folderPath);
    }

    public static List<string> GetFolderNames(string folderPath)
    {
        var folderNames = new List<string>();
        if (!string.IsNullOrEmpty(folderPath))
        {
            folderNames.AddRange(from folder in Directory.GetDirectories(folderPath)
                where !File.Exists($@"{folder}\{IconUtils.GetImageName()}.ico")
                select Path.GetFileName(folder));
        }

        return folderNames;
    }

    /// <summary>
    /// Get List of file in given folder.
    /// </summary>
    /// <param name="folder">Folder to Get File from.</param>
    /// <returns>ArrayList with file Names.</returns>
    public static List<string> GetFileNamesFromFolder(string folder)
    {
        Logger.Debug("Getting File Names from Folder: {Folder}", folder);
        var itemList = new List<string>();
        try
        {
            if (string.IsNullOrEmpty(folder)) return itemList;
            itemList.AddRange(Directory.GetFiles(folder).Select(Path.GetFileName));
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("Error Occurred while Getting File Names from Folder: {Folder}", folder)
                .Exception(e).Log();
            itemList.Add($"Error accessing files: {e.Message}");
        }

        return itemList;
    }

    public static void HideFile(string icoFile)
    {
        Logger.Debug("Hiding File: {IcoFile}", icoFile);
        if (!File.Exists(icoFile))
        {
            Logger.ForErrorEvent().Message("File Not Found: {IcoFile}", icoFile).Log();
            return;
        }
        // Set file attribute to "Hidden"
        if ((File.GetAttributes(icoFile) & FileAttributes.Hidden) != FileAttributes.Hidden)
        {
            Logger.Debug("Setting File Attribute to Hidden");
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) | FileAttributes.Hidden);
        }

        // Set file attribute to "System"
        if ((File.GetAttributes(icoFile) & FileAttributes.System) != FileAttributes.System)
        {
            Logger.Debug("Setting File Attribute to System");
            File.SetAttributes(icoFile, File.GetAttributes(icoFile) | FileAttributes.System);
        }
    }

    public static void SaveMediaInfo(int id, string mediaType, string folderPath)
    {
        Logger.Debug("Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
            mediaType, folderPath);
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        try
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
            }
            InIHelper.AddValue("ID", id.ToString(CultureInfo.InvariantCulture), null, filePath);
            InIHelper.AddValue("MediaType", mediaType, null, filePath);
            HideFile(filePath);
        }
        catch (UnauthorizedAccessException e)
        {
            Logger.ForExceptionEvent(e, LogLevel.Error).Message("Error Occurred while Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
                mediaType, filePath).Log();
            MessageBox.Show(CustomMessageBox.Error(
                e.Message.Contains("The process cannot access the file")
                    ? LangProvider.GetLang("FileIsInUse")
                    : LangProvider.GetLang("FailedToSaveMediaInfoAt").Format(filePath), LangProvider.GetLang("UnauthorizedAccess")));
        }
        catch (Exception e)
        {
            Logger.ForExceptionEvent(e, LogLevel.Error).Message("Error Occurred while Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
                mediaType, folderPath).Log();
            CustomMessageBox.Error(e.Message, LangProvider.GetLang("ExceptionOccurred"));
        }
    }

    public static (string ID, string MediaType) ReadMediaInfo(string folderPath)
    {
        string id = null;
        string mediaType = null;
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);

        if (File.Exists(filePath))
        {
            id = InIHelper.ReadValue("ID", null, filePath);
            mediaType = InIHelper.ReadValue("MediaType", null, filePath);

            if (string.IsNullOrWhiteSpace(id))
            {
                id = null;
                mediaType = null;
            }
        }

        var mediaInfo = (ID: id, MediaType: mediaType);
        Logger.Debug("Media Info Read: {@MediaInfo}", mediaInfo);

        return mediaInfo;
    }

    public static DirectoryPermissionsResult CheckDirectoryPermissions(string dirPath)
    {
        var result = new DirectoryPermissionsResult();

        if (!Directory.Exists(dirPath)) return result;

        result.CanRead = CheckReadPermissions(dirPath, "test.txt");
        result.CanWrite = CheckWritePermissions(dirPath, "test.tmp");

        Logger.Debug("Path: {Path}; Directory Permissions Checked: {@Result}", dirPath, result);

        return result;
    }

    private static bool CheckReadPermissions(string dirPath, string fileName)
    {
        try
        {
            // Attempt to open the file with read permissions
            using var stream = File.OpenRead(Path.Combine(dirPath, fileName));
            return true;
        }
        catch
        {
            // Ignore any exception, it means we don't have read permissions.
            return false;
        }
    }

    private static bool CheckWritePermissions(string dirPath, string fileName)
    {
        try
        {
            // Attempt to open a new file with write permissions
            using (File.Create(Path.Combine(dirPath, fileName)))
            {
            }

            // Successfully created file, try to delete it
            File.Delete(Path.Combine(dirPath, fileName));
            return true;
        }
        catch
        {
            // Ignore any exception, it means we don't have write permissions.
            return false;
        }
    }

    public static string GetResourcePath(string resource)
    {
        var path = Path.Combine(Path.GetTempPath(), resource);
        Logger.Debug("Getting Resource Path, Resource: {Resource}, Path: {Path}", resource, path);
        if (File.Exists(path)) return path;

        var resourceUri = new Uri($"pack://application:,,,/FoliCon;component/Resources/{resource}");
        var resourceStream = Application.GetResourceStream(resourceUri);

        if (resourceStream == null)
        {
            Logger.Warn("Resource {Resource} cannot be found");
            throw new InvalidOperationException($"Resource {resource} cannot be found.");
        }
        using var input = resourceStream.Stream;
        using Stream output = File.Create(path);
        input.CopyTo(output);
        return path;

    }

    public static async void CheckForUpdate(bool onlyShowIfUpdateAvailable = false)
    {
        Logger.Debug("Checking for Update");

        if (!ApplicationHelper.IsConnectedToInternet())
        {
            DialogUtils.ShowGrowlError(LangProvider.GetLang("NetworkNotAvailable"));
            return;
        }

        var ver = await UpdateHelper.CheckUpdateAsync("DineshSolanki", "FoliCon");

        if (!ver.IsExistNewVersion)
        {
            Logger.Debug("No New Version Found");

            if (!onlyShowIfUpdateAvailable)
            {
                DialogUtils.ShowGrowlInfo(LangProvider.GetLang("ThisIsLatestVersion"));
            }

            return;
        }

        Logger.Debug("New Version Found: {}", ver.TagName);
        DialogUtils.ConfirmUpdate(ver);
    }

    public static void HandleUnauthorizedAccessException(UnauthorizedAccessException ex, string path)
    {
        MessageBox.Show(CustomMessageBox.Error(
            ex.Message.Contains("The process cannot access the file")
                ? LangProvider.GetLang("FileIsInUse")
                : LangProvider.GetLang("FailedFileAccessAt").Format(path), LangProvider.GetLang("ExceptionOccurred")));
    }

    /// <summary>
    /// Adds Data to DataTable specifically PickedListDataTable
    /// </summary>
    /// <param name="dataTable">DataTable to insert Data</param>
    /// <param name="poster">Local Poster path</param>
    /// <param name="title">Media Title</param>
    /// <param name="rating">Media Rating</param>
    /// <param name="fullFolderPath">Complete Folder Path</param>
    /// <param name="folderName">Short Folder Name</param>
    /// <param name="year">Media Year</param>
    public static void AddToPickedListDataTable(List<PickedListItem> dataTable, string poster, string title, string rating,
        string fullFolderPath, string folderName, string year = "")
    {
        Logger.Debug("Adding Data to PickedListDataTable");
        if (rating == "0")
        {
            rating = "";
        }
        PickedListItem pickedListItem = new(title, year, rating, fullFolderPath, folderName,poster);
        dataTable.Add(pickedListItem);
        Logger.Trace("Data Added to PickedListDataTable: {@Row}",pickedListItem);
    }

    public static ObservableCollection<ListItem> FetchAndAddDetailsToListView(ResultResponse result, string query,
        bool isPickedById)
    {
        Logger.Trace(
            "Fetching and Adding Details to ListView, Result: {@Result}, Query: {Query}, isPickedById: {IsPickedById}",
            result, query, isPickedById);
        var source = new ObservableCollection<ListItem>();

        if (result.MediaType == MediaTypes.Tv)
        {
            dynamic ob = isPickedById ? (TvShow)result.Result : (SearchContainer<SearchTv>)result.Result;
            source = Tmdb.ExtractTvDetailsIntoListItem(ob);
        }
        else if (result.MediaType == MediaTypes.Movie || result.MediaType == MediaTypes.Collection)
        {
            if (query.ToLower(CultureInfo.InvariantCulture).Contains("collection"))
            {
                dynamic ob = isPickedById
                    ? (Collection)result.Result
                    : (SearchContainer<SearchCollection>)result.Result;
                source = Tmdb.ExtractCollectionDetailsIntoListItem(ob);
            }
            else
            {
                dynamic ob;
                try
                {
                    ob = isPickedById ? (Movie)result.Result : (SearchContainer<SearchMovie>)result.Result;
                    source = Tmdb.ExtractMoviesDetailsIntoListItem(ob);
                }
                catch (Exception e)
                {
                    Logger.ForErrorEvent().Message("Error Occurred while Fetching Movie Details, treating as collection")
                        .Exception(e).Log();
                    ob = isPickedById
                        ? (Collection)result.Result
                        : (SearchContainer<SearchCollection>)result.Result;
                    source = Tmdb.ExtractCollectionDetailsIntoListItem(ob);
                }
            }
        }
        else if (result.MediaType == MediaTypes.Mtv)
        {
            var ob = (SearchContainer<SearchBase>)result.Result;
            source = Tmdb.ExtractResourceDetailsIntoListItem(ob);
        }
        else if (result.MediaType == MediaTypes.Game)
        {
            var ob = (Game[])result.Result;
            source = IgdbClass.ExtractGameDetailsIntoListItem(ob);
        }
        Logger.Trace("Details Added to ListView: {@Source}", source);
        return source;
    }

    /// <summary>
    /// Set folder icon for a given folder.
    /// </summary>
    /// <param name="icoFile"> path to the icon file [MUST BE .Ico]</param>
    /// <param name="folderPath">path to the folder</param>
    public static void SetFolderIcon(string icoFile, string folderPath)
    {
        Logger.Debug("Setting Folder Icon, ICO File: {IcoFile}, Folder Path: {FolderPath}", icoFile, folderPath);
        try
        {
            var folderSettings = new SHFOLDERCUSTOMSETTINGS
            {
                dwMask = FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
                pszIconFile = icoFile,
                dwSize = (uint)Marshal.SizeOf(typeof(SHFOLDERCUSTOMSETTINGS)),
                cchIconFile = 0
            };
            //FolderSettings.iIconIndex = 0;
            var unused =
                SHGetSetFolderCustomSettings(ref folderSettings, folderPath, FCS.FCS_FORCEWRITE);
            Logger.Info("Folder Icon Set, ICO File: {IcoFile}, Folder Path: {FolderPath}", icoFile, folderPath);
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("Error Occurred while Setting Folder Icon, ICO File: {IcoFile}, Folder Path: {FolderPath}", icoFile, folderPath)
                .Exception(e).Log();
            MessageBox.Error(e.Message);
        }

        ApplyChanges(folderPath);
    }

    public static void ApplyChanges(string folderPath)
    {
        Logger.Debug("Applying Changes to Folder: {FolderPath}", folderPath);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_PATHW, folderPath);
    }

    public static void ReadApiConfiguration(out string tmdbkey, out string igdbClientId,
        out string igdbClientSecret, out string dartClientSecret, out string dartId)
    {
        var settings = GlobalDataHelper.Load<AppConfig>();
        tmdbkey = settings.TmdbKey;
        igdbClientId = settings.IgdbClientId;
        igdbClientSecret = settings.IgdbClientSecret;
        dartClientSecret = settings.DevClientSecret;
        dartId = settings.DevClientId;
    }

    public static void WriteApiConfiguration(string tmdbkey, string igdbClientId, string igdbClientSecret,
        string dartClientSecret, string dartId)
    {
        Services.Settings.TmdbKey = tmdbkey;
        Services.Settings.IgdbClientId = igdbClientId;
        Services.Settings.IgdbClientSecret = igdbClientSecret;
        Services.Settings.DevClientId = dartId;
        Services.Settings.DevClientSecret = dartClientSecret;
        Services.Settings.Save();
    }
}