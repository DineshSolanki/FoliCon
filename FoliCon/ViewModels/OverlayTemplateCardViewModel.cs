#nullable enable
using FoliCon.Modules.Overlays.Designer;

namespace FoliCon.ViewModels;

/// <summary>
/// One card in the first-run template picker.
///
/// Overlays are chosen by appearance, so each card renders the template through the real
/// pipeline rather than listing its name alone.
/// </summary>
public sealed class OverlayTemplateCardViewModel(OverlayTemplate template) : BindableBase
{
    public OverlayTemplate Template { get; } = template;

    public string DisplayName => Template.DisplayName;

    public string Description => Template.Description;

    /// <summary>Frozen thumbnail, or null until the render completes.</summary>
    public BitmapSource? PreviewImage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RaisePropertyChanged(nameof(IsPreviewLoading));
            }
        }
    }

    public bool IsPreviewLoading => PreviewImage == null;
}
