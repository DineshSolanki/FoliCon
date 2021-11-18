﻿namespace FoliCon.ViewModels
{
    public class ProSearchResultViewModel : BindableBase, IDialogAware
    {
        private string _title = LangProvider.GetLang("SearchResult");
        private bool _stopSearch;
        private string _searchTitle;
        private string _searchAgainTitle;
        private int _i;
        private string _busyContent = LangProvider.GetLang("Searching");
        private bool _isBusy;
        private DArt _dArtObject;
        private DataTable _listDataTable;
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
        public DelegateCommand SearchAgainCommand { get; set; }
        public DelegateCommand StopSearchCommand { get; set; }

        public ProSearchResultViewModel()
        {
            ImageUrl = new ObservableCollection<DArtImageList>();
            StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
            PickCommand = new DelegateCommand<object>(PickMethod);
            OpenImageCommand = new DelegateCommand<object>(OpenImageMethod);
            SkipCommand = new DelegateCommand(SkipMethod);
            SearchAgainCommand = new DelegateCommand(PrepareForSearch);
        }

        private void OpenImageMethod(object parameter)
        {
            var link = (string)parameter;
            var browser = new ImageBrowser(link)
            {
                ShowTitle = false,
                IsFullScreen = true
            };
            browser.Show();
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
            Index = 0;
            while (true)
            {
                var searchResult = await DArtObject.Browse(query, offset);
                if (searchResult.Results?.Length > 0)
                {
                    TotalPosters = searchResult.Results.Count( result => result.IsDownloadable) + offset;
                    foreach (var item in searchResult.Results.GetEnumeratorWithIndex())
                    {
                        if (!item.Value.IsDownloadable)
                            continue;
                        using (var bm = await Util.GetBitmapFromUrlAsync(item.Value.Thumbs[0].Src))
                        {
                            ImageUrl.Add(new DArtImageList(item.Value.Content.Src, Util.LoadBitmap(bm)));
                            bm.Dispose();
                        }
                        if (_stopSearch)
                        {
                            return;
                        }

                        Index++;
                    }
                    if (searchResult.HasMore)
                    {
                        offset = searchResult.NextOffset;
                        continue;
                    }
                }
                else
                {
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
            SearchAgainTitle = null;
            var link = (string)parameter;
            var currentPath = $@"{_folderPath}\{Fnames[_i]}";
            var tempImage = new ImageToDownload
            {
                LocalPath = $"{currentPath}\\{Fnames[_i]}.png",
                RemotePath = new Uri(link)
            };
            Util.AddToPickedListDataTable(_listDataTable, "", SearchTitle, "", currentPath, Fnames[_i]);
            _imgDownloadList.Add(tempImage);
            _i++;
            if (!(_i > Fnames.Count - 1))
            {
                PrepareForSearch();
            }
            else
            {
                CloseDialog("true");
            }
        }

        private void SkipMethod()
        {
            _i++;
            SearchAgainTitle = null;
            if (!(_i > Fnames.Count - 1))
            {
                PrepareForSearch();
            }
            else
            {
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
            _listDataTable = parameters.GetValue<DataTable>("pickedtable");
            _imgDownloadList = parameters.GetValue<List<ImageToDownload>>("imglist");
            _folderPath = parameters.GetValue<string>("folderpath");
            PrepareForSearch();
        }
    }
}