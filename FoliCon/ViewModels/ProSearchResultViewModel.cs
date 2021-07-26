using FoliCon.Models;
using FoliCon.Modules;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace FoliCon.ViewModels
{
    public class ProSearchResultViewModel : BindableBase, IDialogAware
    {
        private string _title = "Search Result";
        private bool _stopSearch;
        private string _searchTitle;
        private string _searchAgainTitle;
        private int _i;
        private string _busyContent = "searching";
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
        public DelegateCommand SearchAgainCommand { get; set; }
        public DelegateCommand StopSearchCommand { get; set; }

        public ProSearchResultViewModel()
        {
            ImageUrl = new ObservableCollection<DArtImageList>();
            StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
            PickCommand = new DelegateCommand<object>(PickMethod);
            SkipCommand = new DelegateCommand(SkipMethod);
            SearchAgainCommand = new DelegateCommand(PrepareForSearch);
        }

        private async void PrepareForSearch()
        {
            StopSearch = false;
            ImageUrl.Clear();
            SearchTitle = null;
            SearchTitle = !string.IsNullOrEmpty(SearchAgainTitle) ? SearchAgainTitle : TitleCleaner.Clean(Fnames[_i]);
            BusyContent = $"Searching for {SearchTitle}...";
            IsBusy = true;
            Title = $"Pick Icon for {SearchTitle}";
            await Search(SearchTitle);
            SearchAgainTitle = null;
            IsBusy = false;
        }

        private async Task Search(string query, int offset = 0)
        {
            int lastIndex;
            Index = 0;
            while (true)
            {
                var searchResult = await DArtObject.Browse(query, offset);
                lastIndex = Index;
                if (searchResult.Results?.Length > 0)
                {
                    TotalPosters = searchResult.Results.Length + offset;
                    foreach (var item in searchResult.Results.GetEnumeratorWithIndex())
                    {
                        Index = item.Index + lastIndex;
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
                    MessageBox.Warning("No result Found, Try to search again with correct title", "No Result"); //TODO: Solve exception here
                    IsSearchFocused = true;
                }

                break;
            }
        }

        private void PickMethod(object parameter)
        {
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