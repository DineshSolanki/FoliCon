using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.DeviantArt;
using FoliCon.Modules.Extension;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.ViewModels;

public class ProSearchResultViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private string _title = LangProvider.GetLang("SearchResult");
    private bool _stopSearch;
    private string _searchTitle;
    private string _searchAgainTitle;
    private int _i;
    private string _busyContent = LangProvider.GetLang("Searching");
    private bool _isBusy;
    private DArt _dArtObject;
    private List<PickedListItem> _listDataTable;
    private List<ImageToDownload> _imgDownloadList;
    private int _index;
    private int _totalPosters;

    public event Action<IDialogResult> RequestClose;

    private string _folderPath;
    private bool _isSearchFocused;

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public ObservableCollection<DArtImageList> ImageUrl { get; set; }
    public string SearchTitle { get => _searchTitle; set => SetProperty(ref _searchTitle, value); }
    public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
    public List<string> Fnames { get; set; }
    public string SearchAgainTitle { get => _searchAgainTitle; set => SetProperty(ref _searchAgainTitle, value); }
    public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public DArt DArtObject { get => _dArtObject; set => SetProperty(ref _dArtObject, value); }
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

    public DelegateCommand SkipCommand { get; set; }
    public DelegateCommand<object> PickCommand { get; set; }
    public DelegateCommand<object> OpenImageCommand { get; set; }
    public DelegateCommand<object> ExtractManuallyCommand { get; set; }
    public DelegateCommand SearchAgainCommand { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }

    public ProSearchResultViewModel()
    {
        Logger.Debug("ProSearchResultViewModel Constructor");
        ImageUrl = [];
        StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
        PickCommand = new DelegateCommand<object>(PickMethod);
        OpenImageCommand = new DelegateCommand<object>(OpenImageMethod);
        ExtractManuallyCommand = new DelegateCommand<object>(ExtractManually);
        SkipCommand = new DelegateCommand(SkipMethod);
        SearchAgainCommand = new DelegateCommand(PrepareForSearch);
    }

    private void OpenImageMethod(object parameter)
    {
        Logger.Debug("Opening Image {Image}", parameter);
        var link = (string)parameter;
        var browser = new ImageBrowser(link)
        {
            ShowTitle = false,
            IsFullScreen = true
        };
        browser.Show();
    }

    private void ExtractManually(object parameter)
    {
        Logger.Debug("Extracting manually from Deviation ID {DeviationId}", parameter);
        var deviationId = (string)parameter;
        DArtObject.Download(deviationId).ContinueWith(task =>
        {
            Logger.Debug("Downloaded Image from Deviation ID {DeviationId}", deviationId);
        });
    }
    private async void PrepareForSearch()
    {
        StopSearch = false;
        ImageUrl.Clear();
        SearchTitle = null;
        SearchTitle = !string.IsNullOrEmpty(SearchAgainTitle) ? SearchAgainTitle : TitleCleaner.Clean(Fnames[_i]);
        BusyContent = LangProvider.GetLang("SearchingWithName").Format(SearchTitle);
        IsBusy = true;
        Title = LangProvider.GetLang("PickIconWithName").Format(SearchTitle);
        await Search(SearchTitle);
        SearchAgainTitle = SearchTitle;
        IsBusy = false;
    }

    private async Task Search(string query, int offset = 0)
    {
        Logger.Trace("Search Started for {Query}, offset: {Offset}", query, offset);
        Index = 0;
        while (true)
        {
            var searchResult = await DArtObject.Browse(query, offset);
            Logger.Trace("Search Result for {Query} is {@SearchResult}", query, searchResult);
            if (searchResult.Results?.Length > 0)
            {
                TotalPosters = searchResult.Results.Count( result => result.IsDownloadable) + offset;
                Logger.Debug("Total Posters: {TotalPosters} for {Title}", TotalPosters, query);
                foreach (var item in searchResult.Results.GetEnumeratorWithIndex())
                {
                    if (!item.Value.IsDownloadable)
                    {
                        Logger.Warn("Poster {Index} is not downloadable", item.Value.Url);
                        continue;
                    }
                    var response = await Services.HttpC.GetAsync(item.Value.Thumbs[0].Src);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.ForErrorEvent().Message("Could not download image {Image}", item.Value.Thumbs[0].Src)
                            .Property("Response", response).Log();
                        continue;
                    }
                    using (var bm = await response.GetBitmap())
                    {
                        ImageUrl.Add(new DArtImageList(item.Value.Content.Src, ImageUtils.LoadBitmap(bm), item.Value.Deviationid));
                    }
                    if (_stopSearch)
                    {
                        Logger.Debug("Search Stopped by user at {Index}", Index);
                        return;
                    }

                    Index++;
                }
                if (searchResult.HasMore)
                {
                    Logger.Debug("Search Result has more items, offset: {Offset}", searchResult.NextOffset);
                    offset = searchResult.NextOffset;
                    continue;
                }
            }
            else
            {
                Logger.Warn("No Result Found for {Query}", query);
                IsBusy = false;
                MessageBox.Show(CustomMessageBox.Error(LangProvider.GetLang("NoResultFoundTryCorrectTitle"),
                    LangProvider.GetLang("NoResult")));
                IsSearchFocused = true;
            }

            break;
        }
    }

    private void PickMethod(object parameter)
    {
        Logger.Debug("Picking Image {Image}", parameter);
        SearchAgainTitle = null;
        var link = (string)parameter;
        var currentPath = $@"{_folderPath}\{Fnames[_i]}";
        var tempImage = new ImageToDownload
        {
            LocalPath = $"{currentPath}\\{IconUtils.GetImageName()}.png",
            RemotePath = new Uri(link)
        };
        Logger.Debug("Adding Image to Download List {@Image}", tempImage);
        FileUtils.AddToPickedListDataTable(_listDataTable, "", SearchTitle, "", currentPath, Fnames[_i]);
        _imgDownloadList.Add(tempImage);
        _i++;
        if (!(_i > Fnames.Count - 1))
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
        if (!(_i > Fnames.Count - 1))
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

    public virtual void RaiseRequestClose(IDialogResult dialogResult)
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