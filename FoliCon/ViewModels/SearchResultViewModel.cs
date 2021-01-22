using FoliCon.Models;
using FoliCon.Modules;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Globalization;

namespace FoliCon.ViewModels
{
    public class SearchResultViewModel : BindableBase, IDialogAware
    {
        #region Variables

        private string _title = "Search Result";
        private string _searchTitle = " Night of the Day of the Dawn of the Son of the Bride of the Return of the Revenge of the Terror of the Attack of the Evil, Mutant, Hellbound, Flesh-Eating Subhumanoid Zombified Living Dead, Part 2: In Shocking ";
        private string _busyContent = "searching";
        private bool _isBusy;
        private string _searchMode;
        private ListViewData _resultListViewData;
        private string _searchAgainTitle;
        private ArrayList _fileList;
        private ResultResponse _searchResult;
        private string _fullFolderPath;
        private readonly IDialogService _dialogService;
        private bool _isSearchFocused;

        public event Action<IDialogResult> RequestClose;

        private Tmdb _tmdbObject;
        private IgdbClass _igdbObject;

        #endregion Variables

        #region Properties

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string SearchTitle { get => _searchTitle; set => SetProperty(ref _searchTitle, value); }
        public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        //public string LoadingPoster { get; private set; } = @"\Resources\LoadingPosterImage.png";
        //public string NoPoster { get; private set; } = @"\Resources\NoPosterAvailable.png";
        public ListViewData ResultListViewData { get => _resultListViewData; set => SetProperty(ref _resultListViewData, value); }

        public string SearchAgainTitle { get => _searchAgainTitle; set => SetProperty(ref _searchAgainTitle, value); }
        public ArrayList FileList { get => _fileList; set => SetProperty(ref _fileList, value); }
        public ResultResponse SearchResult { get => _searchResult; set => SetProperty(ref _searchResult, value); }
        public string SearchMode { get => _searchMode; set => SetProperty(ref _searchMode, value); }
        public bool IsSearchFocused { get => _isSearchFocused; set => SetProperty(ref _isSearchFocused, value); }

        #endregion Properties

        #region Commands

        public DelegateCommand PickCommand { get; }
        public DelegateCommand SortResultCommand { get; }
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
            PickCommand = new DelegateCommand(PickMethod);
            SortResultCommand = new DelegateCommand(SortResult);
            SkipAllCommand = new DelegateCommand(delegate { GlobalVariables.SkipAll = true; CloseDialog("false"); });
        }

        private void SortResult()
        {
            
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
            LoadData(SearchTitle);
        }

        private async void StartSearch(bool useBusy)
        {
            if (useBusy)
            {
                IsBusy = true;
            }

            var titleToSearch = SearchAgainTitle ?? SearchTitle;
            BusyContent = "Searching for " + titleToSearch + "...";
            ResultResponse result;
            if (SearchMode == MediaTypes.Game)
            {

                result = await _igdbObject.SearchGameAsync(titleToSearch.Replace(@"\", " "));
            }
            else
            {
                result = await _tmdbObject.SearchAsync(titleToSearch.Replace(@"\", " "), SearchMode);
            }
            SearchResult = result;
            if (useBusy)
            {
                IsBusy = false;
            }
            LoadData(titleToSearch);
        }

        private void LoadData(string _searchTitle)
        {
            if (SearchResult != null
                && (SearchMode == "Game" ? SearchResult.Result.Length : SearchResult.Result.TotalResults) != null
                && (SearchMode == "Game" ? SearchResult?.Result.Length : SearchResult?.Result.TotalResults) != 0)
            {
                ResultListViewData.Data = Util.FetchAndAddDetailsToListView(SearchResult, _searchTitle);
                if (ResultListViewData.Data.Count != 0)
                    ResultListViewData.SelectedItem = ResultListViewData.Data[0];
            }
            else
            {
                IsSearchFocused = true;
            }
            FileList = new ArrayList { Util.GetFileNamesFromFolder(_fullFolderPath) };
        }

        private void SearchAgainMethod()
        {
            if (!string.IsNullOrWhiteSpace(SearchAgainTitle))
            {
                StartSearch(false);
            }
        }

        private void PickMethod()
        {
            if (ResultListViewData.SelectedItem == null) return;
            var pickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
            try
            {
                if (SearchMode == MediaTypes.Game)
                {
                    _igdbObject.ResultPicked(SearchResult.Result[pickedIndex], _fullFolderPath);
                }
                else
                {
                    _tmdbObject.ResultPicked(SearchResult.Result.Results[pickedIndex], SearchResult.MediaType, _fullFolderPath);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "NoPoster")
                {
                    var p = new DialogParameters {
                        {"title","No Poster" }, {"message", "No poster found."}
                    };
                    _dialogService.ShowDialog("MessageBox", p, _ => { });
                }
            }
            CloseDialog("true");
        }
    }
}