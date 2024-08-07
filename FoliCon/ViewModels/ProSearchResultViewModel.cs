﻿using ImTools;

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
    
    public event Action<IDialogResult> RequestClose;

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
    public DelegateCommand<object> PickCommand { get; set; }
    public DelegateCommand<object> OpenImageCommand { get; set; }
    public DelegateCommand<object> ExtractManuallyCommand { get; set; }
    public DelegateCommand SearchAgainCommand { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }

    public ProSearchResultViewModel(IDialogService dialogService)
    {
        Logger.Debug("ProSearchResultViewModel Constructor");
        ImageUrl = [];
        StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
        PickCommand = new DelegateCommand<object>(PickMethod);
        OpenImageCommand = new DelegateCommand<object>(link=> UiUtils.ShowImageBrowser(link as string));
        ExtractManuallyCommand = new DelegateCommand<object>(ExtractManually);
        SkipCommand = new DelegateCommand(SkipMethod);
        SearchAgainCommand = new DelegateCommand(PrepareForSearch);
        _dialogService = dialogService;
    }

    private void ExtractManually(object parameter)
    {
        Logger.Debug("Extracting manually from Deviation ID {DeviationId}", parameter);
        var deviationId = (string)parameter;
        _dialogService.ShowManualExplorer(deviationId, DArtObject, result =>
        {
            if (result.Result != ButtonResult.OK)
            {
                return;
            }

            Logger.Debug("Manual Extraction Completed");
            PickMethod(result.Parameters.GetValue<string>("localPath"));
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

    private static bool HasNoResults(DArtBrowseResult searchResult)
    {
        return searchResult.Results.IsNullOrEmpty();
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
        // Counter for the total posters
        TotalPosters = searchResult.Results!.Count(result => result.IsDownloadable) + offset;
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

        if (IsItemDownloadable(item))
        {
            ImageUrl.Add(new DArtImageList(item.Value.Content.Src, item.Value.Thumbs[0].Src, item.Value.Deviationid));
            Index++;
        }
        else
        {
            Logger.Warn("Poster {Index} is not downloadable", item.Value.Url);
        }
    }

    private static bool IsItemDownloadable(EnumeratorWithIndex<Result> item)
    {
        return item.Value.IsDownloadable;
    }
    

    private void PickMethod(object parameter)
    {
        Logger.Debug("Picking Image {Image}", parameter);
        SearchAgainTitle = null;
        var link = (string)parameter;
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

        RaiseRequestClose(new DialogResult(result));
    }

    protected virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose?.Invoke(dialogResult);
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