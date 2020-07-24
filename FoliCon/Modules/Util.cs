using FoliCon.Models;
using HandyControl.Controls;
using HandyControl.Data;
using IGDB.Models;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Shell32;

namespace FoliCon.Modules
{
    internal static class Util
    {
        public static void CheckForUpdate()
        {
            if (IsNetworkAvailable())
            {
                var ver = UpdateHelper.CheckForUpdate("https://raw.githubusercontent.com/DineshSolanki/FoliCon/master/FoliCon/Updater.xml");
                if (ver.IsExistNewVersion)
                {
                    GrowlInfo info = new GrowlInfo()
                    {
                        Message = $"New Version Found!\n Changelog:{ver.Changelog}",
                        ConfirmStr = "Update Now",
                        CancelStr = "Ignore",
                        ShowDateTime = false,
                        ActionBeforeClose = isConfirmed =>
                        {
                            if (isConfirmed)
                                StartProcess(ver.Url);
                            return true;
                        },
                    };
                    Growl.AskGlobal(info);
                }
                else
                {
                    GrowlInfo info = new GrowlInfo()
                    {
                        Message = "Great! you are using the latest version",
                        ShowDateTime = false,
                        StaysOpen = false
                    };
                    Growl.InfoGlobal(info);
                }
            }
            else Growl.ErrorGlobal(new GrowlInfo() { Message = "Network not available!", ShowDateTime = false });

        }

        /// <summary>
        /// Starts Process associated with given path.
        /// </summary>
        /// <param name="processName">if path is a URL it opens url in default browser, if path is File Or folder path it will be started.</param>
        public static void StartProcess(string path)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        public static byte[] ImageSourceToBytes(BitmapEncoder encoder, ImageSource imageSource)
        {
            byte[] bytes = null;

            if (imageSource is BitmapSource bitmapSource)
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    bytes = stream.ToArray();
                }
            }

