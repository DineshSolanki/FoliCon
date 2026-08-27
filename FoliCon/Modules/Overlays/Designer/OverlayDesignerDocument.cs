#nullable enable
using Size = System.Windows.Size;

namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// The designer's mutable edit state for one overlay package.
///
/// Holds typed values (<see cref="Thickness"/>, numbers, booleans) rather than the schema's
/// margin strings, so canvas gestures and numeric editors manipulate the same representation.
/// <see cref="CreateSnapshot"/> projects it back onto an immutable
/// <see cref="PosterOverlayDefinition"/> for rendering, validation, and export.
///
/// The document never mutates the definition it was loaded from.
/// </summary>
public sealed class OverlayDesignerDocument
{
    /// <summary>Absolute path of the folder holding this package's assets.</summary>
    public string AssetFolderPath { get; set; } = string.Empty;

    #region Metadata

    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OverlayVersion { get; set; } = "1.0.0";
    public List<string> Tags { get; } = [];

    #endregion

    #region Canvas compatibility

    public double DesignWidth { get; set; } = 265;
    public double DesignHeight { get; set; } = 256;
    public Thickness RootMargin { get; set; } = new(0, 0, 0, -11);
    public double RenderWidth { get; set; } = 256;
    public double RenderHeight { get; set; } = 256;

    /// <summary>
    /// The effective content box elements are positioned against, accounting for
    /// <see cref="RootMargin"/>. Canvas bounds conversion uses this, never the raw design size.
    /// </summary>
    public Size LayoutSurface => OverlayGeometry.GetLayoutSurface(
        DesignWidth, DesignHeight, OverlayGeometry.FormatThickness(RootMargin));

    #endregion

    #region Layers

    public bool HasBaseLayer { get; set; }
    public string BaseLayerImagePath { get; set; } = string.Empty;
    public Thickness BaseLayerMargin { get; set; }

    public bool HasFrontLayer { get; set; }
    public string FrontLayerImagePath { get; set; } = string.Empty;
    public Thickness FrontLayerMargin { get; set; }

    public Thickness PosterMargin { get; set; }
    public string PosterClipRadius { get; set; } = "0";
    public string? PosterClipRect { get; set; }
    public string? PosterInnerMargin { get; set; }
    public string? PosterOpacityMaskPath { get; set; }

    /// <summary>Explicit z-order. Elements absent from the document are skipped at snapshot time.</summary>
    public List<OverlayElementKind> LayerOrder { get; } = [.. OverlayElementKinds.DefaultOrder];

    #endregion

    #region Rating

    public Thickness RatingShieldMargin { get; set; } = new(160, 97, 6, 5);
    public Thickness RatingTextMargin { get; set; } = new(189, 30, 21, 24);
    public double RatingFontSize { get; set; } = 25;
    public string RatingFontFamily { get; set; } = "Castellar";
    public string? RatingFontSource { get; set; }
    public string RatingFontFallback { get; set; } = "Segoe UI";
    public double RatingTextWidth { get; set; } = 55;
    public double RatingTextHeight { get; set; } = 46;
    public string RatingTextHorizontalAlignment { get; set; } = "Center";
    public string RatingTextVerticalAlignment { get; set; } = "Center";
    public string? RatingTextAnchor { get; set; }

    #endregion

    #region Title

    public bool TitleIsVisible { get; set; }
    public Thickness TitleMargin { get; set; }
    public double TitleRotationAngle { get; set; }
    public string TitleRotationOrigin { get; set; } = "0.5,0.5";
    public string TitleFontFamily { get; set; } = "Cormorant";
    public string? TitleFontSource { get; set; }
    public string TitleFontFallback { get; set; } = "Segoe UI";
    public string TitleForeground { get; set; } = "White";
    public string TitleTrimming { get; set; } = "WordEllipsis";
    public string TitleWrapping { get; set; } = "Wrap";
    public string TitleContainer { get; set; } = "Root";
    public int TitleGridRow { get; set; }
    public string TitleHorizontalAlignment { get; set; } = "Left";
    public string TitleVerticalAlignment { get; set; } = "Top";

    #endregion

    #region Package dates

    /// <summary>Creation date carried over from an opened package; null for a new overlay.</summary>
    public DateTime? CreatedAt { get; set; }

    #endregion

