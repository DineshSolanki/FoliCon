#nullable enable
using FoliCon.Modules.Overlays.Designer;
using Thickness = System.Windows.Thickness;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace FoliCon.ViewModels;

/// <summary>
/// ViewModel for the Overlay Designer dialog.
///
/// Owns an <see cref="OverlayDesignerDocument"/> plus its <see cref="OverlayEditHistory"/>, keeps
/// the canvas and the numeric property editors pointed at the same state, and drives the
/// debounced live preview. Every mutation goes through the history so undo/redo covers the
/// whole surface.
/// </summary>
// ReSharper disable once S3881 — Sealed class uses private Dispose(bool); protected virtual not needed
#pragma warning disable S3881 // Sealed class uses private Dispose(bool); protected virtual not needed
public class OverlayDesignerViewModel : BindableBase, IDialogAware, IDisposable
#pragma warning restore S3881
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>Zoom steps offered in the toolbar. 2x is the default so 256px art is workable.</summary>
    public static readonly double[] ZoomLevels = [1, 2, 4];

    private readonly OverlayTemplateProvider _templateProvider;
    private readonly OverlayPackageLoader _packageLoader;
    private readonly OverlayDesignerPreviewRenderer _previewRenderer;
    private readonly IOverlayProvider _overlayProvider;
    private readonly OverlayDraftStore _draftStore;
    private readonly OverlayExporter _exporter;
    private readonly OverlaySubmissionGuide? _submissionGuide;

    private OverlayDesignerDocument _document = new();
    private OverlayEditHistory _history;

    /// <summary>Suppresses history recording while property setters echo a document reload.</summary>
    private bool _isSyncingFromDocument;

    /// <summary>Margin captured at gesture start, so the whole drag becomes one undo step.</summary>
    private Thickness? _gestureStartMargin;
    private string? _gestureStartCorners;

    private bool _disposed;
    private string? _temporaryWorkingFolder;

    public OverlayDesignerViewModel(
        DialogCloseListener requestClose,
        IOverlayProvider overlayProvider,
        IOverlayRepositoryService repositoryService)
        : this(requestClose, overlayProvider,
               new OverlayTemplateProvider(overlayProvider),
               new OverlayPackageLoader(),
               new OverlayDesignerPreviewRenderer(),
               new OverlayDraftStore(),
               new OverlayExporter(),
               new OverlaySubmissionGuide(repositoryService))
    {
    }

    /// <summary>
    /// Explicit-collaborator constructor. Lets callers substitute the filesystem- and
    /// network-backed defaults.
    /// </summary>
#pragma warning disable S107 // 3 of 8 params are optional for DI flexibility
    public OverlayDesignerViewModel(
        DialogCloseListener requestClose,
        IOverlayProvider overlayProvider,
        OverlayTemplateProvider templateProvider,
        OverlayPackageLoader packageLoader,
        OverlayDesignerPreviewRenderer previewRenderer,
        OverlayDraftStore? draftStore = null,
        OverlayExporter? exporter = null,
        OverlaySubmissionGuide? submissionGuide = null)
