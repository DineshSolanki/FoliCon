#nullable enable
using FoliCon.Modules.Overlays.Designer;

namespace FoliCon.ViewModels;

/// <summary>
/// One row in the designer's element list and one selectable region on the canvas.
/// </summary>
[Localizable(false)]
public sealed class OverlayElementViewModel(OverlayElementKind kind, string displayName) : BindableBase
{
    public OverlayElementKind Kind { get; } = kind;

    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Whether this element currently renders. Absent layers stay listed but greyed, so the
    /// author can see what the overlay could have rather than guessing what is missing.
    /// </summary>
    public bool IsPresent
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Bounds on the canvas in design-surface units, before zoom is applied.</summary>
    public Rect DesignBounds
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(BoundsDescription));
            }
        }
    }

    /// <summary>
    /// Spoken by screen readers when the element gains focus, so keyboard users get the
    /// same position feedback sighted users read off the canvas.
    /// </summary>
    public string BoundsDescription =>
        $"{DisplayName}: {DesignBounds.X:0}, {DesignBounds.Y:0}, {DesignBounds.Width:0} by {DesignBounds.Height:0}";
}