    /// <summary>
    /// Builds an immutable definition from the current state. This is what gets rendered,
    /// validated, and serialized — the single projection point from typed state to schema.
    /// </summary>
    public PosterOverlayDefinition CreateSnapshot()
    {
        var definition = new PosterOverlayDefinition
        {
            SchemaVersion = SchemaVersion,
            Id = Id,
            DisplayName = DisplayName,
            Author = Author,
            Description = Description,
            OverlayVersion = OverlayVersion,
            Tags = [.. Tags],
            IsBuiltIn = false,
            OverlayFolderPath = string.IsNullOrEmpty(AssetFolderPath) ? null : AssetFolderPath,

            DesignWidth = DesignWidth,
            DesignHeight = DesignHeight,
            RootMargin = OverlayGeometry.FormatThickness(RootMargin),
            RenderWidth = RenderWidth,
            RenderHeight = RenderHeight,

            LayerOrder = [.. LayerOrder.Select(OverlayElementKinds.ToSchemaKey)],

            BaseLayer = HasBaseLayer
                ? new LayerDefinition
                {
                    ImagePath = BaseLayerImagePath,
                    Margin = OverlayGeometry.FormatThickness(BaseLayerMargin)
                }
                : null,

            FrontLayer = HasFrontLayer
                ? new LayerDefinition
                {
                    ImagePath = FrontLayerImagePath,
                    Margin = OverlayGeometry.FormatThickness(FrontLayerMargin)
                }
                : null,

            Poster = new PosterConfig
            {
                Margin = OverlayGeometry.FormatThickness(PosterMargin),
                ClipRadius = PosterClipRadius,
                ClipRect = PosterClipRect,
                PosterInnerMargin = PosterInnerMargin,
                OpacityMaskPath = PosterOpacityMaskPath
            },

            Rating = new RatingConfig
            {
                ShieldMargin = OverlayGeometry.FormatThickness(RatingShieldMargin),
                TextMargin = OverlayGeometry.FormatThickness(RatingTextMargin),
                FontSize = RatingFontSize,
                FontFamily = RatingFontFamily,
                FontSource = RatingFontSource,
                FontFallback = RatingFontFallback,
                TextWidth = RatingTextWidth,
                TextHeight = RatingTextHeight,
                TextHorizontalAlignment = RatingTextHorizontalAlignment,
                TextVerticalAlignment = RatingTextVerticalAlignment,
                TextAnchor = RatingTextAnchor
            },

            Title = new TitleConfig
            {
                IsVisible = TitleIsVisible,
                Margin = OverlayGeometry.FormatThickness(TitleMargin),
                RotationAngle = TitleRotationAngle,
                RotationOrigin = TitleRotationOrigin,
                FontFamily = TitleFontFamily,
                FontSource = TitleFontSource,
                FontFallback = TitleFontFallback,
                Foreground = TitleForeground,
                Trimming = TitleTrimming,
                Wrapping = TitleWrapping,
                Container = TitleContainer,
                GridRow = TitleGridRow,
                HorizontalAlignment = TitleHorizontalAlignment,
                VerticalAlignment = TitleVerticalAlignment
            }
        };

        return definition;
    }

    /// <summary>
    /// Loads a definition into a fresh document. The source definition is not retained
    /// or mutated; every value is copied.
    /// </summary>
    public static OverlayDesignerDocument FromDefinition(PosterOverlayDefinition definition, string assetFolderPath)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var document = new OverlayDesignerDocument
        {
            AssetFolderPath = assetFolderPath,

            SchemaVersion = definition.SchemaVersion,
            Id = definition.Id,
            DisplayName = definition.DisplayName,
            Author = definition.Author,
            Description = definition.Description,
            OverlayVersion = definition.OverlayVersion,

            DesignWidth = definition.DesignWidth,
            DesignHeight = definition.DesignHeight,
            RootMargin = OverlayGeometry.ParseThickness(definition.RootMargin),
            RenderWidth = definition.RenderWidth,
            RenderHeight = definition.RenderHeight,

            HasBaseLayer = definition.BaseLayer != null,
            BaseLayerImagePath = definition.BaseLayer?.ImagePath ?? string.Empty,
            BaseLayerMargin = OverlayGeometry.ParseThickness(definition.BaseLayer?.Margin),

            HasFrontLayer = definition.FrontLayer != null,
            FrontLayerImagePath = definition.FrontLayer?.ImagePath ?? string.Empty,
            FrontLayerMargin = OverlayGeometry.ParseThickness(definition.FrontLayer?.Margin),

            PosterMargin = OverlayGeometry.ParseThickness(definition.Poster.Margin),
            PosterClipRadius = definition.Poster.ClipRadius,
            PosterClipRect = definition.Poster.ClipRect,
            PosterInnerMargin = definition.Poster.PosterInnerMargin,
            PosterOpacityMaskPath = definition.Poster.OpacityMaskPath,

            RatingShieldMargin = OverlayGeometry.ParseThickness(definition.Rating.ShieldMargin),
            RatingTextMargin = OverlayGeometry.ParseThickness(definition.Rating.TextMargin),
            RatingFontSize = definition.Rating.FontSize,
            RatingFontFamily = definition.Rating.FontFamily,
            RatingFontSource = definition.Rating.FontSource,
            RatingFontFallback = definition.Rating.FontFallback,
            RatingTextWidth = definition.Rating.TextWidth,
            RatingTextHeight = definition.Rating.TextHeight,
            RatingTextHorizontalAlignment = definition.Rating.TextHorizontalAlignment,
            RatingTextVerticalAlignment = definition.Rating.TextVerticalAlignment,
            RatingTextAnchor = definition.Rating.TextAnchor,

            TitleIsVisible = definition.Title.IsVisible,
            TitleMargin = OverlayGeometry.ParseThickness(definition.Title.Margin),
            TitleRotationAngle = definition.Title.RotationAngle,
            TitleRotationOrigin = definition.Title.RotationOrigin,
            TitleFontFamily = definition.Title.FontFamily,
            TitleFontSource = definition.Title.FontSource,
            TitleFontFallback = definition.Title.FontFallback,
            TitleForeground = definition.Title.Foreground,
            TitleTrimming = definition.Title.Trimming,
            TitleWrapping = definition.Title.Wrapping,
            TitleContainer = definition.Title.Container,
            TitleGridRow = definition.Title.GridRow,
            TitleHorizontalAlignment = definition.Title.HorizontalAlignment,
            TitleVerticalAlignment = definition.Title.VerticalAlignment
        };

