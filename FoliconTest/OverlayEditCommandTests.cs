using FoliCon.Modules.Overlays.Designer;
using Thickness = System.Windows.Thickness;

namespace FoliconTest;

/// <summary>
/// Tests for the designer's edit commands and undo/redo history.
/// </summary>
public class OverlayEditCommandTests
{
    #region Individual commands

    [Fact]
    public void PropertyEditCommand_ExecuteThenUndo_RestoresOriginalValue()
    {
        var document = new OverlayDesignerDocument { DisplayName = "Before" };
        var command = new PropertyEditCommand<string>(
            "Rename", (d, v) => d.DisplayName = v, "Before", "After");

        command.Execute(document);
        Assert.Equal("After", document.DisplayName);

        command.Undo(document);
        Assert.Equal("Before", document.DisplayName);
    }

    [Fact]
    public void ElementBoundsCommand_MovesAndRestoresTheTargetedElement()
    {
        var document = new OverlayDesignerDocument { PosterMargin = new Thickness(10) };
        var command = new ElementBoundsCommand(
            OverlayElementKind.Poster, new Thickness(10), new Thickness(20), "Move poster");

        command.Execute(document);
        Assert.Equal(new Thickness(20), document.PosterMargin);

        command.Undo(document);
        Assert.Equal(new Thickness(10), document.PosterMargin);
    }

    [Fact]
    public void CompositeEditCommand_AppliesAllAndUndoesInReverse()
    {
        var document = new OverlayDesignerDocument();
        var order = new List<string>();

        var composite = new CompositeEditCommand("Enable base layer",
        [
            new PropertyEditCommand<bool>("Show", (d, v) => { d.HasBaseLayer = v; order.Add("flag"); }, false, true),
            new PropertyEditCommand<string>("Path", (d, v) => { d.BaseLayerImagePath = v; order.Add("path"); }, "", "base.png")
        ]);

        composite.Execute(document);
        Assert.True(document.HasBaseLayer);
        Assert.Equal("base.png", document.BaseLayerImagePath);
        Assert.Equal(["flag", "path"], order);

        order.Clear();
        composite.Undo(document);
        Assert.False(document.HasBaseLayer);
        Assert.Equal("", document.BaseLayerImagePath);
        Assert.Equal(["path", "flag"], order);
    }

    #endregion

    #region History basics

    [Fact]
    public void NewHistory_HasNothingToUndoOrRedo()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void Execute_AppliesCommandAndEnablesUndo()
    {
        var document = new OverlayDesignerDocument { DisplayName = "Before" };
        var history = new OverlayEditHistory(document);

        history.Execute(Rename("Before", "After"));

        Assert.Equal("After", document.DisplayName);
        Assert.True(history.CanUndo);
        Assert.True(history.IsDirty);
    }

    [Fact]
    public void UndoRedo_RestoresAndReappliesTheEdit()
    {
        var document = new OverlayDesignerDocument { DisplayName = "Before" };
        var history = new OverlayEditHistory(document);
        history.Execute(Rename("Before", "After"));

        Assert.True(history.Undo());
        Assert.Equal("Before", document.DisplayName);
        Assert.True(history.CanRedo);

        Assert.True(history.Redo());
        Assert.Equal("After", document.DisplayName);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_OnEmptyHistory_ReturnsFalse()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());

