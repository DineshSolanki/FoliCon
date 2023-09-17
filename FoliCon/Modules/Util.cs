using FoliCon.Modules.utils;
using NLog;
using Polly;
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

        if (!ApplicationHelper.IsConnectedToInternet())
        {
            ShowGrowlError(LangProvider.GetLang("NetworkNotAvailable"));
            return;
        }

        var ver = await UpdateHelper.CheckUpdateAsync("DineshSolanki", "FoliCon");

        if (!ver.IsExistNewVersion)
        {
            Logger.Debug("No New Version Found");

            if (!onlyShowIfUpdateAvailable)
            {
                ShowGrowlInfo(LangProvider.GetLang("ThisIsLatestVersion"));
            }

            return;
        }

        Logger.Debug("New Version Found: {}", ver.TagName);
        ConfirmUpdate(ver);
    }

    private static void ShowGrowlError(string message)
    {
        Growl.ErrorGlobal(new GrowlInfo { Message = message, ShowDateTime = false });
    }

    private static void ShowGrowlInfo(string message)
    {
        Growl.InfoGlobal(new GrowlInfo { Message = message, ShowDateTime = false, StaysOpen = false });
    }

    private static void ConfirmUpdate(ReleaseInfo ver)
    {
        var message = LangProvider.GetLang("NewVersionFound")
            .Format(ver.TagName, ver.Changelog.Replace("\\n", Environment.NewLine));
        var confirmMessage = LangProvider.GetLang("UpdateNow");
        var cancelMessage = LangProvider.GetLang("Ignore");

        var info = new GrowlInfo
        {
            Message = message,
            ConfirmStr = confirmMessage,
            CancelStr = cancelMessage,
            ShowDateTime = false,
            ActionBeforeClose = isConfirmed =>
            {
                if (!isConfirmed) return true;
                Logger.Debug("Update Confirmed. Starting Update Process");
                ProcessUtils.StartProcess(ver.ReleaseUrl);

                return true;
            }
        };

        Growl.AskGlobal(info);
    }


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

    //Handle UnauthorizedAccessException
    public static void HandleUnauthorizedAccessException(UnauthorizedAccessException ex, string path)
    {
        MessageBox.Show(CustomMessageBox.Error(
            ex.Message.Contains("The process cannot access the file")
                ? LangProvider.GetLang("FileIsInUse")
                : LangProvider.GetLang("FailedFileAccessAt").Format(path), LangProvider.GetLang("ExceptionOccurred")));
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
            FileUtils.HideFile(targetFile);
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

        var languageCodes = new Dictionary<Languages, string>
        {
            { Languages.English, "en-US" },
            { Languages.Spanish, "es-MX" },
            { Languages.Arabic, "ar-SA" },
            { Languages.Russian, "ru-RU" },
            { Languages.Hindi, "hi-IN" }
        };

        var langCode = languageCodes.GetValueOrDefault(language, "en-US");
        ConfigHelper.Instance.SetLang(langCode.Split("-")[0]);

        return new CultureInfo(langCode);
    }

    public static int GetResultCount(bool isPickedById, dynamic result, string searchMode)
    {
        return isPickedById ? result != null ? 1 : 0 : searchMode == "Game" ? result.Length : result.TotalResults;
    }
}