        document.Tags.AddRange(definition.Tags);

        document.LayerOrder.Clear();
        if (definition.LayerOrder is { Length: > 0 })
        {
            foreach (var key in definition.LayerOrder)
            {
                if (OverlayElementKinds.TryParse(key, out var kind) && !document.LayerOrder.Contains(kind))
                {
                    document.LayerOrder.Add(kind);
                }
            }
        }

        if (document.LayerOrder.Count == 0)
        {
            document.LayerOrder.AddRange(OverlayElementKinds.DefaultOrder);
        }

        return document;
    }

    /// <summary>
    /// Reads the bounds of a positionable element in design-surface coordinates.
    /// Rating and title are positioned by margin against the same surface as the layers.
    /// </summary>
    public Rect GetElementBounds(OverlayElementKind kind)
    {
        if (kind == OverlayElementKind.RatingText)
        {
            return GetRatingTextBounds();
        }
        return OverlayGeometry.MarginToBounds(GetElementMargin(kind), LayoutSurface);
    }

    /// <summary>
    /// Writes an element's position from canvas bounds, converting back to a margin.
    /// </summary>
    public void SetElementBounds(OverlayElementKind kind, Rect bounds)
    {
        if (kind == OverlayElementKind.RatingText)
        {
            SetRatingTextBounds(bounds);
            return;
        }
        SetElementMargin(kind, OverlayGeometry.BoundsToMargin(bounds, LayoutSurface));
    }

    /// <summary>
    /// Computes the rating text bounds on the design surface. When anchored, the text is
    /// centered on the shield with <see cref="RatingTextMargin"/> as an offset.
    /// </summary>
    private Rect GetRatingTextBounds()
    {
        var shieldBounds = OverlayGeometry.MarginToBounds(RatingShieldMargin, LayoutSurface);
        var centerX = shieldBounds.X + shieldBounds.Width / 2;
        var centerY = shieldBounds.Y + shieldBounds.Height / 2;

        // TextMargin acts as offset from shield center when anchored.
        var textCenterX = centerX + (RatingTextMargin.Left - RatingTextMargin.Right) / 2;
        var textCenterY = centerY + (RatingTextMargin.Top - RatingTextMargin.Bottom) / 2;

        return new Rect(
            textCenterX - RatingTextWidth / 2,
            textCenterY - RatingTextHeight / 2,
            RatingTextWidth,
            RatingTextHeight);
    }

    /// <summary>
    /// Converts canvas drag position back to <see cref="RatingTextMargin"/> offset.
    /// </summary>
    private void SetRatingTextBounds(Rect bounds)
    {
        var shieldBounds = OverlayGeometry.MarginToBounds(RatingShieldMargin, LayoutSurface);
        var shieldCenterX = shieldBounds.X + shieldBounds.Width / 2;
        var shieldCenterY = shieldBounds.Y + shieldBounds.Height / 2;

        var textCenterX = bounds.X + bounds.Width / 2;
        var textCenterY = bounds.Y + bounds.Height / 2;

        var offsetX = textCenterX - shieldCenterX;
        var offsetY = textCenterY - shieldCenterY;

        // Convert center offset to margin. The read formula is:
        //   textCenter = shieldCenter + (left - right) / 2
        // So to get offset O, set left=O, right=-O (and same for top/bottom).
        RatingTextMargin = new Thickness(offsetX, offsetY, -offsetX, -offsetY);
    }

    public Thickness GetElementMargin(OverlayElementKind kind) => kind switch
    {
        OverlayElementKind.Base => BaseLayerMargin,
        OverlayElementKind.Poster => PosterMargin,
        OverlayElementKind.Front => FrontLayerMargin,
        OverlayElementKind.Rating => RatingShieldMargin,
        OverlayElementKind.RatingText => RatingTextMargin,
        OverlayElementKind.Title => TitleMargin,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown overlay element kind.")
    };

    public void SetElementMargin(OverlayElementKind kind, Thickness margin)
    {
        switch (kind)
        {
            case OverlayElementKind.Base: BaseLayerMargin = margin; break;
            case OverlayElementKind.Poster: PosterMargin = margin; break;
            case OverlayElementKind.Front: FrontLayerMargin = margin; break;
            case OverlayElementKind.Rating: MoveRatingBadge(margin); break;
            case OverlayElementKind.RatingText: RatingTextMargin = margin; break;
            case OverlayElementKind.Title: TitleMargin = margin; break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown overlay element kind.");
        }
    }

    /// <summary>
    /// Whether a <see cref="RatingText"/> sub-element should appear in the element list.
    /// Only meaningful when the text is anchored to the shield.
    /// </summary>
    public bool HasRatingTextElement =>
        string.Equals(RatingTextAnchor, "Center", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Moves the rating badge as a unit.
    ///
    /// The schema positions the shield and its number with two independent margins, and the
    /// renderer keeps them that way for built-in parity. But they are one badge to the author:
    /// dragging it and leaving the number behind is never what anyone means. The number is
    /// therefore shifted by the same delta, preserving whatever offset it had within the badge.
    /// </summary>
    private void MoveRatingBadge(Thickness newShieldMargin)
    {
        var deltaX = newShieldMargin.Left - RatingShieldMargin.Left;
        var deltaY = newShieldMargin.Top - RatingShieldMargin.Top;

        RatingShieldMargin = newShieldMargin;

        if (deltaX == 0 && deltaY == 0)
        {
            return;
        }

        // When text is anchored to the shield, it moves with the shield automatically
        // via the nested grid — only adjust text margin for independent positioning.
        if (!string.Equals(RatingTextAnchor, "Center", StringComparison.OrdinalIgnoreCase))
        {
            // Right/bottom move opposite to left/top so the text keeps its size.
            RatingTextMargin = new Thickness(
                RatingTextMargin.Left + deltaX,
                RatingTextMargin.Top + deltaY,
                RatingTextMargin.Right - deltaX,
                RatingTextMargin.Bottom - deltaY);
        }
    }

    /// <summary>
    /// Whether an element currently participates in rendering. Absent layers and a hidden
    /// title are shown greyed in the element list rather than being removed from it.
    /// </summary>
    public bool IsElementPresent(OverlayElementKind kind) => kind switch
    {
        OverlayElementKind.Base => HasBaseLayer,
        OverlayElementKind.Front => HasFrontLayer,
        OverlayElementKind.Title => TitleIsVisible,
        OverlayElementKind.RatingText => HasRatingTextElement,
        OverlayElementKind.Poster or OverlayElementKind.Rating => true,
        _ => false
    };

    /// <summary>
    /// Asset file names referenced by the document, relative to <see cref="AssetFolderPath"/>.
    /// Built-in pack paths (leading '/') are excluded — they are not package files.
    /// Used by the exporter and draft store to copy only what is actually referenced.
    /// </summary>
    public IEnumerable<string> GetReferencedAssets()
    {
        if (HasBaseLayer && IsPackageAsset(BaseLayerImagePath))
        {
            yield return BaseLayerImagePath;
        }

        if (HasFrontLayer && IsPackageAsset(FrontLayerImagePath))
        {
            yield return FrontLayerImagePath;
        }

        if (IsPackageAsset(PosterOpacityMaskPath))
        {
            yield return PosterOpacityMaskPath!;
        }

        if (IsPackageAsset(RatingFontSource))
        {
            yield return RatingFontSource!;
        }

        if (IsPackageAsset(TitleFontSource))
        {
            yield return TitleFontSource!;
        }
    }

    private static bool IsPackageAsset(string? path) =>
        !string.IsNullOrWhiteSpace(path) && !path.StartsWith('/');
}