#pragma warning restore S107
    {
        RequestClose = requestClose;
        _overlayProvider = overlayProvider ?? throw new ArgumentNullException(nameof(overlayProvider));
        _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
        _packageLoader = packageLoader ?? throw new ArgumentNullException(nameof(packageLoader));
        _previewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));
        _draftStore = draftStore ?? new OverlayDraftStore();
        _exporter = exporter ?? new OverlayExporter();

        // Null when no repository service is available (tests, previews): the catalog
        // clash check is skipped, but submission guidance still works.
        _submissionGuide = submissionGuide;

        _history = new OverlayEditHistory(_document);
        _history.Changed += OnHistoryChanged;

        _previewRenderer.Rendered += OnPreviewRendered;
        _previewRenderer.Failed += OnPreviewFailed;

        // Commands first: BuildElementList selects an element, and the selection setter
        // refreshes command states.
        InitializeCommands();
        BuildElementList();
    }

    #region Commands

    public DelegateCommand<OverlayTemplateCardViewModel> CreateFromTemplateCommand { get; private set; } = null!;
    public DelegateCommand OpenPackageCommand { get; private set; } = null!;
    public DelegateCommand UndoCommand { get; private set; } = null!;
    public DelegateCommand RedoCommand { get; private set; } = null!;
    public DelegateCommand<OverlayElementViewModel> SelectElementCommand { get; private set; } = null!;
    public DelegateCommand<string> NudgeCommand { get; private set; } = null!;
    public DelegateCommand<string> SetZoomCommand { get; private set; } = null!;
    public DelegateCommand<string> BrowseImageCommand { get; private set; } = null!;
    public DelegateCommand<string> ClearImageCommand { get; private set; } = null!;
    public DelegateCommand BrowseTestPosterCommand { get; private set; } = null!;
    public DelegateCommand OpenHelpCommand { get; private set; } = null!;
    public DelegateCommand<OverlayValidationIssue> FocusIssueCommand { get; private set; } = null!;
    public DelegateCommand SaveDraftCommand { get; private set; } = null!;
    public DelegateCommand ExportPackageCommand { get; private set; } = null!;
    public DelegateCommand InstallLocallyCommand { get; private set; } = null!;
    public DelegateCommand OpenExportFolderCommand { get; private set; } = null!;
    public DelegateCommand OpenForkPageCommand { get; private set; } = null!;
    public DelegateCommand OpenPullRequestPageCommand { get; private set; } = null!;
    public DelegateCommand DismissSubmissionCommand { get; private set; } = null!;
    public DelegateCommand<OverlayElementViewModel> MoveLayerUpCommand { get; private set; } = null!;
    public DelegateCommand<OverlayElementViewModel> MoveLayerDownCommand { get; private set; } = null!;
    public DelegateCommand<OverlayDraftInfo> ResumeDraftCommand { get; private set; } = null!;
    public DelegateCommand<OverlayDraftInfo> DeleteDraftCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        CreateFromTemplateCommand = new DelegateCommand<OverlayTemplateCardViewModel>(
            card => CreateFromTemplate(card?.Template), c => c != null);
        OpenPackageCommand = new DelegateCommand(OpenPackage);
        UndoCommand = new DelegateCommand(Undo, () => _history.CanUndo);
        RedoCommand = new DelegateCommand(Redo, () => _history.CanRedo);
        SelectElementCommand = new DelegateCommand<OverlayElementViewModel>(SelectElement, e => e != null);
        NudgeCommand = new DelegateCommand<string>(Nudge, _ => SelectedElement != null);
        SetZoomCommand = new DelegateCommand<string>(SetZoom);
        BrowseImageCommand = new DelegateCommand<string>(BrowseImage, _ => HasDocument);
        ClearImageCommand = new DelegateCommand<string>(ClearImage, _ => HasDocument);
        BrowseTestPosterCommand = new DelegateCommand(BrowseTestPoster);
        OpenHelpCommand = new DelegateCommand(OpenHelp);
        FocusIssueCommand = new DelegateCommand<OverlayValidationIssue>(FocusIssue, i => i != null);

        SaveDraftCommand = new DelegateCommand(SaveDraft, () => HasDocument);
        ExportPackageCommand = new DelegateCommand(async () => await ExportPackageAsync(), () => CanExport && !IsBusy);
        InstallLocallyCommand = new DelegateCommand(InstallLocally, () => LastExportPath != null && !IsBusy);
        OpenExportFolderCommand = new DelegateCommand(OpenExportFolder, () => LastExportPath != null);
        OpenForkPageCommand = new DelegateCommand(() => OpenUrl(OverlaySubmissionGuide.ForkUrl));
        OpenPullRequestPageCommand = new DelegateCommand(() => OpenUrl(OverlaySubmissionGuide.PullRequestUrl));
        DismissSubmissionCommand = new DelegateCommand(() => IsSubmissionPanelOpen = false);
        MoveLayerUpCommand = new DelegateCommand<OverlayElementViewModel>(
            e => MoveLayer(e, -1), e => CanMoveLayer(e, -1));
        MoveLayerDownCommand = new DelegateCommand<OverlayElementViewModel>(
            e => MoveLayer(e, +1), e => CanMoveLayer(e, +1));
        ResumeDraftCommand = new DelegateCommand<OverlayDraftInfo>(ResumeDraft, d => d != null);
        DeleteDraftCommand = new DelegateCommand<OverlayDraftInfo>(DeleteDraft, d => d != null);
    }

    #endregion

    #region Document state

    public static string Title => Lang.OverlayDesignerTitle;

    /// <summary>False until a template is cloned or a package opened; drives the first-run picker.</summary>
    public bool HasDocument
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(ShowTemplatePicker));
                RefreshCommandStates();
            }
        }
    }

    /// <summary>
    /// The empty state shows the template gallery rather than a blank canvas or a file dialog,
    /// so an author with no packages has an obvious way to start.
    /// </summary>
    public bool ShowTemplatePicker => !HasDocument;

    /// <summary>
    /// Template cards for the first-run picker. Each renders its own thumbnail so the author
    /// picks by appearance rather than by name alone.
    /// </summary>
    public ObservableCollection<OverlayTemplateCardViewModel> Templates { get; } = [];

    /// <summary>
    /// Saved drafts, newest first. Surfaced on the first-run screen so resuming work never
    /// requires remembering where a draft folder lives.
    /// </summary>
    public ObservableCollection<OverlayDraftInfo> Drafts { get; } = [];

    public bool HasDrafts => Drafts.Count > 0;

    public bool IsDirty
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string StatusMessage
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    #endregion

    #region Metadata properties

    public string OverlayId
    {
        get => _document.Id;
        set => SetDocumentProperty(value, _document.Id, (d, v) => d.Id = v, "Change ID", nameof(OverlayId));
    }

    public string DisplayName
    {
        get => _document.DisplayName;
        set => SetDocumentProperty(value, _document.DisplayName, (d, v) => d.DisplayName = v, "Change name", nameof(DisplayName));
    }

    public string Author
    {
        get => _document.Author;
        set => SetDocumentProperty(value, _document.Author, (d, v) => d.Author = v, "Change author", nameof(Author));
    }

    public string Description
    {
        get => _document.Description;
        set => SetDocumentProperty(value, _document.Description, (d, v) => d.Description = v, "Change description", nameof(Description));
    }

    public string OverlayVersion
    {
        get => _document.OverlayVersion;
        set => SetDocumentProperty(value, _document.OverlayVersion, (d, v) => d.OverlayVersion = v, "Change version", nameof(OverlayVersion));
    }

    public string TagsText
    {
        get => string.Join(", ", _document.Tags);
        set
        {
            var parsed = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (_document.Tags.SequenceEqual(parsed, StringComparer.Ordinal))
            {
                return;
            }

            var previous = _document.Tags.ToList();
            ApplyEdit(new PropertyEditCommand<List<string>>("Change tags",
                (d, v) => { d.Tags.Clear(); d.Tags.AddRange(v); }, previous, parsed));
            RaisePropertyChanged(nameof(TagsText));
        }
    }

    #endregion

    #region Selection

    public ObservableCollection<OverlayElementViewModel> Elements { get; } = [];

    public OverlayElementViewModel? SelectedElement
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
            {
                return;
            }

            RaisePropertyChanged(nameof(HasSelection));
            RaisePropertyChanged(nameof(SelectedElementName));
            RaiseSelectedBoundsChanged();
            NudgeCommand.RaiseCanExecuteChanged();
            RefreshLayerCommands();
        }
    }

    public bool HasSelection => SelectedElement != null;

    public string SelectedElementName => SelectedElement?.DisplayName ?? Lang.OverlayDesignerNothingSelected;

    private void SelectElement(OverlayElementViewModel? element)
    {
        foreach (var candidate in Elements)
        {
            candidate.IsSelected = ReferenceEquals(candidate, element);
        }

        SelectedElement = element;
    }

    /// <summary>Selects by kind. Used when a validation issue points at a specific layer.</summary>
    public void SelectElement(OverlayElementKind kind) =>
        SelectElement(Elements.FirstOrDefault(e => e.Kind == kind));

    #endregion

    #region Selected element geometry

    /// <summary>
    /// Position and size of the selection, in design-surface pixels. Bound to the numeric
    /// editors; the canvas writes the same values through <see cref="ApplyGesture"/>.
    /// </summary>
    public double SelectedLeft
    {
        get => SelectedBounds.X;
        set => SetSelectedBounds(new Rect(value, SelectedBounds.Y, SelectedBounds.Width, SelectedBounds.Height));
    }

    public double SelectedTop
    {
        get => SelectedBounds.Y;
        set => SetSelectedBounds(new Rect(SelectedBounds.X, value, SelectedBounds.Width, SelectedBounds.Height));
    }

    public double SelectedWidth
    {
        get => SelectedBounds.Width;
        set => SetSelectedBounds(new Rect(SelectedBounds.X, SelectedBounds.Y, Math.Max(0, value), SelectedBounds.Height));
    }

    public double SelectedHeight
    {
        get => SelectedBounds.Height;
        set => SetSelectedBounds(new Rect(SelectedBounds.X, SelectedBounds.Y, SelectedBounds.Width, Math.Max(0, value)));
    }

    private Rect SelectedBounds =>
        SelectedElement == null ? default : _document.GetElementBounds(SelectedElement.Kind);

    private void SetSelectedBounds(Rect bounds)
    {
        if (SelectedElement == null || _isSyncingFromDocument)
        {
            return;
        }

        var kind = SelectedElement.Kind;
        var snapped = OverlayGeometry.SnapToPixels(bounds);

        if (kind == OverlayElementKind.Poster && !string.IsNullOrWhiteSpace(_document.PosterPerspectiveCorners))
        {
            var oldCorners = _document.PosterPerspectiveCorners;
            var oldBounds = _document.GetElementBounds(kind);
            if (snapped == oldBounds)
            {
                return;
            }

            _document.SetElementBounds(kind, snapped);
            var newCorners = _document.PosterPerspectiveCorners;

            ApplyEdit(new PropertyEditCommand<string?>(
                $"Move {SelectedElement.DisplayName}",
                (d, v) => d.PosterPerspectiveCorners = v,
                oldCorners,
                newCorners));

            RaisePropertyChanged(nameof(PosterPerspectiveCorners));
            RaiseSelectedBoundsChanged();
            return;
        }

        var oldMargin = _document.GetElementMargin(kind);

        // RatingText uses shield-relative coordinates, so route through the document's
        // SetElementBounds which handles the conversion for each element kind.
        var newMargin = kind == OverlayElementKind.RatingText
            ? GetMarginFromBounds(kind, snapped)
            : OverlayGeometry.BoundsToMargin(snapped, _document.LayoutSurface);

        if (newMargin == oldMargin)
        {
            return;
        }

        ApplyEdit(new ElementBoundsCommand(kind, oldMargin, newMargin, $"Move {SelectedElement.DisplayName}"));
    }

    /// <summary>
    /// Converts bounds to a margin for the given element kind. For most elements this is
    /// a simple surface conversion; for <see cref="OverlayElementKind.RatingText"/> it
    /// computes the shield-relative offset.
    /// </summary>
    private Thickness GetMarginFromBounds(OverlayElementKind kind, Rect bounds)
    {
        // Temporarily set bounds via the document (which handles coordinate conversion)
        // then read back the resulting margin.
        var savedMargin = _document.GetElementMargin(kind);
        _document.SetElementBounds(kind, bounds);
        var result = _document.GetElementMargin(kind);
        _document.SetElementMargin(kind, savedMargin); // restore
        return result;
    }

    /// <summary>
    /// Applies a live drag/resize without recording history. The canvas calls this on every
    /// mouse-move; <see cref="EndGesture"/> records the whole gesture as one undo step.
    /// </summary>
    public void ApplyGesture(Rect designBounds)
    {
        if (SelectedElement == null)
        {
            return;
        }

        var kind = SelectedElement.Kind;
        _gestureStartMargin ??= _document.GetElementMargin(kind);
        _gestureStartCorners ??= _document.PosterPerspectiveCorners;

        _document.SetElementBounds(kind, OverlayGeometry.SnapToPixels(designBounds));

        RefreshElementBounds();
        RaiseSelectedBoundsChanged();
        if (kind == OverlayElementKind.Poster && !string.IsNullOrWhiteSpace(_gestureStartCorners))
        {
            RaisePropertyChanged(nameof(PosterPerspectiveCorners));
        }
        RequestPreview();
    }

    /// <summary>Closes a drag/resize, recording one undo entry for the entire gesture.</summary>
    public void EndGesture()
    {
        if (SelectedElement == null)
        {
            return;
        }

        var kind = SelectedElement.Kind;

        if (kind == OverlayElementKind.Poster && _gestureStartCorners != null)
        {
            var startCorners = _gestureStartCorners;
            _gestureStartCorners = null;
            _gestureStartMargin = null;

            var endCorners = _document.PosterPerspectiveCorners;
            if (endCorners == startCorners)
            {
                return;
            }

            _history.PushExecuted(new PropertyEditCommand<string?>(
                $"Move {SelectedElement.DisplayName}",
                (d, v) => d.PosterPerspectiveCorners = v,
                startCorners,
                endCorners));
            return;
        }

        if (_gestureStartMargin is not { } startMargin)
        {
            return;
        }

        _gestureStartMargin = null;

        var endMargin = _document.GetElementMargin(kind);
        if (endMargin == startMargin)
        {
            return; // A click that moved nothing shouldn't create an undo step.
        }

        _history.PushExecuted(new ElementBoundsCommand(
            kind, startMargin, endMargin, $"Move {SelectedElement.DisplayName}"));
    }

    /// <summary>Arrow-key nudge. Shift multiplies the step to 10px.</summary>
    private void Nudge(string? direction)
    {
        if (SelectedElement == null || string.IsNullOrWhiteSpace(direction))
        {
            return;
        }

        var step = direction.Contains("shift", StringComparison.OrdinalIgnoreCase) ? 10d : 1d;
        var (dx, dy) = direction.ToLowerInvariant() switch
        {
            var d when d.Contains("left") => (-step, 0d),
            var d when d.Contains("right") => (step, 0d),
            var d when d.Contains("up") => (0d, -step),
            var d when d.Contains("down") => (0d, step),
            _ => (0d, 0d)
        };

        // ReSharper disable once CompareOfFloatsByEqualityOperator — values are exact 0d literals from the switch
        if (dx == 0d && dy == 0d)
        {
            return;
        }

        SetSelectedBounds(OverlayGeometry.Nudge(SelectedBounds, dx, dy));
    }

    private void RaiseSelectedBoundsChanged()
    {
        RaisePropertyChanged(nameof(SelectedLeft));
        RaisePropertyChanged(nameof(SelectedTop));
        RaisePropertyChanged(nameof(SelectedWidth));
        RaisePropertyChanged(nameof(SelectedHeight));
    }

    #endregion

    #region Layer toggles and images

    public bool HasBaseLayer
    {
        get => _document.HasBaseLayer;
        set => SetDocumentProperty(value, _document.HasBaseLayer, (d, v) => d.HasBaseLayer = v,
            "Toggle base layer", nameof(HasBaseLayer), afterApply: BuildElementList);
    }

    public bool HasFrontLayer
    {
        get => _document.HasFrontLayer;
        set => SetDocumentProperty(value, _document.HasFrontLayer, (d, v) => d.HasFrontLayer = v,
            "Toggle front layer", nameof(HasFrontLayer), afterApply: BuildElementList);
    }

    public bool TitleIsVisible
    {
        get => _document.TitleIsVisible;
        set => SetDocumentProperty(value, _document.TitleIsVisible,
            (d, v) =>
            {
                d.TitleIsVisible = v;

                // Most templates predate the title and omit it from layerOrder. Turning the
                // title on has to add it, or the exported overlay would carry a title the
                // z-order never draws.
                if (v && !d.LayerOrder.Contains(OverlayElementKind.Title))
                {
                    d.LayerOrder.Add(OverlayElementKind.Title);
                }
            },
            "Toggle title", nameof(TitleIsVisible), afterApply: BuildElementList);
    }

    public string BaseLayerImagePath => _document.BaseLayerImagePath;

    public string FrontLayerImagePath => _document.FrontLayerImagePath;

    public string? PosterOpacityMaskPath => _document.PosterOpacityMaskPath;

    /// <summary>
    /// Picks a PNG for a layer. If the file lives outside the package folder, it is automatically
    /// copied into the package folder so the overlay remains self-contained and portable.
    /// </summary>
    private void BrowseImage(string? target)
    {
        if (string.IsNullOrWhiteSpace(target) || !HasDocument)
        {
            return;
        }

        var dialog = DialogUtils.NewOpenFileDialog(
            Lang.OverlayDesignerSelectPngTitle, Lang.OverlayDesignerPngFilesFilter);
        dialog.InitialDirectory = Directory.Exists(_document.AssetFolderPath)
            ? _document.AssetFolderPath
            : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_document.AssetFolderPath))
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "FoliCon", "OverlayDesigner", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempFolder);
            _document.AssetFolderPath = tempFolder;
        }

        var selected = Path.GetFullPath(dialog.FileName);
        var folder = Path.GetFullPath(_document.AssetFolderPath);

        // If the selected image is outside the package folder, copy it in so the package is self-contained.
        if (!selected.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Directory.CreateDirectory(folder);
                var rawFileName = Path.GetFileName(selected);
                var availableFileName = OverlayTemplateProvider.ResolveAvailableFileName(folder, rawFileName);
                var destPath = Path.Combine(folder, availableFileName);

                File.Copy(selected, destPath, overwrite: false);
                selected = destPath;

                Logger.Info("Copied external image '{Source}' into overlay folder '{Dest}'", dialog.FileName, destPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to copy image '{Source}' into overlay folder '{Folder}'", dialog.FileName, folder);
                StatusMessage = string.Format(Lang.OverlayDesignerDraftSaveFailed, ex.Message);
                DialogUtils.ShowGrowlError(StatusMessage);
                return;
            }
        }

        var relativePath = Path.GetRelativePath(folder, selected);
        ApplyImagePath(target, relativePath);
        DialogUtils.ShowGrowlSuccess($"Loaded {target} image: {relativePath}");
    }

    private void ClearImage(string? target)
    {
        if (!string.IsNullOrWhiteSpace(target))
        {
            ApplyImagePath(target, string.Empty);
        }
    }

    private void ApplyImagePath(string target, string relativePath)
    {
        switch (target.ToLowerInvariant())
        {
            case "base":
                var prevBase = _document.BaseLayerImagePath;
                var newHasBase = !string.IsNullOrEmpty(relativePath);
                ApplyEdit(new PropertyEditCommand<string>("Change base image",
                    (d, v) =>
                    {
                        d.BaseLayerImagePath = v;
                        d.HasBaseLayer = !string.IsNullOrEmpty(v);
                    },
                    prevBase, relativePath));
                _document.HasBaseLayer = newHasBase;
                RaisePropertyChanged(nameof(BaseLayerImagePath));
                RaisePropertyChanged(nameof(HasBaseLayer));
                BuildElementList();
                break;

            case "front":
                var prevFront = _document.FrontLayerImagePath;
                var newHasFront = !string.IsNullOrEmpty(relativePath);
                ApplyEdit(new PropertyEditCommand<string>("Change front image",
                    (d, v) =>
                    {
                        d.FrontLayerImagePath = v;
                        d.HasFrontLayer = !string.IsNullOrEmpty(v);
                    },
                    prevFront, relativePath));
                _document.HasFrontLayer = newHasFront;
                RaisePropertyChanged(nameof(FrontLayerImagePath));
                RaisePropertyChanged(nameof(HasFrontLayer));
                BuildElementList();
                break;

            case "mask":
                ApplyEdit(new PropertyEditCommand<string?>("Change opacity mask",
                    (d, v) => d.PosterOpacityMaskPath = v, _document.PosterOpacityMaskPath,
                    string.IsNullOrEmpty(relativePath) ? null : relativePath));
                RaisePropertyChanged(nameof(PosterOpacityMaskPath));
                break;

            default:
                Logger.Warn("Unknown image target '{Target}', ignoring", target);
                break;
        }
    }

    #endregion

    #region Poster, rating, title properties

    public string PosterClipRadius
    {
        get => _document.PosterClipRadius;
        set
        {
            SetDocumentProperty(value, _document.PosterClipRadius, (d, v) => d.PosterClipRadius = v,
                "Change corner radius", nameof(PosterClipRadius));
            RaiseCornerRadiusChanged();
        }
    }

    public double PosterRotationAngle
    {
        get => _document.PosterRotationAngle;
        set => SetDocumentProperty(value, _document.PosterRotationAngle, (d, v) => d.PosterRotationAngle = v,
            "Change poster rotation", nameof(PosterRotationAngle));
    }

    public double PosterSkewX
    {
        get => _document.PosterSkewX;
        set => SetDocumentProperty(value, _document.PosterSkewX, (d, v) => d.PosterSkewX = v,
            "Change poster skew X", nameof(PosterSkewX));
    }

    public double PosterSkewY
    {
        get => _document.PosterSkewY;
        set => SetDocumentProperty(value, _document.PosterSkewY, (d, v) => d.PosterSkewY = v,
            "Change poster skew Y", nameof(PosterSkewY));
    }

    public string? PosterPerspectiveCorners
    {
        get => _document.PosterPerspectiveCorners;
        set => SetDocumentProperty(value, _document.PosterPerspectiveCorners, (d, v) => d.PosterPerspectiveCorners = v,
            "Change poster perspective corners", nameof(PosterPerspectiveCorners), afterApply: () =>
            {
                RefreshElementBounds();
                RaiseSelectedBoundsChanged();
            });
    }

    /// <summary>
    /// Per-corner editing over the same <c>clipRadius</c> field. The schema accepts either one
    /// value for all corners or four as "tl,tr,br,bl"; these four properties read and write that
    /// string so the author never has to hand-assemble it.
    /// </summary>
    public double ClipRadiusTopLeft
    {
        get => GetCorner(0);
        set => SetCorner(0, value);
    }

    public double ClipRadiusTopRight
    {
        get => GetCorner(1);
        set => SetCorner(1, value);
    }

    public double ClipRadiusBottomRight
    {
        get => GetCorner(2);
        set => SetCorner(2, value);
    }

    public double ClipRadiusBottomLeft
    {
        get => GetCorner(3);
        set => SetCorner(3, value);
    }

    /// <summary>True when all four corners share a value, which is the common case.</summary>
    public bool HasUniformCornerRadius
    {
        get
        {
            var corners = ParseCorners(_document.PosterClipRadius);
            return corners.Distinct().Count() == 1;
        }
    }

    private double GetCorner(int index) => ParseCorners(_document.PosterClipRadius)[index];

    private void SetCorner(int index, double value)
    {
        if (_isSyncingFromDocument)
        {
            return;
        }

        var corners = ParseCorners(_document.PosterClipRadius);
        var clamped = Math.Max(0, value);
        if (Math.Abs(corners[index] - clamped) < 0.001)
        {
            return;
        }

        corners[index] = clamped;

        // Collapse back to shorthand when every corner matches, keeping exports tidy.
        var formatted = corners.Distinct().Count() == 1
            ? FormatCorner(corners[0])
            : string.Join(",", corners.Select(FormatCorner));

        ApplyEdit(new PropertyEditCommand<string>("Change corner radius",
            (d, v) => d.PosterClipRadius = v, _document.PosterClipRadius, formatted));

        RaiseCornerRadiusChanged();
    }

    /// <summary>
    /// Parses the schema's one-or-four form into four corners (tl, tr, br, bl).
    /// Anything unparseable degrades to square corners, matching the renderer.
    /// </summary>
    private static double[] ParseCorners(string? clipRadius)
    {
        if (string.IsNullOrWhiteSpace(clipRadius))
        {
            return [0, 0, 0, 0];
        }

        var parts = clipRadius.Split(',');

        if (parts.Length == 1)
        {
            var all = ParseCornerValue(parts[0]);
            return [all, all, all, all];
        }

        if (parts.Length == 4)
        {
            return [.. parts.Select(ParseCornerValue)];
        }

        return [0, 0, 0, 0];
    }

    private static double ParseCornerValue(string value) =>
        double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0, parsed)
            : 0;

    private static string FormatCorner(double value) =>
        Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);

    private void RaiseCornerRadiusChanged()
    {
        RaisePropertyChanged(nameof(PosterClipRadius));
        RaisePropertyChanged(nameof(ClipRadiusTopLeft));
        RaisePropertyChanged(nameof(ClipRadiusTopRight));
        RaisePropertyChanged(nameof(ClipRadiusBottomRight));
        RaisePropertyChanged(nameof(ClipRadiusBottomLeft));
        RaisePropertyChanged(nameof(HasUniformCornerRadius));
    }

    public double RatingFontSize
    {
        get => _document.RatingFontSize;
        set => SetDocumentProperty(value, _document.RatingFontSize, (d, v) => d.RatingFontSize = v,
            "Change rating font size", nameof(RatingFontSize));
    }

    public string RatingFontFamily
    {
        get => _document.RatingFontFamily;
        set => SetDocumentProperty(value, _document.RatingFontFamily, (d, v) => d.RatingFontFamily = v,
            "Change rating font", nameof(RatingFontFamily));
    }

    public double TitleRotationAngle
    {
        get => _document.TitleRotationAngle;
        set => SetDocumentProperty(value, _document.TitleRotationAngle, (d, v) => d.TitleRotationAngle = v,
            "Change title rotation", nameof(TitleRotationAngle));
    }

    public string TitleForeground
    {
        get => _document.TitleForeground;
        set
        {
            SetDocumentProperty(value, _document.TitleForeground, (d, v) => d.TitleForeground = v,
                "Change title colour", nameof(TitleForeground));
            RaisePropertyChanged(nameof(TitleForegroundBrush));
        }
    }

    /// <summary>
    /// The title colour as a brush, for the swatch beside the picker button.
    /// Falls back to white for an unparseable value, matching what the renderer draws.
    /// </summary>
    public Brush TitleForegroundBrush => ParseBrushOrDefault(_document.TitleForeground);

    /// <summary>
    /// Applies a colour chosen in the picker.
    ///
    /// Stored as a hex string because that is what the schema's <c>foreground</c> field holds;
    /// the alpha channel is kept only when it is meaningful, so opaque colours stay in the
    /// familiar <c>#RRGGBB</c> form rather than <c>#FFRRGGBB</c>.
    /// </summary>
    public void ApplyTitleColour(Color colour) =>
        TitleForeground = colour.A == 255
            ? $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}"
            : $"#{colour.A:X2}{colour.R:X2}{colour.G:X2}{colour.B:X2}";

    private static Brush ParseBrushOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Brushes.White;
        }

        try
        {
            return (Brush)new BrushConverter().ConvertFromString(value)!;
        }
        catch (FormatException)
        {
            return Brushes.White;
        }
        catch (NotSupportedException)
        {
            return Brushes.White;
        }
    }

    public string TitleFontFamily
    {
        get => _document.TitleFontFamily;
        set => SetDocumentProperty(value, _document.TitleFontFamily, (d, v) => d.TitleFontFamily = v,
            "Change title font", nameof(TitleFontFamily));
    }

    #endregion

    #region Advanced canvas-compatibility properties

    public double DesignWidth
    {
        get => _document.DesignWidth;
        set => SetDocumentProperty(value, _document.DesignWidth, (d, v) => d.DesignWidth = v,
            "Change design width", nameof(DesignWidth), afterApply: RefreshElementBounds);
    }

    public double DesignHeight
    {
        get => _document.DesignHeight;
        set => SetDocumentProperty(value, _document.DesignHeight, (d, v) => d.DesignHeight = v,
            "Change design height", nameof(DesignHeight), afterApply: RefreshElementBounds);
    }

    public string RootMargin
    {
        get => OverlayGeometry.FormatThickness(_document.RootMargin);
        set => SetDocumentProperty(OverlayGeometry.ParseThickness(value), _document.RootMargin,
            (d, v) => d.RootMargin = v, "Change root margin", nameof(RootMargin), afterApply: RefreshElementBounds);
    }

    #endregion

    #region Preview and test controls

    public OverlayPreviewContext PreviewContext { get; } = new();

    public BitmapSource? PreviewImage
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public double Zoom
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(CanvasWidth));
                RaisePropertyChanged(nameof(CanvasHeight));

                // Re-render at the new scale so the canvas shows one bitmap pixel per screen
                // pixel instead of stretching a 256px frame across the zoomed surface.
                RequestPreview();
            }
        }
    } = 2;

    /// <summary>Canvas size in device pixels. Zoom scales display only, never exported values.</summary>
    public double CanvasWidth => _document.RenderWidth * Zoom;

    public double CanvasHeight => _document.RenderHeight * Zoom;

    public string TestRating
    {
        get => PreviewContext.Rating;
        set
        {
            if (PreviewContext.Rating == value)
            {
                return;
            }

            PreviewContext.Rating = value;
            RaisePropertyChanged(nameof(TestRating));
            RequestPreview();
        }
    }

    public string TestTitle
    {
        get => PreviewContext.MediaTitle;
        set
        {
            if (PreviewContext.MediaTitle == value)
            {
                return;
            }

            PreviewContext.MediaTitle = value;
            RaisePropertyChanged(nameof(TestTitle));
            RequestPreview();
        }
    }

    public bool ShowTestRating
    {
        get => PreviewContext.ShowRating;
        set
        {
            if (PreviewContext.ShowRating == value)
            {
                return;
            }

            PreviewContext.ShowRating = value;
            RaisePropertyChanged(nameof(ShowTestRating));
            RequestPreview();
        }
    }

    public bool ShowTestMockup
    {
        get => PreviewContext.ShowMockup;
        set
        {
            if (PreviewContext.ShowMockup == value)
            {
                return;
            }

            PreviewContext.ShowMockup = value;
            RaisePropertyChanged(nameof(ShowTestMockup));
            RequestPreview();
        }
    }

    private void BrowseTestPoster()
    {
        var dialog = DialogUtils.NewOpenFileDialog(
            Lang.OverlayDesignerSelectSamplePoster, Lang.OverlayDesignerSamplePosterFilter);

        if (dialog.ShowDialog() == true)
        {
            // Test state lives outside the document, so this never dirties the overlay.
            PreviewContext.PosterPath = dialog.FileName;
            RequestPreview();
        }
    }

    private void SetZoom(string? level)
    {
        if (double.TryParse(level, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && ZoomLevels.Contains(parsed))
        {
            Zoom = parsed;
        }
    }

    private void RequestPreview()
    {
        if (!HasDocument)
        {
            return;
        }

        RevalidateDocument();
        _previewRenderer.RequestRender(_document.CreateSnapshot(), PreviewContext, Zoom);
    }

    private void OnPreviewRendered(object? sender, OverlayPreviewRenderedEventArgs e) => PreviewImage = e.Image;

    private void OnPreviewFailed(object? sender, OverlayPreviewFailedEventArgs e) =>
        StatusMessage = string.Format(Lang.OverlayDesignerPreviewFailed, e.Exception.Message);

    #endregion

    #region Validation

    public ObservableCollection<OverlayValidationIssue> ValidationIssues { get; } = [];

    public bool HasValidationErrors
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string ValidationSummary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Export is gated on validation; warnings are advisory and do not block.</summary>
    public bool CanExport => HasDocument && !HasValidationErrors;

    #endregion

    #region Saving, export, and submission

    /// <summary>True while a long-running file or network operation is in flight.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RefreshCommandStates();
            }
        }
    }

    /// <summary>Folder of the most recent successful export; null until one succeeds.</summary>
    public string? LastExportPath
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                RefreshCommandStates();
            }
        }
    }

    /// <summary>Whether the guided submission steps are showing.</summary>
    public bool IsSubmissionPanelOpen
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Result of the pre-submission catalog check, shown above the steps.</summary>
    public string SubmissionCheckMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>False when the ID and version would certainly be rejected upstream.</summary>
    public bool CanSubmit
    {
        get;
        private set => SetProperty(ref field, value);
    } = true;

    /// <summary>Where the exported folder must be copied inside the author's fork.</summary>
    public string SubmissionTargetPath => OverlaySubmissionGuide.TargetPathInRepository(_document.Id);

    public static string ContributingGuideUrl => OverlaySubmissionGuide.ContributingGuideUrl;

    private void SaveDraft()
    {
        try
        {
            IsBusy = true;
            var path = _draftStore.Save(_document);

            // The draft's copies are now the document's assets, so the export reads them.
            _history.MarkClean();
            CleanupTemporaryWorkingFolder();

            // Keep the resume list current so the draft is there when they come back.
            LoadDrafts();

            StatusMessage = string.Format(Lang.OverlayDesignerDraftSavedTo, path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Logger.Error(ex, "Failed to save draft for overlay '{Id}'", _document.Id);
            StatusMessage = string.Format(Lang.OverlayDesignerDraftSaveFailed, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportPackageAsync()
    {
        var folderDialog = DialogUtils.NewFolderBrowserDialog(Lang.OverlayDesignerChooseExportFolder);
        if (folderDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = Lang.OverlayDesignerExporting;

            var destination = folderDialog.SelectedPath;
            var existing = Path.Combine(destination, _document.Id);

            var overwrite = false;
            if (Directory.Exists(existing))
            {
                overwrite = ConfirmOverwrite(_document.Id);
                if (!overwrite)
                {
                    StatusMessage = Lang.OverlayDesignerExportCancelled;
                    return;
                }
            }

            var result = await _exporter.ExportAsync(_document, destination, overwrite);

            if (!result.Succeeded)
            {
                StatusMessage = result.FailureReason ?? Lang.OverlayDesignerExportFailed;
                return;
            }

            LastExportPath = result.PackagePath;
            _history.MarkClean();
            StatusMessage = string.Format(Lang.OverlayDesignerExportedTo, result.PackagePath);

            await OpenSubmissionPanelAsync();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error(ex, "Failed to export overlay '{Id}'", _document.Id);
            StatusMessage = string.Format(Lang.OverlayDesignerExportFailedWithReason, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Shows the submission steps and runs the catalog clash check, so the author learns about
    /// an ID or version problem before spending effort on a pull request.
    /// </summary>
    private async Task OpenSubmissionPanelAsync()
    {
        IsSubmissionPanelOpen = true;
        RaisePropertyChanged(nameof(SubmissionTargetPath));

        if (_submissionGuide == null)
        {
            SubmissionCheckMessage = string.Empty;
            CanSubmit = true;
            return;
        }

        SubmissionCheckMessage = Lang.OverlayDesignerCheckingNameClash;

        var check = await _submissionGuide.CheckAsync(_document.Id, _document.OverlayVersion);
        SubmissionCheckMessage = check.Message;
        CanSubmit = check.CanProceed;
    }

    private void InstallLocally()
    {
        if (LastExportPath == null)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var result = _exporter.InstallLocally(LastExportPath);

            if (!result.Succeeded)
            {
                StatusMessage = result.FailureReason ?? Lang.OverlayDesignerInstallFailed;
                return;
            }

            // The new overlay must appear in the pickers without an app restart.
            _overlayProvider.Refresh();
            OverlayPreviewCache.InvalidateAll();

            StatusMessage = string.Format(Lang.OverlayDesignerInstalledSuccess, _document.DisplayName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error(ex, "Failed to install overlay '{Id}' locally", _document.Id);
            StatusMessage = string.Format(Lang.OverlayDesignerInstallFailedWithReason, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenExportFolder()
    {
        if (LastExportPath != null && Directory.Exists(LastExportPath))
        {
            ProcessUtils.StartProcess(LastExportPath + Path.DirectorySeparatorChar);
        }
    }

    /// <summary>Overridable so tests can exercise the export flow without a modal.</summary>
    protected virtual bool ConfirmOverwrite(string overlayId) =>
        MessageBox.Show(
            string.Format(Lang.OverlayDesignerReplaceExistingBody, overlayId),
            Lang.OverlayDesignerReplaceExistingTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private void RevalidateDocument()
    {
        var result = Modules.Overlays.Internal.OverlayValidator.ValidateDetailed(
            _document.AssetFolderPath, _document.CreateSnapshot());

        ValidationIssues.Clear();
        foreach (var issue in result.Issues)
        {
            ValidationIssues.Add(issue);
        }

        HasValidationErrors = !result.IsValid;
#pragma warning disable S125 // Design comment explaining pluralization strategy, not commented-out code
        // "Noun: count" rather than "{n} error(s)". English pluralises with a trailing "s";
        // ru/ar have several plural forms and ja/zh have none, so no single format string can
        // be translated correctly. Naming the noun and appending the count sidesteps it.
#pragma warning restore S125
        ValidationSummary = result switch
        {
            { ErrorCount: 0, WarningCount: 0 } => Lang.OverlayDesignerNoIssues,
            { ErrorCount: 0 } => string.Format(Lang.OverlayDesignerWarningCount, result.WarningCount),
            { WarningCount: 0 } => string.Format(Lang.OverlayDesignerErrorCount, result.ErrorCount),
            _ => string.Format(Lang.OverlayDesignerErrorAndWarningCount, result.ErrorCount, result.WarningCount)
        };

        RaisePropertyChanged(nameof(CanExport));
    }

    /// <summary>
    /// Selects the element a validation issue belongs to, so clicking a message in the list
    /// takes the author to the thing that is wrong.
    /// </summary>
    private void FocusIssue(OverlayValidationIssue? issue)
    {
        if (issue == null)
        {
            return;
        }

        var kind = issue.Field.Split('.')[0].ToLowerInvariant() switch
        {
            "baselayer" => OverlayElementKind.Base,
            "frontlayer" => OverlayElementKind.Front,
            "poster" => OverlayElementKind.Poster,
            "rating" => OverlayElementKind.Rating,
            "title" => OverlayElementKind.Title,
            _ => (OverlayElementKind?)null
        };

        if (kind.HasValue)
        {
            SelectElement(kind.Value);
        }
    }

    #endregion

    #region Loading

    private void CreateFromTemplate(OverlayTemplate? template)
    {
        if (template == null)
        {
            return;
        }

        try
        {
            // Not localized: this seeds the overlay ID, and a translated seed would
            // produce non-ASCII characters the 'id' validator rejects.
            var id = _templateProvider.SuggestId($"My {template.DisplayName}");
            var folder = Path.Combine(
                Path.GetTempPath(), "FoliCon", "OverlayDesigner", Guid.NewGuid().ToString("N"));
            _temporaryWorkingFolder = folder;

            var document = _templateProvider.CreateFromTemplate(
                template, folder, id,
                string.Format(Lang.OverlayDesignerNewOverlayNamePattern, template.DisplayName),
                Environment.UserName);

            AdoptDocument(document);
            StatusMessage = string.Format(Lang.OverlayDesignerCreatedFromTemplate,
                document.DisplayName, template.DisplayName);
        }
        catch (Exception ex)
        {
            CleanupTemporaryWorkingFolder();
            Logger.Error(ex, "Failed to create overlay from template '{Id}'", template.Id);
            StatusMessage = string.Format(Lang.OverlayDesignerCreateFromTemplateFailed, ex.Message);
        }
    }

    private void OpenPackage()
    {
        var dialog = DialogUtils.NewOpenFileDialog(
            Lang.OverlayDesignerOpenPackageTitle, Lang.OverlayDesignerOverlayJsonFilter);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        LoadPackage(dialog.FileName);
    }

    /// <summary>Loads a package by path. Exposed so launch points can open a specific overlay.</summary>
    public void LoadPackage(string overlayJsonPath)
    {
        var result = _packageLoader.Load(overlayJsonPath);

        if (!result.Succeeded)
        {
            StatusMessage = result.FailureReason ?? Lang.OverlayDesignerCouldNotOpenOverlay;
            return;
        }

        CleanupTemporaryWorkingFolder();
        AdoptDocument(result.Document);
        StatusMessage = result.Validation.IsValid
            ? string.Format(Lang.OverlayDesignerOpened, result.Document.DisplayName)
            : string.Format(Lang.OverlayDesignerOpenedWithProblems,
                result.Document.DisplayName, result.Validation.ErrorCount);
    }

    private void AdoptDocument(OverlayDesignerDocument document)
    {
        _history.Changed -= OnHistoryChanged;

        _document = document;
        _history = new OverlayEditHistory(_document);
        _history.Changed += OnHistoryChanged;

        HasDocument = true;

        // A previous overlay's export has nothing to do with this one.
        LastExportPath = null;
        IsSubmissionPanelOpen = false;
        SubmissionCheckMessage = string.Empty;
        CanSubmit = true;

        BuildElementList();
        SelectElement(OverlayElementKind.Poster);
        SyncAllPropertiesFromDocument();
        RequestPreview();
        OnHistoryChanged(this, EventArgs.Empty);
    }

    /// <summary>
    /// Abandons the open document and returns to the template picker, keeping the author
    /// inside the designer so they can start a different overlay without reopening the dialog.
    /// </summary>
    public void ReturnToTemplates()
    {
        CleanupTemporaryWorkingFolder();
        _history.Changed -= OnHistoryChanged;

        _document = new OverlayDesignerDocument();
        _history = new OverlayEditHistory(_document);
        _history.Changed += OnHistoryChanged;

        HasDocument = false;
        PreviewImage = null;
        ValidationIssues.Clear();
        HasValidationErrors = false;
        ValidationSummary = string.Empty;
        StatusMessage = string.Empty;

        // Export state belongs to the overlay being abandoned.
        LastExportPath = null;
        IsSubmissionPanelOpen = false;
        SubmissionCheckMessage = string.Empty;
        CanSubmit = true;

        BuildElementList();
        SyncAllPropertiesFromDocument();

        // A draft may have been saved during the session just abandoned.
        LoadDrafts();

        OnHistoryChanged(this, EventArgs.Empty);
    }

    #endregion

    #region Element list

    /// <summary>
    /// Rebuilds the element rail in the document's own layer order, so the list doubles as the
    /// z-order editor: the first row is drawn first (furthest back).
    /// Elements absent from <c>layerOrder</c> are appended so they remain reachable.
    /// </summary>
    private void BuildElementList()
    {
        var previousSelection = SelectedElement?.Kind;

        var ordered = _document.LayerOrder
            .Concat(OverlayElementKinds.DefaultOrder.Where(k => !_document.LayerOrder.Contains(k)))
            .ToList();

        Elements.Clear();
        foreach (var kind in ordered)
        {
            Elements.Add(new OverlayElementViewModel(kind, DescribeElement(kind))
            {
                IsPresent = _document.IsElementPresent(kind),
                DesignBounds = _document.GetElementBounds(kind)
            });

            // Insert RatingText right after Rating when anchored — the text is a
            // sub-element of the badge that can be dragged independently.
            if (kind == OverlayElementKind.Rating && _document.HasRatingTextElement)
            {
                Elements.Add(new OverlayElementViewModel(OverlayElementKind.RatingText, DescribeElement(OverlayElementKind.RatingText))
                {
                    IsPresent = true,
                    DesignBounds = _document.GetElementBounds(OverlayElementKind.RatingText)
                });
            }
        }

        SelectElement(Elements.FirstOrDefault(e => e.Kind == previousSelection)
                      ?? Elements.FirstOrDefault(e => e.Kind == OverlayElementKind.Poster));

        RefreshLayerCommands();
    }

    private bool CanMoveLayer(OverlayElementViewModel? element, int offset)
    {
        if (element == null)
        {
            return false;
        }

        var index = _document.LayerOrder.IndexOf(element.Kind);
        var target = index + offset;
        return index >= 0 && target >= 0 && target < _document.LayerOrder.Count;
    }

    /// <summary>
    /// Moves a layer in the z-order by one position, as a single undoable edit.
    /// </summary>
    private void MoveLayer(OverlayElementViewModel? element, int offset)
    {
        if (!CanMoveLayer(element, offset))
        {
            return;
        }

        var before = _document.LayerOrder.ToList();
        var after = before.ToList();
        var index = after.IndexOf(element!.Kind);

        (after[index], after[index + offset]) = (after[index + offset], after[index]);

        var movedKind = element.Kind;

        ApplyEdit(new PropertyEditCommand<List<OverlayElementKind>>(
            $"Reorder {element.DisplayName}",
            (d, v) => { d.LayerOrder.Clear(); d.LayerOrder.AddRange(v); },
            before,
            after));

        // The rail is ordered by layerOrder, so it has to be rebuilt, not just refreshed.
        BuildElementList();
        SelectElement(movedKind);
    }

    private void RefreshLayerCommands()
    {
        MoveLayerUpCommand.RaiseCanExecuteChanged();
        MoveLayerDownCommand.RaiseCanExecuteChanged();
    }

    private void RefreshElementBounds()
    {
        foreach (var element in Elements)
        {
            element.IsPresent = _document.IsElementPresent(element.Kind);
            element.DesignBounds = _document.GetElementBounds(element.Kind);
        }

        RaisePropertyChanged(nameof(CanvasWidth));
        RaisePropertyChanged(nameof(CanvasHeight));
    }

    private static string DescribeElement(OverlayElementKind kind) => kind switch
    {
        OverlayElementKind.Base => Lang.OverlayLayerBase,
        OverlayElementKind.Poster => Lang.Poster,
        OverlayElementKind.Front => Lang.OverlayLayerFront,
        OverlayElementKind.Rating => Lang.OverlayLayerRatingBadge,
        OverlayElementKind.RatingText => Lang.OverlayLayerRatingBadge + " Text",
        OverlayElementKind.Title => Lang.OverlayLayerTitleText,
        _ => kind.ToString()
    };

    #endregion

    #region Edit plumbing

    /// <summary>
    /// Records and applies a typed property edit, skipping no-ops and suppressing history
    /// while the ViewModel is echoing a freshly loaded document.
    /// </summary>
    private void SetDocumentProperty<T>(
        T newValue,
        T currentValue,
        Action<OverlayDesignerDocument, T> setter,
        string description,
        string propertyName,
        Action? afterApply = null)
    {
        if (_isSyncingFromDocument || EqualityComparer<T>.Default.Equals(newValue, currentValue))
        {
            return;
        }

        ApplyEdit(new PropertyEditCommand<T>(description, setter, currentValue, newValue));
        RaisePropertyChanged(propertyName);
        afterApply?.Invoke();
    }

    private void ApplyEdit(IOverlayEditCommand command)
    {
        _history.Execute(command);
        RefreshElementBounds();
        RaiseSelectedBoundsChanged();
        RequestPreview();
    }

    private void Undo()
    {
        if (_history.Undo())
        {
            SyncAllPropertiesFromDocument();
            RequestPreview();
        }
    }

    private void Redo()
    {
        if (_history.Redo())
        {
            SyncAllPropertiesFromDocument();
            RequestPreview();
        }
    }

    /// <summary>
    /// Re-reads every bound property from the document after an undo/redo or load.
    /// The guard stops the setters from recording these echoes as new edits.
    /// </summary>
    private void SyncAllPropertiesFromDocument()
    {
        _isSyncingFromDocument = true;
        try
        {
            RaisePropertyChanged(string.Empty); // Refresh all bindings.

            // The rail is ordered by LayerOrder, which undo/redo can change, so it has to be
            // rebuilt rather than merely refreshed in place.
            BuildElementList();
        }
        finally
        {
            _isSyncingFromDocument = false;
        }
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        IsDirty = _history.IsDirty;
        RefreshCommandStates();
    }

    private void RefreshCommandStates()
    {
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        BrowseImageCommand.RaiseCanExecuteChanged();
        ClearImageCommand.RaiseCanExecuteChanged();
        SaveDraftCommand.RaiseCanExecuteChanged();
        ExportPackageCommand.RaiseCanExecuteChanged();
        InstallLocallyCommand.RaiseCanExecuteChanged();
        OpenExportFolderCommand.RaiseCanExecuteChanged();
        RaisePropertyChanged(nameof(CanExport));
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Logger.Warn(ex, "Could not open {Url}", url);
            StatusMessage = Lang.OverlayCouldNotOpenBrowser;
        }
    }

    #endregion

    #region Help

    [SuppressMessage("Sonar", "S1075:URIs should not be hardcoded",
        Justification = "Canonical location of the community authoring guide.")]
    private const string helpUrl = "https://github.com/DineshSolanki/FoliCon-Overlays/blob/main/CREATING-OVERLAYS.md";

    private void OpenHelp() => OpenUrl(helpUrl);

    #endregion

    #region IDialogAware

    public DialogCloseListener RequestClose { get; }

    /// <summary>
    /// Asks the author to confirm before abandoning edits, then allows the close.
    ///
    /// Prism consults this for every close path including the window's X button, so the
    /// confirmation lives here rather than in the View. Returning a bare <c>!IsDirty</c> would
    /// veto the close outright and leave no way out of the dialog at all.
    /// </summary>
    public virtual bool CanCloseDialog()
    {
        var canClose = ConfirmDiscardIfDirty();
        if (canClose)
        {
            CleanupTemporaryWorkingFolder();
        }

        return canClose;
    }

    /// <summary>
    /// The single gate for every path that abandons unsaved edits: dialog close, the window's
    /// X button, and "← Templates" in the View.
    ///
    /// The View used to carry its own copy of this check and its message. One call site means
    /// one string to translate and no way for the two prompts to drift apart.
    /// </summary>
    /// <returns>True to proceed; false when the author chose to keep editing.</returns>
    public bool ConfirmDiscardIfDirty() => !IsDirty || ConfirmDiscard();

    /// <summary>
    /// Confirmation hook. Overridable so tests can exercise the close paths without a
    /// modal message box.
    /// </summary>
    protected virtual bool ConfirmDiscard() =>
        MessageBox.Show(
            Lang.OverlayDesignerDiscardChangesBody,
            Lang.OverlayDesignerDiscardChangesTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public virtual void OnDialogClosed() => Dispose();

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
        LoadTemplates();
        LoadDrafts();

        // Launch points may pass a package to open directly, skipping the picker.
        if (parameters != null
            && parameters.TryGetValue<string>("overlayJsonPath", out var path)
            && !string.IsNullOrWhiteSpace(path))
        {
            LoadPackage(path);
        }
    }

    /// <summary>
    /// Builds the template cards, then renders each thumbnail in the background so the picker
    /// appears immediately rather than blocking on six renders.
    /// </summary>
    private void LoadTemplates()
    {
        Templates.Clear();
        foreach (var template in _templateProvider.GetTemplates())
        {
            Templates.Add(new OverlayTemplateCardViewModel(template));
        }

        _ = RenderTemplateThumbnailsAsync([.. Templates]);
    }

    /// <summary>
    /// Refreshes the saved-draft list shown on the first-run screen.
    /// </summary>
    private void LoadDrafts()
    {
        Drafts.Clear();
        foreach (var draft in _draftStore.List())
        {
            Drafts.Add(draft);
        }

        RaisePropertyChanged(nameof(HasDrafts));
    }

    private void ResumeDraft(OverlayDraftInfo? draft)
    {
        if (draft == null)
        {
            return;
        }

        LoadPackage(_draftStore.GetDraftDefinitionPath(draft.DraftId));
    }

    private void DeleteDraft(OverlayDraftInfo? draft)
    {
        if (draft == null || !ConfirmDeleteDraft(draft.DisplayName))
        {
            return;
        }

        try
        {
            _draftStore.Delete(draft.DraftId);
            LoadDrafts();
            StatusMessage = string.Format(Lang.OverlayDesignerDraftDeleted, draft.DisplayName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Error(ex, "Failed to delete draft '{Id}'", draft.DraftId);
            StatusMessage = string.Format(Lang.OverlayDesignerDraftDeleteFailed, ex.Message);
        }
    }

    /// <summary>Overridable so tests can exercise deletion without a modal.</summary>
    protected virtual bool ConfirmDeleteDraft(string displayName) =>
        MessageBox.Show(
            string.Format(Lang.OverlayDesignerDeleteDraftBody, displayName),
            Lang.OverlayDesignerDeleteDraftTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private async Task RenderTemplateThumbnailsAsync(IReadOnlyList<OverlayTemplateCardViewModel> cards)
    {
        // A neutral, fixed context so thumbnails are comparable across templates.
        var context = new OverlayPreviewContext { Rating = "8.4", ShowRating = true, ShowMockup = true };

        foreach (var card in cards)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var image = await OverlayDesignerPreviewRenderer.RenderNowAsync(card.Template.Definition, context);
                if (image != null && !_disposed)
                {
                    card.PreviewImage = image;
                }
            }
            catch (Exception ex)
            {
                // A template that will not render still gets a card; the author just sees no art.
                Logger.Warn(ex, "Could not render thumbnail for template '{Id}'", card.Template.Id);
            }
        }
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
            CleanupTemporaryWorkingFolder();
            _previewRenderer.Rendered -= OnPreviewRendered;
            _previewRenderer.Failed -= OnPreviewFailed;
            _previewRenderer.Dispose();
            _history.Changed -= OnHistoryChanged;
        }
    }

    /// <summary>
    /// Removes the asset copy created for a new template before it becomes a user-requested
    /// draft. Saved drafts repoint the document at their persistent copy first.
    /// </summary>
    private void CleanupTemporaryWorkingFolder()
    {
        if (string.IsNullOrWhiteSpace(_temporaryWorkingFolder))
        {
            return;
        }

        var folder = _temporaryWorkingFolder;
        _temporaryWorkingFolder = null;

        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(ex, "Could not remove temporary overlay designer folder {Folder}", folder);
        }
    }
}
