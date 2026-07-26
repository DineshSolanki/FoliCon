#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// A single reversible edit to an <see cref="OverlayDesignerDocument"/>.
///
/// A drag or resize gesture produces one command for the whole gesture (captured on mouse-up
/// with the before/after state), not one per mouse-move, so undo steps match user intent.
/// </summary>
public interface IOverlayEditCommand
{
    /// <summary>Human-readable description, shown in undo tooltips ("Move poster").</summary>
    string Description { get; }

    void Execute(OverlayDesignerDocument document);

    void Undo(OverlayDesignerDocument document);
}

/// <summary>
/// Generic property edit: captures the before and after value of one document property
/// and applies it through a setter delegate.
/// </summary>
[Localizable(false)]
public sealed class PropertyEditCommand<T>(
    string description,
    Action<OverlayDesignerDocument, T> setter,
    T oldValue,
    T newValue) : IOverlayEditCommand
{
    public string Description { get; } = description;

    public T OldValue { get; } = oldValue;

    public T NewValue { get; } = newValue;

    public void Execute(OverlayDesignerDocument document) => setter(document, NewValue);

    public void Undo(OverlayDesignerDocument document) => setter(document, OldValue);
}

/// <summary>
/// Moves or resizes one canvas element. Produced once per completed gesture.
/// </summary>
[Localizable(false)]
public sealed class ElementBoundsCommand(
    OverlayElementKind element,
    Thickness oldMargin,
    Thickness newMargin,
    string description) : IOverlayEditCommand
{
    public OverlayElementKind Element { get; } = element;

    public Thickness OldMargin { get; } = oldMargin;

    public Thickness NewMargin { get; } = newMargin;

    public string Description { get; } = description;

    public void Execute(OverlayDesignerDocument document) => document.SetElementMargin(Element, NewMargin);

    public void Undo(OverlayDesignerDocument document) => document.SetElementMargin(Element, OldMargin);
}

/// <summary>
/// Groups several commands into one undo step. Used when a single user action changes
/// more than one property — for example enabling a layer, which sets both the presence
/// flag and the image path.
/// </summary>
[Localizable(false)]
public sealed class CompositeEditCommand(string description, IReadOnlyList<IOverlayEditCommand> commands)
    : IOverlayEditCommand
{
    public string Description { get; } = description;

    public IReadOnlyList<IOverlayEditCommand> Commands { get; } = commands;

    public void Execute(OverlayDesignerDocument document)
    {
        foreach (var command in Commands)
        {
            command.Execute(document);
        }
    }

    public void Undo(OverlayDesignerDocument document)
    {
        // Reverse order so overlapping edits unwind correctly.
        for (var i = Commands.Count - 1; i >= 0; i--)
        {
            Commands[i].Undo(document);
        }
    }
}