        Assert.False(history.Undo());
        Assert.False(history.Redo());
    }

    [Fact]
    public void NewEditAfterUndo_DiscardsTheRedoBranch()
    {
        var document = new OverlayDesignerDocument { DisplayName = "A" };
        var history = new OverlayEditHistory(document);
        history.Execute(Rename("A", "B"));
        history.Undo();

        Assert.True(history.CanRedo);

        history.Execute(Rename("A", "C"));

        Assert.False(history.CanRedo);
        Assert.Equal("C", document.DisplayName);
    }

    [Fact]
    public void PushExecuted_RecordsWithoutReapplying()
    {
        // Drag gestures mutate the document live, then record one command on mouse-up.
        var document = new OverlayDesignerDocument { PosterMargin = new Thickness(50) };
        var history = new OverlayEditHistory(document);

        history.PushExecuted(new ElementBoundsCommand(
            OverlayElementKind.Poster, new Thickness(10), new Thickness(50), "Move poster"));

        Assert.Equal(new Thickness(50), document.PosterMargin);
        Assert.True(history.CanUndo);

        history.Undo();
        Assert.Equal(new Thickness(10), document.PosterMargin);
    }

    [Fact]
    public void MultipleEdits_UndoInReverseOrder()
    {
        var document = new OverlayDesignerDocument { DisplayName = "A" };
        var history = new OverlayEditHistory(document);
        history.Execute(Rename("A", "B"));
        history.Execute(Rename("B", "C"));

        history.Undo();
        Assert.Equal("B", document.DisplayName);

        history.Undo();
        Assert.Equal("A", document.DisplayName);
    }

    #endregion

    #region Dirty tracking

    [Fact]
    public void MarkClean_ClearsDirtyState()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());
        history.Execute(Rename("A", "B"));

        history.MarkClean();

        Assert.False(history.IsDirty);
    }

    [Fact]
    public void UndoBackToSavedState_ClearsDirtyFlag()
    {
        // Saving then editing then undoing lands back on the saved content, so the
        // document is genuinely not dirty — a plain bool flag would get this wrong.
        var history = new OverlayEditHistory(new OverlayDesignerDocument());
        history.Execute(Rename("A", "B"));
        history.MarkClean();

        history.Execute(Rename("B", "C"));
        Assert.True(history.IsDirty);

        history.Undo();
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void UndoingPastSavedState_MarksDirtyAgain()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());
        history.Execute(Rename("A", "B"));
        history.MarkClean();

        history.Undo();

        Assert.True(history.IsDirty);
    }

    [Fact]
    public void Reset_ClearsHistoryAndDirtyState()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());
        history.Execute(Rename("A", "B"));

        history.Reset();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.IsDirty);
    }

    #endregion

    #region Capacity

    [Fact]
    public void History_TrimsOldestCommandsBeyondCapacity()
    {
        var document = new OverlayDesignerDocument();
        var history = new OverlayEditHistory(document, capacity: 3);

        for (var i = 0; i < 5; i++)
        {
            history.Execute(Rename(i.ToString(), (i + 1).ToString()));
        }

        var undone = 0;
        while (history.Undo())
        {
            undone++;
        }

        Assert.Equal(3, undone);
    }

    [Fact]
    public void TrimmingPastTheBaseline_KeepsDocumentDirty()
    {
        // Once the saved state is trimmed out of history it can't be reached by undoing,
        // so the document must stay dirty rather than falsely reporting saved.
        var history = new OverlayEditHistory(new OverlayDesignerDocument(), capacity: 2);
        history.MarkClean();

        history.Execute(Rename("A", "B"));
        history.Execute(Rename("B", "C"));
        history.Execute(Rename("C", "D"));

        while (history.Undo())
        {
            // drain
        }

        Assert.True(history.IsDirty);
    }

    [Fact]
    public void Constructor_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OverlayEditHistory(new OverlayDesignerDocument(), capacity: 0));
    }

    [Fact]
    public void Constructor_RejectsNullDocument()
    {
        Assert.Throws<ArgumentNullException>(() => new OverlayEditHistory(null!));
    }

    #endregion

    #region Change notification

    [Fact]
    public void Changed_FiresForEveryHistoryOperation()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());
        var count = 0;
        history.Changed += (_, _) => count++;

        history.Execute(Rename("A", "B"));
        history.Undo();
        history.Redo();
        history.MarkClean();
        history.Reset();

        Assert.Equal(5, count);
    }

    [Fact]
    public void NextUndoDescription_NamesThePendingEdit()
    {
        var history = new OverlayEditHistory(new OverlayDesignerDocument());

        Assert.Null(history.NextUndoDescription);

        history.Execute(Rename("A", "B"));

        Assert.Equal("Rename", history.NextUndoDescription);
    }

    #endregion

    private static PropertyEditCommand<string> Rename(string from, string to) =>
        new("Rename", (d, v) => d.DisplayName = v, from, to);
}