            return bytes;
        }

        /// <summary>
        /// Set Column width of list view to fit content
        /// </summary>
        /// <param name="listView"> list view to change width</param>
        public static void SetColumnWidth(ListView listView)
        {
            if (listView.View is GridView gridView)
            {
                foreach (var column in gridView.Columns)
                {
                    if (double.IsNaN(column.Width))
                    {
                        column.Width = column.ActualWidth;
                    }
                    column.Width = double.NaN;
                }
            }
        }

        /// <summary>
        /// Terminates Explorer.exe Process.
        /// </summary>
        public static void KillExplorer()
        {
            ProcessStartInfo taskKill = new ProcessStartInfo("taskkill", "/F /IM explorer.exe")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            using (Process process = new Process { StartInfo = taskKill })
            {
                process.Start();
                process.WaitForExit();
            }
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
            _ = Vanara.PInvoke.Kernel32.Wow64DisableWow64FsRedirection(out _);
            Process objProcess = new Process();
            objProcess.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.System) +
                                            "\\ie4uinit.exe";
            objProcess.StartInfo.Arguments = "-ClearIconCache";
            objProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            objProcess.Start();
            objProcess.WaitForExit();
            objProcess.Close();
            RestartExplorer();
        }

        /// <summary>
        /// Deletes Icons (.ico and Desktop.ini files) from all subfolders of given path.
        /// </summary>
        /// <param name="folderPath">Path to delete Icons from</param>
        public static void DeleteIconsFromPath(string folderPath)
        {
            foreach (var (icoFile, iniFile) in from string folder in Directory.EnumerateDirectories(folderPath)
                                               let folderName = Path.GetFileNameWithoutExtension(folder)
                                               let icoFile = Path.Combine(folder, folderName + ".ico")
                                               let iniFile = Path.Combine(folder, folderName + "desktop.ini")
                                               select (icoFile, iniFile))
            {
                File.Delete(icoFile);
                File.Delete(iniFile);
            }

            SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Checks if Web is accessible from This System
        /// </summary>
        /// <returns> Returns true if Web is accessible</returns>
        public static bool IsNetworkAvailable()
        {
            string host = "www.google.com";
            bool result = false;
            using (Ping p = new Ping())
            {
                try
                {
                    PingReply reply = p.Send(host, 3000);
                    if (reply.Status == IPStatus.Success)
                        return true;
                }
                catch {}
            }
            return result;
        }

        public static List<string> GetFolderNames(string folderPath)
        {
            List<string> FolderNames = new List<string>();
            if (!string.IsNullOrEmpty(folderPath))
            {
                foreach (string folder in Directory.GetDirectories(folderPath))
                {
                    if (!File.Exists(folder + @"\" + Path.GetFileName(folder) + ".ico"))
                    {
                        FolderNames.Add(Path.GetFileName(folder));
                    }
                }
            }
            return FolderNames;
        }

        public static VistaFolderBrowserDialog NewFolderBrowserDialog(string description)
        {
            var folderBrowser = new VistaFolderBrowserDialog()
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
        /// <param name="folder">Media Full Folder Path</param>
        /// <param name="folderName">Short Folder Name</param>
        /// <param name="year">Media Year</param>
        public static void AddToPickedListDataTable(DataTable dataTable, string poster, string title, string rating, string fullFolderPath, string folderName, string year = "")
        {
            if (rating == "0")
            {
                rating = "";
            }
            DataRow nRow = dataTable.NewRow();
            nRow["Poster"] = poster;
            nRow["Title"] = title;
            nRow["Year"] = year;
            nRow["Rating"] = rating;
            nRow["Folder"] = fullFolderPath;
            nRow["FolderName"] = folderName;
            dataTable.Rows.Add(nRow);
        }

        public static ObservableCollection<ListItem> FetchAndAddDetailsToListView(ResultResponse result, string query)
        {
            System.Diagnostics.Contracts.Contract.Requires(result != null);
            ObservableCollection<ListItem> source = new ObservableCollection<ListItem>();

            if (result.MediaType == MediaTypes.TV)
            {
                SearchContainer<SearchTv> ob = (SearchContainer<SearchTv>)result.Result;
                source = TMDB.ExtractTvDetailsIntoListItem(ob);
            }
            else if (result.MediaType == MediaTypes.Movie)
            {
                if (query.ToLower().Contains("collection"))
                {
                    SearchContainer<SearchCollection> ob = (SearchContainer<SearchCollection>)result.Result;
                    source = TMDB.ExtractCollectionDetailsIntoListItem(ob);
                }
                else
                {
                    dynamic ob;
                    try
                    {
                        ob = (SearchContainer<SearchMovie>)result.Result;
                        source = TMDB.ExtractMoviesDetailsIntoListItem(ob);
                    }
                    catch (Exception)
                    {
                        ob = (SearchContainer<SearchCollection>)result.Result;
                        source = TMDB.ExtractCollectionDetailsIntoListItem(ob);
                    }
                }
            }
            else if (result.MediaType == MediaTypes.MTV)
            {
                SearchContainer<SearchBase> ob = (SearchContainer<SearchBase>)result.Result;
                source = TMDB.ExtractResourceDetailsIntoListItem(ob);
            }
            else if (result.MediaType == MediaTypes.Game)
            {
                var ob = (Game[])result.Result;
                source = IGDBClass.ExtractGameDetailsIntoListItem(ob);
            }

            return source;
        }

        /// <summary>
        /// Get List of file in given folder.
        /// </summary>
        /// <param name="folder">Folder to Get File from.</param>
        /// <returns>ArrayList with file Names.</returns>
        public static ArrayList GetFileNamesFromFolder(string folder)
        {
            ArrayList itemList = new ArrayList();
            if (!string.IsNullOrEmpty(folder))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    itemList.Add(Path.GetFileName(file));
                }
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
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;

            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
            var myResponse = (HttpWebResponse)(await myRequest.GetResponseAsync().ConfigureAwait(false));
            Bitmap bmp = new Bitmap(myResponse.GetResponseStream());
            myResponse.Close();
            return bmp;
        }

        /// <summary>
		/// Async function That can Download image from any URL and save to local path
		/// </summary>
		/// <param name="url"> The URL of Image to Download</param>
		/// <param name="saveFilename">The Local Path Of Downloaded Image</param>
		public static async Task DownloadImageFromUrlAsync(Uri url, string saveFileName)
        {
            var response = await Services.HttpC.GetAsync(url);
            using (FileStream fs = new FileStream(saveFileName, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        #region IconUtil

        /// <summary>
        /// Creates Icons from PNG
        /// </summary>
        public static int MakeIco(string IconMode, string selectedFolder, DataTable PickedListDataTable, bool isRatingVisible = false, bool isMockupVisible = true)
        {
            int IconProcessedCount = 0;
            string ratingVisibility = isRatingVisible ? "visible" : "hidden";
            string mockupVisibility = isMockupVisible ? "visible" : "hidden";
            List<string> fNames = new List<string>();
            fNames.AddRange(Directory.GetDirectories(selectedFolder).Select(folder => Path.GetFileName(folder)));
            foreach (string i in fNames)
            {
                var tempI = i;
                var targetFile = selectedFolder + "\\" + i + "\\" + i + ".ico";
                if (File.Exists(selectedFolder + "\\" + i + "\\" + i + ".png") && !File.Exists(targetFile))
                {
                    string rating = PickedListDataTable.AsEnumerable()
                                                       .Where((p) => p["FolderName"].Equals(tempI))
                                                       .Select((p) => p["Rating"].ToString())
                                                       .FirstOrDefault();

                    BuildFolderIco(IconMode, selectedFolder + "\\" + i + "\\" + i + ".png", rating, ratingVisibility, mockupVisibility);
                    IconProcessedCount += 1;
                    File.Delete(selectedFolder + "\\" + i + "\\" + i + ".png"); //<--IO Exception here
                }

                if (File.Exists(targetFile))
                {
                    HideIcons(targetFile);
                    SetFolderIcon(i + ".ico", selectedFolder + "\\" + i);
                }
            }
            SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            return IconProcessedCount;
        }

        /// <summary>
		/// Converts From PNG to ICO
		/// </summary>
		/// <param name="filmFolderPath"> Path where to save and where PNG is Downloaded</param>
		/// <param name="rating"> if Wants to Include rating on Icon</param>
		/// <param name="ratingVisibility">Show rating or NOT</param>
		public static void BuildFolderIco(string IconMode, string filmFolderPath, string rating, string ratingVisibility, string mockupVisibility)
        {
            if (!File.Exists(filmFolderPath))
            {
                return;
            }
            ratingVisibility = string.IsNullOrEmpty(rating) ? "Hidden" : ratingVisibility;
            if (!string.IsNullOrEmpty(rating) && !(rating == "10"))
            {
                rating = (!rating.Contains(".")) ? rating + ".0" : rating;
            }
            Bitmap icon = null;
            if (IconMode == "Professional")
            {
                icon = (new ProIcon(filmFolderPath)).RenderToBitmap();
            }
            else
            {
                using (Task<Bitmap> task = StaTask.Start(() => new Views.PosterIcon(new PosterIcon(filmFolderPath, rating, ratingVisibility, mockupVisibility)).RenderToBitmap()))
                {
                    task.Wait();
                    icon = task.Result;
                }
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
		/// <param name="FolderPath">path to the folder</param>
		private static void SetFolderIcon(string icoFile, string FolderPath)
        {
            try
            {
                SHFOLDERCUSTOMSETTINGS FolderSettings = new SHFOLDERCUSTOMSETTINGS
                {
                    dwMask = FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
                    pszIconFile = icoFile
                };
                //FolderSettings.iIconIndex = 0;
                string pszPath = FolderPath;
                Vanara.PInvoke.HRESULT HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, pszPath, FCS.FCS_FORCEWRITE | FCS.FCS_READ);
            }
            catch (Exception)
            {
                // log exception
            }
            ApplyChanges(FolderPath);
        }

        public static void ApplyChanges(string folderPath)
        {
            PIDL pidl = ILCreateFromPath(folderPath);
            SHChangeNotify(SHCNE.SHCNE_UPDATEDIR, SHCNF.SHCNF_FLUSHNOWAIT, pidl.DangerousGetHandle());
        }

        #endregion IconUtil

        public static void ReadApiConfiguration(out string tmdbkey, out string igdbkey, out string dartClientSecret, out string dartID)
        {
            tmdbkey = GlobalDataHelper<AppConfig>.Config.TMDBKey;
            igdbkey = GlobalDataHelper<AppConfig>.Config.IGDBKey;
            dartClientSecret = GlobalDataHelper<AppConfig>.Config.DevClientSecret;
            dartID = GlobalDataHelper<AppConfig>.Config.DevClientID;
        }

        public static void WriteApiConfiguration(string tmdbkey, string igdbkey, string dartClientSecret, string dartID)
        {
            GlobalDataHelper<AppConfig>.Config.TMDBKey = tmdbkey;
            GlobalDataHelper<AppConfig>.Config.IGDBKey = igdbkey;
            GlobalDataHelper<AppConfig>.Config.DevClientID = dartID;
            GlobalDataHelper<AppConfig>.Config.DevClientSecret = dartClientSecret;
            GlobalDataHelper<AppConfig>.Save();
        }
    }
}