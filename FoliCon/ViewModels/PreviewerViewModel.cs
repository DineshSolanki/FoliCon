namespace FoliCon.ViewModels
{
    public class PreviewerViewModel : BindableBase, IDialogAware
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        public PreviewerViewModel()
        {
            Logger.Debug("PosterIconConfigViewModel created");
            PosterIconInstance = new PosterIcon
            {
                Rating = Rating
            };

            SelectImageCommand = new DelegateCommand(SelectImage);
        }
        
        private PosterIcon _posterIconInstance;
        private string _rating = "3.5";
        public string Title => "Previewer";
        private string _mediaTitle = "Made with ♥ by FoliCon";
        private bool _ratingVisibility = true;
        private bool _overlayVisibility = true;
        
        public PosterIcon PosterIconInstance
        {
            get => _posterIconInstance;
            set => SetProperty(ref _posterIconInstance, value);
        }
        public string Rating
        {
            get => _rating;
            set
            {
                SetProperty(ref _rating, value);
                PosterIconInstance.Rating = value;
            }
        }

        public string MediaTitle
        {
            get => _mediaTitle;
            set
            {
                SetProperty(ref _mediaTitle, value);
                PosterIconInstance.MediaTitle = value;
            }
        }
        
        public bool RatingVisibility
        {
            get => _ratingVisibility;
            set
            {
                SetProperty(ref _ratingVisibility, value);
                PosterIconInstance.RatingVisibility = UiUtils.BooleanToVisibility(value).ToString();
            }
        }
        
        public bool OverlayVisibility
        {
            get => _overlayVisibility;
            set
            {
                SetProperty(ref _overlayVisibility, value);
                PosterIconInstance.MockupVisibility = UiUtils.BooleanToVisibility(value).ToString();
            }
        }
        
        public DelegateCommand SelectImageCommand { get; set; }
        

        private void SelectImage()
        {
            Logger.Debug("Opening image selection dialog");
            var fileDialog = DialogUtils.NewOpenFileDialog(LangProvider.GetLang("ChooseAnImage"), "Image files (*.png, *.jpg, *.gif, *.bmp, *.ico)|*.png;*.jpg;*.gif;*.bmp;*.ico");
            var selected = fileDialog.ShowDialog();

            if (selected != null && (bool)!selected)
            {
                Logger.Warn("No image selected");
                return;
            }

            var thisMemoryStream = new MemoryStream(File.ReadAllBytes(fileDialog.FileName));
            var rt= new PosterIcon
            {
                FolderJpg = (ImageSource)new ImageSourceConverter().ConvertFrom(thisMemoryStream)
            };
            PosterIconInstance = rt;
            Logger.Info("Image selected: {FileName}", fileDialog.FileName);
        }
        
        
        #region DialogMethods
        public event Action<IDialogResult> RequestClose;
        protected virtual void CloseDialog(string parameter)
        {
            Logger.Info("CloseDialog called with parameter {Parameter}", parameter);
            var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
            {
                "true" => ButtonResult.OK,
                "false" => ButtonResult.Cancel,
                _ => ButtonResult.None
            };

            RaiseRequestClose(new DialogResult(result));
        }

        public virtual void RaiseRequestClose(IDialogResult dialogResult) =>
            RequestClose?.Invoke(dialogResult);

        public virtual bool CanCloseDialog() => true;

        public virtual void OnDialogClosed()
        {
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #endregion DialogMethods
    }
}
