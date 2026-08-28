#nullable enable
namespace FoliCon.ViewModels
{
    [Localizable(false)]
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "XAML data binding requires instance properties.")]
    [SuppressMessage("Sonar", "S2325:Methods and properties that don't access instance data should be static",
        Justification = "XAML data binding requires instance properties.")]
    public class PreviewerViewModel : BindableBase, IDialogAware
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public PreviewerViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
            Logger.Debug("PreviewerViewModel created");

            SelectImageCommand = new DelegateCommand(SelectImage);
            OverlayPreviewItems = [];

            // Load overlays and render previews
            _ = LoadPreviewsAsync();
        }

        private string _rating = "3.5";
        private string _mediaTitle = Lang.MadeWithFoliCon;
        private bool _ratingVisibility = true;
        private bool _overlayVisibility = true;
        private string? _selectedPosterPath;

        public string Title => Lang.Previewer;

        public ObservableCollection<OverlayPreviewItem> OverlayPreviewItems { get; }

        public string Rating
        {
            get => _rating;
            set
            {
                if (SetProperty(ref _rating, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        }

        public string MediaTitle
        {
            get => _mediaTitle;
            set
            {
                if (SetProperty(ref _mediaTitle, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        }

        public bool RatingVisibility
        {
            get => _ratingVisibility;
            set
            {
                if (SetProperty(ref _ratingVisibility, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        }

        public bool OverlayVisibility
        {
            get => _overlayVisibility;
            set
            {
                if (SetProperty(ref _overlayVisibility, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        }

        public DelegateCommand SelectImageCommand { get; set; }

        private async Task LoadPreviewsAsync()
        {
            try
            {
                var provider = GlobalVariables.OverlayProvider;
                var previews = await OverlayPreviewCache.GetPreviewsAsync(
                    provider,
                    _selectedPosterPath,
                    Rating,
                    UiUtils.BooleanToVisibility(RatingVisibility).ToString(),
                    UiUtils.BooleanToVisibility(OverlayVisibility).ToString(),
                    MediaTitle);

                OverlayPreviewItems.Clear();
                foreach (var item in previews)
                {
                    OverlayPreviewItems.Add(item);
                }

                Logger.Info("Loaded {Count} overlay previews", previews.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load overlay previews");
            }
        }

        private async Task RebuildPreviewsAsync()
        {
            OverlayPreviewCache.InvalidateAll();
            await LoadPreviewsAsync();
        }

        private void SelectImage()
        {
            Logger.Debug("Opening image selection dialog");
            var fileDialog = DialogUtils.NewOpenFileDialog(Lang.ChooseAnImage, Lang.ImageFilesFilter);
            var selected = fileDialog.ShowDialog();

            if (selected != null && (bool)!selected)
            {
                Logger.Warn("No image selected");
                return;
            }

            _selectedPosterPath = fileDialog.FileName;
            Logger.Info("Image selected: {FileName}", _selectedPosterPath);
            _ = RebuildPreviewsAsync();
        }

        #region DialogMethods
        public DialogCloseListener RequestClose { get; }
        protected virtual void CloseDialog(string parameter)
        {
            Logger.Info("CloseDialog called with parameter {Parameter}", parameter);
            var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
            {
                "true" => ButtonResult.OK,
                "false" => ButtonResult.Cancel,
                _ => ButtonResult.None
            };

            RequestClose.Invoke(result);
        }

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
