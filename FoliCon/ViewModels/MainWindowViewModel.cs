using FoliCon.Models.Configs;
using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.DeviantArt;
using FoliCon.Modules.Extension;
using FoliCon.Modules.IGDB;
using FoliCon.Modules.TMDB;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using HandyControl.Themes;
using NLog;

namespace FoliCon.ViewModels;

public class MainWindowViewModel : BindableBase, IFileDragDropTarget, IDisposable
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
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
    private Languages _appLanguage;
    private TMDbClient _tmdbClient;
    private IGDBClient _igdbClient;
    private IgdbClass _igdbObject;
    private Tmdb _tmdbObject;
    private DArt _dArtObject;
    private bool _isPosterWindowShown;
    private bool _enableErrorReporting;

    private string _tmdbapiKey;
    private string _igdbClientId;
    private string _igdbClientSecret;
    private string _devClientSecret;
    private string _devClientId;
    private ListViewData _finalListViewData;
    private List<ImageToDownload> _imgDownloadList;
    private List<PickedListItem> _pickedListDataTable;
    private bool IsObjectsInitialized { get; set; }

    public bool IsPosterWindowShown
    {
        get => _isPosterWindowShown; set
        {
            IsSkipAmbiguousEnabled = !value;
            SetProperty(ref _isPosterWindowShown, value);
        }
    }
    private bool _isSkipAmbiguousEnabled;
    public bool IsSkipAmbiguousEnabled { get => _isSkipAmbiguousEnabled; set => SetProperty(ref _isSkipAmbiguousEnabled, value); }
    public bool EnableErrorReporting
    {
        get => _enableErrorReporting;
        set
        {
            LogManager.Configuration.SentryConfig(value);
            SetProperty(ref _enableErrorReporting, value);
        }
    }

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
    private FoliconThemes _theme;
    private DirectoryPermissionsResult _directoryPermissionResult;
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
        set
        {
            SetProperty(ref _selectedFolder, value);
            if (value != null)
            {
                DirectoryPermissionsResult = FileUtils.CheckDirectoryPermissions(value);
            }
        }
    }

    public DirectoryPermissionsResult DirectoryPermissionsResult
    {
        get => _directoryPermissionResult; 
        set => SetProperty(ref _directoryPermissionResult, value);
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

    public Languages AppLanguage
    {
        get => _appLanguage;
        set => SetProperty(ref _appLanguage, value);
    }
    public FoliconThemes Theme
    {
        get => _theme;
        set
        {
            SetProperty(ref _theme, value);
            switch (value)
            {
                case FoliconThemes.Light: 
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    ThemeManager.Current.UsingSystemTheme = false;
                    break;
                case FoliconThemes.Dark: 
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    ThemeManager.Current.UsingSystemTheme = false;
                    break;
                case FoliconThemes.System:
                    ThemeManager.Current.UsingSystemTheme = true;
                    break;
                default:
                    ThemeManager.Current.UsingSystemTheme = true;
                    break;
            }
        }
    }

    #endregion GetterSetters

    #region DelegateCommands

    #region MenuItem Commands

    #region SettingMenu

    public DelegateCommand ApiConfigCommand { get; set; }
    public DelegateCommand PosterIconConfigCommand { get; set; }
    public DelegateCommand SubfolderProcessingConfigCommand { get; set; }

    #endregion SettingMenu

    public DelegateCommand RestartExCommand { get; } = new(delegate
    {
        if (MessageBox.Show(CustomMessageBox.Ask(LangProvider.GetLang("RestartExplorerConfirmation"),
                LangProvider.GetLang("ConfirmExplorerRestart"))) != MessageBoxResult.Yes) return;
        ProcessUtils.RefreshIconCache();
        ProcessUtils.RestartExplorer();
    });

    public DelegateCommand CustomIconsCommand { get; private set; }
    public DelegateCommand DeleteIconsCommand { get; private set; }
    public DelegateCommand DeleteMediaInfoCommand { get; private set; }

    public DelegateCommand<string> ExploreIntegrationCommand { get; private set; }

    public DelegateCommand AboutCommand { get; private set; }
    public DelegateCommand ShowPreviewer { get; private set; }
    public DelegateCommand UpdateCommand { get; } = new(() => FileUtils.CheckForUpdate());

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
        ProcessUtils.CheckWebView2();
        ShowPreviewer = new DelegateCommand(() =>
        {
            _dialogService.ShowPreviewer(_ => { });
        });
        Logger.Info("Application Started, Initilizing MainWindowViewModel.");
        _dialogService = dialogService;
        Services.Tracker.Configure<MainWindowViewModel>()
            .Property(p => p.IsRatingVisible, false)
            .Property(p => p.IsPosterMockupUsed, true)
            .Property(p => p.IsPosterWindowShown, false)
            .Property(p => p.AppLanguage, Languages.English)
            .Property(p => p.Theme, FoliconThemes.System)
            .Property(p => p.EnableErrorReporting, false)
            .PersistOn(nameof(PropertyChanged));
        Services.Tracker.Track(this);
        var selectedLanguage = AppLanguage;
        var cultureInfo = CultureUtils.GetCultureInfoByLanguage(selectedLanguage);
        LangProvider.Culture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
        InitializeProperties();
        InitializeDelegates();
        NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            
        var cmdArgs = ProcessUtils.GetCmdArgs();
        if (!cmdArgs.ContainsKey("path")) return;
        
        Logger.Info("Command Line Argument Found, Initializing with Command Line Argument.");
        
        SelectedFolder = cmdArgs["path"];
        var mode = cmdArgs["mode"];
        if (mode != "Professional" &&
            new List<string> { "Auto (Movies & TV Shows)", "TV", "Movie", "Game" }.Contains(mode))
        {
            IconMode = "Poster";
            SearchMode = mode;
        }
        else
        {
            IconMode = "Professional";
        }
        Logger.Info("Command Line argument initialized, selected folder: {SelectedFolder}, mode: {IconMode}",
            SelectedFolder, IconMode);
        SearchAndMakeMethod();

    }

    private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
    {
        Logger.Debug("Network Availability Changed, Updating StatusBar.");
        StatusBarProperties.NetIcon =
            ApplicationHelper.IsConnectedToInternet() ? @"\Resources\icons\Strong-WiFi.png" : @"\Resources\icons\No-WiFi.png";
    }

    private void LoadMethod()
    {
        var folderBrowserDialog = DialogUtils.NewFolderBrowserDialog(LangProvider.GetLang("SelectFolder"));
        if (!SelectedFolder.IsNullOrEmpty())
        {
            folderBrowserDialog.SelectedPath = SelectedFolder;
            Logger.Debug("LoadMethod:Already Selected Folder: {SelectedFolder}", SelectedFolder);
        }
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult) return;
        SelectedFolder = folderBrowserDialog.SelectedPath;
        Logger.Debug("LoadMethod:Selected Folder: {SelectedFolder}", SelectedFolder);
        StatusBarProperties.ResetData();
        IsMakeEnabled = true;
    }

    private async void SearchAndMakeMethod()
    {
        Logger.Debug("SearchAndMakeMethod Called.");
        try
        {
            if (Directory.Exists(SelectedFolder))
            {
                Fnames = FileUtils.GetFolderNames(SelectedFolder);
            }
            else
            {
                Logger.Error("Folder does not exist: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("FolderDoesNotExist"), LangProvider.GetLang("InvalidPath")));
                return;
            }

            if (Fnames.Count != 0)
            {
                if (NetworkUtils.IsNetworkAvailable())
                {
                    if (!IsObjectsInitialized)
                    {
                        Logger.Warn("SearchAndMakeMethod: Client Objects not initialized, Initializing now.");
                        InitializeClientObjects();
                        IsObjectsInitialized = true;
                        Logger.Info("SearchAndMakeMethod: Client Objects Initialized.");
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
                    BusyIndicatorProperties.Text = LangProvider.GetLang("DownloadingIconWithCount").Format(1, _imgDownloadList.Count);
                    Logger.Debug("SearchAndMakeMethod: Start Downloading Icons. Total Icons: {ImgTotalIcons}", _imgDownloadList.Count);
                    if (_imgDownloadList.Count > 0)
                        await StartDownloadingAsync();
                    else
                        IsMakeEnabled = true;
                }
                else
                {
                    MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("NoInternet"), LangProvider.GetLang("NetworkError")));
                }
            }
            else
            {
                Logger.Warn("SearchAndMakeMethod: Folder is empty: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("IconsAlready"), LangProvider.GetLang("FolderError")));
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("SearchAndMakeMethod: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, LangProvider.GetLang("ExceptionOccurred")));
            StatusBarProperties.ResetData();
            IsMakeEnabled = true;
            IsBusy = false;
        }
    }

    private async Task ProcessPosterModeAsync()
    {
        Logger.Debug("Entered ProcessPosterModeAsync method");
        IsMakeEnabled = false;
        await ProcessPosterFolderAsync(SelectedFolder);

        StatusBarProperties.AppStatus = "Idle";
        StatusBarProperties.AppStatusAdditional = "";
    }

    private async Task ProcessPosterFolderAsync(string folderPath)
    {
        var folders = FileUtils.GetAllSubFolders(folderPath);
        foreach (var subFolder in folders)
        {
            await ProcessPosterFolderAsync(subFolder);
        }
        var subfolderNames = FileUtils.GetFolderNames(folderPath);
        foreach (var itemTitle in subfolderNames)
        {
            var fullFolderPath = $@"{folderPath}\{itemTitle}";
            
            Logger.Debug("Processing Folder: {FullFolderPath}", fullFolderPath);
            var (response, parsedTitle, isPickedById, mediaType) = await PerformPreprocessing(itemTitle, fullFolderPath);

            var resultCount = CalculateResultCount(response, isPickedById);
            Logger.Info("Search Result Count: {ResultCount}", resultCount);
            var dialogResult = false;
            var isAutoPicked = false;
            var skipAll = false;
            switch (resultCount)
            {
                case 0:
                    dialogResult = await ProcessNoResultCase(itemTitle, response, fullFolderPath, parsedTitle.Title, isPickedById);
                    break;
                case 1 when !IsPosterWindowShown:
                {
                    isAutoPicked = ProcessSingleResultCase(itemTitle, response, fullFolderPath, isPickedById, mediaType);
                    break;
                }
                default:
                {
                    if (resultCount >= 1)
                    {
                        (dialogResult, skipAll) = await ProcessMultipleResultCase(itemTitle, response, fullFolderPath, parsedTitle.Title, isPickedById);
                    }
                    break;
                }
            }

            if (isAutoPicked || dialogResult)
            {
                Logger.Debug("Auto picked:{IsAutoPicked}, dialog result : {DialogResult} for {ItemTitle}, " +
                             "adding to final list", isAutoPicked, dialogResult, itemTitle);
                if (_pickedListDataTable is not null && _pickedListDataTable.Count != 0)
                {
                    FinalListViewData.Data.Add(_pickedListDataTable.Last());
                }
                // TODO: Set cursor back to arrow here
            }
            StatusBarProperties.ProcessedFolder++;
            if (!skipAll)
            {
                continue;
            }

            Logger.Debug("Skip All selected, breaking loop");
            break;
        }
    }

    private async Task<(ResultResponse response, ParsedTitle parsedTitle, bool isPickedById, string mediaType)> PerformPreprocessing(string itemTitle, string fullFolderPath)
    {
        StatusBarProperties.AppStatus = "Searching";
        StatusBarProperties.AppStatusAdditional = itemTitle;
        var parsedTitle = TitleCleaner.CleanAndParse(itemTitle);
        var (id, mediaType) = FileUtils.ReadMediaInfo(fullFolderPath);
        var isPickedById = false;
        ResultResponse response;
        if (id != null && mediaType != null)
        {
            Logger.Info("MediaInfo found for {ItemTitle}, mediaType: {MediaType}, id: {Id}", itemTitle, mediaType, id);
            isPickedById = true;
            response = mediaType == "Game" ? await _igdbObject.SearchGameByIdAsync(id) : await _tmdbObject.SearchByIdAsync(int.Parse(id), mediaType);
        }
        else
        {
            Logger.Info("MediaInfo not found for {ItemTitle}, Searching by Title", itemTitle);
            response = SearchMode == "Game"
                ? await _igdbObject.SearchGameAsync(parsedTitle.Title)
                : DataUtils.ShouldUseParsedTitle(parsedTitle)
                    ? await _tmdbObject.SearchAsync(parsedTitle, SearchMode)
                    : await _tmdbObject.SearchAsync(parsedTitle.Title, SearchMode);
        }

        return (response, parsedTitle, isPickedById, mediaType);
    }

    private int CalculateResultCount(ResultResponse response, bool isPickedById)
    {
        return isPickedById ? response.Result != null ? 1 : 0 :
            SearchMode == "Game" ? response.Result.Length : response.Result.TotalResults;
    }

    private async Task<bool> ProcessNoResultCase(string itemTitle, ResultResponse response, string fullFolderPath, string parsedTitle, bool isPickedById)
    {
        Logger.Debug("No result found for {ItemTitle}, {Mode}", itemTitle, SearchMode);
        MessageBox.Show(CustomMessageBox.Info(LangProvider.GetLang("NothingFoundFor").Format(itemTitle),
            LangProvider.GetLang("NoResultFound")));
    
        var taskCompletionSource = new TaskCompletionSource<bool>();

        _dialogService.ShowSearchResult(SearchMode, parsedTitle, fullFolderPath, response,
            _tmdbObject, _igdbObject, isPickedById,
            r =>
            {
                var dialogResult = r.Result switch
                {
                    ButtonResult.None => false,
                    ButtonResult.OK => true,
                    ButtonResult.Cancel => false,
                    _ => false
                };
                taskCompletionSource.SetResult(dialogResult);
            });
        return await taskCompletionSource.Task;
    }

    private bool ProcessSingleResultCase(string itemTitle, ResultResponse response, string fullFolderPath, bool isPickedById, string mediaType)
    {
        bool isAutoPicked;
        Logger.Debug("One result found for {ItemTitle}, {Mode}, as always show poster window is not enabled, directly selecting",
            itemTitle, SearchMode);
        try
        {
            if (isPickedById ? mediaType == "Game" : SearchMode == "Game")
            {
                var result = response.Result[0];
                _igdbObject.ResultPicked(result, fullFolderPath);
            }
            else
            {
                var result = isPickedById
                    ? response.Result
                    : response.Result.Results[0];
                _tmdbObject.ResultPicked(result, response.MediaType,
                    fullFolderPath, "", isPickedById);
            }

            isAutoPicked = true;
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("ProcessPosterModeAsync: Exception Occurred. message: {Message}", ex.Message)
                .Exception(ex).Log();
            if (ex.Message == "NoPoster")
            {
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), itemTitle));
            }
#if DEBUG
            MessageBox.Show(CustomMessageBox.Warning(ex.Message, LangProvider.GetLang("ExceptionOccurred")));
#endif
            isAutoPicked = false;
        }
        return isAutoPicked;
    }

    private async Task<(bool dialogResult, bool skipAll)> ProcessMultipleResultCase(string itemTitle, ResultResponse response, string fullFolderPath, string parsedTitle, bool isPickedById)
    {
        var taskCompletionSource = new TaskCompletionSource<(bool dialogResult, bool skipAll)>();
        if (!IsPosterWindowShown && IsSkipAmbiguous)
        {
            return await taskCompletionSource.Task;
        }

        Logger.Debug("More than one result found for {ItemTitle}, {Mode}," +
                     "always show poster window: {IsPosterWindowShown}, Skip ambigous titles: {IsSkipAmbiguous}," +
                     " showing poster window", itemTitle, SearchMode, IsPosterWindowShown, IsSkipAmbiguous);
                            
        _dialogService.ShowSearchResult(SearchMode, parsedTitle, fullFolderPath,
            response, _tmdbObject, _igdbObject, isPickedById,
            r =>
            {
                var dialogResult = r.Result switch
                {
                    ButtonResult.None => false,
                    ButtonResult.OK => true,
                    ButtonResult.Cancel => false,
                    _ => false
                };
                r.Parameters.TryGetValue<bool>("skipAll", out var skipAll);
                taskCompletionSource.SetResult((dialogResult, skipAll));
            });
        return await taskCompletionSource.Task;
    }
    
    private void ProcessProfessionalMode()
    {
        Logger.Debug("Entered ProcessProfessionalMode method");
        StatusBarProperties.AppStatus = "Searching";
        _dialogService.ShowProSearchResult(SelectedFolder, Fnames, _pickedListDataTable, _imgDownloadList,
            _dArtObject, _ => { });
        if (_pickedListDataTable.Count <= 0)
        {
            return;
        }

        Logger.Debug("ProcessProfessionalMode: found {_pickedListDataTable.Rows.Count} results, adding to final list");
        FinalListViewData.Data.AddRange(_pickedListDataTable);
        StatusBarProperties.ProcessedFolder = _pickedListDataTable.Count;
    }

    private void InitializeDelegates()
    {
        Logger.Debug("Initializing Delegates for MainWindow.");
        ApiConfigCommand = new DelegateCommand(delegate { _dialogService.ShowApiConfig(_ => { }); });
        PosterIconConfigCommand = new DelegateCommand(delegate { _dialogService.ShowPosterIconConfig(_ => { }); });
        SubfolderProcessingConfigCommand = new DelegateCommand(delegate { _dialogService.ShowSubfolderProcessingConfig(_ => { }); });
        AboutCommand = new DelegateCommand(AboutMethod);
        DeleteIconsCommand = new DelegateCommand(DeleteIconsMethod);
        DeleteMediaInfoCommand = new DelegateCommand(DeleteMediaInfo);
        ExploreIntegrationCommand = new DelegateCommand<string>(ExplorerIntegrationMethod);
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
                ProcessUtils.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
            }
        });
        Logger.ForDebugEvent().Message("Delegates Initialized for MainWindow").Log();
    }

    private static void ExplorerIntegrationMethod(string isIntegrationEnabled)
    {
        var value = Convert.ToBoolean(isIntegrationEnabled);
        if (value) ProcessUtils.AddToContextMenu();
        else ProcessUtils.RemoveFromContextMenu();
    }

    private void DeleteMediaInfo()
    {
        Logger.Debug("DeleteMediaInfoMethod Called.");
        try
        {
            if (Directory.Exists(SelectedFolder))
            {
                if (MessageBox.Show(CustomMessageBox.Ask(LangProvider.GetLang("DeleteMediaInfoConfirmation"),
                        LangProvider.GetLang("ConfirmMediaInfoDeletion"))) == MessageBoxResult.Yes)
                {
                    FileUtils.DeleteMediaInfoFromSubfolders(SelectedFolder);
                }
            }
            else
            {
                Logger.Debug("DeleteMediaInfoMethod: Directory does not exist: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("DirectoryIsEmpty"),
                    LangProvider.GetLang("EmptyDirectory")));
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("DeleteMediaInfoMethod: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, LangProvider.GetLang("ExceptionOccurred")));
        }
        
    }

    private void InitializeProperties()
    {
        Logger.Debug("Initializing Properties for MainWindow.");
        Fnames = new List<string>();
        BusyIndicatorProperties = new ProgressBarData
        {
            Max = 100,
            Value = 0,
            Text = LangProvider.GetLang("DownloadIt")
        };
        StatusBarProperties = new StatusBarData
        {
            NetIcon = NetworkUtils.IsNetworkAvailable() ? @"\Resources\icons\Strong-WiFi.png" : @"\Resources\icons\No-WiFi.png",
            TotalIcons = 0,
            AppStatus = "Idle",
            AppStatusAdditional = ""
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
        _pickedListDataTable = [];
        if (NetworkUtils.IsNetworkAvailable())
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
        Logger.Debug("Initializing PickedListDataTable.");
        _pickedListDataTable.Clear();
        Logger.Debug("PickedListDataTable Initialized.");
    }

    private void DeleteIconsMethod()
    {
        Logger.Debug("DeleteIconsMethod Called.");
        try
        {
            if (Directory.Exists(SelectedFolder))
            {
                //TODO: Replace with DialogService if efficient.
                if (MessageBox.Show(CustomMessageBox.Ask(LangProvider.GetLang("DeleteIconsConfirmation"),
                        LangProvider.GetLang("ConfirmIconDeletion"))) == MessageBoxResult.Yes)
                {
                    FileUtils.DeleteIconsFromSubfolders(SelectedFolder);
                }
            }
            else
            {
                Logger.Debug("DeleteIconsMethod: Directory does not exist: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("DirectoryIsEmpty"),
                    LangProvider.GetLang("EmptyDirectory")));
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("DeleteIconsMethod: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, LangProvider.GetLang(" ")));
        }
       
    }

    private void ListViewDoubleClickMethod()
    {
        var selectedItem = FinalListViewData.SelectedItem;

        if (selectedItem == null)
        {
            Logger.Warn("No item was selected in the ListView.");
            return;
        }
        
        var folder = selectedItem.Folder;
        Logger.Info("Double Clicked on ListView, selected folder: {SelectedFolder}",SelectedFolder);
        if (folder == null) return;
        if (Directory.Exists(folder))
        {
            ProcessUtils.StartProcess(folder + Path.DirectorySeparatorChar);
        }
    }

    private async Task StartDownloadingAsync()
    {
        IsMakeEnabled = false;
        StatusBarProperties.AppStatus = LangProvider.GetLang("Creating Icons");
        await DownloadAndMakeIconsAsync();
        StatusBarProperties.AppStatus = "Idle";
        StatusBarProperties.AppStatusAdditional = "";
    }

    private async Task DownloadAndMakeIconsAsync()
    {
        StopIconDownload = false;
        IsBusy = true;
        var i = 0;
        Logger.Debug("Start Downloading Icons. Total Icons: {ImgDownloadCount}", _imgDownloadList.Count);
        foreach (var img in _imgDownloadList)
        {
            if (StopIconDownload)
            {
                Logger.Debug("StopIconDownload is true, breaking loop and making icons.");
                MakeIcons();
                IsMakeEnabled = true;
                StatusBarProperties.ProgressBarData.Value = StatusBarProperties.ProgressBarData.Max;
                return;
            }

            try
            {
                await NetworkUtils.DownloadImageFromUrlAsync(img.RemotePath, img.LocalPath);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.ForExceptionEvent(e).Message("UnauthorizedAccessException Occurred while downloading image from url." +
                                                    " message: {Message}", e.Message)
                    .Property("Image", img)
                    .Log();
                MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("FailedFileAccessAt").Format(Directory.GetParent(img.LocalPath)),
                    LangProvider.GetLang("UnauthorizedAccess")));
                continue;
            }
            i += 1;
            BusyIndicatorProperties.Text = LangProvider.GetLang("DownloadingIconWithCount")
                .Format(i, BusyIndicatorProperties.Max);
            BusyIndicatorProperties.Value = i;
            StatusBarProperties.ProgressBarData.Value = i;
        }

        IsBusy = false;
        if (StatusBarProperties.ProgressBarData.Value == StatusBarProperties.ProgressBarData.Max)
        {
            Logger.Debug("All Icons Downloaded, Making Icons.");
            IsBusy = true;
            MakeIcons();
        }

        IsMakeEnabled = true;
    }

    private void MakeIcons()
    {
        IsBusy = true;
        var iconProcessedCount = IconUtils.MakeIco(IconMode, SelectedFolder, _pickedListDataTable, IsRatingVisible, IsPosterMockupUsed);
        StatusBarProperties.ProcessedIcon = iconProcessedCount;
        IsBusy = false;
        var info = new GrowlInfo
        {
            Message = LangProvider.GetLang("IconCreatedWithCount").Format(iconProcessedCount),
            ShowDateTime = false,
            StaysOpen = false,
            ConfirmStr = LangProvider.GetLang("Confirm")
        };
        Growl.SuccessGlobal(info);
        switch (MessageBox.Show(
                    CustomMessageBox.Ask($"{LangProvider.GetLang("IconReloadMayTakeTime")} {Environment.NewLine}{LangProvider.GetLang("ToForceReload")} {Environment.NewLine}{LangProvider.GetLang("ConfirmToOpenFolder")}",
                        LangProvider.GetLang("IconCreated"))))
        {
            case MessageBoxResult.Yes:
                ProcessUtils.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
                break;
        }
    }

    private void InitializeClientObjects()
    {
        Logger.Debug("Initializing Client Objects.");
        FileUtils.ReadApiConfiguration(out _tmdbapiKey, out _igdbClientId, out _igdbClientSecret, out _devClientSecret,
            out _devClientId);
        if (string.IsNullOrEmpty(_tmdbapiKey)
            || string.IsNullOrEmpty(_igdbClientId) || string.IsNullOrEmpty(_igdbClientSecret)
            || string.IsNullOrEmpty(_devClientSecret) || string.IsNullOrEmpty(_devClientId))
        {
            Logger.Warn("API Keys not provided, Showing API Config Window.");
            _dialogService.ShowApiConfig(r =>
            {
                if (r.Result != ButtonResult.Cancel)
                {
                    FileUtils.ReadApiConfiguration(out _tmdbapiKey, out _igdbClientId, out _igdbClientSecret, out _devClientSecret,
                        out _devClientId);
                    return;
                }

                MessageBox.Show(CustomMessageBox.Error($"{LangProvider.GetLang("APIKeysNotProvided")}{Environment.NewLine}" +
                                                       LangProvider.GetLang("AppWillClose"),
                    LangProvider.GetLang("ClosingApplication")));
                Logger.Warn("API Keys not provided, Closing Application.");
                Environment.Exit(0);
            });
        }
        _tmdbClient = new TMDbClient(_tmdbapiKey);
        _igdbClient = new IGDBClient(_igdbClientId, _igdbClientSecret, new IgdbJotTrackerStore());
        _igdbObject = new IgdbClass(ref _pickedListDataTable, ref _igdbClient, ref _imgDownloadList);
        _tmdbObject = new Tmdb(ref _tmdbClient, ref _pickedListDataTable, ref _imgDownloadList);
        _dArtObject = new DArt(_devClientSecret, _devClientId);
        Logger.Debug("Client Objects Initialized.");
    }

    private void AboutMethod()
    {
        _dialogService.ShowAboutBox(_ => { });
    }

    public void OnFileDrop(string[] filePaths, string senderName)
    {
        SelectedFolder = filePaths.GetValue(0)?.ToString();
        StatusBarProperties.ResetData();
        IsMakeEnabled = true;
        Logger.Debug("Folder Dropped on MainWindow: {SelectedFolder}", SelectedFolder);
    }

    public void Dispose()
    {
        _tmdbClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}