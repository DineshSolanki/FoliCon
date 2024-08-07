﻿using FoliCon.Models.Data.Wrapper;
using Artwork = IGDB.Models.Artwork;

namespace FoliCon.ViewModels;

[Localizable(false)]
public class PosterPickerViewModel : BindableBase, IDialogAware
{
    private const string PosterPathMessage = "Poster Path: {PosterPath}";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    #region Variables
    private string _title = "";
    private bool _stopSearch;
    private int _index;
    private string _busyContent = Lang.LoadingPosters;
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
    private ObservableCollection<ListItem> _resultList;

    public ObservableCollection<DArtImageList> ImageUrl { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }
    public DelegateCommand<DArtImageList> PickCommand { get; set; }
    public DelegateCommand<DArtImageList> OpenImageCommand { get; set; }
    #endregion
    public PosterPickerViewModel()
    {
        ImageUrl = [];
        StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
        PickCommand = new DelegateCommand<DArtImageList>(PickMethod);
        OpenImageCommand = new DelegateCommand<DArtImageList>(OpenImageMethod);
    }

    private void OpenImageMethod(DArtImageList selectedImage)
    {
        Logger.Info("Open Image Method called with parameter: {Parameter}, MediaType: {MediaType}",selectedImage, Result.MediaType );
        var link = selectedImage.Url;
        link = Result.MediaType == MediaTypes.Game
            ? IgdbDataTransformer.GetPosterUrl(selectedImage.DeviationId, ImageSize.HD720)
            : link;
        UiUtils.ShowImageBrowser(link);
    }

    protected virtual void CloseDialog(string parameter)
    {
        Logger.Info("Close Dialog called with parameter: {Parameter}", parameter);
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RaiseRequestClose(new DialogResult(result));
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

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Result = parameters.GetValue<ResultResponse>("result");
        PickedIndex = parameters.GetValue<int>("pickedIndex");
        TmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
        IgdbObject = parameters.GetValue<IgdbClass>("igdbObject");
        _resultList = parameters.GetValue<ObservableCollection<ListItem>>("resultList");
        _isPickedById = parameters.GetValue<bool>("isPickedById");
        LoadData();
            
    }

    private async Task LoadData()
    {
        var resultType = _result.MediaType;
        var response = GetResponse(resultType);

        if (resultType == MediaTypes.Game)
        {
            LoadGameData(response);
        }
        else
        {
            await LoadMediaData(resultType, response);
        }
    }

    #region load data

    private dynamic GetResponse(string resultType)
    {
        return (_isPickedById, resultType) switch
        {
            (true, MediaTypes.Game) => Result.Result[0],
            (true, _) => Result.Result,
            (false, MediaTypes.Game) => Result.Result[PickedIndex],
            (false, _) => Result.Result.Results[PickedIndex]
        };
    }
    private async Task LoadMediaData(string resultType, dynamic response)
    {
        ImagesWithId images = await GetTitleAndImages(resultType, response);

        if (images != null)
        {
            var imageDataWrappers = images.Posters.Select(imageData => new ImageDataWrapper(images.Id, imageData, TmdbObject.GetClient()));
            LoadImages(imageDataWrappers);
        }
    }

    private async Task<ImagesWithId> GetTitleAndImages(string resultType, dynamic response)
    {
        (string title, ImagesWithId images) result = resultType switch
        {
            MediaTypes.Tv => await GetTvData(response),
            MediaTypes.Movie => await GetMovieData(response),
            MediaTypes.Collection => await GetCollectionData(response),
            MediaTypes.Mtv => await GetMtvData(response),
            _ => throw new ArgumentException($"Unexpected media type: {resultType}")
        };
        
        Title = result.title;
        return result.images;
    }

    private async Task<(string title, ImagesWithId images)> GetTvData(dynamic response)
    {
        dynamic pickedResult = _isPickedById ? (TvShow)response : (SearchTv)response;
        var images = await TmdbObject.SearchTvImages(pickedResult.Id);
        return (pickedResult.Name, images);
    }

