#nullable enable
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Central conversion between WPF <see cref="Thickness"/> margin strings (the schema's
/// coordinate language) and the typed <see cref="Rect"/> bounds the designer canvas manipulates.
///
/// All margin/bounds translation in the designer must route through here. Built-in overlays use a
/// 265x256 design surface with a negative root margin, so a second implementation would drift.
/// </summary>
public static class OverlayGeometry
{
    /// <summary>
    /// Parses a WPF Thickness string ("l,t,r,b", "h,v", or "all"). Invalid segments become 0,
    /// matching <see cref="Views.DynamicPosterIcon"/>'s parsing so the canvas shows what renders.
    /// </summary>
    public static Thickness ParseThickness(string? margin)
    {
        if (string.IsNullOrWhiteSpace(margin))
        {
            return new Thickness(0);
        }

        var parts = margin.Split(',');
        return parts.Length switch
        {
            1 => new Thickness(ParseDouble(parts[0])),
            2 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[0]), ParseDouble(parts[1])),
            3 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[2]), ParseDouble(parts[1])),
            4 => new Thickness(ParseDouble(parts[0]), ParseDouble(parts[1]),
                               ParseDouble(parts[2]), ParseDouble(parts[3])),
            _ => new Thickness(0)
        };
    }

    /// <summary>
    /// Formats a Thickness back to the canonical 4-value schema string. Always emits four
    /// invariant-culture values so exports are byte-stable regardless of the author's locale.
    /// </summary>
    public static string FormatThickness(Thickness thickness) =>
        string.Join(",",
            FormatDouble(thickness.Left),
            FormatDouble(thickness.Top),
            FormatDouble(thickness.Right),
            FormatDouble(thickness.Bottom));

    /// <summary>
    /// The effective design surface an element is laid out against. This is the root Grid's
    /// content box: the design size grown by any negative root margin, which is how the
    /// built-in overlays reach beyond their declared 265x256.
    /// </summary>
    public static Size GetLayoutSurface(double designWidth, double designHeight, string? rootMargin)
    {
        var root = ParseThickness(rootMargin);

        // A negative root margin expands the content box; a positive one shrinks it.
        var width = designWidth - root.Left - root.Right;
        var height = designHeight - root.Top - root.Bottom;

        return new Size(Math.Max(0, width), Math.Max(0, height));
    }

    /// <summary>
    /// Converts a margin into the bounds it produces inside the given layout surface.
    /// A stretched element (the default for overlay layers) is inset on all four sides.
    /// </summary>
    public static Rect MarginToBounds(Thickness margin, Size surface)
    {
        var x = margin.Left;
        var y = margin.Top;
        var width = surface.Width - margin.Left - margin.Right;
        var height = surface.Height - margin.Top - margin.Bottom;

        return new Rect(x, y, Math.Max(0, width), Math.Max(0, height));
    }

    /// <summary>
    /// Converts canvas bounds back into the margin that reproduces them. Inverse of
    /// <see cref="MarginToBounds"/> for the same surface.
    /// </summary>
    public static Thickness BoundsToMargin(Rect bounds, Size surface)
    {
        var left = bounds.X;
        var top = bounds.Y;
        var right = surface.Width - bounds.X - bounds.Width;
        var bottom = surface.Height - bounds.Y - bounds.Height;

        return new Thickness(left, top, right, bottom);
    }

    /// <summary>
    /// Snaps bounds to whole pixels. Canvas gestures call this so authored margins never
    /// acquire sub-pixel noise that would churn the exported JSON.
    /// </summary>
    public static Rect SnapToPixels(Rect bounds) =>
        new(Math.Round(bounds.X),
            Math.Round(bounds.Y),
            Math.Round(bounds.Width),
            Math.Round(bounds.Height));

    /// <summary>
    /// Offsets bounds by a whole-pixel delta, used by arrow-key nudge.
    /// </summary>
    public static Rect Nudge(Rect bounds, double deltaX, double deltaY) =>
        new(bounds.X + deltaX, bounds.Y + deltaY, bounds.Width, bounds.Height);

    /// <summary>
    /// Maps a point in zoomed canvas space back to design-surface coordinates.
    /// Zoom is a display concern only; exported values never depend on it.
    /// </summary>
    public static Point CanvasToDesign(Point canvasPoint, double zoom) =>
        zoom <= 0 ? canvasPoint : new Point(canvasPoint.X / zoom, canvasPoint.Y / zoom);

    /// <summary>
    /// Maps design-surface bounds into zoomed canvas space for hit-testing and handle placement.
    /// </summary>
    public static Rect DesignToCanvas(Rect designBounds, double zoom) =>
        zoom <= 0
            ? designBounds
            : new Rect(designBounds.X * zoom, designBounds.Y * zoom,
                       designBounds.Width * zoom, designBounds.Height * zoom);

    private static double ParseDouble(string value) =>
        double.TryParse(value.Trim(), CultureInfo.InvariantCulture, out var result) ? result : 0;

    private static string FormatDouble(double value) =>
        // "R" would emit 1E-05 style output; the schema expects plain decimals.
        Math.Round(value, 4).ToString("0.####", CultureInfo.InvariantCulture);
}
