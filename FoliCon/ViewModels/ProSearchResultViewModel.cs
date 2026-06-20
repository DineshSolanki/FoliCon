namespace FoliCon.ViewModels;

[Localizable(false)]
public class ProSearchResultViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _title = Lang.SearchResult;
    private bool _stopSearch;
    private string _searchTitle;
    private string _searchAgainTitle;
    private int _i;
    private string _busyContent = Lang.Searching;
    private bool _isBusy;
    private DArt _dArtObject;
    private List<PickedListItem> _listDataTable;
    private List<ImageToDownload> _imgDownloadList;
    private int _index;
    private int _totalPosters;
    private readonly IDialogService _dialogService;
    private bool _subfolderProcessingEnabled = Services.Settings.SubfolderProcessingEnabled;
    private readonly HashSet<string> _autoWatchedAuthors = new();

    public DialogCloseListener RequestClose { get; }

    private string _folderPath;
    private bool _isSearchFocused;

    public string Title { get => _title;
        private set => SetProperty(ref _title, value); }
    public ObservableCollection<DArtImageList> ImageUrl { get; set; }
    private string SearchTitle { get => _searchTitle; set => SetProperty(ref _searchTitle, value); }
    public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
    private List<string> Fnames { get; set; }
    public string SearchAgainTitle { get => _searchAgainTitle; set => SetProperty(ref _searchAgainTitle, value); }
    public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    private DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
    public int Index { get => _index; set => SetProperty(ref _index, value); }
    public int TotalPosters { get => _totalPosters; set => SetProperty(ref _totalPosters, value); }

    public bool IsSearchFocused
    {
        get => _isSearchFocused;
        set
        {
            if (_isSearchFocused == value)

            {
                _isSearchFocused = false;
                RaisePropertyChanged();
            }
            SetProperty(ref _isSearchFocused, value);
        }
    }

    public bool SubfolderProcessingEnabled
    {
        get => _subfolderProcessingEnabled;
        set
        {
            SetProperty(ref _subfolderProcessingEnabled, value);
            Services.Settings.SubfolderProcessingEnabled = value;
            Services.Settings.Save();
        }
    }

    public DelegateCommand SkipCommand { get; set; }
    public DelegateCommand<DArtImageList> PickCommand { get; set; }
    public DelegateCommand<DArtImageList> OpenImageCommand { get; set; }
    public DelegateCommand<DArtImageList> ExtractManuallyCommand { get; set; }
    public DelegateCommand SearchAgainCommand { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }

    public ProSearchResultViewModel(IDialogService dialogService)
    {
        Logger.Debug("ProSearchResultViewModel Constructor");
        ImageUrl = [];
        StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
        PickCommand = new DelegateCommand<DArtImageList>(PickMethod);
        OpenImageCommand = new DelegateCommand<DArtImageList>(OpenImageMethod);
        ExtractManuallyCommand = new DelegateCommand<DArtImageList>(ExtractManually);
        SkipCommand = new DelegateCommand(SkipMethod);
        SearchAgainCommand = new DelegateCommand(PrepareForSearch);
        _dialogService = dialogService;
    }

    /// <summary>
    /// Ensures the deviation's author is watched if the deviation is watcher-gated.
    /// Returns true if watching succeeded or was not required; false if the user cancelled or watching failed.
    /// </summary>
    private async Task<bool> EnsureWatchAsync(DArtImageList parameter)
    {
        if (!parameter.RequiresWatch)
        {
            return true;
        }

        var authorUsername = parameter.AuthorUsername;

        if (string.IsNullOrEmpty(authorUsername))
        {
            return true;
        }

        if (!Services.Settings.DeviantArtWatchEnabled)
        {
            Logger.Warn("Watcher-gated deviation but watch scope is not enabled.");
            MessageBox.Show(CustomMessageBox.Warning(Lang.DeviantArtWatchScopeNotEnabled, Lang.WatcherWallWatchFailedTitle));
            return false;
        }

        if (MessageBox.Show(CustomMessageBox.Ask(
                Lang.WatcherWallConfirmMessage.Format(authorUsername),
                Lang.WatcherWallConfirmTitle)) != MessageBoxResult.Yes)
        {
            return false;
        }

        IsBusy = true;
        BusyContent = Lang.WatchingArtist.Format(authorUsername);

        var success = await DArtObject.WatchAsync(authorUsername);
        IsBusy = false;

        if (!success)
        {
            MessageBox.Show(CustomMessageBox.Error(
                Lang.WatcherWallWatchFailedMessage, Lang.WatcherWallWatchFailedTitle));
            return false;
        }

        parameter.RequiresWatch = false;
        Growl.SuccessGlobal(new GrowlInfo
        {
            Message = Lang.WatcherWallWatchSuccess.Format(authorUsername),
            ShowDateTime = false
        });

        return true;
    }

    private async void ExtractManually(DArtImageList parameter)
    {
        Logger.Debug("Extracting manually from Deviation ID {DeviationId}", parameter?.DeviationId);

        if (DArtObject == null)
        {
            Logger.Warn("DeviantArt client is not available. Cannot extract manually.");
            MessageBox.Show(CustomMessageBox.Warning(Lang.DAUnavailableRestartMessage, Lang.DAUnavailableTitle));
            return;
        }

        if (parameter == null || string.IsNullOrEmpty(parameter.DeviationId))
        {
            Logger.Warn("No deviation ID available for manual extraction.");
            return;
        }

        var authorUsername = parameter.AuthorUsername;

        if (!await EnsureWatchAsync(parameter))
        {
            return;
        }

        _dialogService.ShowManualExplorer(parameter.DeviationId, DArtObject, result =>
        {
            if (!string.IsNullOrEmpty(authorUsername))
            {
                _ = DArtObject.UnwatchAsync(authorUsername);
            }

            if (result.Result != ButtonResult.OK)
            {
                return;
            }

            Logger.Debug("Manual Extraction Completed");
            ProcessPick(result.Parameters.GetValue<string>("localPath"));
        });
    }
    private async void PrepareForSearch()
    {
        StopSearch = false;
        ImageUrl.Clear();
        SearchTitle = null;
        SearchTitle = !string.IsNullOrEmpty(SearchAgainTitle) ? SearchAgainTitle : TitleCleaner.Clean(Fnames[_i]);
        BusyContent = Lang.SearchingWithName.Format(SearchTitle);
        IsBusy = true;
        Title = Lang.PickIconWithName.Format(SearchTitle);
        await Search(SearchTitle);
        SearchAgainTitle = SearchTitle;
        IsBusy = false;
        Index = 0;
        TotalPosters = 0;
    }

    private async Task Search(string query, int offset = 0)
    {
        Logger.Trace("Search Started for {Query}, offset: {Offset}", query, offset);

        if (DArtObject == null)
        {
            Logger.Warn("DeviantArt client is not available. Skipping DeviantArt search.");
            MessageBox.Show(CustomMessageBox.Warning(Lang.DAUnavailableRetryMessage, Lang.DAUnavailableTitle));
            return;
        }

        try
        {
            while (true)
            {
                var searchResult = await DArtObject.Browse(query, offset);
                Logger.Trace("Search Result for {Query} is {@SearchResult}", query, searchResult);

                if (HasNoResults(searchResult))
                {
                    ProcessNoResults(query, offset);
                    break;
                }

                offset = ProcessSearchResults(query, searchResult, offset);

                if (!searchResult.HasMore || _stopSearch)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "DeviantArt search failed for query: {Query}", query);
            IsBusy = false;
            MessageBox.Show(CustomMessageBox.Error(Lang.DAConnectionFailedMessage, Lang.DAConnectionFailedTitle));
        }
    }

    private static bool HasNoResults(DArtBrowseResult searchResult)
    {
        return searchResult.Results is null or { Length: 0 };
    }

    private void ProcessNoResults(string query, int offset)
    {
        Logger.Warn("No Result Found for {Query}", query);
        IsBusy = false;
        if (offset == 0)
        {
            MessageBox.Show(
                CustomMessageBox.Error(
                    Lang.NoResultFoundTryCorrectTitle,
                    Lang.NoResult
                )
            );
        }

        IsSearchFocused = true;
    }

    private int ProcessSearchResults(string query, DArtBrowseResult searchResult, int offset)
    {
        // Counter for the total posters (downloadable + watcher-gated)
        TotalPosters = searchResult.Results!.Count(result =>
            result.IsDownloadable || result.PremiumFolderData?.Type == "watchers") + offset;
        Logger.Debug("Total Posters: {TotalPosters} for {Title}", TotalPosters, query);

        foreach (var item in searchResult.Results.GetEnumeratorWithIndex())
        {
            ProcessResultItems(item);

            if (!_stopSearch)
            {
                continue;
            }

            Logger.Debug("Search Stopped by user at {Index}", Index);
            break;
        }

        if (!searchResult.HasMore)
        {
            return offset;
        }

        Logger.Debug("Search Result has more items, offset: {Offset}", searchResult.NextOffset);
        return searchResult.NextOffset;

    }

    private void ProcessResultItems(EnumeratorWithIndex<Result> item)
    {
        Logger.Trace("Deviation {Index} is {@Item}", item.Index, item.Value);

        if (item.Value.IsDownloadable)
        {
            ImageUrl.Add(new DArtImageList(item.Value.Content.Src, item.Value.Thumbs[0].Src, item.Value.Deviationid));
            Index++;
            return;
        }

        // Watcher-gated: show if it's a "watchers" type premium folder
        if (item.Value.PremiumFolderData?.Type == "watchers")
        {
            ImageUrl.Add(new DArtImageList(item.Value.Content.Src, item.Value.Thumbs[0].Src,
                item.Value.Deviationid, true, item.Value.Author?.Username));
            Index++;
            Logger.Info("Watcher-gated poster {Url} added (author: {Author})",
                item.Value.Url, item.Value.Author?.Username);
        }
        else
        {
            Logger.Warn("Poster {Url} is not downloadable", item.Value.Url);
        }
    }


    private void OpenImageMethod(DArtImageList parameter)
    {
        Logger.Debug("Opening Image {Image}", parameter);
        UiUtils.ShowImageBrowser(parameter.Url);
    }

    private async void PickMethod(DArtImageList parameter)
    {
        Logger.Debug("Picking Image {Image}", parameter);

        if (parameter.RequiresWatch)
        {
            if (!await EnsureWatchAsync(parameter))
            {
                return;
            }

            // Open the Manual Explorer to handle download/extraction (may be a zip).
            // Unwatch after the user picks a file.
            SearchAgainTitle = null;
            var authorUsername = parameter.AuthorUsername;
            _dialogService.ShowManualExplorer(parameter.DeviationId, DArtObject, result =>
            {
                _ = DArtObject.UnwatchAsync(authorUsername);

                if (result.Result != ButtonResult.OK)
                {
                    return;
                }

                Logger.Debug("Watch+Extract completed for {Author}", authorUsername);
                ProcessPick(result.Parameters.GetValue<string>("localPath"));
            });
            return;
        }

        SearchAgainTitle = null;
        ProcessPick(parameter.Url);
    }

    private void ProcessPick(string link)
    {
        Logger.Debug("Picking Image {Image}", link);
        var currentPath = $@"{_folderPath}\{Fnames[_i]}";
        var tempImage = new ImageToDownload
        {
            LocalPath = $@"{currentPath}\{IconUtils.GetImageName()}.png",
            RemotePath = new Uri(link)
        };
        Logger.Debug("Adding Image to Download List {@Image}", tempImage);
        FileUtils.AddToPickedListDataTable(_listDataTable, "", SearchTitle, "", currentPath, Fnames[_i]);
        _imgDownloadList.Add(tempImage);
        _i++;
        if (_i <= Fnames.Count - 1)
        {
            Logger.Info("Some titles are left, processed: {Processed}, total: {Total}", _i, Fnames.Count);
            PrepareForSearch();
        }
        else
        {
            Logger.Info("All titles are processed, processed: {Processed}, total: {Total}", _i, Fnames.Count);
            CloseDialog("true");
        }
    }

    private void SkipMethod()
    {
        Logger.Debug("Skipping title");
        _i++;
        SearchAgainTitle = null;
        if (_i <= Fnames.Count - 1)
        {
            Logger.Info("Some titles are left, processed: {Processed}, total: {Total}", _i, Fnames.Count);
            PrepareForSearch();
        }
        else
        {
            Logger.Info("All titles are processed, processed: {Processed}, total: {Total}", _i, Fnames.Count);
            CloseDialog("false");
        }
    }

    protected virtual void CloseDialog(string parameter)
    {
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RequestClose.Invoke(result);
    }

    public virtual bool CanCloseDialog()
    {
        return true;
    }

    public virtual void OnDialogClosed()
    {
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
        DArtObject = parameters.GetValue<DArt>("dartobject");
        Fnames = parameters.GetValue<List<string>>("fnames");
        _listDataTable = parameters.GetValue<List<PickedListItem>>("pickedtable");
        _imgDownloadList = parameters.GetValue<List<ImageToDownload>>("imglist");
        _folderPath = parameters.GetValue<string>("folderpath");
        PrepareForSearch();
    }
}