    private async Task<(string title, ImagesWithId images)> GetMovieData(dynamic response)
    {
        dynamic pickedResult = _isPickedById ? (Movie)response : (SearchMovie)response;
        var images = await TmdbObject.SearchMovieImages(pickedResult.Id);
        return (pickedResult.Title, images);
    }

    private async Task<(string title, ImagesWithId images)> GetCollectionData(dynamic response)
    {
        dynamic pickedResult = _isPickedById ? (Collection)response : (SearchCollection)response;
        var images = await TmdbObject.SearchCollectionImages(pickedResult.Id);
        return (pickedResult.Name, images);
    }

    private async Task<(string title, ImagesWithId images)> GetMtvData(dynamic response)
    {
        return response.MediaType switch
        {
            MediaType.Tv => await GetTvData(response),
            MediaType.Movie => await GetMovieData(response),
            _ => throw new ArgumentException($"Unexpected MTV media type: {response.MediaType}")
        };
    }

    private void LoadGameData(dynamic response)
    {
        Logger.Debug("Media Type is Game, loading images from IGDB");
        Artwork[] images = response.Artworks.Values;
        var artworkWrappers = images.Select(artwork => new ArtworkWrapper(artwork));
        LoadImages(artworkWrappers);
    }

    #endregion
    
    
    private void LoadImages(IEnumerable<IImage> images)
    {
        ResetState();
        IsBusy = true;
        var imageList = images.ToList();
        if (imageList.Any())
        {
            ProcessImages(imageList);
        }
        else
        {
            HandleNoImagesFound();
        }
        IsBusy = false;
    }

    #region LoadImages

    private void ResetState()
    {
        StopSearch = false;
        ImageUrl.Clear();
    }
    
    private void ProcessImages(IEnumerable<IImage> images)
    {
        var imageList = images.ToList();
        TotalPosters = imageList.Count;
        Logger.Debug("Total Posters: {TotalPosters}", TotalPosters);
    
        foreach (var item in imageList.GetEnumeratorWithIndex())
        {
            Index = item.Index + 1;
            TryLoadImage(item.Value);
            if (!StopSearch)
            {
                continue;
            }

            Logger.Trace("Stop Search is true, breaking loop");
            break;
        }
    }
    
    private void TryLoadImage(IImage image)
    {
        var thumbnailUrl = image.GetThumbnailUrl();
        var qualityPath = image.GetPosterUrl();
        Logger.Info(PosterPathMessage, thumbnailUrl);
        Logger.Trace("Quality Path: {QualityPath}", qualityPath);
        AddImageToCollection(qualityPath,thumbnailUrl, image.Id);
    }
    
    private void AddImageToCollection(string qualityUrl, string thumbnailUrl, string id)
    {
        ImageUrl.Add(new DArtImageList(qualityUrl, thumbnailUrl, id));
    }
    
    private void HandleNoImagesFound()
    {
        Logger.Warn("No posters found for {Title}", Title);
        MessageBox.Show(CustomMessageBox.Warning(Lang.NoPosterFound, Title));
    }

    #endregion
    
    private void PickMethod(DArtImageList pickedImage)
    {
        Logger.Info("Pick Method called with parameter: {Parameter}", pickedImage);
        var link = pickedImage.Url;
        dynamic result;
        if (_isPickedById)
        {
            result = Result.MediaType == MediaTypes.Game ? Result.Result[0] : Result.Result;
        }
        else
        {
            result = Result.MediaType == MediaTypes.Game
                ? Result.Result[PickedIndex]
                : Result.Result.Results[PickedIndex];
        }

        _resultList[PickedIndex].SetInitialPoster();
        if (Result.MediaType == MediaTypes.Game)
        {
            result.Cover.Value.ImageId = pickedImage.DeviationId;
            _resultList[PickedIndex].Poster =IgdbDataTransformer.GetPosterUrl(pickedImage.DeviationId, ImageSize.HD720);
        }
        else
        {
            result.PosterPath = link;
            _resultList[PickedIndex].Poster = link;
        }

        Logger.Trace(PosterPathMessage, _resultList[PickedIndex].Poster);
        CloseDialog("true");
    }

}