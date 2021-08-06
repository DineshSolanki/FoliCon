using FoliCon.Models;
using FoliCon.Modules;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using FoliCon.Properties.Langs;
using IGDB;
using IGDB.Models;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Collection = TMDbLib.Objects.Collections.Collection;

namespace FoliCon.ViewModels
{
    public class PosterPickerViewModel : BindableBase, IDialogAware
    {
        #region Variables
        private string _title = "";
        private bool _stopSearch;
        private int _index;
        private string _busyContent = LangProvider.GetLang("LoadingPosters");
        private bool _isBusy;
        public event Action<IDialogResult> RequestClose;
        private ResultResponse _result;
        private int _totalPosters;
        private bool _isPickedById;
        #endregion

        #region Properties
        public int Index { get => _index; set => SetProperty(ref _index, value); }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
        public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public ResultResponse Result { get => _result; set => SetProperty(ref _result, value); }
        public int PickedIndex { get; private set; }
        public Tmdb TmdbObject { get; private set; }
        public IgdbClass IgdbObject { get; private set; }
        public int TotalPosters { get => _totalPosters; set => SetProperty(ref _totalPosters, value); }
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
            TmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
            IgdbObject = parameters.GetValue<IgdbClass>("igdbObject");
            resultList = parameters.GetValue<ObservableCollection<ListItem>>("resultList");
            _isPickedById = parameters.GetValue<bool>("isPickedById");
            LoadData();
            
        }
        public async void LoadData()
        {
            var resultType = Result.MediaType;
            var response = _isPickedById
                ? resultType == MediaTypes.Game ? Result.Result[0] : Result.Result
                : resultType == MediaTypes.Game ? Result.Result : Result.Result.Results[PickedIndex];

            if (resultType != MediaTypes.Game)
            {
                ImagesWithId images = new();
                if (resultType == MediaTypes.Tv)
                {
                    dynamic pickedResult = _isPickedById ? (TvShow)response : (SearchTv)response;
                    Title = pickedResult.Name;
                    images = TmdbObject.SearchTvImages(pickedResult.Id);
                }
                else if (resultType == MediaTypes.Movie)
                {
                    dynamic pickedResult = _isPickedById ? (Movie)response : (SearchMovie)response;
                    Title = pickedResult.Title;
                    images = TmdbObject.SearchMovieImages(pickedResult.Id);
                }
                else if (resultType == MediaTypes.Collection)
                {
                    dynamic pickedResult = _isPickedById ? (Collection)response : (SearchCollection)response;
                    Title = pickedResult.Name;
                    images = TmdbObject.SearchCollectionImages(pickedResult.Id);
                }
                else if (resultType == MediaTypes.Mtv)
                {
                    MediaType mediaType = response.MediaType;
                    switch (mediaType)
                    {
                        case MediaType.Tv:
                        {
                            SearchTv pickedResult = response;
                            Title = pickedResult.Name;
                            images = TmdbObject.SearchTvImages(pickedResult.Id);
                            break;
                        }
                        case MediaType.Movie:
                        {
                            SearchMovie pickedResult = response;
                            Title = pickedResult.Title;
                            images = TmdbObject.SearchMovieImages(pickedResult.Id);
                            break;
                        }
                    }
                }
                LoadImages(images);
            }
            else
            {
                Artwork[] images =response.Artworks.Values;
                LoadImages(images);
            }
        }

        private async void LoadImages(ImagesWithId images)
        {
            
            StopSearch = false;
            ImageUrl.Clear();
            IsBusy = true;
            if (images is not null && images.Posters.Count > 0)
            {
                TotalPosters = images.Posters.Count;

                foreach (var item in images.Posters.GetEnumeratorWithIndex())
                {
                    var image = item.Value;
                    Index = item.Index + 1;
                    if (image is not null)
                    {
                        var posterPath = image.FilePath != null ? TmdbObject.GetClient().GetImageUrl(PosterSize.W342, image.FilePath).ToString() : null;
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
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), Title));
            }
            IsBusy = false;
        }
        private async void LoadImages(Artwork[] images)
        {
            StopSearch = false;
            ImageUrl.Clear();
            IsBusy = true;
            if (images is not null && images.Length > 0)
            {
                TotalPosters = images.Length;

                foreach (var item in images.GetEnumeratorWithIndex())
                {
                    var image = item.Value;
                    Index = item.Index + 1;
                    if (image is not null)
                    {
                        var posterPath = image.ImageId != null ? "https://" + ImageHelper.GetImageUrl(item.Value.ImageId, ImageSize.ScreenshotMed)[2..] : null;
                        var bm = await Util.GetBitmapFromUrlAsync(posterPath);
                        ImageUrl.Add(new DArtImageList(image.ImageId, Util.LoadBitmap(bm)));
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
                MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), Title));
            }
            IsBusy = false;
        }
        private void PickMethod(object parameter)
        {
            var link = (string)parameter;
            var result = _isPickedById
                ? Result.MediaType == MediaTypes.Game ? Result.Result[0] : Result.Result
                : Result.MediaType == MediaTypes.Game ? Result.Result : Result.Result.Results[PickedIndex];
            if (Result.MediaType == MediaTypes.Game)
            {
                result.Cover.Value.ImageId = link;
                resultList[PickedIndex].Poster = "https://" + ImageHelper.GetImageUrl(link, ImageSize.HD720)[2..];
            }
            else
            {
                result.PosterPath = link;
                resultList[PickedIndex].Poster = link;
            }
            CloseDialog("true");
        }

    }
}
