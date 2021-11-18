using DelegateCommand = Prism.Commands.DelegateCommand;

namespace FoliCon.ViewModels;

public class SearchResultViewModel : BindableBase, IDialogAware
{
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
    private string _customRating;

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

    public string CustomRating
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
        FileList = Util.GetFileNamesFromFolder(_fullFolderPath);
    }

    private async void StartSearch(bool useBusy)
    {
        if (useBusy)
        {
            IsBusy = true;
        }

        _isPickedById = false;
        var titleToSearch = SearchAgainTitle ?? SearchTitle;
        BusyContent = LangProvider.GetLang("SearchingWithName").Format(titleToSearch);
        var result = SearchMode == MediaTypes.Game
            ? await _igdbObject.SearchGameAsync(titleToSearch.Replace(@"\", " "))
            : await _tmdbObject.SearchAsync(titleToSearch.Replace(@"\", " "), SearchMode);
        if (Util.GetResultCount(_isPickedById, result.Result, SearchMode) == 0) return;
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
            ResultListViewData.Data = Util.FetchAndAddDetailsToListView(SearchResult, searchTitle, _isPickedById);
            if (ResultListViewData.Data.Count != 0)
                ResultListViewData.SelectedItem = ResultListViewData.Data[0];
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
        if (eventArgs is not null)
        {
            var dataContext = ((FrameworkElement)eventArgs.OriginalSource).DataContext;
            if (dataContext is not ListItem) return;
        }

        if (ResultListViewData.SelectedItem == null) return;
        var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
        var rating = "";
        if (CustomRating is not null && _customRating != "_._")
        {
            rating = CustomRating.Replace('_', '0');
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
        if (ResultListViewData.SelectedItem == null) return;
        var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
        try
        {
            if (SearchResult.MediaType == MediaTypes.Game)
            {
                if (SearchResult.Result[pickedIndex].Artworks is null)
                {
                    MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), SearchTitle));
                    return;
                }
            }

            _dialogService.ShowPosterPicker(_tmdbObject, _igdbObject, SearchResult, pickedIndex,
                ResultListViewData.Data,
                _isPickedById, r => { });
        }
        catch (Exception ex)
        {
            if (ex.Message == "NoPoster")
            {
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), SearchTitle));
            }
#if DEBUG
            MessageBox.Show(CustomMessageBox.Warning(ex.Message, LangProvider.GetLang("ExceptionOccurred")));
#endif
        }
    }
}