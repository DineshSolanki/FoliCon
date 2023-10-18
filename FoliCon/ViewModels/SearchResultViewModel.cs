using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Modules.Extension;
using FoliCon.Modules.IGDB;
using FoliCon.Modules.TMDB;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using NLog;
using DelegateCommand = Prism.Commands.DelegateCommand;
using Logger = NLog.Logger;

namespace FoliCon.ViewModels;

public class SearchResultViewModel : BindableBase, IDialogAware
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    #region Variables

    private string _title = LangProvider.GetLang("SearchResult");
    private string _searchTitle;
    private string _busyContent;
    private bool _isBusy;
    private string _searchMode;
    private ListViewData _resultListViewData;
    private string _searchAgainTitle;
    private List<string> _fileList;
    private ResultResponse _searchResult;
    private string _fullFolderPath;
    private readonly IDialogService _dialogService;
    private bool _isSearchFocused;
    private bool _isPickedById;
    public event Action<IDialogResult> RequestClose;

    private Tmdb _tmdbObject;
    private IgdbClass _igdbObject;
    private double _customRating;

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

    #endregion Properties

    #region Commands

    public DelegateCommand<MouseButtonEventArgs> PickCommand { get; }
    public DelegateCommand SkipCommand { get; }
    public DelegateCommand SkipAllCommand { get; }
    public DelegateCommand SearchAgainCommand { get; }

    #endregion Commands

    public SearchResultViewModel(IDialogService dialogService)
    {
        //TODO:Localize 'Video Not Available' string
        _dialogService = dialogService;
        SearchAgainCommand = new DelegateCommand(SearchAgainMethod);
        SkipCommand = new DelegateCommand(delegate { CloseDialog("false"); });
        ResultListViewData = new ListViewData { Data = null, SelectedItem = null };
        PickCommand = new DelegateCommand<MouseButtonEventArgs>(PickMethod);
        SkipAllCommand = new DelegateCommand(delegate
        {
            GlobalVariables.SkipAll = true;
            CloseDialog("false");
        });
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
        SearchTitle = parameters.GetValue<string>("query");
        SearchResult = parameters.GetValue<ResultResponse>("result");
        SearchMode = parameters.GetValue<string>("searchmode");
        _tmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
        _igdbObject = parameters.GetValue<IgdbClass>("igdbObject");
        _fullFolderPath = parameters.GetValue<string>("folderpath");
        _isPickedById = parameters.GetValue<bool>("isPickedById");
        LoadData(SearchTitle);
        SearchAgainTitle = SearchTitle;
        FileList = FileUtils.GetFileNamesFromFolder(_fullFolderPath);
    }

    private async void StartSearch(bool useBusy)
    {
        Logger.Debug("StartSearch called, show loader: {UseBusy}", useBusy);
        if (useBusy)
        {
            IsBusy = true;
        }

        _isPickedById = false;
        var titleToSearch = SearchAgainTitle ?? SearchTitle;
        Logger.Info("Searching for {TitleToSearch}", titleToSearch);
        BusyContent = LangProvider.GetLang("SearchingWithName").Format(titleToSearch);
        var result = SearchMode == MediaTypes.Game
            ? await _igdbObject.SearchGameAsync(titleToSearch.Replace(@"\", " "))
            : await _tmdbObject.SearchAsync(titleToSearch.Replace(@"\", " "), SearchMode);
        if (DataUtils.GetResultCount(_isPickedById, result.Result, SearchMode) == 0) return;
        SearchResult = result;
        if (useBusy)
        {
            IsBusy = false;
        }

        LoadData(titleToSearch);
    }

    private void LoadData(string searchTitle)
    {
        if (SearchResult != null
            && (_isPickedById ? SearchResult.Result != null ? 1 :
                null :
                SearchMode == "Game" ? SearchResult.Result.Length : SearchResult.Result.TotalResults) != null
            && (_isPickedById ? SearchResult?.Result != null ? 1 :
                0 :
                SearchMode == "Game" ? SearchResult?.Result?.Length : SearchResult?.Result?.TotalResults) != 0)
        {
            Logger.Info("Search result found for {SearchTitle}", searchTitle);
            ResultListViewData.Data = FileUtils.FetchAndAddDetailsToListView(SearchResult, searchTitle, _isPickedById);
            if (ResultListViewData.Data.Count == 0) return;
            ResultListViewData.SelectedItem = ResultListViewData.Data[0];
            PerformSelectionChanged();
            
        }
        else
        {
            IsSearchFocused = true;
        }
    }

    private void SearchAgainMethod()
    {
        if (!string.IsNullOrWhiteSpace(SearchAgainTitle))
        {
            StartSearch(false);
        }
    }

    private void PickMethod(MouseButtonEventArgs eventArgs)
    {
        Logger.Trace("PickMethod called with {@EventArgs}", eventArgs);
        if (eventArgs is not null)
        {
            var dataContext = ((FrameworkElement)eventArgs.OriginalSource).DataContext;
            if (dataContext is not ListItem) return;
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
                ? CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), SearchTitle)
                : CustomMessageBox.Error(ex.Message, SearchTitle));
        }

        CloseDialog("true");
    }

    private DelegateCommand _mouseDoubleClickCommand;
    public ICommand MouseDoubleClickCommand => _mouseDoubleClickCommand ??= new DelegateCommand(MouseDoubleClick);


    private void MouseDoubleClick()
    {
        Logger.Debug("MouseDoubleClick called with {@SelectedItem}", ResultListViewData.SelectedItem);
        if (ResultListViewData.SelectedItem == null) return;
        var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
        try
        {
            if (SearchResult.MediaType == MediaTypes.Game)
            {
                if (SearchResult.Result[pickedIndex].Artworks is null)
                {
                    Logger.Warn("No more poster found for {SearchTitle}", SearchTitle);
                    MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), SearchTitle));
                    return;
                }
            }

            _dialogService.ShowPosterPicker(_tmdbObject, _igdbObject, SearchResult, pickedIndex,
                ResultListViewData.Data,
                _isPickedById, _ => { });
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Exception occurred while showing more poster, {Message}", ex.Message)
                .Exception(ex)
                .Log();
            if (ex.Message == "NoPoster")
            {
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), SearchTitle));
            }
        }
    }

    private DelegateCommand _selectionChanged;
    public ICommand SelectionChanged => _selectionChanged ??= new DelegateCommand(PerformSelectionChanged);

    private async void PerformSelectionChanged()
    {
        if (ResultListViewData.SelectedItem == null || !ResultListViewData.SelectedItem.TrailerKey.IsNullOrEmpty())
            return;

        var itemId = ResultListViewData.SelectedItem.Id;

        if (SearchResult.MediaType == MediaTypes.Game)
        {
            await HandleMediaType(itemId, GetGameTrailer);
        }
        else if (SearchResult.MediaType == MediaTypes.Movie)
        {
            await HandleMediaType(itemId, GetMovieTrailer);
        }
        else if (SearchResult.MediaType == MediaTypes.Tv)
        {
            await HandleMediaType(itemId, GetTvTrailer);
        }
        else if (SearchResult.MediaType == MediaTypes.Mtv)
        {
            await HandleMtvMediaType(itemId);
        }
        else
        {
            Logger.Warn("Unknown media type {MediaType}", SearchResult.MediaType);
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

    private async Task HandleMediaType(string itemId, Func<string, Task> handleAction)
    {
        await handleAction(itemId);
    }

    private async Task GetGameTrailer(string itemId)
    {
        var r = await _igdbObject.GetGameVideo(itemId);
        if (r == null || r.Length == 0) return;
        if (r.All(v => v.Name != "Trailer")) return;
        var key = r.First(v => v.Name == "Trailer");
        SetTrailer(key.VideoId);
    }

    private async Task GetMovieTrailer(string itemId)
    {
        await _tmdbObject.GetClient().GetMovieVideosAsync(itemId.ConvertToInt()).ContinueWith(item =>
        {
            if (item.Result == null) return;
            var i = ChooseTrailer(item.Result.Results);
            if (i != null)
            {
                SetTrailer(i.Key);
            }
            else
            {
                Logger.Warn("No trailer found for {Title}", ResultListViewData.SelectedItem.Title);
            }
        });
    }

    private async Task GetTvTrailer(string itemId)
    {
        await _tmdbObject.GetClient().GetTvShowVideosAsync(itemId.ConvertToInt()).ContinueWith(item =>
        {
            if (item.Result == null) return;
            var i = ChooseTrailer(item.Result.Results);
            if (i != null)
            {
                SetTrailer(i.Key);
            }
            else
            {
                Logger.Warn("No trailer found for {Title}", ResultListViewData.SelectedItem.Title);
            }
        });
    }

    private dynamic? ChooseTrailer(IReadOnlyCollection<dynamic> results)
    {
        if (!results.Any()) return null;
        return results.Any(i => i.Type == "Trailer" && i.Site == "YouTube")
            ? results.First(i => i.Type == "Trailer")
            : results.First();
    }

    private void SetTrailer(string trailerKey)
    {
        ResultListViewData.SelectedItem.TrailerKey = trailerKey;
        ResultListViewData.SelectedItem.Trailer =
            new Uri("https://www.youtube.com/embed/" + trailerKey);
        Logger.Debug("Trailer for {Title} is {Trailer}", ResultListViewData.SelectedItem.Title,
            ResultListViewData.SelectedItem.Trailer);
    }
}