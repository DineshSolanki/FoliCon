#nullable enable
using FoliCon.Modules.Overlays.Designer;

namespace FoliCon.ViewModels;

/// <summary>
/// One row in the designer's element list and one selectable region on the canvas.
/// </summary>
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
    ///
    /// The whole sentence is one resource because the " by " between width and height is
    /// English prose, not punctuation, and other languages put it elsewhere or drop it.
    /// </summary>
    public string BoundsDescription =>
        string.Format(
            Lang.OverlayDesignerBoundsDescription,
            DisplayName,
            DesignBounds.X.ToString("0", CultureInfo.CurrentCulture),
            DesignBounds.Y.ToString("0", CultureInfo.CurrentCulture),
            DesignBounds.Width.ToString("0", CultureInfo.CurrentCulture),
            DesignBounds.Height.ToString("0", CultureInfo.CurrentCulture));
}
