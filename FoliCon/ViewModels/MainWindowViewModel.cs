using FoliCon.Models.Configs;
using FoliCon.Models.Themes;
using FoliCon.Modules.Convertor;
using FoliCon.Modules.Themes;
using HandyControl.Themes;

namespace FoliCon.ViewModels;

public sealed class MainWindowViewModel : BindableBase, IFileDragDropTarget, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static MainWindowViewModel()
    {
        try
        {
            LightBackdropBrush.Freeze();
            DarkBackdropBrush.Freeze();
            LightHeroCardBg.Freeze();
            DarkHeroCardBg.Freeze();
            LightHeroCardBorder.Freeze();
            DarkHeroCardBorder.Freeze();
            LightHeaderGradient.Freeze();
            DarkHeaderGradient.Freeze();
            PrimaryActionBrush.Freeze();

            void AttachThemeListener()
            {
                ThemeManager.Current.SystemThemeChanged += (_, _) =>
                {
                    if (ThemeManager.Current.UsingSystemTheme)
                    {
                        bool isDark = IsSystemThemeDark();
                        UpdateThemeBrushes(isDark, null);
                        WindowBackdropHelper.UpdateAllOpenWindows(isDark);
                    }
                };
            }

            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(AttachThemeListener);
            }
            else
            {
                AttachThemeListener();
            }
        }
        catch
        {
            // Fail safely in headless/test environments
        }
    }

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
    private readonly IThemeStorageService? _themeStorageService;
    private readonly IThemeParser? _themeParser;
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

    public ObservableCollection<ThemeOption> AvailableThemes { get; } = [];

    public bool IsPreviewingTheme { get; set; }

    private ThemeOption? _selectedThemeOption;
    public ThemeOption? SelectedThemeOption
    {
        get => _selectedThemeOption;
        set
        {
            if (SetProperty(ref _selectedThemeOption, value) && value != null)
            {
                ApplyThemeOption(value, isPreview: IsPreviewingTheme);
            }
        }
    }

    private string _customThemeId = string.Empty;
    public string CustomThemeId
    {
        get => _customThemeId;
        set => SetProperty(ref _customThemeId, value);
    }

    private bool _isSynchronizingTheme;

    private FoliconThemes _theme = FoliconThemes.System;
    public FoliconThemes Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
            {
                if (!_isSynchronizingTheme)
                {
                    ApplyAppTheme(value);
                }
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
    public DelegateCommand StoreCommand { get; private set; }
    public DelegateCommand OverlayStoreCommand { get; private set; }
    public DelegateCommand OverlayDesignerCommand { get; private set; }
    public DelegateCommand UpdateCommand { get; } = new(async void () =>
    {
        try
        {
            await FileUtils.CheckForUpdate();
        }
        catch (Exception e)
        {
            Logger.ForErrorEvent().Message("UpdateCommand: Exception Occurred. message: {Message}", e.Message)
                .Exception(e).Log();
            MessageBox.Show(CustomMessageBox.Error(e.Message, Lang.ExceptionOccurred));
        }
    });

    #endregion MenuItem Commands

    public DelegateCommand LoadCommand { get; private set; }
    public DelegateCommand SearchAndMakeCommand { get; private set; }
    public DelegateCommand<object> IconModeChangedCommand { get; private set; }
    public DelegateCommand<object> SearchModeChangedCommand { get; private set; }
    public DelegateCommand ListViewDoubleClickCommand { get; private set; }
    public DelegateCommand SelectedFolderDClickCommand { get; private set; }
    public DelegateCommand DownloadCancelCommand { get; private set; }
    public DelegateCommand<ListItem> RemoveListItemCommand { get; private set; }
    public DelegateCommand<string> CopyFolderCommand { get; private set; }
    public DelegateCommand ClearListCommand { get; private set; }
    public DelegateCommand<object> OpenFolderInExplorerCommand { get; private set; }

    #endregion DelegateCommands

    public MainWindowViewModel(
        IDialogService dialogService,
        IThemeStorageService? themeStorageService = null,
        IThemeParser? themeParser = null)
    {
        _dialogService = dialogService;
        _themeStorageService = themeStorageService;
        _themeParser = themeParser;

        ActiveThemeChanged += OnActiveThemeChanged;

        Logger.Info("Application Started, Initializing MainWindowViewModel.");
        try
        {
            ProcessUtils.CheckWebView2();
            TrackProperties();
            RefreshAvailableThemes();
            ApplyInitialTheme();
            SetCultureInfo();
            InitializeDelegates();
            InitializeProperties();
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            ProcessCommandLineArgs();
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Fatal error during MainWindowViewModel initialization");
        }
    }

    private void ApplyInitialTheme()
    {
        if (!string.IsNullOrEmpty(CustomThemeId))
        {
            var manifest = _themeStorageService?.GetThemeById(CustomThemeId);
            if (manifest != null)
            {
                ApplyThemeManifest(manifest, _themeParser ?? new HybridThemeParser());
                SyncSelectedThemeOptionWithActive(CustomThemeId);
                return;
            }
        }
        ApplyAppTheme(Theme);
        SyncSelectedThemeOptionWithActive();
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
        if (IsBusy)
        {
            Logger.Debug("SearchAndMakeMethod: Already busy, ignoring.");
            return;
        }

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

            StopIconDownload = false;
            IsBusy = true;
            BusyIndicatorProperties.Title = Lang.Searching;
            BusyIndicatorProperties.Text = Lang.Searching;
            BusyIndicatorProperties.Value = 0;
            StatusBarProperties.AppStatus = Lang.Searching;
            StatusBarProperties.AppStatusAdditional = "";

            if (IconMode == "Poster")
            {
                await ProcessPosterModeAsync();
            }
            else
            {
                ProcessProfessionalMode(SelectedFolder);
            }

            if (StopIconDownload)
            {
                Logger.Debug("Search cancelled by user; proceeding to download and make icons. Total Icons: {ImgTotalIcons}",
                    _imgDownloadList.Count);
            }
            else
            {
                Logger.Debug("Search completed. Total Icons: {ImgTotalIcons}",
                    _imgDownloadList.Count);
            }

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
            IsBusy = false;
            IsMakeEnabled = true;
            StatusBarProperties.AppStatus = Lang.Idle;
            StatusBarProperties.AppStatusAdditional = "";
            StatusBarProperties.ProgressBarData.Value = 0;
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
    }

    private async Task ProcessPosterFolderAsync(string rootFolderPath)
    {
        var folderQueue = new Queue<(string path, int depth)>();
        folderQueue.Enqueue((rootFolderPath, 0));
        var maxDepth = Services.Settings.SubfolderDepthLimit;
        var isRootProcessed = false;

        while (folderQueue.Count > 0)
        {
            if (StopIconDownload)
            {
                Logger.Debug("Search cancelled by user.");
                break;
            }

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
            if (StopIconDownload)
            {
                return true;
            }

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

        BusyIndicatorProperties.Text = $"{StatusBarProperties.ProcessedFolder + 1}/{StatusBarProperties.TotalFolders}: {itemTitle}";
        BusyIndicatorProperties.Value = StatusBarProperties.ProcessedFolder;
        BusyIndicatorProperties.Max = StatusBarProperties.TotalFolders;

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
                var picked = ProcessSingleResultCase(itemTitle, response, fullFolderPath, isPickedById, mediaType);
                return (picked, false);
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
        var parsedTitle = TitleCleaner.CleanAndParse(itemTitle, SearchMode);
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
        if (response is null)
        {
            return 0;
        }

        if (response.Result is null)
        {
            return 0;
        }

        if (isPickedById)
        {
            return 1;
        }

        var result = response.Result;
        return SearchMode == MediaTypes.game ? result.Length : (int)result.TotalResults;
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

    private bool ProcessSingleResultCase(string itemTitle, ResultResponse response, string fullFolderPath,
        bool isPickedById, string mediaType)
    {
        Logger.Debug("One result found for {ItemTitle}, {Mode}, as always show poster window is not enabled, directly selecting",
            itemTitle, SearchMode);
        try
        {
            if (isPickedById ? mediaType == MediaTypes.game : SearchMode == MediaTypes.game)
            {
                if (response.Result.Length == 0)
                {
                    Logger.Warn("Result reported 1 match but results array is empty for {ItemTitle}", itemTitle);
                    return false;
                }
                var result = response.Result[0];
                _igdbObject.ResultPicked(result, fullFolderPath);
            }
            else
            {
                if (!isPickedById && response.Result.Results.Count == 0)
                {
                    Logger.Warn("Result reported 1 match but Results list is empty for {ItemTitle}", itemTitle);
                    return false;
                }
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
            return false;
        }
        return true;
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
            if (StopIconDownload)
            {
                Logger.Debug("Professional search cancelled by user.");
                break;
            }

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
        SetupWizardCommand = new DelegateCommand(ShowOnboardingWizard);
        PosterIconConfigCommand = new DelegateCommand(() => _dialogService.ShowPosterIconConfig(_ => { }));
        SubfolderProcessingConfigCommand = new DelegateCommand(() => _dialogService.ShowSubfolderProcessingConfig(_ => { }));
        AboutCommand = new DelegateCommand(AboutMethod);
        DeleteIconsCommand = new DelegateCommand(DeleteIconsMethod);
        DeleteMediaInfoCommand = new DelegateCommand(DeleteMediaInfo);
        ExploreIntegrationCommand = new DelegateCommand<string>(ExplorerIntegrationMethod);
        CustomIconsCommand = new DelegateCommand(() => _dialogService.ShowCustomIconWindow(_ => { }));
        LoadCommand = new DelegateCommand(LoadMethod);
        SearchAndMakeCommand = new DelegateCommand(SearchAndMakeMethod);
        IconModeChangedCommand = new DelegateCommand<object>(parameter => IconMode = (string)parameter);
        SearchModeChangedCommand = new DelegateCommand<object>(parameter => SearchMode = (string)parameter);
        ListViewDoubleClickCommand = new DelegateCommand(ListViewDoubleClickMethod);
        DownloadCancelCommand = new DelegateCommand(() => StopIconDownload = true);
        SelectedFolderDClickCommand = new DelegateCommand(OpenSelectedFolder);
        RemoveListItemCommand = new DelegateCommand<ListItem>(RemoveListItem);
        CopyFolderCommand = new DelegateCommand<string>(CopyFolderPath);
        ClearListCommand = new DelegateCommand(() => FinalListViewData.Data.Clear());
        OpenFolderInExplorerCommand = new DelegateCommand<object>(OpenFolderInExplorer);
        ShowPreviewer = new DelegateCommand(() => _dialogService.ShowPreviewer(_ => { }));

        // Store discovery: unified community themes & icon overlays
        StoreCommand = new DelegateCommand(() =>
        {
            _dialogService.ShowFoliConStore(_ => OverlayPreviewCache.InvalidateAll(), tab: "themes");
        });
        OverlayStoreCommand = StoreCommand;
        OverlayDesignerCommand = new DelegateCommand(() =>
        {
            _dialogService.ShowOverlayDesigner(_ => OverlayPreviewCache.InvalidateAll());
        });

        Logger.ForDebugEvent().Message("Delegates Initialized for MainWindow").Log();
    }

    private void ShowOnboardingWizard()
    {
        _dialogService.ShowOnboardingWizard(async _ =>
        {
            // Always reinitialize after wizard closes — keys are already persisted
            // and the user may have validated them without clicking Finish.
            ReinitializeTmdbAndIgdb();
            await ReinitializeDeviantArtAsync();
        });
    }

    private void OpenSelectedFolder()
    {
        if (Directory.Exists(SelectedFolder))
        {
            ProcessUtils.StartProcess(SelectedFolder + Path.DirectorySeparatorChar);
        }
    }

    private void RemoveListItem(ListItem item)
    {
        var target = item ?? FinalListViewData.SelectedItem;
        if (target != null && FinalListViewData.Data.Contains(target))
        {
            FinalListViewData.Data.Remove(target);
        }
    }

    private void CopyFolderPath(string folder)
    {
        var path = !string.IsNullOrEmpty(folder) ? folder : FinalListViewData.SelectedItem?.Folder;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        try
        {
            Clipboard.SetText(path);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to copy folder path to clipboard.");
        }
    }

    private void OpenFolderInExplorer(object param)
    {
        var folder = param switch
        {
            string s when !string.IsNullOrEmpty(s) => s,
            ListItem item => item.Folder,
            _ => FinalListViewData.SelectedItem?.Folder
        };

        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            ProcessUtils.StartProcess(folder + Path.DirectorySeparatorChar);
        }
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
        BusyIndicatorProperties.Title = Lang.DownloadingIcons;
        StatusBarProperties.AppStatus = Lang.DownloadingIcons;
        StatusBarProperties.AppStatusAdditional = $"0 / {_imgDownloadList.Count}";
        await DownloadAndMakeIconsAsync();
        StatusBarProperties.AppStatus = Lang.Idle;
        StatusBarProperties.AppStatusAdditional = "";
        StatusBarProperties.ProgressBarData.Value = 0;
    }

    private async Task DownloadAndMakeIconsAsync()
    {
        StopIconDownload = false;
        var completedCount = 0;
        var totalCount = _imgDownloadList.Count;
        Logger.Debug("Start Downloading Icons. Total Icons: {ImgDownloadCount}", totalCount);

        await Parallel.ForEachAsync(_imgDownloadList,
            new ParallelOptions { MaxDegreeOfParallelism = 4 },
            async (img, _) =>
            {
                if (StopIconDownload)
                {
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
                    Logger.ForExceptionEvent(e)
                        .Message("UnauthorizedAccessException Occurred while downloading image from url. message: {Message}", e.Message)
                        .Property("Image", img)
                        .Log();
                    MessageBox.Show(CustomMessageBox.Error(
                        Lang.FailedFileAccessAt.Format(Directory.GetParent(img.LocalPath)),
                        Lang.UnauthorizedAccess));
                    return;
                }
                catch (HttpRequestException e)
                {
                    Logger.Warn(e, "Failed to download image from {Url}", img.RemotePath);
                    return;
                }

                var current = Interlocked.Increment(ref completedCount);
                BusyIndicatorProperties.Text = Lang.DownloadingIconWithCount
                    .Format(current, totalCount);
                BusyIndicatorProperties.Value = current;
                StatusBarProperties.ProgressBarData.Value = current;
                StatusBarProperties.ProgressBarData.Max = totalCount;
                StatusBarProperties.AppStatusAdditional = $"{current} / {totalCount}";
            });

        IsBusy = false;
        if (completedCount > 0)
        {
            if (completedCount < totalCount)
            {
                var failedCount = totalCount - completedCount;
                Logger.Warn("Only {Completed} of {Total} images downloaded successfully.", completedCount, totalCount);
                Growl.WarningGlobal(new GrowlInfo
                {
                    Message = Lang.DownloadFailedWithCount.Format(failedCount, totalCount),
                    ShowDateTime = false,
                    StaysOpen = false,
                    ConfirmStr = Lang.Confirm
                });
            }
            Logger.Debug("Making Icons from {Count} downloaded images.", completedCount);
            IsBusy = true;
            await MakeIcons();
        }
        else if (StopIconDownload)
        {
            Logger.Debug("StopIconDownload is true before any downloads completed.");
            StatusBarProperties.AppStatus = Lang.Idle;
            StatusBarProperties.AppStatusAdditional = "";
            StatusBarProperties.ProgressBarData.Value = 0;
        }
        else
        {
            Growl.WarningGlobal(new GrowlInfo
            {
                Message = Lang.DownloadFailedWithCount.Format(totalCount, totalCount),
                ShowDateTime = false,
                StaysOpen = false,
                ConfirmStr = Lang.Confirm
            });
            StatusBarProperties.AppStatus = Lang.Idle;
            StatusBarProperties.AppStatusAdditional = "";
            StatusBarProperties.ProgressBarData.Value = 0;
        }

        IsMakeEnabled = true;
    }

    private async Task MakeIcons()
    {
        IsBusy = true;
        BusyIndicatorProperties.Title = Lang.CreatingIcons;
        StatusBarProperties.AppStatus = Lang.CreatingIcons;
        StatusBarProperties.AppStatusAdditional = "";
        var iconProcessedCount = await Task.Run(()=>IconUtils.MakeIco(IconMode,
            SelectedFolder,
            _pickedListDataTable,
            IsRatingVisible,
            IsPosterMockupUsed,
            new Progress<ProgressBarData>(value =>
            {
                value.Title = Lang.CreatingIcons;
                BusyIndicatorProperties = value;
                StatusBarProperties.ProgressBarData.Value = value.Value;
                StatusBarProperties.ProgressBarData.Max = value.Max;
                StatusBarProperties.AppStatusAdditional = $"{value.Value} / {value.Max}";
            }),
            IncludeAlreadyProcessed));
        StatusBarProperties.ProcessedIcon = iconProcessedCount;
        IsBusy = false;
        StatusBarProperties.AppStatus = Lang.Idle;
        StatusBarProperties.AppStatusAdditional = "";
        StatusBarProperties.ProgressBarData.Value = 0;
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

    /// <summary>
    /// Reinitializes TMDB and IGDB clients after the setup wizard modifies API keys.
    /// Called when the user completes the setup wizard from the menu (not the first-launch wizard).
    /// </summary>
    private void ReinitializeTmdbAndIgdb()
    {
        var (tmdbApiKey, igdbClientId, igdbClientSecret) = FileUtils.ReadApiConfiguration();

        // Reinitialize TMDB
        if (!string.IsNullOrEmpty(tmdbApiKey))
        {
            try
            {
                _tmdbClient = new TMDbClient(tmdbApiKey);
                _tmdbObject = new Tmdb(ref _tmdbClient, ref _pickedListDataTable, ref _imgDownloadList);
                Logger.Info("TMDB client reinitialized successfully after setup wizard.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to reinitialize TMDB client after setup wizard.");
                _tmdbClient = null;
                _tmdbObject = null;
            }
        }
        else
        {
            _tmdbClient = null;
            _tmdbObject = null;
            Logger.Info("TMDB not configured after setup wizard.");
        }

        // Reinitialize IGDB
        if (!string.IsNullOrEmpty(igdbClientId) && !string.IsNullOrEmpty(igdbClientSecret))
        {
            try
            {
                var igdbClient = new IGDBClient(igdbClientId, igdbClientSecret, new IgdbJotTrackerStore());
                _igdbObject = new IgdbClass(ref _pickedListDataTable, ref igdbClient, ref _imgDownloadList);
                Logger.Info("IGDB client reinitialized successfully after setup wizard.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to reinitialize IGDB client after setup wizard.");
                _igdbObject = null;
            }
        }
        else
        {
            _igdbObject = null;
            Logger.Info("IGDB not configured after setup wizard.");
        }
    }

    private void TrackProperties()
    {
        Logger.Debug("TrackProperties: Configuring tracker.");
        Services.Tracker.Configure<MainWindowViewModel>()
            .Property(p => p.IsRatingVisible, false)
            .Property(p => p.IsPosterMockupUsed, true)
            .Property(p => p.IsPosterWindowShown, false)
            .Property(p => p.AppLanguage, Languages.English)
            .Property(p => p.Theme, FoliconThemes.System)
            .Property(p => p.CustomThemeId, string.Empty)
            .Property(p => p.EnableErrorReporting, false)
            .Property(p => p.IncludeAlreadyProcessed, false)
            .Property(p => p.ProcessSelectedFolder, false)
            .PersistOn(nameof(PropertyChanged));
        Services.Tracker.Track(this);
        Logger.Debug("TrackProperties: Tracker configured and tracking started.");
    }

    private void SetCultureInfo()
    {
        var selectedLanguage = AppLanguage;
        Logger.Debug("SetCultureInfo: Selected language: {Language}", selectedLanguage);
        var cultureInfo = CultureUtils.GetCultureInfoByLanguage(selectedLanguage);
        Logger.Debug("SetCultureInfo: Got CultureInfo: {Culture}", cultureInfo.EnglishName);
        LangProvider.Culture = cultureInfo;
        Logger.Debug("SetCultureInfo: LangProvider.Culture set successfully");
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
        Logger.Debug("SetCultureInfo: Completed successfully");
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

    public static FoliconThemes CurrentTheme { get; private set; } = FoliconThemes.Dark;
    public static ThemeManifest? ActiveThemeManifest { get; private set; }
    public static string? ActiveThemeId { get; private set; }
    private static ResourceDictionary? _currentCommunityThemeDictionary;

    private static readonly SolidColorBrush LightBackdropBrush = new(System.Windows.Media.Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush DarkBackdropBrush = new(System.Windows.Media.Color.FromArgb(0xD9, 0x1C, 0x1C, 0x1C));

    private static readonly SolidColorBrush LightHeroCardBg = new(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush DarkHeroCardBg = new(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2D));

    private static readonly SolidColorBrush LightHeroCardBorder = new(System.Windows.Media.Color.FromRgb(0xE2, 0xE8, 0xF0));
    private static readonly SolidColorBrush DarkHeroCardBorder = new(System.Windows.Media.Color.FromRgb(0x3D, 0x3D, 0x3D));

    private static readonly SolidColorBrush LightHeaderGradient = new(System.Windows.Media.Color.FromRgb(0xF8, 0xFA, 0xFC));
    private static readonly SolidColorBrush DarkHeaderGradient = new(System.Windows.Media.Color.FromRgb(0x24, 0x24, 0x24));

    private static readonly SolidColorBrush PrimaryActionBrush = new(System.Windows.Media.Color.FromRgb(0x3B, 0x82, 0xF6));

    public static void ApplyAppTheme(FoliconThemes theme)
    {
        CurrentTheme = theme;
        ActiveThemeId = theme switch
        {
            FoliconThemes.Light => "light",
            FoliconThemes.Dark => "dark",
            FoliconThemes.System => "system",
            FoliconThemes.TheButterfly => "the-butterfly",
            FoliconThemes.TokyoNeon => "tokyo-neon",
            FoliconThemes.MicaDarkObsidian => "mica-dark-obsidian",
            FoliconThemes.NordicPine => "nordic-pine",
            FoliconThemes.Cinema35mm => "cinema-35mm",
            _ => "system"
        };
        ActiveThemeManifest = BuiltInThemes.GetAll().FirstOrDefault(t => string.Equals(t.Id, ActiveThemeId, StringComparison.OrdinalIgnoreCase));

        if (Application.Current != null && _currentCommunityThemeDictionary != null)
        {
            Application.Current.Resources.MergedDictionaries.Remove(_currentCommunityThemeDictionary);
            _currentCommunityThemeDictionary = null;
        }

        ResourceDictionary? customThemeDict = null;

        switch (theme)
        {
            case FoliconThemes.Light:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                ThemeManager.Current.UsingSystemTheme = false;
                ApplyCustomThemeOverride(null);
                break;
            case FoliconThemes.Dark:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                ApplyCustomThemeOverride(null);
                break;
            case FoliconThemes.System:
                ThemeManager.Current.UsingSystemTheme = true;
                ApplyCustomThemeOverride(null);
                break;
            case FoliconThemes.TheButterfly:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                customThemeDict = ApplyCustomThemeOverride("ThemeTheButterfly.xaml");
                break;
            case FoliconThemes.TokyoNeon:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                customThemeDict = ApplyCustomThemeOverride("ThemeTokyoNeon.xaml");
                break;
            case FoliconThemes.MicaDarkObsidian:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                customThemeDict = ApplyCustomThemeOverride("ThemeMicaDarkObsidian.xaml");
                break;
            case FoliconThemes.NordicPine:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                customThemeDict = ApplyCustomThemeOverride("ThemeNordicPine.xaml");
                break;
            case FoliconThemes.Cinema35mm:
                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                ThemeManager.Current.UsingSystemTheme = false;
                customThemeDict = ApplyCustomThemeOverride("ThemeCinema35mm.xaml");
                break;
            default:
                ThemeManager.Current.UsingSystemTheme = true;
                ApplyCustomThemeOverride(null);
                break;
        }

        bool isDark = IsDarkTheme(theme);
        UpdateThemeBrushes(isDark, customThemeDict);
        WindowBackdropHelper.UpdateAllOpenWindows(isDark);
        ActiveThemeChanged?.Invoke(ActiveThemeId);
    }

    public static void ApplyCommunityTheme(ThemeManifest manifest, ResourceDictionary customThemeDict)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(customThemeDict);

        ActiveThemeManifest = manifest;
        ActiveThemeId = manifest.Id;
        CurrentTheme = manifest.IsDark ? FoliconThemes.Dark : FoliconThemes.Light;

        ApplyCustomThemeOverride(null);

        ThemeManager.Current.ApplicationTheme = manifest.IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ThemeManager.Current.UsingSystemTheme = false;

        if (Application.Current != null)
        {
            var merged = Application.Current.Resources.MergedDictionaries;
            if (_currentCommunityThemeDictionary != null)
            {
                merged.Remove(_currentCommunityThemeDictionary);
            }
            _currentCommunityThemeDictionary = customThemeDict;
            merged.Add(customThemeDict);
        }

        UpdateThemeBrushes(manifest.IsDark, customThemeDict);
        WindowBackdropHelper.UpdateAllOpenWindows(manifest.IsDark);
        ActiveThemeChanged?.Invoke(ActiveThemeId);
    }

    public static event Action<string?>? ActiveThemeChanged;

    public void RefreshAvailableThemes()
    {
        var installed = _themeStorageService != null
            ? _themeStorageService.GetInstalledThemes()
            : BuiltInThemes.GetAll();

        var currentActiveId = !string.IsNullOrEmpty(CustomThemeId)
            ? CustomThemeId
            : (ActiveThemeId ?? ActiveThemeManifest?.Id ?? Theme.ToString().ToLowerInvariant());

        AvailableThemes.Clear();
        foreach (var manifest in installed)
        {
            FoliconThemes? builtInEnum = manifest.Id.ToLowerInvariant() switch
            {
                "light" => FoliconThemes.Light,
                "dark" => FoliconThemes.Dark,
                "the-butterfly" => FoliconThemes.TheButterfly,
                "tokyo-neon" => FoliconThemes.TokyoNeon,
                "mica-dark-obsidian" => FoliconThemes.MicaDarkObsidian,
                "nordic-pine" => FoliconThemes.NordicPine,
                "cinema-35mm" => FoliconThemes.Cinema35mm,
                "system" => FoliconThemes.System,
                _ => null
            };

            var displayName = builtInEnum.HasValue
                ? ThemeToDisplayConverter.GetLocalizedThemeName(builtInEnum.Value)
                : manifest.Name;

            var option = new ThemeOption
            {
                Id = manifest.Id,
                DisplayName = displayName,
                IsBuiltIn = manifest.IsBuiltIn,
                BuiltInTheme = builtInEnum,
                Manifest = manifest
            };

            AvailableThemes.Add(option);
        }

        SyncSelectedThemeOptionWithActive(currentActiveId);
    }

    public void SyncSelectedThemeOptionWithActive(string? targetId = null)
    {
        var id = targetId ?? (!string.IsNullOrEmpty(CustomThemeId) ? CustomThemeId : ActiveThemeId);
        ThemeOption? match = null;

        if (!string.IsNullOrEmpty(id))
        {
            match = AvailableThemes.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        if (match == null)
        {
            match = AvailableThemes.FirstOrDefault(t => t.BuiltInTheme == Theme)
                 ?? AvailableThemes.FirstOrDefault(t => t.BuiltInTheme == FoliconThemes.System)
                 ?? AvailableThemes.FirstOrDefault();
        }

        if (match != null && !ReferenceEquals(_selectedThemeOption, match))
        {
            _selectedThemeOption = match;
            RaisePropertyChanged(nameof(SelectedThemeOption));
        }
    }

    public void ApplyThemeOption(ThemeOption option, bool isPreview)
    {
        if (_isSynchronizingTheme) return;
        try
        {
            _isSynchronizingTheme = true;
            if (option.Manifest != null && !option.IsBuiltIn)
            {
                if (!isPreview)
                {
                    CustomThemeId = option.Id;
                }
                ApplyThemeManifest(option.Manifest, _themeParser ?? new HybridThemeParser());
            }
            else if (option.BuiltInTheme.HasValue)
            {
                if (!isPreview)
                {
                    CustomThemeId = string.Empty;
                    _theme = option.BuiltInTheme.Value;
                    RaisePropertyChanged(nameof(Theme));
                }
                ApplyAppTheme(option.BuiltInTheme.Value);
            }
        }
        finally
        {
            _isSynchronizingTheme = false;
        }
    }

    private void OnActiveThemeChanged(string? activeId)
    {
        if (string.IsNullOrEmpty(activeId) || _isSynchronizingTheme) return;
        try
        {
            _isSynchronizingTheme = true;
            RefreshAvailableThemes();
            var match = AvailableThemes.FirstOrDefault(t => string.Equals(t.Id, activeId, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _selectedThemeOption = match;
                if (!match.IsBuiltIn)
                {
                    CustomThemeId = match.Id;
                }
                else if (match.BuiltInTheme.HasValue)
                {
                    CustomThemeId = string.Empty;
                    _theme = match.BuiltInTheme.Value;
                    RaisePropertyChanged(nameof(Theme));
                }
                RaisePropertyChanged(nameof(SelectedThemeOption));
            }
        }
        finally
        {
            _isSynchronizingTheme = false;
        }
    }

    public static void ApplyThemeManifest(ThemeManifest manifest, IThemeParser parser)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(parser);

        if (manifest.IsBuiltIn)
        {
            var builtInEnum = manifest.Id.ToLowerInvariant() switch
            {
                "light" => FoliconThemes.Light,
                "dark" => FoliconThemes.Dark,
                "the-butterfly" => FoliconThemes.TheButterfly,
                "tokyo-neon" => FoliconThemes.TokyoNeon,
                "mica-dark-obsidian" => FoliconThemes.MicaDarkObsidian,
                "nordic-pine" => FoliconThemes.NordicPine,
                "cinema-35mm" => FoliconThemes.Cinema35mm,
                _ => FoliconThemes.System
            };
            ApplyAppTheme(builtInEnum);
            return;
        }

        ResourceDictionary customDict;
        if (!string.IsNullOrEmpty(manifest.XamlPackageFile) && !string.IsNullOrEmpty(manifest.PackageDirectory))
        {
            var xamlPath = Path.Combine(manifest.PackageDirectory, manifest.XamlPackageFile);
            customDict = File.Exists(xamlPath) ? parser.LoadPackageResourceDictionary(xamlPath) : parser.SynthesizeResourceDictionary(manifest);
        }
        else
        {
            customDict = parser.SynthesizeResourceDictionary(manifest);
        }

        ApplyCommunityTheme(manifest, customDict);
    }

    /// <summary>
    /// Creates a memento snapshot of the current active theme state for transient live preview rollback.
    /// </summary>
    public static ThemeStateSnapshot CaptureThemeSnapshot()
    {
        return new ThemeStateSnapshot
        {
            ActiveThemeManifest = ActiveThemeManifest,
            ActiveThemeId = ActiveThemeId,
            CurrentTheme = CurrentTheme,
            UsingSystemTheme = ThemeManager.Current.UsingSystemTheme
        };
    }

    /// <summary>
    /// Restores theme state from a previously captured memento snapshot.
    /// </summary>
    public static void RestoreThemeSnapshot(ThemeStateSnapshot snapshot, IThemeParser? parser = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.ActiveThemeManifest != null)
        {
            ApplyThemeManifest(snapshot.ActiveThemeManifest, parser ?? new HybridThemeParser());
        }
        else
        {
            ApplyAppTheme(snapshot.CurrentTheme);
        }
    }

    public static bool IsDarkTheme(FoliconThemes theme)
    {
        return theme switch
        {
            FoliconThemes.Light => false,
            FoliconThemes.Dark => true,
            FoliconThemes.TheButterfly => true,
            FoliconThemes.TokyoNeon => true,
            FoliconThemes.MicaDarkObsidian => true,
            FoliconThemes.NordicPine => true,
            FoliconThemes.Cinema35mm => true,
            FoliconThemes.System => IsSystemThemeDark(),
            _ => IsSystemThemeDark()
        };
    }

    public static bool IsSystemThemeDark()
    {
        try
        {
            return ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark;
        }
        catch
        {
            return true;
        }
    }

    public static bool IsCurrentThemeDark() => IsDarkTheme(CurrentTheme);

    private static void UpdateThemeBrushes(bool isDark, ResourceDictionary? customDict)
    {
        if (Application.Current == null) return;

        var res = Application.Current.Resources;
        if (customDict != null)
        {
            SyncResource(res, customDict, "WindowBackdropBrush");
            SyncResource(res, customDict, "HeroCardBackgroundBrush");
            SyncResource(res, customDict, "HeroCardBorderBrush");
            SyncResource(res, customDict, "PrimaryActionGradientBrush");
            SyncResource(res, customDict, "HeaderGradientBrush");
            SyncResource(res, customDict, "CardBorderGradientBrush");
            SyncResource(res, customDict, "FolderBadgeGradientBrush");
        }
        else
        {
            res["WindowBackdropBrush"] = isDark ? DarkBackdropBrush : LightBackdropBrush;
            res["HeroCardBackgroundBrush"] = isDark ? DarkHeroCardBg : LightHeroCardBg;
            res["HeroCardBorderBrush"] = isDark ? DarkHeroCardBorder : LightHeroCardBorder;
            res["PrimaryActionGradientBrush"] = PrimaryActionBrush;
            res["HeaderGradientBrush"] = isDark ? DarkHeaderGradient : LightHeaderGradient;
            res["CardBorderGradientBrush"] = isDark ? DarkHeroCardBorder : LightHeroCardBorder;
            res["FolderBadgeGradientBrush"] = PrimaryActionBrush;
        }
    }

    private static void SyncResource(ResourceDictionary target, ResourceDictionary source, string key)
    {
        if (source.Contains(key))
        {
            target[key] = source[key];
        }
        else
        {
            target.Remove(key);
        }
    }

    private static readonly Dictionary<string, ResourceDictionary> CustomThemeDictionaries = new(StringComparer.OrdinalIgnoreCase);

    private static ResourceDictionary? ApplyCustomThemeOverride(string? themeXamlName)
    {
        try
        {
            if (Application.Current == null)
            {
                return null;
            }

            var merged = Application.Current.Resources.MergedDictionaries;
            if (_currentCommunityThemeDictionary != null)
            {
                merged.Remove(_currentCommunityThemeDictionary);
                _currentCommunityThemeDictionary = null;
            }

            for (var i = merged.Count - 1; i >= 0; i--)
            {
                var dict = merged[i];
                if (dict.Source != null && (
                    dict.Source.OriginalString.Contains("ThemeTheButterfly", StringComparison.OrdinalIgnoreCase) ||
                    dict.Source.OriginalString.Contains("ThemeTokyoNeon", StringComparison.OrdinalIgnoreCase) ||
                    dict.Source.OriginalString.Contains("ThemeMicaDarkObsidian", StringComparison.OrdinalIgnoreCase) ||
                    dict.Source.OriginalString.Contains("ThemeNordicPine", StringComparison.OrdinalIgnoreCase) ||
                    dict.Source.OriginalString.Contains("ThemeCinema35mm", StringComparison.OrdinalIgnoreCase)))
                {
                    merged.RemoveAt(i);
                }
            }

            if (string.IsNullOrEmpty(themeXamlName))
            {
                return null;
            }

            if (!CustomThemeDictionaries.TryGetValue(themeXamlName, out var themeDict))
            {
                themeDict = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/FoliCon;component/XamlResources/{themeXamlName}", UriKind.Absolute)
                };
                CustomThemeDictionaries[themeXamlName] = themeDict;
            }
            merged.Add(themeDict);
            return themeDict;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to apply custom theme override for {ThemeXaml}", themeXamlName);
            return null;
        }
    }

    public void Dispose()
    {
        _tmdbClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
