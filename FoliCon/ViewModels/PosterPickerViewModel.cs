using FoliCon.Models;
using FoliCon.Modules;
using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;

namespace FoliCon.ViewModels
{
    public class PosterPickerViewModel : BindableBase, IDialogAware
    {
        #region Variables
        private string _title = "Search Result";
        private bool _stopSearch;
        private int _i;
        private string _busyContent = "searching";
        private bool _isBusy;
        public event Action<IDialogResult> RequestClose;
        private ResultResponse _result;
        private const string PosterBase = "http://image.tmdb.org/t/p/original";
        private const string SmallPosterBase = "https://image.tmdb.org/t/p/w200";
        #endregion

        #region Properties
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
        public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public ResultResponse Result { get => _result; set => SetProperty(ref _result, value); }
        public int PickedIndex { get; private set; }
        public Tmdb tmdbObject { get; private set; }

        private ObservableCollection<ListItem> resultList;

        public ObservableCollection<DArtImageList> ImageUrl { get; set; }
        public DelegateCommand StopSearchCommand { get; set; }
        public DelegateCommand<object> PickCommand { get; set; }
        #endregion

        public PosterPickerViewModel()
        {
            ImageUrl = new ObservableCollection<DArtImageList>();
            StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
            PickCommand = new DelegateCommand<object>(PickMethod);
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

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Result = parameters.GetValue<ResultResponse>("result");
            PickedIndex = parameters.GetValue<int>("pickedIndex");
            tmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
            resultList = parameters.GetValue<ObservableCollection<ListItem>>("resultList");
            LoadData(Result.Result.Results[PickedIndex], Result.MediaType);
        }
        public void LoadData(dynamic result, string resultType)
        {
            ImagesWithId images = new();
            if (resultType == MediaTypes.Tv)
            {
                var pickedResult = (SearchTv)result;
                images = tmdbObject.SearchTvImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Movie)
            {
                var pickedResult = (SearchMovie)result;
                images = tmdbObject.SearchMovieImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Collection)
            {
                var pickedResult = (SearchCollection)result;
                images = tmdbObject.SearchCollectionImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Mtv)
            {
                MediaType mediaType = result.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                        {
                            SearchTv pickedResult = result;
                            images = tmdbObject.SearchTvImages(pickedResult.Id);
                            break;
                        }
                    case MediaType.Movie:
                        {
                            SearchMovie pickedResult = result;
                            images = tmdbObject.SearchMovieImages(pickedResult.Id);
                            break;
                        }
                }
            }
            LoadImages(images);
        }

        private async void LoadImages(ImagesWithId images)
        {
            StopSearch = false;
            ImageUrl.Clear();
            BusyContent = $"Loading posters...";
            IsBusy = true;
            if (images is not null && images.Posters.Count>=0)
            {
                foreach (var image in images.Posters)
                {
                    if (image is not null)
                    {
                        string posterPath = image.FilePath != null ? tmdbObject.GetClient().GetImageUrl(PosterSize.W342, image.FilePath).ToString() : null;
                        var bm = await Util.GetBitmapFromUrlAsync(posterPath);
                        ImageUrl.Add(new DArtImageList(posterPath, Util.LoadBitmap(bm)));
                        bm.Dispose();
                    }
                    if (_stopSearch)
                    {
                        break;
                    }
                }
            }
            else
            {
                IsBusy = false;
                MessageBox.Warning("No posters found!", "No Result");
                CloseDialog("true");
            }
            IsBusy = false;
        }
        private void PickMethod(object parameter)
        {
            var link = (string)parameter;
            Result.Result.Results[PickedIndex].PosterPath = link;
            resultList[PickedIndex].Poster = link;
            CloseDialog("true");
        }
    }
}
