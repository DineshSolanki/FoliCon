#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// The selectable elements on the designer canvas. Values match the schema's
/// <c>layerOrder</c> keys so selection and z-order speak the same language.
/// </summary>
public enum OverlayElementKind
{
    Base,
    Poster,
    Front,
    Rating,
    RatingText,
    Title
}

/// <summary>
/// Helpers for mapping <see cref="OverlayElementKind"/> to and from the lowercase
/// schema strings used in <c>layerOrder</c>.
/// </summary>
public static class OverlayElementKinds
{
    /// <summary>Default z-order when a definition omits <c>layerOrder</c>.</summary>
    public static readonly OverlayElementKind[] DefaultOrder =
    [
        OverlayElementKind.Base,
        OverlayElementKind.Poster,
        OverlayElementKind.Front,
        OverlayElementKind.Rating,
        OverlayElementKind.Title
    ];

    public static string ToSchemaKey(OverlayElementKind kind) => kind switch
    {
        OverlayElementKind.Base => "base",
        OverlayElementKind.Poster => "poster",
        OverlayElementKind.Front => "front",
        OverlayElementKind.Rating => "rating",
        OverlayElementKind.Title => "title",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown overlay element kind.")
    };

    public static bool TryParse(string? key, out OverlayElementKind kind)
    {
        switch (key?.Trim().ToLowerInvariant())
        {
            case "base": kind = OverlayElementKind.Base; return true;
            case "poster": kind = OverlayElementKind.Poster; return true;
            case "front": kind = OverlayElementKind.Front; return true;
            case "rating": kind = OverlayElementKind.Rating; return true;
            case "title": kind = OverlayElementKind.Title; return true;
            default: kind = OverlayElementKind.Poster; return false;
        }
    }
}
