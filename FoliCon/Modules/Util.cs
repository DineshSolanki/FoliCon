using NLog;
using NLog.Config;
using NLog.Targets;
using Polly;
using Sentry;
using Sentry.NLog;
using Collection = TMDbLib.Objects.Collections.Collection;
using Logger = NLog.Logger;
using PosterIcon = FoliCon.Models.PosterIcon;

namespace FoliCon.Modules;

internal static class Util
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static async void CheckForUpdate(bool onlyShowIfUpdateAvailable = false)
    {
        Logger.Debug("Checking for Update");
        if (ApplicationHelper.IsConnectedToInternet())
        {
            var ver = await UpdateHelper.CheckUpdateAsync("DineshSolanki", "FoliCon");
            if (ver.IsExistNewVersion)
            {
                Logger.Debug("New Version Found: {}", ver.TagName);
                var info = new GrowlInfo
                {
                    Message = LangProvider.GetLang("NewVersionFound").Format(ver.TagName,
                        ver.Changelog.Replace("\\n", Environment.NewLine)),
                    ConfirmStr = LangProvider.GetLang("UpdateNow"),
                    CancelStr = LangProvider.GetLang("Ignore"),
                    ShowDateTime = false,
                    ActionBeforeClose = isConfirmed =>
                    {
                        switch (isConfirmed)
                        {
                            case true:
                                Logger.Debug("Update Confirmed. Starting Update Process");
                                StartProcess(ver.ReleaseUrl);
                                break;
                        }

                        return true;
                    }
                };
                Growl.AskGlobal(info);
            }
            else
            {
                Logger.Debug("No New Version Found");
                if (onlyShowIfUpdateAvailable is not false) return;
                var info = new GrowlInfo
                {
                    Message = LangProvider.GetLang("ThisIsLatestVersion"),
                    ShowDateTime = false,
                    StaysOpen = false
                };
                Growl.InfoGlobal(info);
            }
        }
        else
            Growl.ErrorGlobal(new GrowlInfo
                { Message = LangProvider.GetLang("NetworkNotAvailable"), ShowDateTime = false });
    }

    /// <summary>
    /// Starts Process associated with given path.
    /// </summary>
    /// <param name="path">if path is a URL it opens url in default browser, if path is File Or folder path it will be started.</param>
    public static void StartProcess(string path)
    {
        Logger.Debug("Starting Process: {}", path);
        Process.Start(new ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
    }

    private static bool EndsIn(string value, IEnumerable<string> fileExtensions)
    {
        return fileExtensions.Any(fileExtension => value.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase));
    }

    
    public static bool IsPngOrIco(string fileName) =>
        fileName != null &&
        EndsIn(fileName, new[] { ".png", ".ico" });
    
    /// <summary>
    /// Set Column width of list view to fit content
    /// </summary>
    /// <param name="listView"> list view to change width</param>
    public static void SetColumnWidth(ListView listView)
    {
        if (listView.View is not GridView gridView) return;
        foreach (var column in gridView.Columns)
        {
            if (double.IsNaN(column.Width))
            {
                column.Width = column.ActualWidth;
            }

            column.Width = double.NaN;
        }
    }

    /// <summary>
    /// Terminates Explorer.exe Process.
    /// </summary>
    private static void KillExplorer()
    {
        Logger.Debug("Killing Explorer.exe");
        var taskKill = new ProcessStartInfo("taskkill", "/F /IM explorer.exe")
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        };
        using var process = new Process();
        process.StartInfo = taskKill;
        process.Start();
        process.WaitForExit();
        Logger.Debug("Explorer.exe Killed");
    }

    /// <summary>
    /// Terminates and Restart Explorer.exe process.
    /// </summary>
    public static void RestartExplorer()
    {
        KillExplorer();
        Process.Start("explorer.exe");
        Logger.Debug("Explorer.exe Restarted");
    }

    public static void RefreshIconCache()
    {
        Logger.Debug("Refreshing Icon Cache");
        _ = Kernel32.Wow64DisableWow64FsRedirection(out _);
        var objProcess = new Process
        {
            StartInfo =
            {
                FileName = Environment.GetFolderPath(Environment.SpecialFolder.System) +
                           "\\ie4uinit.exe",
                Arguments = "-ClearIconCache",
                WindowStyle = ProcessWindowStyle.Normal
            }
        };
        objProcess.Start();
        objProcess.WaitForExit();
        objProcess.Close();
        Logger.Debug("Icon Cache Refreshed");
        Kernel32.Wow64EnableWow64FsRedirection(true);
    }

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

        RefreshIconCache();
        Logger.Debug("Icons Deleted from Subfolders of: {FolderPath}", folderPath);
        SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT, folderPath);
    }

    public static void DeleteIconsFromFolder(string folderPath)
    {
        Logger.Debug("Deleting Icons from: {FolderPath}", folderPath);
        var folderName = Path.GetFileName(folderPath);
        var icoFile = Path.Combine(folderPath, $"{folderName}.ico");
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

    //Handle UnauthorizedAccessException
    public static void HandleUnauthorizedAccessException(UnauthorizedAccessException ex, string path)
    {
        MessageBox.Show(CustomMessageBox.Error(
            ex.Message.Contains("The process cannot access the file")
                ? LangProvider.GetLang("FileIsInUse")
                : LangProvider.GetLang("FailedFileAccessAt").Format(path), LangProvider.GetLang("ExceptionOccurred")));
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

    /// <summary>
    /// Checks if Web is accessible from This System
    /// </summary>
    /// <returns> Returns true if Web is accessible</returns>
    public static bool IsNetworkAvailable()
    {
        Logger.ForDebugEvent().Message("Network Availability Check Started").Log();
        const string host = "8.8.8.8";
        var result = false;
        using var p = new Ping();
        try
        {
            Logger.Debug("Pinging {Host}", host);
            var reply = p.Send(host, 5000, new byte[32], new PingOptions { DontFragment = true, Ttl = 32 });
            if (reply is { Status: IPStatus.Success })
            {
                result = true;
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("Error Occurred while checking Network Availability : {Message}", e.Message)
                .Exception(e).Log();
            // ignored
        }
        Logger.Debug("Network availability: {}", result);
        return result;
    }

    public static List<string> GetFolderNames(string folderPath)
    {
        var folderNames = new List<string>();
        if (!string.IsNullOrEmpty(folderPath))
        {
            folderNames.AddRange(from folder in Directory.GetDirectories(folderPath)
                where !File.Exists(folder + @"\" + Path.GetFileName(folder) + ".ico")
                select Path.GetFileName(folder));
        }

        return folderNames;
    }

    public static VistaFolderBrowserDialog NewFolderBrowserDialog(string description)
    {
        Logger.Debug("Creating New Folder Browser Dialog");
        var folderBrowser = new VistaFolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true
        };
        return folderBrowser;
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
    public static void AddToPickedListDataTable(DataTable dataTable, string poster, string title, string rating,
        string fullFolderPath, string folderName, string year = "")
    {
        Logger.Debug("Adding Data to PickedListDataTable");
        if (rating == "0")
        {
            rating = "";
        }

        var nRow = dataTable.NewRow();
        nRow["Poster"] = poster;
        nRow["Title"] = title;
        nRow["Year"] = year;
        nRow["Rating"] = rating;
        nRow["Folder"] = fullFolderPath;
        nRow["FolderName"] = folderName;
        dataTable.Rows.Add(nRow);
        Logger.Trace("Data Added to PickedListDataTable: {@Row}",nRow);
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

    /// <summary>
    /// Converts Bitmap to BitmapSource
    /// </summary>
    /// <param name="source">Bitmap object</param>
    /// <returns></returns>
    public static BitmapSource LoadBitmap(Bitmap source)
    {
        Logger.Trace("Converting Bitmap to BitmapSource");
        var ip = source.GetHbitmap();
        BitmapSource bs;

        try
        {
            bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(ip);
            // _ = NativeMethods.DeleteObject(ip);
        }

        Logger.Trace("Bitmap Converted to BitmapSource");
        return bs;
    }
    
    /// <summary>
    /// Async function That can Download image from any URL and save to local path
    /// </summary>
    /// <param name="url"> The URL of Image to Download</param>
    /// <param name="saveFileName">The Local Path Of Downloaded Image</param>
    public static async Task DownloadImageFromUrlAsync(Uri url, string saveFileName)
    {
        const int maxRetry = 2;
    
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, retryCount, context) => 
                {
                    Logger.Warn(exception, $"Failed to download image from URL: {url}. Retrying... Attempt {retryCount}");
                });

        var fallbackPolicy = Policy
            .Handle<HttpRequestException>()
            .FallbackAsync(async ct => 
            { 
                Logger.Error($"All attempts to download image from URL: {url} have failed.");
                throw new HttpRequestException($"Failed to download the image after {maxRetry} attempts");
            });

        await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () => await DownloadAndSaveImageAsync(url, saveFileName));
    }

    private static async Task DownloadAndSaveImageAsync(Uri url, string saveFileName)
    {
        Logger.Info($"Downloading Image from URL: {url}");
        using var response = await Services.HttpC.GetAsync(url);
        await using var fs = new FileStream(saveFileName, FileMode.Create);
        Logger.Info("Saving Image to Path: {Path}", saveFileName);
        await response.Content.CopyToAsync(fs);
    }

    #region IconUtil

    /// <summary>
    /// Creates Icons from PNG
    /// </summary>
    public static int MakeIco(string iconMode, string selectedFolder, DataTable pickedListDataTable,
        bool isRatingVisible = false, bool isMockupVisible = true)
    {
        Logger.Debug(
            "Creating Icons from PNG, Icon Mode: {IconMode}, Selected Folder: {SelectedFolder}, isRatingVisible: {IsRatingVisible}, isMockupVisible: {IsMockupVisible}",
            iconMode, selectedFolder, isRatingVisible, isMockupVisible);
        
        var iconProcessedCount = 0;
        var ratingVisibility = isRatingVisible ? "visible" : "hidden";
        var mockupVisibility = isMockupVisible ? "visible" : "hidden";
        var fNames = new List<string>();
        fNames.AddRange(Directory.GetDirectories(selectedFolder).Select(Path.GetFileName));
        foreach (var i in fNames)
        {
            var tempI = i;
            var targetFile = $@"{selectedFolder}\{i}\{i}.ico";
            var pngFilePath = $@"{selectedFolder}\{i}\{i}.png";
            if (File.Exists(pngFilePath) && !File.Exists(targetFile))
            {
                var rating = pickedListDataTable.AsEnumerable()
                    .Where(p => p["FolderName"].Equals(tempI))
                    .Select(p => p["Rating"].ToString())
                    .FirstOrDefault();
                var mediaTitle = pickedListDataTable.AsEnumerable()
                    .Where(p => p["FolderName"].Equals(tempI))
                    .Select(p => p["Title"].ToString())
                    .FirstOrDefault();
                BuildFolderIco(iconMode, pngFilePath, rating, ratingVisibility,
                    mockupVisibility, mediaTitle);
                iconProcessedCount += 1;
                
                Logger.Info("Icon Created for Folder: {Folder}", i);
                Logger.Debug("Deleting PNG File: {PngFilePath}", pngFilePath);
                
                File.Delete(pngFilePath); //<--IO Exception here
            }

            if (!File.Exists(targetFile)) continue;
            HideFile(targetFile);
            SetFolderIcon($"{i}.ico", $@"{selectedFolder}\{i}");
        }

        ApplyChanges(selectedFolder);
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
        SHChangeNotify(SHCNE.SHCNE_UPDATEDIR, SHCNF.SHCNF_PATHW, folderPath);
    }

    #endregion IconUtil

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

    public static CultureInfo GetCultureInfoByLanguage(Languages language)
    {
        Logger.Debug("Getting CultureInfo by Language: {Language}", language);
        CultureInfo cultureInfo;
        switch (language)
        {
            case Languages.English:
                ConfigHelper.Instance.SetLang("en");
                cultureInfo = new CultureInfo("en-US");
                break;
            case Languages.Spanish:
                ConfigHelper.Instance.SetLang("es");
                cultureInfo = new CultureInfo("es-MX");
                break;
            case Languages.Arabic:
                ConfigHelper.Instance.SetLang("ar");
                cultureInfo = new CultureInfo("ar-SA");
                break;
            case Languages.Russian:
                ConfigHelper.Instance.SetLang("ru");
                cultureInfo = new CultureInfo("ru-RU");
                break;
            case Languages.Hindi:
                ConfigHelper.Instance.SetLang("hi");
                cultureInfo = new CultureInfo("hi-IN");
                break;
            default:
                ConfigHelper.Instance.SetLang("en");
                cultureInfo = new CultureInfo("en-US");
                break;
        }

        return cultureInfo;
    }

    public static void SaveMediaInfo(int id, string mediaType, string folderPath)
    {
        Logger.Debug("Saving Media Info, ID: {Id}, Media Type: {MediaType}, Folder Path: {FolderPath}", id,
            mediaType, folderPath);
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        try
        {
            File.Create(filePath);
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
        var filePath = Path.Combine(folderPath, GlobalVariables.MediaInfoFile);
        var id = File.Exists(filePath) ? InIHelper.ReadValue("ID", null, filePath) : null;
        var mediaType = File.Exists(filePath) ? InIHelper.ReadValue("MediaType", null, filePath) : null;
        if (string.IsNullOrWhiteSpace(id))
        {
            id = null;
            mediaType = null;
        }
        var mediaInfo = (ID: id, MediaType: mediaType);
        Logger.Debug("Media Info Read: {@MediaInfo}", mediaInfo);
        return mediaInfo;
    }

    public static int GetResultCount(bool isPickedById, dynamic result, string searchMode)
    {
        return isPickedById ? result != null ? 1 : 0 : searchMode == "Game" ? result.Length : result.TotalResults;
    }
    public static IDictionary<string, string> GetCmdArgs()
    {
        Logger.Info("Getting Command Line Arguments");
        IDictionary<string, string> arguments = new Dictionary<string, string>();
        var args = Environment.GetCommandLineArgs();

        for (var index = 1; index < args.Length; index += 2)
        {
            var arg = args[index].Replace("--", "");
            arguments.Add(arg, args[index + 1]);
        }
        Logger.Info("Command Line Arguments: {@Arguments}", arguments);
        return arguments;
    }

    public static bool? IfNotAdminRestartAsAdmin()
    {
        if (ApplicationHelper.IsAdministrator())
        {
            Logger.Info("Application is running as Administrator");
            return null;
        }
        if (MessageBox.Show(CustomMessageBox.Ask(LangProvider.GetLang("RestartAsAdmin"),
                LangProvider.GetLang("Error"))) != MessageBoxResult.Yes) return false;
        
        StartAppAsAdmin();
        return true;
    }

    private static void StartAppAsAdmin()
    {
        Logger.Info("Starting Application as Administrator");

        var appPath = Path.ChangeExtension(AppPath, "exe");

        if (string.IsNullOrWhiteSpace(appPath))
        {
            Logger.Error("AppPath: {AppPath} is not valid", appPath);
            return;
        }

        var elevated = new ProcessStartInfo(appPath)
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            using var process = Process.Start(elevated);
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Failed to start process with elevated rights {Message}",
                ex.Message).Exception(ex).Log();
        }

        Environment.Exit(0);
    }

    public static void AddToContextMenu()
    {
        Logger.Info("Modifying Context Menu");
        if (IfNotAdminRestartAsAdmin() == false) return;
        switch (Services.Settings.IsExplorerIntegrated)
        {
            case true when Services.Settings.ContextEntryName == LangProvider.GetLang("CreateIconsWithFoliCon"):
                return;
            case true when Services.Settings.ContextEntryName != LangProvider.GetLang("CreateIconsWithFoliCon"):
            {
                Logger.Info("Removing Old Context Menu Entry");
                RemoveFromContextMenu();
                break;
            }
        }
        Services.Settings.ContextEntryName = LangProvider.GetLang("CreateIconsWithFoliCon");
        Services.Settings.IsExplorerIntegrated = true;
        Services.Settings.Save();
        var commandS = $"""
                        "{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode Professional
                        """;
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Professional"), commandS);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Professional"), commandS.Replace("%1", "%V"));


        commandS = $"""
                    "{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode Movie
                    """;
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Movie"), commandS);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Movie"), commandS.Replace("%1", "%V"));

        commandS = $"""
                    "{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode TV
                    """;
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("TV"), commandS);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("TV"), commandS.Replace("%1", "%V"));

        commandS = $"""
                    "{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode Game
                    """;
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Game"), commandS);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Game"), commandS.Replace("%1", "%V"));

        commandS = $"""
                    "{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode "Auto (Movies & TV Shows)"
                    """;
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Auto"), commandS);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang("Auto"), commandS.Replace("%1", "%V"));
        Logger.Info("Context Menu Modified");
        //Growl.SuccessGlobal("Merge Subtitle option added to context menu!");
    }

    public static void RemoveFromContextMenu()
    {
        if (IfNotAdminRestartAsAdmin() == false) return;
        Services.Settings.IsExplorerIntegrated = false;
        Services.Settings.Save();
        ApplicationHelper.UnRegisterCascadeContextMenuFromDirectory(Services.Settings.ContextEntryName, "");
        ApplicationHelper.UnRegisterCascadeContextMenuFromBackground(Services.Settings.ContextEntryName, "");

        //Growl.InfoGlobal("Merge Subtitle option removed from context menu!");
    }

    public static LoggingConfiguration GetNLogConfig()
    {
        Logger.Info("Getting NLog Configuration");
        LogManager.Setup().LoadConfigurationFromFile();
        var config = new LoggingConfiguration();
        
        var logPath = LogManager.Configuration.Variables["logDirectory"].Render(LogEventInfo.CreateNullEvent());
        var fileLogLevel = LogLevel.FromString(LogManager.Configuration.Variables["fileLogLevel"].Render(LogEventInfo.CreateNullEvent()));
        
        var fileTarget = new FileTarget
        {
            Name = "fileTarget",
            FileName = logPath,
            Layout = "[${date:format=yyyy-MM-dd HH\\:mm\\:ss.ffff}]-[v${assembly-version:format=Major.Minor.Build}]-${callsite}:${callsite-linenumber}-|${uppercase:${level}}: ${message} ${exception:format=tostring}"
        };

        var consoleTarget = new ColoredConsoleTarget
        {
            Name = "consoleTarget",
            UseDefaultRowHighlightingRules = true,
            EnableAnsiOutput = true,
            Layout = "[${date:format=yyyy-MM-dd HH\\:mm\\:ss.ffff}]-[v${assembly-version:format=Major.Minor.Build}]-${callsite}:${callsite-linenumber}-|${uppercase:${level}}: ${message} ${exception:format=tostring}"
        };

        config.AddRule(fileLogLevel, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        Logger.Info("NLog configuration loaded");
        return config;
    }

    internal static SentryTarget GetSentryTarget()
    {
        var sentryTarget = new SentryTarget
        {
            Name = "sentry",
            Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN"),
            Layout = "${message} ${exception:format=tostring}",
            BreadcrumbLayout = "${message}",
            Environment = "Development",
            MinimumBreadcrumbLevel = LogLevel.Debug.ToString()!,
            MinimumEventLevel = LogLevel.Error.ToString()!,
            Options =
            {
                SendDefaultPii = true, ShutdownTimeoutSeconds = 5, Debug = false, IsGlobalModeEnabled = true,
                AutoSessionTracking = true,
                User = new SentryNLogUser { Username = Environment.MachineName }
            },
            IncludeEventDataOnBreadcrumbs = true,
        };
        sentryTarget.Options.AddExceptionFilterForType<UnauthorizedAccessException>();
        sentryTarget.Tags.Add(new TargetPropertyWithContext("exception", "${exception:format=shorttype}"));
        return sentryTarget;
    }

    public static DirectoryPermissionsResult CheckDirectoryPermissions(string dirPath)
    {
        var result = new DirectoryPermissionsResult
        {
            CanRead = false,
            CanWrite = false
        };

        if (!Directory.Exists(dirPath)) return result;
        try
        {
            // Attempt to open.txt with read permissions
            using var stream = File.OpenRead(Path.Combine(dirPath, "test.txt"));
            result.CanRead = true;
        }
        catch
        {
            // Ignore any exception, it means we don't have read permissions
        }

        try
        {
            // Attempt to open a new file with write permissions
            using (File.Create(Path.Combine(dirPath, "test.tmp")))
            {
                result.CanWrite = true;
            }

            // Successfully created file, try to delete it
            File.Delete(Path.Combine(dirPath, "test.tmp"));
        }
        catch
        {
            // Ignore any exception, it means we don't have write permissions
        }
        Logger.Debug("Path: {Path};Directory Permissions Checked: {@Result}",dirPath, result);
        return result;
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
    private static string AppPath { get; } = ApplicationHelper.GetExecutablePathNative();
}