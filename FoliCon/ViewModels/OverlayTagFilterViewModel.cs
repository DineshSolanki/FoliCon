#nullable enable
namespace FoliCon.ViewModels;

/// <summary>
/// One selectable tag chip in the store's filter bar.
///
/// Selection state lives here rather than in a single "selected tag" string so several tags
/// can be active at once — a dropdown can only ever express one.
/// </summary>
[Localizable(false)]
public sealed class OverlayTagFilterViewModel(string tag, int count) : BindableBase
{
    public string Tag { get; } = tag;

    /// <summary>How many catalog overlays carry this tag. Shown so empty filters are obvious up front.</summary>
    public int Count { get; } = count;

    public string DisplayText => $"{Tag} ({Count})";

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }
}
