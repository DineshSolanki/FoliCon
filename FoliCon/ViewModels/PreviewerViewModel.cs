#nullable enable
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

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

        private string? _selectedPosterPath;
        private CancellationTokenSource? _rebuildCts;
        private string _searchFilter = string.Empty;
        private OverlayPreviewItem? _inspectedItem;
        private int _loadGeneration;

        public PreviewerViewModel() : this(new DialogCloseListener())
        {
        }

        public PreviewerViewModel(DialogCloseListener requestClose)
        {
            RequestClose = requestClose;
            Logger.Debug("PreviewerViewModel created");

            SelectImageCommand = new DelegateCommand(SelectImage);
            ResetPosterCommand = new DelegateCommand(ResetPoster);
            SelectCanvasThemeCommand = new DelegateCommand<string>(SetCanvasTheme);
            InspectItemCommand = new DelegateCommand<OverlayPreviewItem>(item => InspectedItem = item);
            CloseInspectCommand = new DelegateCommand(() => InspectedItem = null);
            CopyPreviewImageCommand = new DelegateCommand<OverlayPreviewItem>(CopyPreviewImage);
            CloseCommand = new DelegateCommand(() => CloseDialog("true"));

            OverlayPreviewItems = [];
            FilteredPreviewItems = [];

            // Load overlays and render previews
            _ = LoadPreviewsAsync(CancellationToken.None);
        }

        public string Title => Lang.Previewer;

        public ObservableCollection<OverlayPreviewItem> OverlayPreviewItems { get; }
        public ObservableCollection<OverlayPreviewItem> FilteredPreviewItems { get; }

        public string Rating
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        } = "3.5";

        public string MediaTitle
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        } = Lang.MadeWithFoliCon;

        public bool RatingVisibility
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        } = true;

        public bool OverlayVisibility
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    _ = RebuildPreviewsAsync();
                }
            }
        } = true;

        public string? SelectedPosterPath
        {
            get => _selectedPosterPath;
            set
            {
                if (!SetProperty(ref _selectedPosterPath, value))
                {
                    return;
                }

                RaisePropertyChanged(nameof(PosterFileName));
                RaisePropertyChanged(nameof(HasCustomPoster));
                _ = RebuildPreviewsAsync();
            }
        }

        public string PosterFileName => !string.IsNullOrEmpty(_selectedPosterPath)
            ? Path.GetFileName(_selectedPosterPath)
            : "Sample Poster";

        public bool HasCustomPoster => !string.IsNullOrEmpty(_selectedPosterPath);

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool IsLoading
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string CanvasTheme
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    UpdateCanvasBrush();
                }
            }
        } = "Theme";

        public Brush CanvasBrush
        {
            get;
            private set => SetProperty(ref field, value);
        } = Brushes.Transparent;

        public OverlayPreviewItem? InspectedItem
        {
            get => _inspectedItem;
            set
            {
                if (SetProperty(ref _inspectedItem, value))
                {
                    RaisePropertyChanged(nameof(IsInspecting));
                }
            }
        }

        public bool IsInspecting => _inspectedItem != null;
        public int FilteredOverlayCount => FilteredPreviewItems.Count;
        public int TotalOverlayCount => OverlayPreviewItems.Count;
        public bool HasNoResults => FilteredPreviewItems.Count == 0 && !IsLoading;

        #region Commands

        public DelegateCommand SelectImageCommand { get; }
        public DelegateCommand ResetPosterCommand { get; }
        public DelegateCommand<string> SelectCanvasThemeCommand { get; }
        public DelegateCommand<OverlayPreviewItem> InspectItemCommand { get; }
        public DelegateCommand CloseInspectCommand { get; }
        public DelegateCommand<OverlayPreviewItem> CopyPreviewImageCommand { get; }
        public DelegateCommand CloseCommand { get; }

        #endregion

        private void SetCanvasTheme(string? theme)
        {
            CanvasTheme = theme ?? "Theme";
        }

        private void UpdateCanvasBrush()
        {
            CanvasBrush = CanvasTheme switch
            {
                "Dark" => new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1B)),
                "Light" => new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF5)),
                "Folder" => new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0)),
                _ => Brushes.Transparent
            };
        }

        private void ApplyFilter()
        {
            FilteredPreviewItems.Clear();
            var query = _searchFilter?.Trim() ?? string.Empty;
            var matchingItems = OverlayPreviewItems.Where(item =>
                string.IsNullOrEmpty(query) ||
                item.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                item.OverlayId.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var item in matchingItems)
            {
                FilteredPreviewItems.Add(item);
            }

            RaisePropertyChanged(nameof(FilteredOverlayCount));
            RaisePropertyChanged(nameof(TotalOverlayCount));
            RaisePropertyChanged(nameof(HasNoResults));
        }

        private async Task LoadPreviewsAsync(CancellationToken token = default)
        {
            var generation = Interlocked.Increment(ref _loadGeneration);
            try
            {
                IsLoading = true;
                var provider = GlobalVariables.OverlayProvider;
                var previews = await OverlayPreviewCache.GetPreviewsAsync(
                    provider,
                    _selectedPosterPath,
                    Rating,
                    UiUtils.BooleanToVisibility(RatingVisibility).ToString(),
                    UiUtils.BooleanToVisibility(OverlayVisibility).ToString(),
                    MediaTitle);

                token.ThrowIfCancellationRequested();
                if (generation != Volatile.Read(ref _loadGeneration))
                {
                    return;
                }

                OverlayPreviewItems.Clear();
                foreach (var item in previews)
                {
                    OverlayPreviewItems.Add(item);
                }

                ApplyFilter();
                if (_inspectedItem != null)
                {
                    InspectedItem = OverlayPreviewItems.FirstOrDefault(item => item.OverlayId == _inspectedItem.OverlayId);
                }
                Logger.Info("Loaded {Count} overlay previews", previews.Count);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Debug(ex, "Preview load cancelled — newer rebuild superseded this one.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load overlay previews");
            }
            finally
            {
                if (generation == Volatile.Read(ref _loadGeneration))
                {
                    IsLoading = false;
                    RaisePropertyChanged(nameof(HasNoResults));
                }
            }
        }

        private async Task RebuildPreviewsAsync()
        {
            if (_rebuildCts != null)
            {
                await _rebuildCts.CancelAsync();
                _rebuildCts.Dispose();
            }
            _rebuildCts = new CancellationTokenSource();
            var token = _rebuildCts.Token;

            try
            {
                await Task.Delay(200, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            OverlayPreviewCache.InvalidateAll();
            await LoadPreviewsAsync(token);
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

            SelectedPosterPath = fileDialog.FileName;
            Logger.Info("Image selected: {FileName}", _selectedPosterPath);
        }

        private void ResetPoster() => SelectedPosterPath = null;

        private void CopyPreviewImage(OverlayPreviewItem? item)
        {
            if (item?.PreviewImage == null)
            {
                return;
            }
            try
            {
                Clipboard.SetImage(item.PreviewImage);
                Growl.SuccessGlobal(new GrowlInfo
                {
                    Message = string.Format(CultureInfo.CurrentCulture, Lang.PreviewerImageCopied, item.DisplayName),
                    ShowDateTime = false,
                    StaysOpen = false
                });
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to copy preview image to clipboard");
            }
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
            _rebuildCts?.Cancel();
            _rebuildCts?.Dispose();
            _rebuildCts = null;
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
        }

        #endregion DialogMethods
    }
}
