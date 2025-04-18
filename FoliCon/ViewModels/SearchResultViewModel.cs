using DelegateCommand = Prism.Commands.DelegateCommand;

namespace FoliCon.ViewModels;

[Localizable(false)]
public class SearchResultViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string YoutubeEmbedUrl = "https://www.youtube.com/embed/{0}";

    #region Variables

    private string _title = Lang.SearchResult;
    private string _searchTitle;
    private string _busyContent;
    private bool _isBusy;
    private string _searchMode;
    private ListViewData _resultListViewData;
    private string _searchAgainTitle;
    private string _skipAllText = Lang.SkipThisPlaceholder;
    private List<string> _fileList;
    private ResultResponse _searchResult;
    private string _fullFolderPath;
    private readonly IDialogService _dialogService;
    private bool _isSearchFocused;
    private bool _isPickedById;
    private bool _subfolderProcessingEnabled = Services.Settings.SubfolderProcessingEnabled;
    public event Action<IDialogResult> RequestClose;

    private Tmdb _tmdbObject;
    private IgdbClass _igdbObject;
    private double _customRating;
    
    private PosterPickerDialogParams _dialogParams;

    #endregion Variables

    #region Properties

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string SearchTitle
    {
        get => _searchTitle;
        set => SetProperty(ref _searchTitle, value);
    }

    public string BusyContent
    {
        get => _busyContent;
        set => SetProperty(ref _busyContent, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public double CustomRating
    {
        get => _customRating;
        set => SetProperty(ref _customRating, value);
    }

    public ListViewData ResultListViewData
    {
        get => _resultListViewData;
        set => SetProperty(ref _resultListViewData, value);
    }

    public string SearchAgainTitle
    {
        get => _searchAgainTitle;
        set => SetProperty(ref _searchAgainTitle, value);
    }

    public string SkipAllText
    {
        get => _skipAllText;
        set => SetProperty(ref _skipAllText, value);
    }

    public List<string> FileList
    {
        get => _fileList;
        set => SetProperty(ref _fileList, value);
    }

    public ResultResponse SearchResult
    {
        get => _searchResult;
        set => SetProperty(ref _searchResult, value);
    }

    public string SearchMode
    {
        get => _searchMode;
        set => SetProperty(ref _searchMode, value);
    }

    public bool IsSearchFocused
    {
        get => _isSearchFocused;
        set => SetProperty(ref _isSearchFocused, value);
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

    #endregion Properties

    #region Commands

    public DelegateCommand<MouseButtonEventArgs> PickCommand { get; }
    public DelegateCommand SkipCommand { get; }
    public DelegateCommand SkipAllCommand { get; }
    public DelegateCommand SearchAgainCommand { get; }
    public DelegateCommand ResetPosterCommand { get; }

    #endregion Commands

    public SearchResultViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        SearchAgainCommand = new DelegateCommand(SearchAgainMethod);
        SkipCommand = new DelegateCommand(delegate { CloseDialog("false"); });
        ResultListViewData = new ListViewData { Data = new ObservableCollection<ListItem>(), SelectedItem = null };
        PickCommand = new DelegateCommand<MouseButtonEventArgs>(PickMethod);
        ResetPosterCommand = new DelegateCommand(ResetPoster);
        SkipAllCommand = new DelegateCommand(delegate
        {
            CloseDialog("false", true);
        });
    }

    protected virtual void CloseDialog(string parameter)
    {
        CloseDialog(parameter, false);
    }

    protected virtual void CloseDialog(string parameter, bool skipAll)
    {
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };
        var parameters = new DialogParameters
        {
            { "skipAll", skipAll }
        };
        RaiseRequestClose(new DialogResult(result, parameters));
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
        SearchTitle = parameters.GetValue<string>("query");
        SearchResult = parameters.GetValue<ResultResponse>("result");
        SearchMode = parameters.GetValue<string>("searchmode");
        _tmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
        _igdbObject = parameters.GetValue<IgdbClass>("igdbObject");
        _fullFolderPath = parameters.GetValue<string>("folderpath");
        _isPickedById = parameters.GetValue<bool>("isPickedById");
        var parent = Directory.GetParent(_fullFolderPath);
        if (parent != null)
        {
            SkipAllText = Lang.SkipThisPlaceholderParent.Format(parent.Name);
        }
        _dialogParams = new PosterPickerDialogParams
        {
            TmdbObject = _tmdbObject,
            IgdbObject = _igdbObject,
            IsPickedById = _isPickedById,
            Result = SearchResult,
            ResultData = ResultListViewData.Data
        };
        LoadData(SearchTitle);
        SearchAgainTitle = SearchTitle;
        FileList = FileUtils.GetFileNamesFromFolder(_fullFolderPath);
    }

    private async Task StartSearch(bool useBusy)
    {
        Logger.Debug("StartSearch called, show loader: {UseBusy}", useBusy);
        if (useBusy)
        {
            IsBusy = true;
        }

        _isPickedById = false;
        var titleToSearch = SearchAgainTitle ?? SearchTitle;
        Logger.Info("Searching for {TitleToSearch}", titleToSearch);
        BusyContent = Lang.SearchingWithName.Format(titleToSearch);
        var result = SearchMode == MediaTypes.Game
            ? await _igdbObject.SearchGameAsync(titleToSearch.Replace(@"\", " "))
            : await _tmdbObject.SearchAsync(titleToSearch.Replace(@"\", " "), SearchMode);
        if (DataUtils.GetResultCount(_isPickedById, result.Result, SearchMode) == 0)
        {
            return;
        }

        SearchResult = result;
        if (useBusy)
        {
            IsBusy = false;
        }

        LoadData(titleToSearch);
    }

    private void LoadData(string searchTitle)
    {
        if (SearchResult?.Result is null)
        {
            IsSearchFocused = true;
            return;
        }
        
        if (!HasSearchResult())
        {
            return;
        }

        Logger.Info("Search result found for {SearchTitle}", searchTitle);
        ResultListViewData.Data = FileUtils.FetchAndAddDetailsToListView(SearchResult, searchTitle, _isPickedById);
        if (ResultListViewData.Data.Count == 0)
        {
            return;
        }

        ResultListViewData.SelectedItem = ResultListViewData.Data[0];
        PerformSelectionChanged();
    }

    private async void SearchAgainMethod()
    {
        if (!string.IsNullOrWhiteSpace(SearchAgainTitle))
        {
            await StartSearch(false);
        }
    }

    private void PickMethod(MouseButtonEventArgs eventArgs)
    {
        Logger.Trace("PickMethod called with {@EventArgs}", eventArgs);
        if (eventArgs is not null)
        {
            var dataContext = ((FrameworkElement)eventArgs.OriginalSource).DataContext;
            if (dataContext is not ListItem)
            {
                return;
            }
        }

        if (ResultListViewData.SelectedItem == null)
        {
            Logger.Warn("No Item selected");
            return;
        }
        var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
        var rating = "";
        if (CustomRating is not 0)
        {
            rating = CustomRating.ToString(CultureInfo.InvariantCulture);
            Logger.Info("Custom rating {Rating} selected", rating);
        }

        try
        {
            if (_isPickedById)
            {
                if (SearchResult.MediaType == MediaTypes.Game)
                {
                    _igdbObject.ResultPicked(SearchResult.Result[pickedIndex], _fullFolderPath, rating);
                }
                else
                {
                    _tmdbObject.ResultPicked(SearchResult.Result, SearchResult.MediaType,
                        _fullFolderPath, rating, _isPickedById);
                }
            }
            else if (SearchMode == MediaTypes.Game)
            {
                _igdbObject.ResultPicked(SearchResult.Result[pickedIndex], _fullFolderPath, rating);
            }
            else
            {
                _tmdbObject.ResultPicked(SearchResult.Result.Results[pickedIndex], SearchResult.MediaType,
                    _fullFolderPath, rating);
            }
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Exception occurred while picking result, {Message}", ex.Message)
                .Exception(ex)
                .Log();
            MessageBox.Show(ex.Message == "NoPoster"
                ? CustomMessageBox.Warning(Lang.NoPosterFound, SearchTitle)
                : CustomMessageBox.Error(ex.Message, SearchTitle));
        }

        CloseDialog("true");
    }

    private DelegateCommand _mouseDoubleClickCommand;
    public ICommand MouseDoubleClickCommand => _mouseDoubleClickCommand ??= new DelegateCommand(MouseDoubleClick);


    private void MouseDoubleClick()
    {
        Logger.Debug("MouseDoubleClick called with {@SelectedItem}", ResultListViewData.SelectedItem);
        if (ResultListViewData.SelectedItem == null)
        {
            return;
        }

        var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
        try
        {
            if (SearchResult.MediaType == MediaTypes.Game && SearchResult.Result[pickedIndex].Artworks is null)
            {
                Logger.Warn("No more poster found for {SearchTitle}", SearchTitle);
                MessageBox.Show(CustomMessageBox.Warning(Lang.NoPosterFound, SearchTitle));
                return;
            }
            _dialogParams.PickedIndex = pickedIndex;
            _dialogParams.ResultData = ResultListViewData.Data;
            _dialogParams.Result = SearchResult;
            _dialogService.ShowPosterPicker(_dialogParams);
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Exception occurred while showing more poster, {Message}", ex.Message)
                .Exception(ex)
                .Log();
            if (ex.Message == "NoPoster")
            {
                MessageBox.Show(CustomMessageBox.Warning(Lang.NoPosterFound, SearchTitle));
            }
        }
    }

    private DelegateCommand _selectionChanged;
    public ICommand SelectionChanged => _selectionChanged ??= new DelegateCommand(PerformSelectionChanged);

    private async void PerformSelectionChanged()
    {
        if (ResultListViewData.SelectedItem == null || !ResultListViewData.SelectedItem.TrailerKey.IsNullOrEmpty())
        {
            return;
        }

        var itemId = ResultListViewData.SelectedItem.Id;

        switch (SearchResult.MediaType)
        {
            case MediaTypes.Game:
                await HandleMediaType(itemId, GetGameTrailer);
                break;
            case MediaTypes.Movie:
                await HandleMediaType(itemId, GetMovieTrailer);
                break;
            case MediaTypes.Tv:
                await HandleMediaType(itemId, GetTvTrailer);
                break;
            case MediaTypes.Mtv:
                await HandleMtvMediaType(itemId);
                break;
            case MediaTypes.Collection:
                Logger.Warn("Collection media type not supported for trailer");
                break;
            default:
                Logger.Warn("Unknown media type {MediaType}", SearchResult.MediaType);
                break;
        }
    }

    private async Task HandleMtvMediaType(string itemId)
    {
        switch (ResultListViewData.SelectedItem.MediaType)
        {
            case MediaType.Tv:
                await HandleMediaType(itemId, GetTvTrailer);
                break;
            case MediaType.Movie:
                await HandleMediaType(itemId, GetMovieTrailer);
                break;
            default:
                Logger.Warn("Unknown media type {MediaType}", ResultListViewData.SelectedItem.MediaType);
                break;
        }
    }

    private static async Task HandleMediaType(string itemId, Func<string, Task> handleAction)
    {
        await handleAction(itemId);
    }

    private async Task GetGameTrailer(string itemId)
    {
        var r = await _igdbObject.GetGameVideo(itemId);
        if (r == null || r.Length == 0)
        {
            return;
        }

        if (Array.TrueForAll(r, v => v.Name != "Trailer"))
        {
            return;
        }
        SetVideos(r);
    }
    

    private async Task GetMovieTrailer(string itemId)
    {
        try 
        {
            var result = await _tmdbObject.GetClient().GetMovieVideosAsync(itemId.ConvertToInt());

            if (result != null)
            {
                SetVideos(result.Results);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error fetching movie trailer for {Title}", ResultListViewData.SelectedItem.Title);
        }
    }

    private async Task GetTvTrailer(string itemId)
    {
        var result = await _tmdbObject.GetClient().GetTvShowVideosAsync(itemId.ConvertToInt());

        if (result == null)
        {
            return;
        }
        SetVideos(result.Results);
    }

    private static Video ChooseTrailer(IReadOnlyCollection<Video> results)
    {
        var trailerYouTube = results.FirstOrDefault(item => item is { Type: "Trailer", Site: "YouTube" });
        return trailerYouTube ?? results.FirstOrDefault();
    }

    private void SetVideos(List<Video> videos)
    {
        if (videos == null || videos.Count == 0)
        {
            return;
        }

        var trailer = ChooseTrailer(videos);
        var videoList = new List<MediaVideo>
        {
            new(trailer.Name, YoutubeEmbedUrl.Format(trailer.Key))
        };

        videoList.AddRange(
            videos.Where(video => video != trailer)
                .Select(video => new MediaVideo(video.Name, YoutubeEmbedUrl.Format(video.Key)))
        );
        
        ResultListViewData.SelectedItem.Videos = videoList;
    }

    private void SetVideos(GameVideo[] videos)
    {
        var trailer = videos.First(v => v.Name == "Trailer");
        
        var videoList = new List<MediaVideo>
        {
            new(trailer.Name, YoutubeEmbedUrl.Format(trailer.VideoId))
        };

        videoList.AddRange(
            videos.Where(video => video != trailer)
                .Select(video => new MediaVideo(video.Name, YoutubeEmbedUrl.Format(video.VideoId)))
        );

        ResultListViewData.SelectedItem.Videos = videoList;
    }
    
    private void ResetPoster()
    {
        if (ResultListViewData.SelectedItem == null)
        {
            Logger.Warn("ResetPoster called with null SelectedItem");
            return;
        }
        ResultListViewData.SelectedItem.ResetInitialPoster();
    }
    
    private bool HasSearchResult()
    {
        if (_isPickedById)
        {
            return SearchResult.Result != null;
        }

        if (SearchMode == MediaTypes.Game)
        {
            return (SearchResult.Result as Game[])?.Length != 0;
        }
    
        return SearchResult.Result.TotalResults != 0;
    }
}