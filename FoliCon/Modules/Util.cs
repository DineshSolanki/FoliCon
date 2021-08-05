using FoliCon.Models;
using FoliCon.Views;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
using IGDB.Models;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FoliCon.Properties.Langs;
using HandyControl.Tools.Extension;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;
using Collection = TMDbLib.Objects.Collections.Collection;
using MessageBox = HandyControl.Controls.MessageBox;
using PosterIcon = FoliCon.Models.PosterIcon;

namespace FoliCon.Modules
{
    internal static class Util
    {
        public static async void CheckForUpdate(bool onlyShowIfUpdateAvailable = false)
        {
            if (ApplicationHelper.IsConnectedToInternet())
            {
                var ver = await UpdateHelper.CheckUpdateAsync("DineshSolanki", "FoliCon");
                if (ver.IsExistNewVersion)
                {
                    var info = new GrowlInfo
                    {

                        Message = LangProvider.GetLang("NewVersionFound").Format(ver.TagName, ver.Changelog.Replace("\\n", Environment.NewLine)),
                        ConfirmStr = LangProvider.GetLang("UpdateNow"),
                        CancelStr = LangProvider.GetLang("Ignore"),
                        ShowDateTime = false,
                        ActionBeforeClose = isConfirmed =>
                        {
                            if (isConfirmed)
                                StartProcess(ver.ReleaseUrl);
                            return true;
                        }
                    };
                    Growl.AskGlobal(info);
                }
                else
                {
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
            else Growl.ErrorGlobal(new GrowlInfo { Message = LangProvider.GetLang("NetworkNotAvailable"), ShowDateTime = false });
        }

        /// <summary>
        /// Starts Process associated with given path.
        /// </summary>
        /// <param name="path">if path is a URL it opens url in default browser, if path is File Or folder path it will be started.</param>
        public static void StartProcess(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public static bool IsPngOrIco(string f) =>
            f != null && (
                f.EndsWith(".png", StringComparison.Ordinal) ||
                f.EndsWith(".ico", StringComparison.Ordinal));

        public static byte[] ImageSourceToBytes(BitmapEncoder encoder, ImageSource imageSource)
        {
            if (imageSource is not BitmapSource bitmapSource) return null;
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            var bytes = stream.ToArray();
            return bytes;
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

        /// <summary>
        /// Terminates Explorer.exe Process.
        /// </summary>
        public static void KillExplorer()
        {
            var taskKill = new ProcessStartInfo("taskkill", "/F /IM explorer.exe")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            using var process = new Process { StartInfo = taskKill };
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Terminates and Restart Explorer.exe process.
        /// </summary>
        public static void RestartExplorer()
        {
            KillExplorer();
            Process.Start("explorer.exe");
        }

        public static void RefreshIconCache()
        {
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
            Kernel32.Wow64EnableWow64FsRedirection(true);
        }

        /// <summary>
        /// Deletes Icons (.ico and Desktop.ini files) from all subfolders of given path.
        /// </summary>
        /// <param name="folderPath">Path to delete Icons from</param>
        public static void DeleteIconsFromSubfolders(string folderPath)
        {
            foreach (var folder in Directory.EnumerateDirectories(folderPath))
            {
                DeleteIconsFromFolder(folder);
            }
            RefreshIconCache();
            SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST | SHCNF.SHCNF_FLUSHNOWAIT, folderPath);
        }

        public static void DeleteIconsFromFolder(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            var icoFile = Path.Combine(folderPath, $"{folderName}.ico");
            var iniFile = Path.Combine(folderPath, "desktop.ini");
            File.Delete(icoFile);
            File.Delete(iniFile);
        }
        /// <summary>
        /// Checks if Web is accessible from This System
        /// </summary>
        /// <returns> Returns true if Web is accessible</returns>
        public static bool IsNetworkAvailable()
        {
            const string host = "8.8.8.8";
            var result = false;
            using var p = new Ping();
            try
            {
                var reply = p.Send(host, 5000, new byte[32], new PingOptions { DontFragment = true, Ttl = 32 });
                if (reply is { Status: IPStatus.Success })
                    result = true;
            }
            catch (Exception)
            {
                // ignored
            }

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
            string fullFolderPath, string folderName, string year = "", int id = 0)
        {
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
        }

        public static ObservableCollection<ListItem> FetchAndAddDetailsToListView(ResultResponse result, string query, bool isPickedById)
        {
            var source = new ObservableCollection<ListItem>();

            if (result.MediaType == MediaTypes.Tv)
            {
                dynamic ob = isPickedById ? (TvShow) result.Result : (SearchContainer<SearchTv>)result.Result;
                source = Tmdb.ExtractTvDetailsIntoListItem(ob);
            }
            else if (result.MediaType == MediaTypes.Movie || result.MediaType == MediaTypes.Collection)
            {
                if (query.ToLower(CultureInfo.InvariantCulture).Contains("collection"))
                {
                    dynamic ob = isPickedById ? (Collection) result.Result : (SearchContainer<SearchCollection>)result.Result;
                    source = Tmdb.ExtractCollectionDetailsIntoListItem(ob);
                }
                else
                {
                    dynamic ob;
                    try
                    {
                        ob = isPickedById ? (Movie) result.Result : (SearchContainer<SearchMovie>)result.Result;
                        source = Tmdb.ExtractMoviesDetailsIntoListItem(ob);
                    }
                    catch (Exception)
                    {
                        ob = isPickedById ? (Collection) result.Result : (SearchContainer<SearchCollection>)result.Result;
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

            return source;
        }

        /// <summary>
        /// Get List of file in given folder.
        /// </summary>
        /// <param name="folder">Folder to Get File from.</param>
        /// <returns>ArrayList with file Names.</returns>
        public static List<string> GetFileNamesFromFolder(string folder)
        {
            var itemList = new List<string>();
            try
            {
                if (string.IsNullOrEmpty(folder)) return itemList;
                itemList.AddRange(Directory.GetFiles(folder).Select(Path.GetFileName));
            }
            catch (Exception e)
            {
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

            return bs;
        }

        /// <summary>
        /// Get Bitmap from URL
        /// </summary>
        /// <param name="url">Url of image</param>
        /// <returns>Bitmap object</returns>
        public static async Task<Bitmap> GetBitmapFromUrlAsync(string url)
        {
            var myRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));
            myRequest.Method = "GET";
            var myResponse = (HttpWebResponse)await myRequest.GetResponseAsync().ConfigureAwait(false);
            var bmp = new Bitmap(myResponse.GetResponseStream());
            myResponse.Close();
            return bmp;
        }

        /// <summary>
        /// Async function That can Download image from any URL and save to local path
        /// </summary>
        /// <param name="url"> The URL of Image to Download</param>
        /// <param name="saveFileName">The Local Path Of Downloaded Image</param>
        public static async Task DownloadImageFromUrlAsync(Uri url, string saveFileName)
        {
            var response = await Services.HttpC.GetAsync(url);
            await using var fs = new FileStream(saveFileName, FileMode.Create);
            await response.Content.CopyToAsync(fs);
        }

        #region IconUtil

        /// <summary>
        /// Creates Icons from PNG
        /// </summary>
        public static int MakeIco(string iconMode, string selectedFolder, DataTable pickedListDataTable,
            bool isRatingVisible = false, bool isMockupVisible = true)
        {
            var iconProcessedCount = 0;
            var ratingVisibility = isRatingVisible ? "visible" : "hidden";
            var mockupVisibility = isMockupVisible ? "visible" : "hidden";
            var fNames = new List<string>();
            fNames.AddRange(Directory.GetDirectories(selectedFolder).Select(Path.GetFileName));
            foreach (var i in fNames)
            {
                var tempI = i;
                var targetFile = $@"{selectedFolder}\{i}\{i}.ico";
                if (File.Exists($@"{selectedFolder}\{i}\{i}.png") && !File.Exists(targetFile))
                {
                    var rating = pickedListDataTable.AsEnumerable()
                        .Where(p => p["FolderName"].Equals(tempI))
                        .Select(p => p["Rating"].ToString())
                        .FirstOrDefault();

                    BuildFolderIco(iconMode, $@"{selectedFolder}\{i}\{i}.png", rating, ratingVisibility,
                        mockupVisibility);
                    iconProcessedCount += 1;
                    File.Delete($@"{selectedFolder}\{i}\{i}.png"); //<--IO Exception here
                }

                if (!File.Exists(targetFile)) continue;
                HideIcons(targetFile);
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
        public static void BuildFolderIco(string iconMode, string filmFolderPath, string rating,
            string ratingVisibility, string mockupVisibility)
        {
            if (!File.Exists(filmFolderPath))
            {
                return;
            }

            ratingVisibility = string.IsNullOrEmpty(rating) ? "Hidden" : ratingVisibility;
            if (!string.IsNullOrEmpty(rating) && rating != "10")
            {
                rating = !rating.Contains(".") ? rating + ".0" : rating;
            }

            Bitmap icon;
            if (iconMode == "Professional")
            {
                icon = new ProIcon(filmFolderPath).RenderToBitmap();
            }
            else
            {
                using var task = GlobalVariables.IconOverlayType switch
                {
                    IconOverlay.Legacy => StaTask.Start(() =>
                        new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                            .RenderToBitmap()),
                    IconOverlay.Alternate => StaTask.Start(() =>
                        new PosterIconAlt(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                            .RenderToBitmap()),
                    _ => StaTask.Start(() =>
                        new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility))
                            .RenderToBitmap())
                };
                task.Wait();
                icon = task.Result;
            }

            PngToIcoService.Convert(icon, filmFolderPath.Replace("png", "ico"));
            icon.Dispose();
        }

        public static void HideIcons(string icoFile)
        {
            // Set icon file attribute to "Hidden"
            if ((File.GetAttributes(icoFile) & FileAttributes.Hidden) != FileAttributes.Hidden)
            {
                File.SetAttributes(icoFile, File.GetAttributes(icoFile) | FileAttributes.Hidden);
            }

            // Set icon file attribute to "System"
            if ((File.GetAttributes(icoFile) & FileAttributes.System) != FileAttributes.System)
            {
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
            }
            catch (Exception e)
            {
                MessageBox.Error(e.Message);
            }

            ApplyChanges(folderPath);
        }

        public static void ApplyChanges(string folderPath)
        {
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
            var settings = GlobalDataHelper.Load<AppConfig>();
            settings.TmdbKey = tmdbkey;
            settings.IgdbClientId = igdbClientId;
            settings.IgdbClientSecret = igdbClientSecret;
            settings.DevClientId = dartId;
            settings.DevClientSecret = dartClientSecret;
            settings.Save();
        }

        public static CultureInfo GetCultureInfoByLanguage(Languages language)
        {
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
            var filePath = $@"{folderPath}\folicon.folicon";
            InIHelper.AddValue("ID", id.ToString(),null,filePath);
            InIHelper.AddValue("MediaType", mediaType,null,filePath);
        }

        public static (string ID, string MediaType) ReadMediaInfo(string folderPath)
        {
            var filePath = $@"{folderPath}\folicon.folicon";
            var id = File.Exists(filePath) ? InIHelper.ReadValue("ID", null, filePath) : null;
            var mediaType = File.Exists(filePath) ? InIHelper.ReadValue("MediaType", null, filePath) : null;
            var mediaInfo = (ID:id, MediaType:mediaType);
            return mediaInfo;
        }
    }
}