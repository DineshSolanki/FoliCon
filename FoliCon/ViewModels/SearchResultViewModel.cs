using FoliCon.Models;
using FoliCon.Modules;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.ObjectModel;

namespace FoliCon.ViewModels
{
    public class SearchResultViewModel : BindableBase, IDialogAware
    {
        #region Variables
        private string _title = "Search Result";
        private string _searchTitle = " Night of the Day of the Dawn of the Son of the Bride of the Return of the Revenge of the Terror of the Attack of the Evil, Mutant, Hellbound, Flesh-Eating Subhumanoid Zombified Living Dead, Part 2: In Shocking ";
        private string _busyContent = "searching";
        private bool _isBusy = false;
        private string _searchMode;
        private ListViewData _resultListViewData;
        private string _searchAgainTitle;
        private ArrayList _fileList;
        private ResultResponse _searchResult;
        private string _fullFolderPath;
        private readonly IDialogService _dialogService;
        private bool _isSearchFocused = false;

        public event Action<IDialogResult> RequestClose;
        public TMDB tmdbObject;
        public IGDBClass igdbObject;

        #endregion

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
        public ResultResponse SearchResult { get=> _searchResult;set=> SetProperty(ref _searchResult,value);}
        public string SearchMode { get=> _searchMode;set=> SetProperty(ref _searchMode,value);}
        public bool IsSearchFocused { get => _isSearchFocused; set => SetProperty(ref _isSearchFocused, value); }
        #endregion

        #region Commands
        public DelegateCommand PickCommand { get; private set; }
        public DelegateCommand SkipCommand { get; private set; }
        public DelegateCommand SkipAllCommand { get; private set; }
        public DelegateCommand SearchAgainCommand { get; private set; }

        #endregion
        public SearchResultViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            SearchAgainCommand = new DelegateCommand(SearchAgainMethod);
            SkipCommand = new DelegateCommand(delegate { CloseDialog("false"); });
            ResultListViewData = new ListViewData { Data = null, SelectedItem = null };
            PickCommand=new DelegateCommand(PickMethod);
            SkipAllCommand=new DelegateCommand(delegate { GlobalVariables.SkipAll=true; CloseDialog("false");});
        }


        protected virtual void CloseDialog(string parameter)
        {
            ButtonResult result = ButtonResult.None;

            if (parameter?.ToLower() == "true")
                result = ButtonResult.OK;
            else if (parameter?.ToLower() == "false")
                result = ButtonResult.Cancel;

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
            SearchMode=parameters.GetValue<string>("searchmode");
            tmdbObject=parameters.GetValue<TMDB>("tmdbObject");
            igdbObject=parameters.GetValue<IGDBClass>("igdbObject");
            _fullFolderPath=parameters.GetValue<string>("folderpath");
           LoadData();
        }
        private async void StartSearch(bool useBusy)
        {
            if (useBusy)
            {
                IsBusy=true;
            }

            string titleToSearch;
            if (SearchAgainTitle != null)
            {
                titleToSearch = SearchAgainTitle;
            }
            else
            {
                titleToSearch = SearchTitle;
            }
            BusyContent = "Searching for " + titleToSearch + "...";
            ResultResponse result;
            if (SearchMode == MediaTypes.Game)
            {
               result=await igdbObject.SearchGameAsync(titleToSearch);
            }
            else
            {
                result=await tmdbObject.SearchAsync(titleToSearch,SearchMode);
            }
            SearchResult=result;
            if (useBusy)
            {
                IsBusy = false;
            }
           LoadData();
        }
        private void LoadData()
        {
            if (SearchResult != null
                && ((SearchMode == "Game") ? SearchResult.Result.Length : SearchResult.Result.TotalResults) != null
                && ((SearchMode == "Game") ? SearchResult.Result.Length : SearchResult.Result.TotalResults) != 0)
            {
                ResultListViewData.Data = Util.FetchAndAddDetailsToListView(SearchResult, SearchTitle);
                ResultListViewData.SelectedItem=ResultListViewData.Data[0];
            }
            else
            {
                IsSearchFocused=true;
            }
            FileList = Util.GetFileNamesFromFolder(_fullFolderPath);
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
            if (ResultListViewData.SelectedItem!=null)
            {
                int PickedIndex = ResultListViewData.Data.IndexOf(ResultListViewData.SelectedItem);
                try
                {
                    if (SearchMode == MediaTypes.Game)
                    {
                        igdbObject.ResultPicked(SearchResult.Result[PickedIndex],_fullFolderPath);
                    }
                    else
                    {
                        tmdbObject.ResultPicked(SearchResult.Result.Results[PickedIndex],SearchResult.MediaType,_fullFolderPath);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "NoPoster")
                    {
                        DialogParameters p= new DialogParameters {
                            {"title","No Poster" }, {"message", "No poster found."}
                            };
                       _dialogService.ShowDialog("MessageBox",p,result=>{ });
                    }
                }
                CloseDialog("true");
            }
        }
    }
}

