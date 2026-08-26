#nullable enable
using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;
using FoliCon.ViewModels;
using Prism.Dialogs;
using Rect = System.Windows.Rect;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayDesignerViewModel"/>: selection, synchronization between the
/// canvas and the numeric editors, command enablement, validation gating, and dirty tracking.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class OverlayDesignerViewModelTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly string _tempDir;
    private readonly string _draftsRoot;
    private readonly StubOverlayProvider _provider = new();

    public OverlayDesignerViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconVmTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Isolated so tests never write into the user's real %AppData% drafts folder.
        _draftsRoot = Path.Combine(_tempDir, "drafts");
    }

    public void Dispose()
    {
        _host.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    #region First-run state

    [Fact]
    public void NewViewModel_ShowsTheTemplatePickerNotAnEmptyCanvas()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.HasDocument);
        Assert.True(viewModel.ShowTemplatePicker);
    }

    [Fact]
    public void OnDialogOpened_PopulatesTemplates()
    {
        var viewModel = CreateViewModel();

        viewModel.OnDialogOpened(new DialogParameters());

        Assert.NotEmpty(viewModel.Templates);
    }

    [Fact]
    public void TemplateCards_CarryReadableNamesAndDescriptions()
    {
        // Blank labels render as unidentifiable empty cards.
        var viewModel = CreateViewModel();

        viewModel.OnDialogOpened(new DialogParameters());

        foreach (var card in viewModel.Templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(card.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(card.Description));
        }
    }

    [Fact]
    public void TemplateCards_StartInTheLoadingStateUntilArtworkArrives()
    {
        var viewModel = CreateViewModel();

        viewModel.OnDialogOpened(new DialogParameters());

        // Thumbnails render in the background, so cards must advertise the pending state
        // rather than showing an empty frame with no explanation.
        Assert.All(viewModel.Templates, card => Assert.True(card.IsPreviewLoading));
    }

    [Fact]
    public void CreateFromTemplateCommand_RequiresACard()
    {
        var viewModel = CreateViewModel();

        Assert.False(viewModel.CreateFromTemplateCommand.CanExecute(null!));
    }

    [Fact]
    public void OnDialogOpened_WithPath_OpensThatPackageDirectly()
    {
        var path = WritePackage("direct-open");
        var viewModel = CreateViewModel();

        viewModel.OnDialogOpened(new DialogParameters { { "overlayJsonPath", path } });

        Assert.True(viewModel.HasDocument);
        Assert.False(viewModel.ShowTemplatePicker);
        Assert.Equal("direct-open", viewModel.OverlayId);
    }

    [Fact]
    public void ExportIsBlocked_UntilADocumentExists()
    {
        Assert.False(CreateViewModel().CanExport);
    }

    #endregion

    #region Loading

    [Fact]
    public void LoadPackage_PopulatesMetadataAndSelectsThePoster()
    {
        var viewModel = CreateViewModel();

        viewModel.LoadPackage(WritePackage("loaded"));

        Assert.True(viewModel.HasDocument);
        Assert.Equal("loaded", viewModel.OverlayId);
        Assert.Equal("Loaded Overlay", viewModel.DisplayName);
        Assert.Equal(OverlayElementKind.Poster, viewModel.SelectedElement?.Kind);
    }

    [Fact]
    public void LoadPackage_MissingFile_ReportsStatusWithoutLoading()
    {
        var viewModel = CreateViewModel();

        viewModel.LoadPackage(Path.Combine(_tempDir, "nope.json"));

        Assert.False(viewModel.HasDocument);
        Assert.Contains("not found", viewModel.StatusMessage);
    }

    [Fact]
    public void LoadedDocument_StartsClean()
    {
        var viewModel = CreateViewModel();

        viewModel.LoadPackage(WritePackage("clean"));

        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void LoadPackage_BuildsTheFullElementList()
    {
        var viewModel = CreateViewModel();

        viewModel.LoadPackage(WritePackage("elements"));

        Assert.Equal(5, viewModel.Elements.Count);
        Assert.Contains(viewModel.Elements, e => e.Kind == OverlayElementKind.Base);
        Assert.Contains(viewModel.Elements, e => e.Kind == OverlayElementKind.Title);
    }

    [Fact]
    public void AbsentLayers_AreListedButMarkedNotPresent()
    {
        // They stay visible so the author can see what the overlay could have.
        var viewModel = CreateViewModel();
        viewModel.LoadPackage(WritePackage("absent", includeFrontLayer: false));

        var front = viewModel.Elements.First(e => e.Kind == OverlayElementKind.Front);
        Assert.False(front.IsPresent);
    }

    #endregion

    #region Selection

    [Fact]
    public void SelectElement_MarksExactlyOneElementSelected()
    {
        var viewModel = LoadedViewModel();

        viewModel.SelectElement(OverlayElementKind.Rating);

        Assert.Equal(OverlayElementKind.Rating, viewModel.SelectedElement?.Kind);
        Assert.Single(viewModel.Elements, e => e.IsSelected);
    }

    [Fact]
    public void SelectedElementName_TracksTheSelection()
    {
        var viewModel = LoadedViewModel();

        viewModel.SelectElement(OverlayElementKind.Title);

        Assert.Equal("Title text", viewModel.SelectedElementName);
    }

    [Fact]
    public void SelectionGeometry_ReflectsTheSelectedElement()
    {
        var viewModel = LoadedViewModel();

        viewModel.SelectElement(OverlayElementKind.Poster);
        var posterLeft = viewModel.SelectedLeft;

        viewModel.SelectElement(OverlayElementKind.Base);

        Assert.NotEqual(posterLeft, viewModel.SelectedLeft);
    }

    #endregion

    #region Canvas and numeric synchronization

    [Fact]
    public void NumericEdit_MovesTheElementAndMarksDirty()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        viewModel.SelectedLeft = 55;

        Assert.Equal(55, viewModel.SelectedLeft);
        Assert.True(viewModel.IsDirty);
    }

    [Fact]
    public void CanvasGestureAndNumericEdit_ConvergeOnTheSameBounds()
    {
        // The acceptance criterion for hybrid editing: both paths must agree exactly.
        var viaGesture = LoadedViewModel();
        var viaNumeric = LoadedViewModel();

        viaGesture.SelectElement(OverlayElementKind.Poster);
        viaNumeric.SelectElement(OverlayElementKind.Poster);

        viaGesture.ApplyGesture(new Rect(40, 50, 150, 160));
        viaGesture.EndGesture();

        viaNumeric.SelectedLeft = 40;
        viaNumeric.SelectedTop = 50;
        viaNumeric.SelectedWidth = 150;
        viaNumeric.SelectedHeight = 160;

        Assert.Equal(viaNumeric.SelectedLeft, viaGesture.SelectedLeft);
        Assert.Equal(viaNumeric.SelectedTop, viaGesture.SelectedTop);
        Assert.Equal(viaNumeric.SelectedWidth, viaGesture.SelectedWidth);
        Assert.Equal(viaNumeric.SelectedHeight, viaGesture.SelectedHeight);
    }

    [Fact]
    public void DragGesture_CollapsesIntoASingleUndoStep()
    {
        // A drag fires many mouse-moves; undo must rewind the gesture, not one frame of it.
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var startLeft = viewModel.SelectedLeft;

        for (var x = 10; x <= 60; x += 10)
        {
            viewModel.ApplyGesture(new Rect(x, 20, 100, 100));
        }
        viewModel.EndGesture();

        Assert.Equal(60, viewModel.SelectedLeft);

        viewModel.UndoCommand.Execute();

        Assert.Equal(startLeft, viewModel.SelectedLeft);
        Assert.False(viewModel.UndoCommand.CanExecute());
    }

    [Fact]
    public void GestureThatMovesNothing_CreatesNoUndoStep()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        var bounds = new Rect(viewModel.SelectedLeft, viewModel.SelectedTop,
                              viewModel.SelectedWidth, viewModel.SelectedHeight);
        viewModel.ApplyGesture(bounds);
        viewModel.EndGesture();

        Assert.False(viewModel.UndoCommand.CanExecute());
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void GestureSnapsToWholePixels()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        viewModel.ApplyGesture(new Rect(40.7, 50.2, 150.4, 160.8));
        viewModel.EndGesture();

        Assert.Equal(41, viewModel.SelectedLeft);
        Assert.Equal(50, viewModel.SelectedTop);
    }

    #endregion

    #region Keyboard nudge

    [Theory]
    [InlineData("left", -1, 0)]
    [InlineData("right", 1, 0)]
    [InlineData("up", 0, -1)]
    [InlineData("down", 0, 1)]
    public void Nudge_MovesByOnePixel(string direction, double dx, double dy)
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var startX = viewModel.SelectedLeft;
        var startY = viewModel.SelectedTop;

        viewModel.NudgeCommand.Execute(direction);

        Assert.Equal(startX + dx, viewModel.SelectedLeft);
        Assert.Equal(startY + dy, viewModel.SelectedTop);
    }

    [Theory]
    [InlineData("left-shift", -10)]
    [InlineData("right-shift", 10)]
    public void ShiftNudge_MovesByTenPixels(string direction, double dx)
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var startX = viewModel.SelectedLeft;

        viewModel.NudgeCommand.Execute(direction);

        Assert.Equal(startX + dx, viewModel.SelectedLeft);
    }

    [Fact]
    public void Nudge_IsAvailableAsSoonAsTheDialogOpens()
    {
        // The constructor selects the poster, so arrow keys work immediately —
        // a keyboard user never has to click the canvas first.
        var viewModel = CreateViewModel();

        Assert.NotNull(viewModel.SelectedElement);
        Assert.True(viewModel.NudgeCommand.CanExecute("left"));
    }

    [Fact]
    public void Nudge_WithAnUnrecognisedDirection_DoesNothing()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var startX = viewModel.SelectedLeft;

        viewModel.NudgeCommand.Execute("sideways");

        Assert.Equal(startX, viewModel.SelectedLeft);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void Nudge_IsUndoable()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var startX = viewModel.SelectedLeft;

        viewModel.NudgeCommand.Execute("right");
        viewModel.UndoCommand.Execute();

        Assert.Equal(startX, viewModel.SelectedLeft);
    }

    #endregion

    #region Undo/redo

    [Fact]
    public void UndoRedo_AreDisabledUntilThereIsHistory()
    {
        var viewModel = LoadedViewModel();

        Assert.False(viewModel.UndoCommand.CanExecute());
        Assert.False(viewModel.RedoCommand.CanExecute());
    }

    [Fact]
    public void UndoThenRedo_RoundTripsAMetadataEdit()
    {
        var viewModel = LoadedViewModel();
        var original = viewModel.DisplayName;

        viewModel.DisplayName = "Renamed";
        Assert.True(viewModel.UndoCommand.CanExecute());

        viewModel.UndoCommand.Execute();
        Assert.Equal(original, viewModel.DisplayName);

        viewModel.RedoCommand.Execute();
        Assert.Equal("Renamed", viewModel.DisplayName);
    }

    [Fact]
    public void UndoingBackToTheLoadedState_ClearsDirty()
    {
        var viewModel = LoadedViewModel();

        viewModel.DisplayName = "Changed";
        Assert.True(viewModel.IsDirty);

        viewModel.UndoCommand.Execute();
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void SettingAPropertyToItsCurrentValue_IsNotAnEdit()
    {
        var viewModel = LoadedViewModel();

#pragma warning disable S1656 // Intentional self-assignment to verify property setter idempotency
        viewModel.DisplayName = viewModel.DisplayName;
#pragma warning restore S1656

        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.UndoCommand.CanExecute());
    }

    #endregion

    #region Layer toggles

    [Fact]
    public void TogglingALayer_UpdatesPresenceAndIsUndoable()
    {
        var viewModel = LoadedViewModel();

        viewModel.HasFrontLayer = false;
        Assert.False(viewModel.Elements.First(e => e.Kind == OverlayElementKind.Front).IsPresent);

        viewModel.UndoCommand.Execute();
        Assert.True(viewModel.HasFrontLayer);
    }

    [Fact]
    public void TogglingTitleVisibility_UpdatesTheElementList()
    {
        var viewModel = LoadedViewModel();

        viewModel.TitleIsVisible = true;

        Assert.True(viewModel.Elements.First(e => e.Kind == OverlayElementKind.Title).IsPresent);
    }

    #endregion

    #region Tags

    [Fact]
    public void TagsText_ParsesCommaSeparatedValues()
    {
        var viewModel = LoadedViewModel();

        viewModel.TagsText = "retro, vhs , classic";

        Assert.Equal("retro, vhs, classic", viewModel.TagsText);
    }

    [Fact]
    public void TagsText_IsUndoable()
    {
        var viewModel = LoadedViewModel();
        var original = viewModel.TagsText;

        viewModel.TagsText = "one, two";
        viewModel.UndoCommand.Execute();

        Assert.Equal(original, viewModel.TagsText);
    }

    #endregion

    #region Validation

    [Fact]
    public void LoadingAValidPackage_ReportsNoErrors()
    {
        var viewModel = LoadedViewModel();

        Assert.False(viewModel.HasValidationErrors);
        Assert.True(viewModel.CanExport);
    }

    [Fact]
    public void AnInvalidEdit_BlocksExportAndSurfacesTheIssue()
    {
        var viewModel = LoadedViewModel();

        viewModel.OverlayId = "Not A Valid Id";

        Assert.True(viewModel.HasValidationErrors);
        Assert.False(viewModel.CanExport);
        Assert.Contains(viewModel.ValidationIssues, i => i.Field == "id");
    }

    [Fact]
    public void FixingTheIssue_RestoresExport()
    {
        var viewModel = LoadedViewModel();
        viewModel.OverlayId = "Bad Id";
        Assert.False(viewModel.CanExport);

        viewModel.OverlayId = "good-id";

        Assert.True(viewModel.CanExport);
    }

    [Fact]
    public void ClickingAnIssue_SelectsTheElementItBelongsTo()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Base);

        viewModel.FocusIssueCommand.Execute(
            new OverlayValidationIssue(OverlayValidationSeverity.Error, "rating.fontSize", "bad"));

        Assert.Equal(OverlayElementKind.Rating, viewModel.SelectedElement?.Kind);
    }

    [Fact]
    public void ValidationSummary_DescribesTheCounts()
    {
        var viewModel = LoadedViewModel();
        viewModel.OverlayId = "Bad Id";

        Assert.Contains("Errors", viewModel.ValidationSummary);
    }

    /// <summary>
    /// The summary must not build plurals by appending "s" — that has no equivalent in
    /// ru/ar/ja/hi, so the phrasing has to work unchanged for any count.
    /// </summary>
    [Fact]
    public void ValidationSummary_UsesNoEnglishPluralSuffix()
    {
        var viewModel = LoadedViewModel();
        viewModel.OverlayId = "Bad Id";

        Assert.DoesNotContain("(s)", viewModel.ValidationSummary);
        Assert.DoesNotContain("errors:", viewModel.ValidationSummary);
        Assert.StartsWith("Errors: ", viewModel.ValidationSummary);
    }

    #endregion

    #region Zoom and test controls

    [Fact]
    public void DefaultZoomIsTwo_SoSmallArtworkIsWorkable()
    {
        Assert.Equal(2, CreateViewModel().Zoom);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("4", 4)]
    public void SetZoom_AcceptsTheOfferedLevels(string parameter, double expected)
    {
        var viewModel = CreateViewModel();

        viewModel.SetZoomCommand.Execute(parameter);

        Assert.Equal(expected, viewModel.Zoom);
    }

    [Fact]
    public void SetZoom_IgnoresUnsupportedValues()
    {
        var viewModel = CreateViewModel();

        viewModel.SetZoomCommand.Execute("7");

        Assert.Equal(2, viewModel.Zoom);
    }

    [Fact]
    public void Zoom_ScalesTheCanvasButNotTheDocument()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var leftBefore = viewModel.SelectedLeft;

        viewModel.SetZoomCommand.Execute("4");

        Assert.Equal(1024, viewModel.CanvasWidth);
        Assert.Equal(leftBefore, viewModel.SelectedLeft);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void ChangingTestControls_DoesNotDirtyTheOverlay()
    {
        // Preview state lives outside the document by design.
        var viewModel = LoadedViewModel();

        viewModel.TestRating = "9.9";
        viewModel.TestTitle = "A different title";
        viewModel.ShowTestRating = false;
        viewModel.ShowTestMockup = false;

        Assert.False(viewModel.IsDirty);
    }

    #endregion

    #region Returning to the template picker

    [Fact]
    public void ReturnToTemplates_ShowsThePickerAgain()
    {
        // Leaving an overlay should keep the author in the designer, not eject them
        // back to the overlay chooser.
        var viewModel = LoadedViewModel();
        Assert.True(viewModel.HasDocument);

        viewModel.ReturnToTemplates();

        Assert.False(viewModel.HasDocument);
        Assert.True(viewModel.ShowTemplatePicker);
    }

    [Fact]
    public void ReturnToTemplates_KeepsTheTemplateListAvailable()
    {
        var viewModel = CreateViewModel();
        viewModel.OnDialogOpened(new DialogParameters());
        var templateCount = viewModel.Templates.Count;
        viewModel.LoadPackage(WritePackage("back-to-list"));

        viewModel.ReturnToTemplates();

        Assert.Equal(templateCount, viewModel.Templates.Count);
    }

    [Fact]
    public void ReturnToTemplates_ClearsTheEditingState()
    {
        var viewModel = LoadedViewModel();
        viewModel.OverlayId = "Bad Id";
        Assert.NotEmpty(viewModel.ValidationIssues);

        viewModel.ReturnToTemplates();

        Assert.Empty(viewModel.ValidationIssues);
        Assert.False(viewModel.HasValidationErrors);
        Assert.Null(viewModel.PreviewImage);
        Assert.False(viewModel.IsDirty);
        Assert.False(viewModel.CanExport);
    }

    [Fact]
    public void ReturnToTemplates_DropsUndoHistoryFromThePreviousOverlay()
    {
        // Undoing into an abandoned document would resurrect state the author left behind.
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Changed";
        Assert.True(viewModel.UndoCommand.CanExecute());

        viewModel.ReturnToTemplates();

        Assert.False(viewModel.UndoCommand.CanExecute());
        Assert.False(viewModel.RedoCommand.CanExecute());
    }

    [Fact]
    public void AfterReturningToTemplates_ADifferentOverlayCanBeOpened()
    {
        var viewModel = CreateViewModel();
        viewModel.LoadPackage(WritePackage("first-overlay"));

        viewModel.ReturnToTemplates();
        viewModel.LoadPackage(WritePackage("second-overlay"));

        Assert.True(viewModel.HasDocument);
        Assert.Equal("second-overlay", viewModel.OverlayId);
        Assert.False(viewModel.IsDirty);
    }

    #endregion

    #region Layer reordering

    [Fact]
    public void ElementList_IsOrderedByTheDocumentsLayerOrder()
    {
        // The rail doubles as the z-order editor, so its order must mirror layerOrder.
        var viewModel = LoadedViewModel();

        var railOrder = viewModel.Elements.Select(e => e.Kind).Take(4).ToList();

        Assert.Equal(
            [OverlayElementKind.Base, OverlayElementKind.Poster, OverlayElementKind.Front, OverlayElementKind.Rating],
            railOrder);
    }

    [Fact]
    public void ElementList_IncludesElementsMissingFromLayerOrder()
    {
        // The test package omits "title" from layerOrder; it must still be reachable.
        var viewModel = LoadedViewModel();

        Assert.Contains(viewModel.Elements, e => e.Kind == OverlayElementKind.Title);
    }

    [Fact]
    public void MoveLayerUp_MovesTheElementFurtherBack()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        viewModel.MoveLayerUpCommand.Execute(viewModel.SelectedElement!);

        Assert.Equal(OverlayElementKind.Poster, viewModel.Elements[0].Kind);
        Assert.Equal(OverlayElementKind.Base, viewModel.Elements[1].Kind);
    }

    [Fact]
    public void MoveLayerDown_MovesTheElementFurtherForward()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Base);

        viewModel.MoveLayerDownCommand.Execute(viewModel.SelectedElement!);

        Assert.Equal(OverlayElementKind.Poster, viewModel.Elements[0].Kind);
        Assert.Equal(OverlayElementKind.Base, viewModel.Elements[1].Kind);
    }

    [Fact]
    public void MoveLayer_KeepsTheMovedElementSelected()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        viewModel.MoveLayerUpCommand.Execute(viewModel.SelectedElement!);

        Assert.Equal(OverlayElementKind.Poster, viewModel.SelectedElement?.Kind);
    }

    [Fact]
    public void MoveLayer_IsUndoable()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);
        var originalFirst = viewModel.Elements[0].Kind;

        viewModel.MoveLayerUpCommand.Execute(viewModel.SelectedElement!);
        viewModel.UndoCommand.Execute();

        Assert.Equal(originalFirst, viewModel.Elements[0].Kind);
    }

    [Fact]
    public void MoveLayer_ChangesTheExportedLayerOrder()
    {
        var viewModel = LoadedViewModel();
        viewModel.SelectElement(OverlayElementKind.Poster);

        viewModel.MoveLayerUpCommand.Execute(viewModel.SelectedElement!);

        // Reordering must reach the schema, not just the UI list.
        Assert.True(viewModel.IsDirty);
        Assert.Equal(OverlayElementKind.Poster, viewModel.Elements[0].Kind);
    }

    [Fact]
    public void MoveLayer_AtTheEdges_IsDisabled()
    {
        var viewModel = LoadedViewModel();

        // First rail entry is the back of the z-order: nothing to move behind.
        viewModel.SelectElement(viewModel.Elements[0].Kind);
        Assert.False(viewModel.MoveLayerUpCommand.CanExecute(viewModel.SelectedElement!));

        // Last entry that appears in layerOrder is the front: nothing to move ahead of.
        viewModel.SelectElement(viewModel.Elements[3].Kind);
        Assert.False(viewModel.MoveLayerDownCommand.CanExecute(viewModel.SelectedElement!));
    }

    #endregion

    #region Per-corner clip radius

    [Fact]
    public void CornerRadius_ReadsTheUniformShorthand()
    {
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "8";

        Assert.Equal(8, viewModel.ClipRadiusTopLeft);
        Assert.Equal(8, viewModel.ClipRadiusBottomRight);
        Assert.True(viewModel.HasUniformCornerRadius);
    }

    [Fact]
    public void CornerRadius_ReadsTheFourValueForm()
    {
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "1,2,3,4";

        Assert.Equal(1, viewModel.ClipRadiusTopLeft);
        Assert.Equal(2, viewModel.ClipRadiusTopRight);
        Assert.Equal(3, viewModel.ClipRadiusBottomRight);
        Assert.Equal(4, viewModel.ClipRadiusBottomLeft);
        Assert.False(viewModel.HasUniformCornerRadius);
    }

    [Fact]
    public void SettingOneCorner_ExpandsToTheFourValueForm()
    {
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "8";

        viewModel.ClipRadiusTopLeft = 16;

        Assert.Equal("16,8,8,8", viewModel.PosterClipRadius);
    }

    [Fact]
    public void SettingAllCornersAlike_CollapsesBackToShorthand()
    {
        // Keeps exported JSON tidy rather than always emitting four identical values.
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "1,2,3,4";

        viewModel.ClipRadiusTopLeft = 5;
        viewModel.ClipRadiusTopRight = 5;
        viewModel.ClipRadiusBottomRight = 5;
        viewModel.ClipRadiusBottomLeft = 5;

        Assert.Equal("5", viewModel.PosterClipRadius);
    }

    [Fact]
    public void CornerRadius_RejectsNegativeValues()
    {
        var viewModel = LoadedViewModel();

        viewModel.ClipRadiusTopLeft = -10;

        Assert.Equal(0, viewModel.ClipRadiusTopLeft);
    }

    [Fact]
    public void CornerRadius_IsUndoable()
    {
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "8";

        viewModel.ClipRadiusTopLeft = 20;
        viewModel.UndoCommand.Execute();

        Assert.Equal(8, viewModel.ClipRadiusTopLeft);
    }

    [Fact]
    public void CornerRadius_UnparseableValue_DegradesToSquare()
    {
        var viewModel = LoadedViewModel();
        viewModel.PosterClipRadius = "not-a-number";

        Assert.Equal(0, viewModel.ClipRadiusTopLeft);
    }

    #endregion

    #region Draft saving

    [Fact]
    public void SaveDraft_IsUnavailableUntilAnOverlayIsOpen()
    {
        Assert.False(CreateViewModel().SaveDraftCommand.CanExecute());
    }

    [Fact]
    public void CreateFromTemplate_DoesNotCreateADraftUntilSaveDraftIsRequested()
    {
        var viewModel = CreateViewModel();
        viewModel.OnDialogOpened(new DialogParameters());

        viewModel.CreateFromTemplateCommand.Execute(viewModel.Templates[0]);

        Assert.True(viewModel.HasDocument);
        Assert.False(Directory.Exists(Path.Combine(_draftsRoot, viewModel.OverlayId)));
    }

    [Fact]
    public void SaveDraft_WritesTheDraftAndClearsDirty()
    {
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Work in progress";
        Assert.True(viewModel.IsDirty);

        viewModel.SaveDraftCommand.Execute();

        Assert.False(viewModel.IsDirty);
        Assert.Contains("Draft saved", viewModel.StatusMessage);
        Assert.True(File.Exists(Path.Combine(_draftsRoot, viewModel.OverlayId, "overlay.json")));
    }

    [Fact]
    public void SaveDraft_ThenEditAgain_MarksDirtyOnceMore()
    {
        var viewModel = LoadedViewModel();
        viewModel.SaveDraftCommand.Execute();

        viewModel.DisplayName = "Changed after saving";

        Assert.True(viewModel.IsDirty);
    }

    #endregion

    #region Title colour

    [Fact]
    public void ApplyTitleColour_OpaqueColour_UsesShortHex()
    {
        // #RRGGBB is what an author expects to see; #FFRRGGBB is noise for an opaque colour.
        var viewModel = LoadedViewModel();

        viewModel.ApplyTitleColour(System.Windows.Media.Color.FromRgb(0xFF, 0x55, 0x00));

        Assert.Equal("#FF5500", viewModel.TitleForeground);
    }

    [Fact]
    public void ApplyTitleColour_TranslucentColour_KeepsTheAlphaChannel()
    {
        var viewModel = LoadedViewModel();

        viewModel.ApplyTitleColour(System.Windows.Media.Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

        Assert.Equal("#80FFFFFF", viewModel.TitleForeground);
    }

    [Fact]
    public void TitleForegroundBrush_TracksTheStoredValue()
    {
        var viewModel = LoadedViewModel();

        viewModel.ApplyTitleColour(System.Windows.Media.Color.FromRgb(0x00, 0x80, 0xFF));

        var brush = Assert.IsType<System.Windows.Media.SolidColorBrush>(viewModel.TitleForegroundBrush);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0x00, 0x80, 0xFF), brush.Color);
    }

    [Fact]
    public void TitleForegroundBrush_AcceptsNamedColours()
    {
        // The text field stays available, so named colours must still resolve for the swatch.
        var viewModel = LoadedViewModel();

        viewModel.TitleForeground = "Gold";

        var brush = Assert.IsType<System.Windows.Media.SolidColorBrush>(viewModel.TitleForegroundBrush);
        Assert.Equal(System.Windows.Media.Colors.Gold, brush.Color);
    }

    [Fact]
    public void TitleForegroundBrush_UnparseableValue_FallsBackToWhite()
    {
        // Matches what the renderer draws for a bad value, so the swatch never lies.
        var viewModel = LoadedViewModel();

        viewModel.TitleForeground = "not-a-colour";

        var brush = Assert.IsType<System.Windows.Media.SolidColorBrush>(viewModel.TitleForegroundBrush);
        Assert.Equal(System.Windows.Media.Colors.White, brush.Color);
    }

    [Fact]
    public void ApplyTitleColour_IsUndoable()
    {
        var viewModel = LoadedViewModel();
        viewModel.TitleForeground = "White";

        viewModel.ApplyTitleColour(System.Windows.Media.Color.FromRgb(0xFF, 0x00, 0x00));
        viewModel.UndoCommand.Execute();

        Assert.Equal("White", viewModel.TitleForeground);
    }

    #endregion

    #region Resuming drafts

    [Fact]
    public void Drafts_AreListedOnTheFirstRunScreen()
    {
        // Resuming must not require remembering where a draft folder lives.
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Half-finished work";
        viewModel.SaveDraftCommand.Execute();

        viewModel.ReturnToTemplates();

        Assert.True(viewModel.HasDrafts);
        Assert.Contains(viewModel.Drafts, d => d.DisplayName == "Half-finished work");
    }

    [Fact]
    public void Drafts_AreEmptyWhenNoneHaveBeenSaved()
    {
        var viewModel = CreateViewModel();

        viewModel.OnDialogOpened(new DialogParameters());

        Assert.False(viewModel.HasDrafts);
        Assert.Empty(viewModel.Drafts);
    }

    [Fact]
    public void SavingADraft_AddsItToTheResumeListImmediately()
    {
        var viewModel = LoadedViewModel();

        viewModel.SaveDraftCommand.Execute();

        Assert.True(viewModel.HasDrafts);
    }

    [Fact]
    public void ResumeDraft_ReopensTheSavedWork()
    {
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Resumable";
        viewModel.SelectedLeft = 77;
        viewModel.SaveDraftCommand.Execute();
        var draft = viewModel.Drafts.Single();
        viewModel.ReturnToTemplates();

        viewModel.ResumeDraftCommand.Execute(draft);

        Assert.True(viewModel.HasDocument);
        Assert.Equal("Resumable", viewModel.DisplayName);
        viewModel.SelectElement(OverlayElementKind.Poster);
        Assert.Equal(77, viewModel.SelectedLeft);
    }

    [Fact]
    public void ResumedDraft_StartsClean()
    {
        var viewModel = LoadedViewModel();
        viewModel.SaveDraftCommand.Execute();
        var draft = viewModel.Drafts.Single();
        viewModel.ReturnToTemplates();

        viewModel.ResumeDraftCommand.Execute(draft);

        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void DeleteDraft_RemovesItFromTheListAndDisk()
    {
        var viewModel = LoadedViewModel();
        viewModel.SaveDraftCommand.Execute();
        var draft = viewModel.Drafts.Single();

        viewModel.DeleteDraftCommand.Execute(draft);

        Assert.False(viewModel.HasDrafts);
        Assert.False(Directory.Exists(draft.FolderPath));
    }

    [Fact]
    public void DeleteDraft_WhenDeclined_KeepsTheDraft()
    {
        var viewModel = LoadedViewModel();
        viewModel.ConfirmDeleteDraftResult = false;
        viewModel.SaveDraftCommand.Execute();
        var draft = viewModel.Drafts.Single();

        viewModel.DeleteDraftCommand.Execute(draft);

        Assert.True(viewModel.HasDrafts);
        Assert.True(Directory.Exists(draft.FolderPath));
    }

    #endregion

    #region Export

    [Fact]
    public void Export_IsBlockedWhileValidationFails()
    {
        var viewModel = LoadedViewModel();
        Assert.True(viewModel.ExportPackageCommand.CanExecute());

        viewModel.OverlayId = "Not Valid";

        Assert.False(viewModel.ExportPackageCommand.CanExecute());
    }

    [Fact]
    public void SubmissionPanel_IsHiddenBeforeAnyExport()
    {
        var viewModel = LoadedViewModel();

        Assert.False(viewModel.IsSubmissionPanelOpen);
        Assert.Null(viewModel.LastExportPath);
    }

    [Fact]
    public void InstallLocally_IsUnavailableBeforeAnExport()
    {
        // Installing copies the exported folder, so there is nothing to install yet.
        var viewModel = LoadedViewModel();

        Assert.False(viewModel.InstallLocallyCommand.CanExecute());
        Assert.False(viewModel.OpenExportFolderCommand.CanExecute());
    }

    [Fact]
    public void SubmissionTargetPath_MatchesTheRepositoryLayout()
    {
        var viewModel = LoadedViewModel();

        Assert.Equal($"overlays/{viewModel.OverlayId}/", viewModel.SubmissionTargetPath);
    }

    [Fact]
    public void ReturnToTemplates_ClearsExportState()
    {
        // A previous overlay's export has nothing to do with the next one.
        var viewModel = LoadedViewModel();

        viewModel.ReturnToTemplates();

        Assert.Null(viewModel.LastExportPath);
        Assert.False(viewModel.IsSubmissionPanelOpen);
        Assert.True(viewModel.CanSubmit);
    }

    [Fact]
    public void OpeningAnotherOverlay_ClearsExportState()
    {
        var viewModel = CreateViewModel();
        viewModel.LoadPackage(WritePackage("first-pkg"));

        viewModel.LoadPackage(WritePackage("second-pkg"));

        Assert.Null(viewModel.LastExportPath);
        Assert.False(viewModel.IsSubmissionPanelOpen);
    }

    #endregion

    #region Dialog lifecycle

    [Fact]
    public void CanCloseDialog_WhenClean_ClosesWithoutAsking()
    {
        var viewModel = LoadedViewModel();

        Assert.True(viewModel.CanCloseDialog());
        Assert.Equal(0, viewModel.ConfirmDiscardCallCount);
    }

    [Fact]
    public void CanCloseDialog_WhenDirty_AsksBeforeClosing()
    {
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Changed";

        Assert.True(viewModel.CanCloseDialog());
        Assert.Equal(1, viewModel.ConfirmDiscardCallCount);
    }

    [Fact]
    public void CanCloseDialog_WhenAuthorKeepsEditing_StaysOpen()
    {
        var viewModel = LoadedViewModel();
        viewModel.ConfirmDiscardResult = false;
        viewModel.DisplayName = "Changed";

        Assert.False(viewModel.CanCloseDialog());
    }

    [Fact]
    public void CanCloseDialog_AfterDecliningOnce_CanStillCloseLater()
    {
        // The dialog must never become a trap: declining once cannot permanently veto exit.
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Changed";

        viewModel.ConfirmDiscardResult = false;
        Assert.False(viewModel.CanCloseDialog());

        viewModel.ConfirmDiscardResult = true;
        Assert.True(viewModel.CanCloseDialog());
    }

    [Fact]
    public void CanCloseDialog_AfterUndoingBackToClean_ClosesWithoutAsking()
    {
        var viewModel = LoadedViewModel();
        viewModel.DisplayName = "Changed";
        viewModel.UndoCommand.Execute();

        Assert.True(viewModel.CanCloseDialog());
        Assert.Equal(0, viewModel.ConfirmDiscardCallCount);
    }

    #endregion

    #region Helpers

    private TestableDesignerViewModel CreateViewModel() =>
        WpfTestHost.Invoke(() => new TestableDesignerViewModel(
            new DialogCloseListener(),
            _provider,
            new OverlayTemplateProvider(_provider),
            new OverlayPackageLoader(),
            // A long debounce keeps background renders out of the assertions.
            new OverlayDesignerPreviewRenderer(TimeSpan.FromSeconds(30)),
            new OverlayDraftStore(_draftsRoot)));

    private TestableDesignerViewModel LoadedViewModel()
    {
        var viewModel = CreateViewModel();
        viewModel.LoadPackage(WritePackage($"pkg{Guid.NewGuid():N}"[..12]));
        return viewModel;
    }

    private string WritePackage(string id, bool includeFrontLayer = true)
    {
        var folder = Path.Combine(_tempDir, id);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "base.png"), new byte[100]);
        File.WriteAllBytes(Path.Combine(folder, "front.png"), new byte[100]);

        var definition = new PosterOverlayDefinition
        {
            SchemaVersion = 1,
            Id = id,
            DisplayName = "Loaded Overlay",
            Author = "Test Author",
            OverlayVersion = "1.0.0",
            Tags = ["alpha", "beta"],
            DesignWidth = 265,
            DesignHeight = 256,
            RootMargin = "0,0,0,-11",
            RenderWidth = 256,
            RenderHeight = 256,
            LayerOrder = ["base", "poster", "front", "rating"],
            BaseLayer = new LayerDefinition { ImagePath = "base.png", Margin = "30,14,48,15" },
            FrontLayer = includeFrontLayer
                ? new LayerDefinition { ImagePath = "front.png", Margin = "16,14,35,15" }
                : null,
            Poster = new PosterConfig { Margin = "31,42,50,19", ClipRadius = "0" },
            Rating = new RatingConfig
            {
                ShieldMargin = "160,97,6,5",
                TextMargin = "189,30,21,24",
                FontSize = 25,
                FontFamily = "Castellar"
            },
            Title = new TitleConfig { IsVisible = false, RotationOrigin = "0.5,0.5" }
        };

        var path = Path.Combine(folder, "overlay.json");
        File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(definition, Newtonsoft.Json.Formatting.Indented));
        return path;
    }

    /// <summary>
    /// Replaces the modal discard confirmation with a scripted answer so the close paths
    /// can be tested headlessly.
    /// </summary>
    private sealed class TestableDesignerViewModel(
        DialogCloseListener requestClose,
        IOverlayProvider overlayProvider,
        OverlayTemplateProvider templateProvider,
        OverlayPackageLoader packageLoader,
        OverlayDesignerPreviewRenderer previewRenderer,
        OverlayDraftStore draftStore)
        : OverlayDesignerViewModel(requestClose, overlayProvider, templateProvider, packageLoader, previewRenderer, draftStore)
    {
        /// <summary>What the author "answers" when asked to discard. Defaults to yes.</summary>
        public bool ConfirmDiscardResult { get; set; } = true;

        public int ConfirmDiscardCallCount { get; private set; }

        /// <summary>What the author "answers" when asked to delete a draft. Defaults to yes.</summary>
        public bool ConfirmDeleteDraftResult { get; set; } = true;

        protected override bool ConfirmDiscard()
        {
            ConfirmDiscardCallCount++;
            return ConfirmDiscardResult;
        }

        protected override bool ConfirmDeleteDraft(string displayName) => ConfirmDeleteDraftResult;
    }

    /// <summary>Keeps the ViewModel away from the user's real %AppData% overlay folder.</summary>
    private sealed class StubOverlayProvider : IOverlayProvider
    {
        private readonly List<PosterOverlayDefinition> _overlays =
        [
            new()
            {
                Id = "stub-template",
                DisplayName = "Stub Template",
                Author = "FoliCon",
                OverlayVersion = "1.0.0",
                IsBuiltIn = true,
                Poster = new PosterConfig { Margin = "10,10,10,10", ClipRadius = "0" },
                Rating = new RatingConfig(),
                Title = new TitleConfig()
            }
        ];

        public IReadOnlyList<PosterOverlayDefinition> GetAllOverlays() => _overlays;

        public IReadOnlyList<PosterOverlayDefinition> GetUserOverlays() => [];

        public PosterOverlayDefinition? GetOverlayById(string id) =>
            _overlays.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        public PosterOverlayDefinition ResolveActiveOverlayOrDefault(string? activeOverlayId) => _overlays[0];

        public bool IsOverlayInstalled(string id) => GetOverlayById(id) != null;

        public string GetOverlayFolderPath(string id) => Path.Combine(Path.GetTempPath(), id);

        public void Refresh() { /* No-op: stub for interface; overlays are static in tests */ }
    }

    #endregion
}
