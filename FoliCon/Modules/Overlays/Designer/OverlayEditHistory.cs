#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Undo/redo stack over an <see cref="OverlayDesignerDocument"/>.
///
/// History is session-only: saving a draft establishes a new clean baseline but does not
/// persist the stack. Dirty state is tracked against the last baseline marker rather than
/// a simple flag, so undoing back to the saved state correctly reports "not dirty".
/// </summary>
public sealed class OverlayEditHistory(OverlayDesignerDocument document, int capacity = 100)
{
    private readonly OverlayDesignerDocument _document = document
        ?? throw new ArgumentNullException(nameof(document));

    private readonly List<IOverlayEditCommand> _undoStack = [];
    private readonly List<IOverlayEditCommand> _redoStack = [];

    /// <summary>
    /// Depth of <see cref="_undoStack"/> at the last save. Dirty is "current depth != this",
    /// which makes undo-to-saved-state clear the dirty flag.
    /// </summary>
    private int _baselineDepth;

    /// <summary>
    /// Set when history is trimmed past the baseline: the saved state is no longer reachable
    /// by undoing, so the document must be treated as dirty from then on.
    /// </summary>
    private bool _baselineLost;

    public int Capacity { get; } = capacity > 0
        ? capacity
        : throw new ArgumentOutOfRangeException(nameof(capacity), "History capacity must be positive.");

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public bool IsDirty => _baselineLost || _undoStack.Count != _baselineDepth;

    public string? NextUndoDescription => CanUndo ? _undoStack[^1].Description : null;

    public string? NextRedoDescription => CanRedo ? _redoStack[^1].Description : null;

    /// <summary>Raised after any operation that can change <see cref="CanUndo"/>, <see cref="CanRedo"/>, or <see cref="IsDirty"/>.</summary>
    // ReSharper disable once S3264 — Invoked via OnChanged() helper method
#pragma warning disable S3264 // Invoked via OnChanged() helper method
    public event EventHandler? Changed;
#pragma warning restore S3264

    /// <summary>
    /// Applies a command and pushes it onto the undo stack, discarding any redo branch.
    /// </summary>
    public void Execute(IOverlayEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        command.Execute(_document);
        _undoStack.Add(command);
        _redoStack.Clear();

        TrimToCapacity();
        OnChanged();
    }

    /// <summary>
    /// Pushes an already-applied command without re-executing it. Used for gestures where the
    /// document was updated live during the drag and the command only records before/after.
    /// </summary>
    public void PushExecuted(IOverlayEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        _undoStack.Add(command);
        _redoStack.Clear();

        TrimToCapacity();
        OnChanged();
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        var command = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        command.Undo(_document);
        _redoStack.Add(command);

        OnChanged();
        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        var command = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        command.Execute(_document);
        _undoStack.Add(command);

        OnChanged();
        return true;
    }

    /// <summary>
    /// Marks the current state as saved. Called after a draft save or a successful export.
    /// </summary>
    public void MarkClean()
    {
        _baselineDepth = _undoStack.Count;
        _baselineLost = false;
        OnChanged();
    }

    /// <summary>
    /// Drops all history and treats the current state as clean. Called when loading a
    /// different package into the same designer session.
    /// </summary>
    public void Reset()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _baselineDepth = 0;
        _baselineLost = false;
        OnChanged();
    }

    private void TrimToCapacity()
    {
        while (_undoStack.Count > Capacity)
        {
            _undoStack.RemoveAt(0);

            // The dropped command sat below the baseline, so the saved state can no longer
            // be reached by undoing. Everything from here on counts as dirty.
            if (_baselineDepth > 0)
            {
                _baselineDepth--;
            }
            else
            {
                _baselineLost = true;
            }
        }
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
