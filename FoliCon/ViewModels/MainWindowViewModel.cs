using FoliCon.Models;
using FoliCon.Modules;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Net.NetworkInformation;

namespace FoliCon.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Variables

        private string _selectedFolder;
        private string _title = "Folicon MVVM";
        private bool _isRatingVisible = true;
        private bool _isPosterMockupUsed = true;
        private bool _isBusy = false;
        private bool _isMakeEnabled = false;
        private bool _isSkipAmbigous = false;
        private string _iconMode = "Poster";
        private string _searchMode = "Movie";
        private bool _isSearchModeVisible = true;
        private bool _stopIconDownload = false;
        public TMDbLib.Client.TMDbClient tMDbClient;
        public IGDB.IGDBApi igdbClient;
        public IGDBClass IGDBObject;
        public TMDB TMDBObject;
        public DArt DArtObject;

        public string TMDBAPIKey = GlobalDataHelper<AppConfig>.Config.TMDBKey;
        public string IGDBKey = GlobalDataHelper<AppConfig>.Config.IGDBKey;
        public string DevClientSecret = GlobalDataHelper<AppConfig>.Config.DevClientSecret;
        public string DevClientID = GlobalDataHelper<AppConfig>.Config.DevClientID;
        private ListViewData _finalListViewData;
        public List<ImageToDownload> ImgDownloadList;
        public DataTable PickedListDataTable;
        public bool IsObjectsInitlized { get; set; } = false;
        public bool IsSearchModeVisible { get => _isSearchModeVisible; set => SetProperty(ref _isSearchModeVisible, value); }
        public ListViewData FinalListViewData { get => _finalListViewData; set => SetProperty(ref _finalListViewData, value); }
        private readonly IDialogService _dialogService;
        public StatusBarData StatusBarProperties { get; set; }
        public ProgressBarData BusyIndicatorProperties { get; set; }
        public string Text { get; set; }
        public bool IgnoreAmbigousTitle { get; set; }
        public List<string> Fnames { get; set; }
        public bool IsMakeEnabled { get => _isMakeEnabled; set => SetProperty(ref _isMakeEnabled, value); }
        public bool IsSkipAmbigous { get => _isSkipAmbigous; set => SetProperty(ref _isSkipAmbigous, value); }
        public bool StopIconDownload { get => _stopIconDownload; set => SetProperty(ref _stopIconDownload, value); }

        public string IconMode
        {
            get
            {
                return _iconMode;
            }
            set
            {
                SetProperty(ref _iconMode, value);
                if (value == "Professional")
                    IsSearchModeVisible = false;
                else
                    IsSearchModeVisible = true;
            }
        }

        public string SearchMode { get => _searchMode; set => SetProperty(ref _searchMode, value); }

        #endregion Variables

        #region GetterSetters

        public string SelectedFolder
        {
            get { return _selectedFolder; }
            set
            {
                SetProperty(ref _selectedFolder, value);
            }
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public bool IsRatingVisible
        {
            get { return _isRatingVisible; }
            set { SetProperty(ref _isRatingVisible, value); }
        }

        public bool IsPosterMockupUsed
        {
            get { return _isPosterMockupUsed; }
            set { SetProperty(ref _isPosterMockupUsed, value); }
        }

        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        #endregion GetterSetters

        #region DelegateCommands

        #region MenuItem Commands

        #region SettingMenu

        public DelegateCommand ShowRatingBadgeCommand { get; private set; }
        public DelegateCommand UsePosterOverlatCommand { get; private set; }
        public DelegateCommand IgnoreAmbigousCommand { get; private set; }
        public DelegateCommand APIConfigCommand { get; private set; }

        #endregion SettingMenu

        public DelegateCommand RestartExCommand { get; private set; } = new DelegateCommand(Util.RefreshIconCache);
        public DelegateCommand DeleteIconsCommand { get; private set; }
        public DelegateCommand ApiConfigCommand { get; private set; }

        public DelegateCommand HelpCommand { get; private set; } = new DelegateCommand(delegate
        {
            Util.StartProcess("https://github.com/DineshSolanki/FoliCon");
        });

        public DelegateCommand AboutCommand { get; private set; }
        public DelegateCommand UpdateCommand { get; private set; } = new DelegateCommand(delegate { Util.CheckForUpdate(); });

        #endregion MenuItem Commands

        public DelegateCommand LoadCommand { get; private set; }
        public DelegateCommand SearchAndMakeCommand { get; private set; }
        public DelegateCommand<object> IconModeChangedCommand { get; private set; }
        public DelegateCommand<object> SearchModeChangedCommand { get; private set; }
        public DelegateCommand ListViewDoubleClickCommand { get; private set; }
        public DelegateCommand SelectedFolderDClickCommand { get; private set; }
        public DelegateCommand DownloadCancelCommand { get; private set; }

        #endregion DelegateCommands

        public MainWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            InitilizeProperties();
            InitilizeDelegates();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            
            Services.Tracker.Configure<MainWindowViewModel>()
                .Property(p => p.IsRatingVisible, true)
                .Property(p => p.IsPosterMockupUsed, true)
                .PersistOn(nameof(PropertyChanged));
            Services.Tracker.Track(this);
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            StatusBarProperties.NetIcon = Util.IsNetworkAvailable() ? @"\Resources\Strong-WiFi.png" : @"\Resources\No-WiFi.png";
        }

        private void LoadMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Folder");
            if ((bool)folderBrowserDialog.ShowDialog())
            {
                SelectedFolder = folderBrowserDialog.SelectedPath;
                StatusBarProperties.ResetData();
                IsMakeEnabled = true;
            }
        }

        private async void SearchAndMakeMethod()
        {
            if (Directory.Exists(SelectedFolder))
            {
                Fnames = Util.GetFolderNames(SelectedFolder);
            }
            else
            {
                MessageBox.Error("Folder does not exist!", "Invalid Path");
                return;
            }
            if (Fnames.Count != 0)
            {
                if (Util.IsNetworkAvailable())
                {
                    if (!IsObjectsInitlized)
                    {
                        InitlizeClientObjects();
                        IsObjectsInitlized = true;
                    }
                    StatusBarProperties.ResetData();
                    FinalListViewData.Data.Clear();
                    PrepareForSearch();
                    if (IconMode == "Poster")
                        await ProcessPosterModeAsync();
                    else
                    {
                        ProcessProfessionalMode();
                    }
                    StatusBarProperties.ProcessedFolder = Fnames.Count;
                    StatusBarProperties.TotalIcons = BusyIndicatorProperties.Max =
                        StatusBarProperties.ProgressBarData.Max = ImgDownloadList.Count;
                    BusyIndicatorProperties.Text = $"Downloading Icon 1/{ImgDownloadList.Count}...";
                    if (ImgDownloadList.Count > 0)
                    {
                        await StartDownloadingAsync();
                    }
                    else
                    {
                        IsMakeEnabled = true;
                    }
                }
                else
                {
                    MessageBox.Error("Sorry, Internet is Not available.", "Network Error");
                    //    DialogParameters p = new DialogParameters
                    //{
                    //    { "message", "Sorry, Internet is Not available." },
                    //    { "title", "Network Error" }
                    //};
                    //    _dialogService.ShowDialog("MessageBox", p, result => { });
                }
            }
            else
            {
                MessageBox.Warning("Folder already have Icons or is Empty!", "Folder Error"); //TODO: Handy Exception here
                //DialogParameters p = new DialogParameters
                //{
                //    { "message", "Folder already have Icons or is Empty!" },
                //    { "title", "Folder Error" }
                //};
                //_dialogService.ShowDialog("MessageBox", p, result => { });
            }
        }

        private async System.Threading.Tasks.Task ProcessPosterModeAsync()
        {
            IsMakeEnabled = false;
            GlobalVariables.SkipAll = false;
            bool isAutoPicked;
            string searchTitle;
            foreach (string itemTitle in Fnames)
            {
                string fullFolderPath = SelectedFolder + "\\" + itemTitle;
                bool dialogResult = false;
                StatusBarProperties.AppStatus = "Searching...";
                // TODO: Set cursor to WAIT.
                isAutoPicked = false;
                searchTitle = TitleCleaner.Clean(itemTitle);
                ResultResponse response = (SearchMode == "Game") ? await IGDBObject.SearchGameAsync(searchTitle) : await TMDBObject.SearchAsync(searchTitle, SearchMode);
                int resultCount = (SearchMode == "Game") ? response.Result.Length : response.Result.TotalResults;
                if (resultCount == 0)
                {
                    MessageBox.Info($"Nothing found for { itemTitle}\n Try Searching with Other Title\n or check Search Mode", "No Result Found");
                    //DialogParameters p=new DialogParameters
                    //{
                    //    {"message",$"Nothing found for {itemTitle}\n Try Searching with Other Title\n or check Search Mode" },
                    //    {"title","No Result Found" }
                    //};
                    //_dialogService.ShowDialog("MessageBox", p, result => { });

                    _dialogService.ShowSearchResult(SearchMode, searchTitle, fullFolderPath, response, TMDBObject, IGDBObject,
                        r =>
                    {
                        if (r.Result == ButtonResult.None)
                            dialogResult = false;
                        else if (r.Result == ButtonResult.OK)
                            dialogResult = true;
                        else if (r.Result == ButtonResult.Cancel)
                            dialogResult = false;
                        else
                            dialogResult = false;
                    });
                }
                else if (resultCount == 1)
                {
                   // try
                  //  {
                        if (SearchMode == "Game")
                        {
                            IGDBObject.ResultPicked(response.Result[0], fullFolderPath);
                        }
                        else
                        {
                            TMDBObject.ResultPicked(response.Result.Results[0], response.MediaType, fullFolderPath);
                        }
                  //  }
                  //  catch (Exception e)
                  //  {
                      //  if (e.Message == "NoPoster")
                      //  {
                      //      MessageBox.Error($"No poster found for \"{searchTitle}\"", "No Poster found");
                     //       continue;
                      //  }
                   // }

                    isAutoPicked = true;
                }
                else if (resultCount > 1)
                {
                    if (!IgnoreAmbigousTitle)
                    {
                        _dialogService.ShowSearchResult(SearchMode, searchTitle, fullFolderPath, response, TMDBObject, IGDBObject,
                        r =>
                        {
                            if (r.Result == ButtonResult.None)
                                dialogResult = false;
                            else if (r.Result == ButtonResult.OK)
                                dialogResult = true;
                            else if (r.Result == ButtonResult.Cancel)
                                dialogResult = false;
                            else
                                dialogResult = false;
                        });
                    }
                }
                if (isAutoPicked || dialogResult)
                {
                    FinalListViewData.Data.Add(new ListItem()
                    {
                        Title = PickedListDataTable.Rows[PickedListDataTable.Rows.Count - 1]["Title"].ToString(),
                        Year = PickedListDataTable.Rows[PickedListDataTable.Rows.Count - 1]["Year"].ToString(),
                        Rating = PickedListDataTable.Rows[PickedListDataTable.Rows.Count - 1]["Rating"].ToString(),
                        Folder = PickedListDataTable.Rows[PickedListDataTable.Rows.Count - 1]["Folder"].ToString()
                    });
                    // TODO: Set cursor back to arrow here
                }
                if (GlobalVariables.SkipAll)
                    break;
            }
            StatusBarProperties.AppStatus = "Idle";
        }

        private void ProcessProfessionalMode()
        {
            StatusBarProperties.AppStatus = "Searching...";
            //DialogParameters p=CreateProSearchParameters();
            _dialogService.ShowProSearchResult(SelectedFolder, Fnames, PickedListDataTable, ImgDownloadList, DArtObject, r => { });
            //_dialogService.ShowDialog("ProSearchResult", p, r => { });
            if (PickedListDataTable.Rows.Count > 0)
            {
                foreach (DataRow v in PickedListDataTable.Rows)
                {
                    FinalListViewData.Data.Add(new ListItem()
                    {
                        Title = v["Title"].ToString(),
                        Year = v["Year"].ToString(),
                        Rating = v["Rating"].ToString(),
                        Folder = v["Folder"].ToString()
                    });
                }
            }
        }

        private void InitilizeDelegates()
        {
            APIConfigCommand = new DelegateCommand(delegate
             {
                 _dialogService.ShowApiConfig(r => { });
             });
            AboutCommand = new DelegateCommand(AboutMethod);
            DeleteIconsCommand = new DelegateCommand(DeleteIconsMethod);
            LoadCommand = new DelegateCommand(LoadMethod);
            SearchAndMakeCommand = new DelegateCommand(SearchAndMakeMethod);
            IconModeChangedCommand = new DelegateCommand<object>(delegate (object parameter) { IconMode = (string)parameter; });
            SearchModeChangedCommand = new DelegateCommand<object>(delegate (object parameter) { SearchMode = (string)parameter; });
            ListViewDoubleClickCommand = new DelegateCommand(ListViewDoubleClickMethod);
            DownloadCancelCommand = new DelegateCommand(delegate { StopIconDownload = true; });

            SelectedFolderDClickCommand = new DelegateCommand(delegate
            {
                if (Directory.Exists(SelectedFolder))
                {
                    Util.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
                }
            });
        }

        private void InitilizeProperties()
        {
            Fnames = new List<string>();
            BusyIndicatorProperties = new ProgressBarData
            {
                Max = 100,
                Value = 0,
                Text = "Download it"
            };
            StatusBarProperties = new StatusBarData
            {
                NetIcon = Util.IsNetworkAvailable() ? @"\Resources\Strong-WiFi.png" : @"\Resources\No-WiFi.png",
                TotalIcons = 0,
                AppStatus = "IDLE"
            };
            FinalListViewData = new ListViewData()
            {
                Data = new ObservableCollection<ListItem>(),
                SelectedItem = new ListItem(),
                SelectedCount = 0
            };
            StatusBarProperties.ProgressBarData.Max = 100;
            StatusBarProperties.ProgressBarData.Value = 0;
            ImgDownloadList = new List<ImageToDownload>();
            PickedListDataTable = new DataTable();
            if (Util.IsNetworkAvailable())
            {
                InitlizeClientObjects();
                IsObjectsInitlized = true;
            }
            else
                IsObjectsInitlized = false;
        }

        private void PrepareForSearch()
        {
            ImgDownloadList.Clear();
            InitPickedListDataTable();
        }

        public void InitPickedListDataTable()
        {
            PickedListDataTable.Columns.Clear();
            PickedListDataTable.Rows.Clear();
            DataColumn column1 = new DataColumn("Poster") { DataType = Type.GetType("System.String") };
            DataColumn column2 = new DataColumn("Title") { DataType = Type.GetType("System.String") };
            DataColumn column3 = new DataColumn("Year") { DataType = Type.GetType("System.String") };
            DataColumn column4 = new DataColumn("Rating") { DataType = Type.GetType("System.String") };
            DataColumn column5 = new DataColumn("Folder") { DataType = Type.GetType("System.String") };
            DataColumn column6 = new DataColumn("FolderName") { DataType = Type.GetType("System.String") };
            PickedListDataTable.Columns.Add(column1);
            PickedListDataTable.Columns.Add(column2);
            PickedListDataTable.Columns.Add(column3);
            PickedListDataTable.Columns.Add(column4);
            PickedListDataTable.Columns.Add(column5);
            PickedListDataTable.Columns.Add(column6);
        }

        private DialogParameters CreateSearchResultParameters(string searchTitle, dynamic result, string folderPath)
        {
            DialogParameters p = new DialogParameters
            {
                {"query", searchTitle},{"result",result},{"searchmode",SearchMode},{"tmdbObject",TMDBObject},{"igdbObject",IGDBObject},
                {"folderpath",folderPath }
            };
            return p;
        }

        private DialogParameters CreateProSearchParameters()
        {
            DialogParameters p = new DialogParameters
            {
                {"dartobject",DArtObject },{"fnames",Fnames},{"pickedtable",PickedListDataTable},{"imglist",ImgDownloadList},
                {"folderpath",SelectedFolder}
            };
            return p;
        }

        private void DeleteIconsMethod()
        {
            if (Directory.Exists(SelectedFolder))
            {
                //TODO: Replace with DialogService if efficient.
                if (MessageBox.Ask("Are you sure you want to delete all Icons?", "Confirm Icon Deletion") == System.Windows.MessageBoxResult.OK)
                {
                    Util.DeleteIconsFromPath(SelectedFolder);
                }
            }
            else
            {
                MessageBox.Error("Directory is Empty", "Empty Directory");
            }
        }

        private void ListViewDoubleClickMethod()
        {
            if (FinalListViewData.SelectedItem.Folder != null)
            {
                if (Directory.Exists(FinalListViewData.SelectedItem.Folder))
                {
                    Util.StartProcess(FinalListViewData.SelectedItem.Folder + Path.DirectorySeparatorChar);
                }
            }
        }

        private async System.Threading.Tasks.Task StartDownloadingAsync()
        {
            IsMakeEnabled = false;
            StatusBarProperties.AppStatus = "Creating Icons...";
            await DownloadAndMakeIconsAsync();
            StatusBarProperties.AppStatus = "IDLE";
        }

        private async System.Threading.Tasks.Task DownloadAndMakeIconsAsync()
        {
            StopIconDownload = false;
            IsBusy = true;
            var i = 0;
            foreach (ImageToDownload img in ImgDownloadList)
            {
                if (StopIconDownload)
                {
                    MakeIcons();
                    IsMakeEnabled = true;
                    StatusBarProperties.ProgressBarData.Value = StatusBarProperties.ProgressBarData.Max;
                    return;
                }
                await Util.DownloadImageFromUrlAsync(img.RemotePath, img.LocalPath);
                i += 1;
                BusyIndicatorProperties.Text = "Downloading Icon " + i + "/" + BusyIndicatorProperties.Max + "...";
                BusyIndicatorProperties.Value = i;
                StatusBarProperties.ProgressBarData.Value = i;
            }
            IsBusy = false;
            if (StatusBarProperties.ProgressBarData.Value == StatusBarProperties.ProgressBarData.Max)
            {
                IsBusy = true;
                MakeIcons();
            }
            IsMakeEnabled = true;
        }

        private void MakeIcons()
        {
            IsBusy = true;
            int IconProcessedCount;
            if (IconMode.Equals("Poster") && !SearchMode.Equals("Game"))
            {
                IconProcessedCount = Util.MakeIco(IconMode, SelectedFolder, PickedListDataTable, IsRatingVisible, IsPosterMockupUsed);
            }
            else
            {
                IconProcessedCount = Util.MakeIco(IconMode, SelectedFolder, PickedListDataTable);
            }
            StatusBarProperties.ProcessedIcon = IconProcessedCount;
            IsBusy = false;
            Growl.SuccessGlobal($"{IconProcessedCount} Icon created");
            switch (MessageBox.Ask("Note:The Icon may take some time to reload. " + Environment.NewLine + " To Force Reload, click on Restart Explorer " + Environment.NewLine + "Click \"Confirm\" to open folder.", "Icon(s) Created"))
            {
                case System.Windows.MessageBoxResult.OK:
                    Util.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
                    break;
            }
        }

        private void InitlizeClientObjects()
        {
            if (string.IsNullOrEmpty(TMDBAPIKey) || string.IsNullOrEmpty(IGDBKey) || string.IsNullOrEmpty(DevClientSecret) || string.IsNullOrEmpty(DevClientID))
            {
                _dialogService.ShowApiConfig(r =>
                {
                    if (r.Result == ButtonResult.Cancel)
                    {
                        MessageBox.Error($"API keys not provided{Environment.NewLine}The Application will close.", "Closing Application");
                        Environment.Exit(0);
                    }
                });
            }
            Util.ReadApiConfiguration(out TMDBAPIKey, out IGDBKey, out DevClientSecret, out DevClientID);
            tMDbClient = new TMDbLib.Client.TMDbClient(TMDBAPIKey);
            igdbClient = IGDB.Client.Create(IGDBKey);
            IGDBObject = new IGDBClass(ref PickedListDataTable, ref igdbClient, ref ImgDownloadList);
            TMDBObject = new TMDB(ref tMDbClient, ref PickedListDataTable, ref ImgDownloadList);
            DArtObject = new DArt(DevClientSecret, DevClientID);
        }

        private void AboutMethod()
        {
            _dialogService.ShowAboutBox(r => { });
        }
    }
}