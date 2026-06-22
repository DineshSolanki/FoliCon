using FoliCon.Models.Configs;
using HandyControl.Themes;

namespace FoliCon.ViewModels;

public sealed class MainWindowViewModel : BindableBase, IFileDragDropTarget, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string professionalMode = "Professional";

    #region Variables

    private TMDbClient _tmdbClient;
    private IgdbClass _igdbObject;
    private Tmdb _tmdbObject;
    private DArt _dArtObject;

    private List<ImageToDownload> _imgDownloadList = [];
    private List<PickedListItem> _pickedListDataTable = [];
    private bool IsObjectsInitialized { get; set; }

    public bool IsPosterWindowShown
    {
        get;
        set
        {
            IsSkipAmbiguousEnabled = !value;
            SetProperty(ref field, value);
        }
    }

    public bool IsSkipAmbiguousEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool EnableErrorReporting
    {
        get;
        set
        {
            LogManager.Configuration.SentryConfig(value);
            SetProperty(ref field, value);
        }
    }

    public bool IsSearchModeVisible
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public ListViewData FinalListViewData
    {
        get;
        private set => SetProperty(ref field, value);
    }

    private readonly IDialogService _dialogService;
    public StatusBarData StatusBarProperties { get; set; }

    public ProgressBarData BusyIndicatorProperties
    {
        get;
        set => SetProperty(ref field, value);
    }

    private List<string> Fnames { get; set; } = [];

    public bool IsMakeEnabled
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsSkipAmbiguous
    {
        get;
        set => SetProperty(ref field, value);
    }

    private bool StopIconDownload
    {
        get;
        set => SetProperty(ref field, value);
    }

    private string IconMode
    {
        get;
        set
        {
            SetProperty(ref field, value);
            IsSearchModeVisible = value != professionalMode;
        }
    } = "Poster";

    private string SearchMode
    {
        get;
        set => SetProperty(ref field, value);
    } = MediaTypes.movie;

    #endregion Variables

    #region GetterSetters

    public string SelectedFolder
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value != null)
            {
                DirectoryPermissionsResult = FileUtils.CheckDirectoryPermissions(value);
            }
        }
    }

    public DirectoryPermissionsResult DirectoryPermissionsResult
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    } = "FoliCon";

    public bool IsRatingVisible
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsPosterMockupUsed
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsBusy
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Languages AppLanguage
    {
        get;
        set => SetProperty(ref field, value);
    }

    public FoliconThemes Theme
    {
        get;
        set
        {
            SetProperty(ref field, value);
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

    public bool IncludeAlreadyProcessed
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool ProcessSelectedFolder
    {
        get;
        set => SetProperty(ref field, value);
    }

    #endregion GetterSetters

    #region DelegateCommands

    #region MenuItem Commands

    #region SettingMenu

    public DelegateCommand SetupWizardCommand { get; set; }
    public DelegateCommand PosterIconConfigCommand { get; set; }
    public DelegateCommand SubfolderProcessingConfigCommand { get; set; }

    #endregion SettingMenu

    public DelegateCommand RestartExCommand { get; } = new(delegate
    {
        if (MessageBox.Show(CustomMessageBox.Ask(Lang.RestartExplorerConfirmation,
                Lang.ConfirmExplorerRestart)) != MessageBoxResult.Yes)
        {
            return;
        }

        Task.Run(async () => await RefreshAndRestart());

    });

    private static async Task RefreshAndRestart()
    {
        await ProcessUtils.RefreshIconCacheAsync();
        ProcessUtils.RestartExplorer();
    }

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
        _dialogService = dialogService;
        Logger.Info("Application Started, Initializing MainWindowViewModel.");
        ProcessUtils.CheckWebView2();
        TrackProperties();
        SetCultureInfo();
        InitializeDelegates();
        InitializeProperties();
        NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        ProcessCommandLineArgs();
    }

    private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
    {
        Logger.Debug("Network Availability Changed, Updating StatusBar.");
        StatusBarProperties.NetIcon =
            ApplicationHelper.IsConnectedToInternet() ? @"\Resources\icons\Strong-WiFi.png" : @"\Resources\icons\No-WiFi.png";
    }

    private void LoadMethod()
    {
        var folderBrowserDialog = DialogUtils.NewFolderBrowserDialog(Lang.SelectFolder);
        if (!SelectedFolder.IsNullOrEmpty())
        {
            folderBrowserDialog.SelectedPath = SelectedFolder;
            Logger.Debug("LoadMethod:Already Selected Folder: {SelectedFolder}", SelectedFolder);
        }
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult != null && (bool)!dialogResult)
        {
            return;
        }

        SelectedFolder = folderBrowserDialog.SelectedPath;
        Logger.Debug("LoadMethod:Selected Folder: {SelectedFolder}", SelectedFolder);
        StatusBarProperties.ResetData();
        IsMakeEnabled = true;
    }

    #region SearchAndMake

    private async void SearchAndMakeMethod()
    {
        Logger.Debug("SearchAndMakeMethod Called.");
        try
        {
            if (!ValidateFolder())
            {
                return;
            }

            if (!ValidateNetwork())
            {
                return;
            }

            if (!IsServiceAvailableForCurrentMode())
            {
                return;
            }

            await InitializeClientIfRequired();

            PrepareForSearchOperations();

            if (IconMode == "Poster")
            {
                await ProcessPosterModeAsync();
            }
            else
            {
                ProcessProfessionalMode(SelectedFolder);
            }

            Logger.Debug("SearchAndMakeMethod: Start Downloading Icons. Total Icons: {ImgTotalIcons}",
                _imgDownloadList.Count);
            await DownloadIconsOrEnableMake();
        }
        catch (Exception e)
        {
            HandleException(e);
        }
    }

    /// <summary>
    /// Checks whether the required API service is configured for the current search mode.
    /// Shows a warning if the service is not available and returns false.
    /// </summary>
    private bool IsServiceAvailableForCurrentMode()
    {
        if (IconMode == "Professional")
        {
            if (_dArtObject != null)
            {
                return true;
            }
            Logger.Warn("DeviantArt not configured. Professional mode unavailable.");
            MessageBox.Show(CustomMessageBox.Warning(
                Lang.ServiceNotConfigured.Format("DeviantArt"),
                Lang.Configuration));
            return false;
        }

        // Poster mode
        if (SearchMode == MediaTypes.game)
        {
            if (_igdbObject != null)
            {
                return true;
            }
            Logger.Warn("IGDB not configured. Game search unavailable.");
            MessageBox.Show(CustomMessageBox.Warning(
                Lang.ServiceNotConfigured.Format("IGDB/Twitch"),
                Lang.Configuration));
            return false;
        }

        // Movie, TV, Auto — all require TMDB
        if (_tmdbObject != null)
        {
            return true;
        }
        Logger.Warn("TMDB not configured. Movie/TV search unavailable.");
        MessageBox.Show(CustomMessageBox.Warning(
            Lang.ServiceNotConfigured.Format("TMDB"),
            Lang.Configuration));
        return false;

    }

    private bool ValidateFolder()
    {
        if (Directory.Exists(SelectedFolder))
        {
            Fnames = FileUtils.GetFolderNames(SelectedFolder, IncludeAlreadyProcessed);
            var isProcessSelectedFolderEffective = ProcessSelectedFolder
                && (IncludeAlreadyProcessed || !FileUtils.FileExists(Path.Combine(SelectedFolder, $"{IconUtils.GetImageName()}.ico")));
            if (Fnames.Count != 0 || Services.Settings.SubfolderProcessingEnabled || isProcessSelectedFolderEffective)
            {
                return true;
            }

            Logger.Warn("SearchAndMakeMethod: Folder is empty: {SelectedFolder}", SelectedFolder);
            MessageBox.Show(CustomMessageBox.Warning(Lang.IconsAlready,
                Lang.FolderError));
            return false;
        }

        Logger.Error("Folder does not exist: {SelectedFolder}", SelectedFolder);
        MessageBox.Show(CustomMessageBox.Error(Lang.FolderDoesNotExist,
            Lang.InvalidPath));
        return false;
    }

    private static bool ValidateNetwork()
    {
        if (NetworkUtils.IsNetworkAvailable())
        {
            return true;
        }

        MessageBox.Show(CustomMessageBox.Error(Lang.NoInternet,
            Lang.NetworkError));
        return false;

    }

    private async Task InitializeClientIfRequired()
    {
        if (!IsObjectsInitialized)
        {
            Logger.Warn("SearchAndMakeMethod: Client Objects not initialized, Initializing now.");
            await InitializeClientObjects();
            IsObjectsInitialized = true;
            Logger.Info("SearchAndMakeMethod: Client Objects Initialized.");
        }
    }

    private void PrepareForSearchOperations()
    {
        StatusBarProperties.ResetData();
        FinalListViewData.Data.Clear();
        PrepareForSearch();
    }

    private async Task DownloadIconsOrEnableMake()
    {
        StatusBarProperties.TotalIcons = BusyIndicatorProperties.Max =
            StatusBarProperties.ProgressBarData.Max = _imgDownloadList.Count;
        BusyIndicatorProperties.Text =
            Lang.DownloadingIconWithCount.Format(1, _imgDownloadList.Count);

        if (_imgDownloadList.Count > 0)
        {
            await StartDownloadingAsync();
        }
        else
        {
            IsMakeEnabled = true;
        }
    }

    private void HandleException(Exception e)
    {
        Logger.ForErrorEvent().Message("SearchAndMakeMethod: Exception Occurred. message: {Message}", e.Message)
            .Exception(e).Log();
        MessageBox.Show(CustomMessageBox.Error(e.Message, Lang.ExceptionOccurred));
        StatusBarProperties.ResetData();
        IsMakeEnabled = true;
        IsBusy = false;
    }

    #endregion
    private async Task ProcessPosterModeAsync()
    {
        Logger.Debug("Entered ProcessPosterModeAsync method");
        IsMakeEnabled = false;
        await ProcessPosterFolderAsync(SelectedFolder);

        StatusBarProperties.AppStatus = Lang.Idle;
        StatusBarProperties.AppStatusAdditional = "";
    }

    private async Task ProcessPosterFolderAsync(string rootFolderPath)
    {
        var folderQueue = new Queue<(string path, int depth)>();
        folderQueue.Enqueue((rootFolderPath, 0));
        var maxDepth = Services.Settings.SubfolderDepthLimit;
        var isRootProcessed = false;

        while (folderQueue.Count > 0)
        {
            var (folderPath, depth) = folderQueue.Dequeue();
            var subfolderNames = FileUtils.GetFolderNames(folderPath, IncludeAlreadyProcessed);

            var (skipAll, processed) = await TryProcessSelectedFolder(rootFolderPath, folderPath, isRootProcessed);
            isRootProcessed |= processed;
            if (skipAll)
            {
                break;
            }

            if (subfolderNames.Count == 0)
            {
                continue;
            }

            StatusBarProperties.TotalFolders += subfolderNames.Count;
            skipAll = await ProcessFolderItems(folderPath, subfolderNames);
            if (skipAll)
            {
                break;
            }

            if (Services.Settings.SubfolderProcessingEnabled && (maxDepth == 0 || depth + 1 < maxDepth))
            {
                ProcessSubfolders(folderPath, folderQueue, depth + 1);
            }
        }
    }

    private async Task<(bool skipAll, bool rootProcessed)> TryProcessSelectedFolder(string rootFolderPath, string folderPath, bool isRootProcessed)
    {
        if (isRootProcessed || !ProcessSelectedFolder || folderPath != rootFolderPath)
        {
            return (false, false);
        }
        if (!IncludeAlreadyProcessed && FileUtils.FileExists(Path.Combine(folderPath, $"{IconUtils.GetImageName()}.ico")))
        {
            return (false, true);
        }

        var folderName = Path.GetFileName(folderPath);
        var parentPath = Path.GetDirectoryName(folderPath);
        if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(parentPath))
        {
            return (false, true);
        }

        StatusBarProperties.TotalFolders++;
        var skipAll = await ProcessItemAsync(parentPath, folderName);
        return (skipAll, true);
    }

    private async Task<bool> ProcessFolderItems(string folderPath, List<string> subfolderNames)
    {
        foreach (var itemTitle in subfolderNames)
        {
            if (await ProcessItemAsync(folderPath, itemTitle))
            {
                return true;
            }
        }
        return false;
    }

    private async Task<bool> ProcessItemAsync(string folderPath, string itemTitle)
    {
        var fullFolderPath = Path.Combine(folderPath, itemTitle);
        Logger.Debug("Processing Folder: {FullFolderPath}", fullFolderPath);

        var (response, parsedTitle, isPickedById, mediaType) = await PerformPreprocessing(itemTitle, fullFolderPath);
        var resultCount = CalculateResultCount(response, isPickedById);
        Logger.Info("Search Result Count: {ResultCount}", resultCount);

        var (dialogResult, skipAll) = await ProcessResultsAsync(resultCount, itemTitle, response, fullFolderPath,
            parsedTitle.Title, isPickedById, mediaType);

        if (dialogResult)
        {
            AddToFinalList(itemTitle);
        }

        StatusBarProperties.ProcessedFolder++;
        return skipAll;
    }

    private async Task<(bool dialogResult, bool skipAll)> ProcessResultsAsync(int resultCount, string itemTitle,
        dynamic response, string fullFolderPath, string parsedTitle, bool isPickedById, string mediaType)
    {
        switch (resultCount)
        {
            case 0 when !IsSkipAmbiguous:
                return await ProcessNoResultCase(itemTitle, response, fullFolderPath, parsedTitle, isPickedById);
            case 1 when !IsPosterWindowShown:
                ProcessSingleResultCase(itemTitle, response, fullFolderPath, isPickedById, mediaType);
                return (true, false);
            default:
                return resultCount >= 1
                    ? await ProcessMultipleResultCase(itemTitle, response, fullFolderPath, parsedTitle, isPickedById)
                    : (false, false);
        }
    }

    private void AddToFinalList(string itemTitle)
    {
        Logger.Debug("Adding to final list for {ItemTitle}", itemTitle);
        if (_pickedListDataTable is not null && _pickedListDataTable.Count != 0)
        {
            FinalListViewData.Data.Add(_pickedListDataTable[^1]);
        }
    }

    private static void ProcessSubfolders(string folderPath, Queue<(string path, int depth)> folderQueue, int depth)
    {
        Logger.Trace("Subfolder Processing Enabled, Processing Subfolders.");
        var folders = FileUtils.GetAllSubFolders(folderPath, Services.Settings.Patterns);
        foreach (var folder in folders)
        {
            folderQueue.Enqueue((folder, depth));
        }
    }

    private async Task<(ResultResponse response, ParsedTitle parsedTitle, bool isPickedById, string mediaType)> PerformPreprocessing(string itemTitle, string fullFolderPath)
    {
        StatusBarProperties.AppStatus = Lang.Searching;
        StatusBarProperties.AppStatusAdditional = itemTitle;
        var parsedTitle = TitleCleaner.CleanAndParse(itemTitle);
        var (id, mediaType) = FileUtils.ReadMediaInfo(fullFolderPath);
        var isPickedById = false;
        ResultResponse response;
        if (id != null && mediaType != null)
        {
            Logger.Info("MediaInfo found for {ItemTitle}, mediaType: {MediaType}, id: {Id}", itemTitle, mediaType, id);
            isPickedById = true;
            response = mediaType == MediaTypes.game ? await _igdbObject.SearchGameByIdAsync(id) : await _tmdbObject.SearchByIdAsync(int.Parse(id), mediaType);
        }
        else
        {
            Logger.Info("MediaInfo not found for {ItemTitle}, Searching by Title", itemTitle);
            if (SearchMode == MediaTypes.game)
            {
                response = await _igdbObject.SearchGameAsync(parsedTitle.Title);
            }
            else
            {
                if (DataUtils.ShouldUseParsedTitle(parsedTitle))
                {
                    response = await _tmdbObject.SearchAsync(parsedTitle, SearchMode);
                }
                else
                {
                    response = await _tmdbObject.SearchAsync(parsedTitle.Title, SearchMode);
                }
            }
        }

        return (response, parsedTitle, isPickedById, mediaType);
    }

    private int CalculateResultCount(ResultResponse response, bool isPickedById)
    {
        if (isPickedById)
        {
            return response.Result != null ? 1 : 0;
        }

        return SearchMode == MediaTypes.game ? response.Result.Length : response.Result.TotalResults;
    }

    private async Task<(bool dialogResult, bool skipAll)> ProcessNoResultCase(
        string itemTitle, ResultResponse response, string fullFolderPath,
        string parsedTitle, bool isPickedById)
    {
        Logger.Debug("No result found for {ItemTitle}, {Mode}", itemTitle, SearchMode);
        MessageBox.Show(CustomMessageBox.Info(Lang.NothingFoundFor.Format(itemTitle),
            Lang.NoResultFound));

        return await ShowSearchResultDialog(parsedTitle, fullFolderPath, response, isPickedById);
    }

    private void ProcessSingleResultCase(string itemTitle, ResultResponse response, string fullFolderPath,
        bool isPickedById, string mediaType)
    {
        Logger.Debug("One result found for {ItemTitle}, {Mode}, as always show poster window is not enabled, directly selecting",
            itemTitle, SearchMode);
        try
        {
            if (isPickedById ? mediaType == MediaTypes.game : SearchMode == MediaTypes.game)
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
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("ProcessPosterModeAsync: Exception Occurred. message: {Message}", ex.Message)
                .Exception(ex).Log();
            if (ex.Message == "NoPoster")
            {
                MessageBox.Show(CustomMessageBox.Warning(Lang.NoPosterFound, itemTitle));
            }
#if DEBUG
            MessageBox.Show(CustomMessageBox.Warning(ex.Message, Lang.ExceptionOccurred));
#endif
        }
    }

    private async Task<(bool dialogResult, bool skipAll)> ProcessMultipleResultCase(
        string itemTitle, ResultResponse response, string fullFolderPath,
        string parsedTitle, bool isPickedById)
    {
        if (!IsPosterWindowShown && IsSkipAmbiguous)
        {
            return (false, false);
        }

        Logger.Debug("More than one result found for {ItemTitle}, {Mode}," +
                     "always show poster window: {IsPosterWindowShown}, Skip ambiguous titles: {IsSkipAmbiguous}," +
                     " showing poster window", itemTitle, SearchMode, IsPosterWindowShown, IsSkipAmbiguous);

        return await ShowSearchResultDialog(parsedTitle, fullFolderPath, response, isPickedById);
    }

    private async Task<(bool dialogResult, bool skipAll)> ShowSearchResultDialog(
        string parsedTitle, string fullFolderPath, ResultResponse response, bool isPickedById)
    {
        var taskCompletionSource = new TaskCompletionSource<(bool dialogResult, bool skipAll)>();

        var dialogParams = new SearchResultDialogParams
        {
            SearchMode = SearchMode,
            Query = parsedTitle,
            FolderPath = fullFolderPath,
            Result = response,
            TmdbObject = _tmdbObject,
            IgdbObject = _igdbObject,
            IsPickedById = isPickedById,
            CallBack = dialog =>
            {
                var dialogResult = dialog.Result switch
                {
                    ButtonResult.None => false,
                    ButtonResult.OK => true,
                    ButtonResult.Cancel => false,
                    _ => false
                };
                dialog.Parameters.TryGetValue<bool>("skipAll", out var skipAll);
                taskCompletionSource.SetResult((dialogResult, skipAll));
            }
        };
        _dialogService.ShowSearchResult(dialogParams);

        return await taskCompletionSource.Task;
    }

    private void ProcessProfessionalMode(string initialFolderPath)
    {
        Logger.Debug("Entered ProcessProfessionalMode method");
        var foldersQueue = new Queue<(string path, int depth)>();
        foldersQueue.Enqueue((initialFolderPath, 0));
        var maxDepth = Services.Settings.SubfolderDepthLimit;
        var isRootProcessed = false;

        while (foldersQueue.Count > 0)
        {
            var (folderPath, depth) = foldersQueue.Dequeue();
            Logger.Trace($"Processing Folder: {folderPath}");
            var subfolderNames = FileUtils.GetFolderNames(folderPath, IncludeAlreadyProcessed);

            TryIncludeSelectedFolder(subfolderNames, initialFolderPath, folderPath, ref isRootProcessed);

            if (subfolderNames.Count == 0)
            {
                continue;
            }

            ProcessProfessionalFolder(folderPath, subfolderNames);
            EnqueueSubfoldersIfEnabled(folderPath, depth, maxDepth, foldersQueue);
        }
    }

    private void TryIncludeSelectedFolder(List<string> subfolderNames, string initialFolderPath, string folderPath, ref bool isRootProcessed)
    {
        if (isRootProcessed || !ProcessSelectedFolder || folderPath != initialFolderPath)
        {
            return;
        }
        if (!IncludeAlreadyProcessed && FileUtils.FileExists(Path.Combine(folderPath, $"{IconUtils.GetImageName()}.ico")))
        {
            isRootProcessed = true;
            return;
        }
        isRootProcessed = true;
        var folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(folderName))
        {
            subfolderNames.Add(folderName);
        }
    }

    private void ProcessProfessionalFolder(string folderPath, List<string> subfolderNames)
    {
        StatusBarProperties.TotalFolders += subfolderNames.Count;
        StatusBarProperties.AppStatus = Lang.Searching;
        _dialogService.ShowProSearchResult(folderPath, subfolderNames, _pickedListDataTable, _imgDownloadList,
            _dArtObject, _ => { });
        Logger.Debug("ProcessProfessionalMode: found {ResultCount} results, adding to final list", _pickedListDataTable.Count);
        FinalListViewData.Data.AddRange(_pickedListDataTable);
        StatusBarProperties.ProcessedFolder = _pickedListDataTable.Count;
    }

    private static void EnqueueSubfoldersIfEnabled(string folderPath, int depth, int maxDepth, Queue<(string path, int depth)> foldersQueue)
    {
        if (!Services.Settings.SubfolderProcessingEnabled || (maxDepth != 0 && depth + 1 >= maxDepth))
        {
            return;
        }
        Logger.Trace("Subfolder Processing Enabled, Adding Subfolders.");
        var subFolders = FileUtils.GetAllSubFolders(folderPath, Services.Settings.Patterns);
        foreach (var subFolder in subFolders)
        {
            foldersQueue.Enqueue((subFolder, depth + 1));
        }
    }

    private void InitializeDelegates()
    {
        Logger.Debug("Initializing Delegates for MainWindow.");
        SetupWizardCommand = new DelegateCommand(delegate
        {
            _dialogService.ShowOnboardingWizard(async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    await ReinitializeDeviantArtAsync();
                }
            });
        });
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
        ShowPreviewer = new DelegateCommand(() => { _dialogService.ShowPreviewer(_ => { }); });
        Logger.ForDebugEvent().Message("Delegates Initialized for MainWindow").Log();
    }

    private static void ExplorerIntegrationMethod(string isIntegrationEnabled)
    {
        var value = Convert.ToBoolean(isIntegrationEnabled);
        if (value)
        {
            ProcessUtils.AddToContextMenu();
        }
        else
        {
            ProcessUtils.RemoveFromContextMenu();
        }
    }

    private void DeleteMediaInfo()
    {
        Logger.Debug("DeleteMediaInfoMethod Called.");
        try
        {
            if (Directory.Exists(SelectedFolder))
            {
                if (MessageBox.Show(CustomMessageBox.Ask(Lang.DeleteMediaInfoConfirmation,
                        Lang.ConfirmMediaInfoDeletion)) == MessageBoxResult.Yes)
                {
                    FileUtils.DeleteMediaInfoFromSubfolders(SelectedFolder);
                }
            }
            else
            {
                Logger.Debug("DeleteMediaInfoMethod: Directory does not exist: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Error(Lang.DirectoryIsEmpty,
                    Lang.EmptyDirectory));
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("DeleteMediaInfoMethod: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, Lang.ExceptionOccurred));
        }

    }

    private async Task InitializeProperties()
    {
        Logger.Debug("Initializing Properties for MainWindow.");
        BusyIndicatorProperties = new ProgressBarData
        {
            Max = 100,
            Value = 0,
            Text = Lang.DownloadIt
        };
        StatusBarProperties = new StatusBarData
        {
            NetIcon = NetworkUtils.IsNetworkAvailable() ? @"\Resources\icons\Strong-WiFi.png" : @"\Resources\icons\No-WiFi.png",
            TotalIcons = 0,
            AppStatus = Lang.Idle,
            AppStatusAdditional = "",
            ProgressBarData =
            {
                Max = 100,
                Value = 0
            }
        };
        FinalListViewData = new ListViewData
        {
            Data = [],
            SelectedItem = new ListItem(),
            SelectedCount = 0
        };
        try
        {
            if (NetworkUtils.IsNetworkAvailable())
            {
                await InitializeClientObjects();
                IsObjectsInitialized = true;
            }
            else
            {
                IsObjectsInitialized = false;
            }
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Failed to initialize client objects").Exception(ex).Log();
            IsObjectsInitialized = false;
        }
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

    private async void DeleteIconsMethod()
    {
        Logger.Debug("DeleteIconsMethod Called.");
        try
        {
            if (Directory.Exists(SelectedFolder))
            {
                if (MessageBox.Show(CustomMessageBox.Ask(Lang.DeleteIconsConfirmation,
                        Lang.ConfirmIconDeletion)) == MessageBoxResult.Yes)
                {
                    await FileUtils.DeleteIconsFromSubfoldersAsync(SelectedFolder);
                }
            }
            else
            {
                Logger.Debug("DeleteIconsMethod: Directory does not exist: {SelectedFolder}", SelectedFolder);
                MessageBox.Show(CustomMessageBox.Error(Lang.DirectoryIsEmpty,
                    Lang.EmptyDirectory));
            }
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("DeleteIconsMethod: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, Lang.ExceptionOccurred));
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
        if (folder == null)
        {
            return;
        }

        if (Directory.Exists(folder))
        {
            ProcessUtils.StartProcess(folder + Path.DirectorySeparatorChar);
        }
    }

    private async Task StartDownloadingAsync()
    {
        IsMakeEnabled = false;
        StatusBarProperties.AppStatus = Lang.CreatingIcons;
        await DownloadAndMakeIconsAsync();
        StatusBarProperties.AppStatus = Lang.Idle;
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
                await MakeIcons();
                IsMakeEnabled = true;
                StatusBarProperties.ProgressBarData.Value = StatusBarProperties.ProgressBarData.Max;
                return;
            }

            try
            {
                if (img.RemotePath.IsFile)
                {
                    File.Move(img.RemotePath.LocalPath, img.LocalPath, true);
                }
                else
                {
                    await NetworkUtils.DownloadImageFromUrlAsync(img.RemotePath, img.LocalPath);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.ForExceptionEvent(e).Message("UnauthorizedAccessException Occurred while downloading image from url." +
                                                    " message: {Message}", e.Message)
                    .Property("Image", img)
                    .Log();
                MessageBox.Show(CustomMessageBox.Error(Lang.FailedFileAccessAt.Format(Directory.GetParent(img.LocalPath)),
                    Lang.UnauthorizedAccess));
                continue;
            }
            i += 1;
            BusyIndicatorProperties.Text = Lang.DownloadingIconWithCount
                .Format(i, BusyIndicatorProperties.Max);
            BusyIndicatorProperties.Value = i;
            StatusBarProperties.ProgressBarData.Value = i;
        }

        IsBusy = false;
        if (StatusBarProperties.ProgressBarData.Value == StatusBarProperties.ProgressBarData.Max)
        {
            Logger.Debug("All Icons Downloaded, Making Icons.");
            IsBusy = true;
            await MakeIcons();
        }

        IsMakeEnabled = true;
    }

    private async Task MakeIcons()
    {
        IsBusy = true;
        var iconProcessedCount = await Task.Run(()=>IconUtils.MakeIco(IconMode,
            SelectedFolder,
            _pickedListDataTable,
            IsRatingVisible,
            IsPosterMockupUsed,
            new Progress<ProgressBarData>(value => BusyIndicatorProperties = value),
            IncludeAlreadyProcessed));
        StatusBarProperties.ProcessedIcon = iconProcessedCount;
        IsBusy = false;
        var info = new GrowlInfo
        {
            Message = Lang.IconCreatedWithCount.Format(iconProcessedCount),
            ShowDateTime = false,
            StaysOpen = false,
            ConfirmStr = Lang.Confirm
        };
        Growl.SuccessGlobal(info);
        if (MessageBox.Show(
                CustomMessageBox.Ask(
                    $"{Lang.IconReloadMayTakeTime} {Environment.NewLine}{Lang.ToForceReload} {Environment.NewLine}{Lang.ConfirmToOpenFolder}",
                    Lang.IconCreated)) == MessageBoxResult.Yes)
        {
            ProcessUtils.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
        }
    }

    private async Task InitializeClientObjects()
    {
        Logger.Debug("Initializing Client Objects.");

        if (!Services.Settings.OnboardingCompleted)
        {
            Logger.Info("Onboarding not completed, showing Setup Wizard.");
            _dialogService.ShowOnboardingWizard(_ => { });
            // Re-read config after wizard (user may have entered keys)
        }

        var (tmdbapiKey, igdbClientId, igdbClientSecret) = FileUtils.ReadApiConfiguration();

        // Initialize TMDB (optional)
        if (!string.IsNullOrEmpty(tmdbapiKey))
        {
            try
            {
                _tmdbClient = new TMDbClient(tmdbapiKey);
                _tmdbObject = new Tmdb(ref _tmdbClient, ref _pickedListDataTable, ref _imgDownloadList);
                Logger.Info("TMDB client initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize TMDB client. Movie/TV search will be unavailable.");
                _tmdbClient = null;
                _tmdbObject = null;
            }
        }
        else
        {
            Logger.Info("TMDB API key not configured. Movie/TV search will be unavailable.");
            _tmdbClient = null;
            _tmdbObject = null;
        }

        // Initialize IGDB (optional)
        if (!string.IsNullOrEmpty(igdbClientId) && !string.IsNullOrEmpty(igdbClientSecret))
        {
            try
            {
                var igdbClient = new IGDBClient(igdbClientId, igdbClientSecret, new IgdbJotTrackerStore());
                _igdbObject = new IgdbClass(ref _pickedListDataTable, ref igdbClient, ref _imgDownloadList);
                Logger.Info("IGDB client initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize IGDB client. Game search will be unavailable.");
                _igdbObject = null;
            }
        }
        else
        {
            Logger.Info("IGDB credentials not configured. Game search will be unavailable.");
            _igdbObject = null;
        }

        // Initialize DeviantArt (optional — uses stored OAuth tokens)
        var deviantArtAccessToken = Services.Settings.DeviantArtAccessToken;
        if (!string.IsNullOrEmpty(deviantArtAccessToken))
        {
            try
            {
                Logger.Debug("Initializing DeviantArt client with stored tokens...");
                _dArtObject = await DArt.GetInstanceAsync();
                Logger.Debug("DeviantArt client initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize DeviantArt client. Professional mode will be unavailable.");
                _dArtObject = null;

                MessageBox.Show(CustomMessageBox.Warning(
                    Lang.DAConnectionFailedMessage,
                    Lang.DAConnectionFailedTitle));
            }
        }
        else
        {
            Logger.Info("DeviantArt not connected. Professional mode will be unavailable.");
            _dArtObject = null;
        }

        Logger.Debug("Client Objects Initialized.");
    }

    /// <summary>
    /// Reinitializes the DeviantArt client after the setup wizard modifies OAuth tokens.
    /// Called when the user completes the setup wizard from the menu (not the first-launch wizard).
    /// </summary>
    private async Task ReinitializeDeviantArtAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(Services.Settings.DeviantArtAccessToken))
            {
                Logger.Info("Reinitializing DeviantArt client after setup wizard...");
                _dArtObject = await DArt.GetInstanceAsync();
                Logger.Info("DeviantArt client reinitialized successfully.");
            }
            else
            {
                _dArtObject = null;
                Logger.Info("DeviantArt not connected after setup wizard.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to reinitialize DeviantArt client after setup wizard.");
            _dArtObject = null;
        }
    }

    private void TrackProperties()
    {
        Services.Tracker.Configure<MainWindowViewModel>()
            .Property(p => p.IsRatingVisible, false)
            .Property(p => p.IsPosterMockupUsed, true)
            .Property(p => p.IsPosterWindowShown, false)
            .Property(p => p.AppLanguage, Languages.English)
            .Property(p => p.Theme, FoliconThemes.System)
            .Property(p => p.EnableErrorReporting, false)
            .Property(p => p.IncludeAlreadyProcessed, false)
            .Property(p => p.ProcessSelectedFolder, false)
            .PersistOn(nameof(PropertyChanged));
        Services.Tracker.Track(this);
    }

    private void SetCultureInfo()
    {
        var selectedLanguage = AppLanguage;
        var cultureInfo = CultureUtils.GetCultureInfoByLanguage(selectedLanguage);
        LangProvider.Culture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
    }

    private void ProcessCommandLineArgs()
    {
        var cmdArgs = ProcessUtils.GetCmdArgs();
        if (!cmdArgs.TryGetValue("path", out var selectedFolder))
        {
            return;
        }

        SelectedFolder = selectedFolder;
        var mode = cmdArgs["mode"];
        if (mode != professionalMode &&
            new List<string> { MediaTypes.mtv, MediaTypes.tv, MediaTypes.movie, MediaTypes.game }.Contains(mode))
        {
            IconMode = "Poster";
            SearchMode = mode;
        }
        else
        {
            IconMode = professionalMode;
        }

        Logger.Info("Command Line argument initialized, selected folder: {SelectedFolder}, mode: {IconMode}",
            SelectedFolder, IconMode);
        SearchAndMakeMethod();
    }

    private void AboutMethod() => _dialogService.ShowAboutBox(_ => { });

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
