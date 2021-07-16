using FoliCon.Models;
using FoliCon.Modules;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Tools;
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
    public class MainWindowViewModel : BindableBase, IFileDragDropTarget, IDisposable
    {
        #region Variables

        private string _selectedFolder;
        private string _title = "FoliCon";
        private bool _isRatingVisible = true;
        private bool _isPosterMockupUsed = true;
        private bool _isBusy;
        private bool _isMakeEnabled;
        private bool _isSkipAmbiguous;
        private string _iconMode = "Poster";
        private string _searchMode = "Movie";
        private bool _isSearchModeVisible = true;
        private bool _stopIconDownload;
        private TMDbLib.Client.TMDbClient _tmdbClient;
        private IGDB.IGDBClient _igdbClient;
        private IgdbClass _igdbObject;
        private Tmdb _tmdbObject;
        private DArt _dArtObject;
        private bool isPosterWindowShown;

        AppConfig settings = GlobalDataHelper.Load<AppConfig>();
        private string _tmdbapiKey;
        private string _igdbClientId;
        private string _igdbClientSecret;
        private string _devClientSecret;
        private string _devClientId;
        private ListViewData _finalListViewData;
        private List<ImageToDownload> _imgDownloadList;
        private DataTable _pickedListDataTable;
        private bool IsObjectsInitialized { get; set; }
        public bool IsPosterWindowShown
        {
            get => isPosterWindowShown; set
            {
                IsSkipAmbiguousEnabled = !value;
                SetProperty(ref isPosterWindowShown, value);
            }
        }
        private bool isSkipAmbiguousEnabled;
        public bool IsSkipAmbiguousEnabled { get => isSkipAmbiguousEnabled; set => SetProperty(ref isSkipAmbiguousEnabled, value); }

        public bool IsSearchModeVisible
        {
            get => _isSearchModeVisible;
            set => SetProperty(ref _isSearchModeVisible, value);
        }

        public ListViewData FinalListViewData
        {
            get => _finalListViewData;
            set => SetProperty(ref _finalListViewData, value);
        }

        private readonly IDialogService _dialogService;
        public StatusBarData StatusBarProperties { get; set; }
        public ProgressBarData BusyIndicatorProperties { get; set; }
        public List<string> Fnames { get; set; }

        public bool IsMakeEnabled
        {
            get => _isMakeEnabled;
            set => SetProperty(ref _isMakeEnabled, value);
        }

        public bool IsSkipAmbiguous
        {
            get => _isSkipAmbiguous;
            set => SetProperty(ref _isSkipAmbiguous, value);
        }

        public bool StopIconDownload
        {
            get => _stopIconDownload;
            set => SetProperty(ref _stopIconDownload, value);
        }

        public string IconMode
        {
            get => _iconMode;
            set
            {
                SetProperty(ref _iconMode, value);
                IsSearchModeVisible = value != "Professional";
            }
        }

        public string SearchMode
        {
            get => _searchMode;
            set => SetProperty(ref _searchMode, value);
        }

        #endregion Variables

        #region GetterSetters

        public string SelectedFolder
        {
            get => _selectedFolder;
            set => SetProperty(ref _selectedFolder, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsRatingVisible
        {
            get => _isRatingVisible;
            set => SetProperty(ref _isRatingVisible, value);
        }

        public bool IsPosterMockupUsed
        {
            get => _isPosterMockupUsed;
            set => SetProperty(ref _isPosterMockupUsed, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        #endregion GetterSetters

        #region DelegateCommands

        #region MenuItem Commands

        #region SettingMenu

        public DelegateCommand ApiConfigCommand { get; set; }
        public DelegateCommand PosterIconConfigCommand { get; set; }

        #endregion SettingMenu

        public DelegateCommand RestartExCommand { get; } = new(delegate
        {
            if (MessageBox.Ask("Are you sure you want to \nRestart Explorer and refresh Icon Cache?",
                "Confirm Explorer Restart") != System.Windows.MessageBoxResult.OK) return;
            Util.RefreshIconCache();
            Util.RestartExplorer();
        });

        public DelegateCommand CustomIconsCommand { get; private set; }
        public DelegateCommand DeleteIconsCommand { get; private set; }

        public DelegateCommand HelpCommand { get; } = new(delegate
        {
            Util.StartProcess("https://github.com/DineshSolanki/FoliCon");
        });

        public DelegateCommand AboutCommand { get; private set; }
        public DelegateCommand UpdateCommand { get; } = new(Util.CheckForUpdate);

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
            InitializeProperties();
            InitializeDelegates();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            Services.Tracker.Configure<MainWindowViewModel>()
                .Property(p => p.IsRatingVisible, true)
                .Property(p => p.IsPosterMockupUsed, true)
                .Property(p => p.IsPosterWindowShown)
                .PersistOn(nameof(PropertyChanged));
            Services.Tracker.Track(this);
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            StatusBarProperties.NetIcon =
                ApplicationHelper.IsConnectedToInternet() ? @"\Resources\Strong-WiFi.png" : @"\Resources\No-WiFi.png";
        }

        private void LoadMethod()
        {
            var folderBrowserDialog = Util.NewFolderBrowserDialog("Select Folder");
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != null && (bool)!dialogResult) return;
            SelectedFolder = folderBrowserDialog.SelectedPath;
            StatusBarProperties.ResetData();
            IsMakeEnabled = true;
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
                    if (!IsObjectsInitialized)
                    {
                        InitializeClientObjects();
                        IsObjectsInitialized = true;
                    }

                    StatusBarProperties.ResetData();
                    FinalListViewData.Data.Clear();
                    StatusBarProperties.TotalFolders = Fnames.Count;
                    PrepareForSearch();
                    if (IconMode == "Poster")
                        await ProcessPosterModeAsync();
                    else
                    {
                        ProcessProfessionalMode();
                    }
                    StatusBarProperties.TotalIcons = BusyIndicatorProperties.Max =
                        StatusBarProperties.ProgressBarData.Max = _imgDownloadList.Count;
                    BusyIndicatorProperties.Text = $"Downloading Icon 1/{_imgDownloadList.Count}...";
                    if (_imgDownloadList.Count > 0)
                        await StartDownloadingAsync();
                    else
                        IsMakeEnabled = true;
                }
                else
                {
                    MessageBox.Error("Sorry, Internet is Not available.", "Network Error");
                }
            }
            else
            {
                MessageBox.Warning("Folder already have Icons or is Empty!", "Folder Error");
                //TODO: Handy Exception here
            }
        }

        private async System.Threading.Tasks.Task ProcessPosterModeAsync()
        {
            IsMakeEnabled = false;
            GlobalVariables.SkipAll = false;
            foreach (var itemTitle in Fnames)
            {
                var fullFolderPath = SelectedFolder + "\\" + itemTitle;
                var dialogResult = false;
                StatusBarProperties.AppStatus = $"Searching...{itemTitle}";
                // TODO: Set cursor to WAIT.
                var isAutoPicked = false;
                var searchTitle = TitleCleaner.Clean(itemTitle);
                var response = SearchMode == "Game"
                    ? await _igdbObject.SearchGameAsync(searchTitle)
                    : await _tmdbObject.SearchAsync(searchTitle, SearchMode);
                int resultCount = SearchMode == "Game" ? response.Result.Length : response.Result.TotalResults;
                switch (resultCount)
                {
                    case 0:
                        MessageBox.Info($"Nothing found for {itemTitle}\n Try Searching with Other Title\n" +
                                        " or check Search Mode", "No Result Found");
                        _dialogService.ShowSearchResult(SearchMode, searchTitle, fullFolderPath, response,
                            _tmdbObject, _igdbObject,
                            r =>
                            {
                                dialogResult = r.Result switch
                                {
                                    ButtonResult.None => false,
                                    ButtonResult.OK => true,
                                    ButtonResult.Cancel => false,
                                    _ => false
                                };
                            });
                        break;
                    case 1 when !IsPosterWindowShown:
                        {
                            try
                            {
                                if (SearchMode == "Game")
                                {
                                    _igdbObject.ResultPicked(response.Result[0], fullFolderPath);
                                }
                                else
                                {
                                    _tmdbObject.ResultPicked(response.Result.Results[0], response.MediaType,
                                        fullFolderPath);
                                }

                                isAutoPicked = true;
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message == "NoPoster")
                                {
                                    MessageBox.Warning("No poster found.", itemTitle);
                                }

                                isAutoPicked = false;
                            }

                            break;
                        }
                    default:
                        {
                            if (resultCount >= 1)
                            {
                                if (IsPosterWindowShown || !IsSkipAmbiguous)
                                {
                                    _dialogService.ShowSearchResult(SearchMode, searchTitle, fullFolderPath,
                                        response, _tmdbObject, _igdbObject,
                                        r =>
                                        {
                                            dialogResult = r.Result switch
                                            {
                                                ButtonResult.None => false,
                                                ButtonResult.OK => true,
                                                ButtonResult.Cancel => false,
                                                _ => false
                                            };
                                        });
                                }
                            }

                            break;
                        }
                }

                if (isAutoPicked || dialogResult)
                {
                    if (_pickedListDataTable is not null && _pickedListDataTable.Rows.Count != 0)
                    {
                        FinalListViewData.Data.Add(new ListItem
                        {
                            Title = _pickedListDataTable.Rows[^1]["Title"].ToString(),
                            Year = _pickedListDataTable.Rows[^1]["Year"].ToString(),
                            Rating = _pickedListDataTable.Rows[^1]["Rating"].ToString(),
                            Folder = _pickedListDataTable.Rows[^1]["Folder"].ToString()
                        });
                    }
                    // TODO: Set cursor back to arrow here
                }
                StatusBarProperties.ProcessedFolder++;
                if (GlobalVariables.SkipAll)
                    break;
            }

            StatusBarProperties.AppStatus = "Idle";
        }

        private void ProcessProfessionalMode()
        {
            StatusBarProperties.AppStatus = "Searching...";
            //DialogParameters p=CreateProSearchParameters();
            _dialogService.ShowProSearchResult(SelectedFolder, Fnames, _pickedListDataTable, _imgDownloadList,
                _dArtObject, _ => { });
            //_dialogService.ShowDialog("ProSearchResult", p, r => { });
            if (_pickedListDataTable.Rows.Count <= 0) return;
            foreach (DataRow v in _pickedListDataTable.Rows)
            {
                FinalListViewData.Data.Add(new ListItem
                {
                    Title = v["Title"].ToString(),
                    Year = v["Year"].ToString(),
                    Rating = v["Rating"].ToString(),
                    Folder = v["Folder"].ToString()
                });
            }
            StatusBarProperties.ProcessedFolder = _pickedListDataTable.Rows.Count;
        }

        private void InitializeDelegates()
        {
            ApiConfigCommand = new DelegateCommand(delegate { _dialogService.ShowApiConfig(_ => { }); });
            PosterIconConfigCommand = new DelegateCommand(delegate { _dialogService.ShowPosterIconConfig(_ => { }); });
            AboutCommand = new DelegateCommand(AboutMethod);
            DeleteIconsCommand = new DelegateCommand(DeleteIconsMethod);
            CustomIconsCommand = new DelegateCommand(delegate
            {
                _dialogService.ShowCustomIconWindow(
                    _ => { }
                );
            }
            );
            LoadCommand = new DelegateCommand(LoadMethod);
            SearchAndMakeCommand = new DelegateCommand(SearchAndMakeMethod);
            IconModeChangedCommand = new DelegateCommand<object>(delegate (object parameter)
            {
                IconMode = (string)parameter;
            });
            SearchModeChangedCommand = new DelegateCommand<object>(delegate (object parameter)
            {
                SearchMode = (string)parameter;
            });
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

        private void InitializeProperties()
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
            FinalListViewData = new ListViewData
            {
                Data = new ObservableCollection<ListItem>(),
                SelectedItem = new ListItem(),
                SelectedCount = 0
            };
            StatusBarProperties.ProgressBarData.Max = 100;
            StatusBarProperties.ProgressBarData.Value = 0;
            _imgDownloadList = new List<ImageToDownload>();
            _pickedListDataTable = new DataTable();
            if (Util.IsNetworkAvailable())
            {
                InitializeClientObjects();
                IsObjectsInitialized = true;
            }
            else
                IsObjectsInitialized = false;
        }

        private void PrepareForSearch()
        {
            _imgDownloadList.Clear();
            InitPickedListDataTable();
        }

        public void InitPickedListDataTable()
        {
            _pickedListDataTable.Columns.Clear();
            _pickedListDataTable.Rows.Clear();
            var column1 = new DataColumn("Poster") { DataType = typeof(string) };
            var column2 = new DataColumn("Title") { DataType = typeof(string) };
            var column3 = new DataColumn("Year") { DataType = typeof(string) };
            var column4 = new DataColumn("Rating") { DataType = typeof(string) };
            var column5 = new DataColumn("Folder") { DataType = typeof(string) };
            var column6 = new DataColumn("FolderName") { DataType = typeof(string) };
            _pickedListDataTable.Columns.Add(column1);
            _pickedListDataTable.Columns.Add(column2);
            _pickedListDataTable.Columns.Add(column3);
            _pickedListDataTable.Columns.Add(column4);
            _pickedListDataTable.Columns.Add(column5);
            _pickedListDataTable.Columns.Add(column6);
        }

        private void DeleteIconsMethod()
        {
            if (Directory.Exists(SelectedFolder))
            {
                //TODO: Replace with DialogService if efficient.
                if (MessageBox.Ask("Are you sure you want to delete all Icons?", "Confirm Icon Deletion") ==
                    System.Windows.MessageBoxResult.OK)
                {
                    Util.DeleteIconsFromSubfolders(SelectedFolder);
                }
            }
            else
            {
                MessageBox.Error("Directory is Empty", "Empty Directory");
            }
        }

        private void ListViewDoubleClickMethod()
        {
            if (FinalListViewData.SelectedItem.Folder == null) return;
            if (Directory.Exists(FinalListViewData.SelectedItem.Folder))
            {
                Util.StartProcess(FinalListViewData.SelectedItem.Folder + Path.DirectorySeparatorChar);
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
            foreach (var img in _imgDownloadList)
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
            int iconProcessedCount;
            if (IconMode == "Poster" && SearchMode != "Game")
            {
                iconProcessedCount = Util.MakeIco(IconMode, SelectedFolder, _pickedListDataTable,
                    IsRatingVisible, IsPosterMockupUsed);
            }
            else
            {
                iconProcessedCount = Util.MakeIco(IconMode, SelectedFolder, _pickedListDataTable);
            }

            StatusBarProperties.ProcessedIcon = iconProcessedCount;
            IsBusy = false;
            var info = new GrowlInfo
            {
                Message = $"{iconProcessedCount} Icon created",
                ShowDateTime = false,
                StaysOpen = false
            };
            Growl.SuccessGlobal(info);
            switch (MessageBox.Ask("Note:The Icon may take some time to reload. " + Environment.NewLine +
                                   " To Force Reload, click on Restart Explorer " + Environment.NewLine +
                                   @"Click ""Confirm"" to open folder.", "Icon(s) Created"))
            {
                case System.Windows.MessageBoxResult.OK:
                    Util.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
                    break;
            }
        }

        private void InitializeClientObjects()
        {
            Util.ReadApiConfiguration(out _tmdbapiKey, out _igdbClientId, out _igdbClientSecret, out _devClientSecret,
                out _devClientId);
            if (string.IsNullOrEmpty(_tmdbapiKey)
                || string.IsNullOrEmpty(_igdbClientId) || string.IsNullOrEmpty(_igdbClientSecret)
                || string.IsNullOrEmpty(_devClientSecret) || string.IsNullOrEmpty(_devClientId))
            {
                _dialogService.ShowApiConfig(r =>
                {
                    if (r.Result != ButtonResult.Cancel) return;
                    MessageBox.Error($"API keys not provided{Environment.NewLine}" +
                                     "The Application will close.", "Closing Application");
                    Environment.Exit(0);
                });
            }
            _tmdbClient = new TMDbLib.Client.TMDbClient(_tmdbapiKey);
            _igdbClient = new IGDB.IGDBClient(_igdbClientId, _igdbClientSecret, new IgdbJotTrackerStore());
            _igdbObject = new IgdbClass(ref _pickedListDataTable, ref _igdbClient, ref _imgDownloadList);
            _tmdbObject = new Tmdb(ref _tmdbClient, ref _pickedListDataTable, ref _imgDownloadList);
            _dArtObject = new DArt(_devClientSecret, _devClientId);
        }

        private void AboutMethod()
        {
            _dialogService.ShowAboutBox(_ => { });
        }

        public void OnFileDrop(string[] filepaths, string senderName)
        {
            SelectedFolder = filepaths.GetValue(0)?.ToString();
            StatusBarProperties.ResetData();
            IsMakeEnabled = true;
        }

        public void Dispose()
        {
            _tmdbClient?.Dispose();
            _pickedListDataTable?.Dispose();
        }


    }
}