#nullable enable
namespace FoliCon.Models.Data;

/// <summary>
/// Defines a base or front image layer in an overlay.
/// </summary>
[Localizable(false)]
public class LayerDefinition
{
    /// <summary>
    /// Relative path to the image file within the overlay folder.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// WPF Thickness string: "left,top,right,bottom". Supports negative values.
    /// </summary>
    public string Margin { get; set; } = "0,0,0,0";
}

/// <summary>
/// Configuration for the poster image layer within the overlay.
/// </summary>
[Localizable(false)]
public class PosterConfig
{
    /// <summary>
    /// WPF Thickness string: "left,top,right,bottom".
    /// Applied to the Border when clip/mask is used, or directly to the Image otherwise.
    /// </summary>
    public string Margin { get; set; } = "0,0,0,0";

    /// <summary>
    /// CornerRadius string: single value or "tl,tr,br,bl".
    /// "0" means no clipping.
    /// </summary>
    public string ClipRadius { get; set; } = "0";

    /// <summary>
    /// Explicit clip rectangle as "x,y,width,height".
    /// When set, overrides the calculated clip from Margin.
    /// Matches the original XAML RectangleGeometry Rect values exactly.
    /// </summary>
    public string? ClipRect { get; set; }

    /// <summary>
    /// Optional WPF Thickness for the poster Image inside the Border (when clip/mask is active).
    /// Some overlays need a small margin on the inner Image (e.g. "0,0,0,-1").
    /// When null, the inner Image has no margin and fills the Border content area.
    /// </summary>
    public string? PosterInnerMargin { get; set; }

    /// <summary>
    /// Optional relative path to an opacity mask image within the overlay folder.
    /// </summary>
    public string? OpacityMaskPath { get; set; }
}

/// <summary>
/// Configuration for the rating badge (shield + text).
/// </summary>
[Localizable(false)]
public class RatingConfig
{
    /// <summary>
    /// WPF Thickness string for the shield image margin.
    /// </summary>
    public string ShieldMargin { get; set; } = "160,97,6,5";

    /// <summary>
    /// WPF Thickness string for the rating text margin.
    /// </summary>
    public string TextMargin { get; set; } = "189,30,21,24";

    public double FontSize { get; set; } = 25;
    public string FontFamily { get; set; } = "Castellar";

    /// <summary>
    /// Optional path to a bundled .ttf/.otf font file within the overlay folder.
    /// </summary>
    public string? FontSource { get; set; }

    /// <summary>
    /// System font to use if the primary font is not available.
    /// </summary>
    public string FontFallback { get; set; } = "Segoe UI";

    /// <summary>
    /// Maximum width of the rating text area. Text scales down via Viewbox if wider.
    /// </summary>
    public double TextWidth { get; set; } = 55;

    /// <summary>
    /// Maximum height of the rating text area. Text scales down via Viewbox if taller.
    /// </summary>
    public double TextHeight { get; set; } = 46;

    /// <summary>
    /// Horizontal alignment of the rating text.
    /// </summary>
    public string TextHorizontalAlignment { get; set; } = "Center";

    /// <summary>
    /// Vertical alignment of the rating text.
    /// </summary>
    public string TextVerticalAlignment { get; set; } = "Center";

    /// <summary>
    /// When set to "Center", the rating text is co-located with the shield image
    /// and automatically centered on it. <see cref="TextMargin"/> becomes an offset
    /// from the shield center rather than an absolute grid margin.
    /// When null, the text is positioned independently using <see cref="TextMargin"/>.
    /// </summary>
    public string? TextAnchor { get; set; }
}

/// <summary>
/// Configuration for the title text (optional).
/// </summary>
[Localizable(false)]
public class TitleConfig
{
    public bool IsVisible { get; set; }

    /// <summary>
    /// WPF Thickness string for the title text margin.
    /// </summary>
    public string Margin { get; set; } = "0,0,0,0";

    /// <summary>
    /// Rotation angle in degrees (0-360).
    /// </summary>
    public double RotationAngle { get; set; }

    /// <summary>
    /// Rotation origin as normalized point "x,y" (0.0–1.0).
    /// </summary>
    public string RotationOrigin { get; set; } = "0.5,0.5";

    public string FontFamily { get; set; } = "Cormorant";

    /// <summary>
    /// Optional path to a bundled .ttf/.otf font file within the overlay folder.
    /// </summary>
    public string? FontSource { get; set; }

    /// <summary>
    /// System font to use if the primary font is not available.
    /// </summary>
    public string FontFallback { get; set; } = "Segoe UI";

    /// <summary>
    /// Text foreground color (WPF color name or hex).
    /// </summary>
    public string Foreground { get; set; } = "White";

    /// <summary>
    /// Text trimming mode: None, WordEllipsis, CharacterEllipsis.
    /// </summary>
    public string Trimming { get; set; } = "WordEllipsis";

    /// <summary>
    /// Text wrapping mode: NoWrap, Wrap, WrapWithOverflow.
    /// </summary>
    public string Wrapping { get; set; } = "Wrap";

    /// <summary>
    /// Visual container for the title: Root or RatingGrid.
    /// Root preserves the default behavior for community overlays.
    /// </summary>
    public string Container { get; set; } = "Root";

    /// <summary>
    /// Grid row used when Container is RatingGrid.
    /// </summary>
    public int GridRow { get; set; }

    /// <summary>
    /// Horizontal alignment of the title text.
    /// </summary>
    public string HorizontalAlignment { get; set; } = "Left";

    /// <summary>
    /// Vertical alignment of the title text.
    /// </summary>
    public string VerticalAlignment { get; set; } = "Top";
}
