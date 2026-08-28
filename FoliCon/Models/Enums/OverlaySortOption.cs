namespace FoliCon.Models.Enums;

/// <summary>
/// Sort order for the overlay store grid.
///
/// Deliberately an enum rather than the display string, for the same reason as
/// <see cref="OverlayStatusFilter"/>: sorting must not depend on the (translatable)
/// dropdown label.
/// </summary>
public enum OverlaySortOption
{
    Newest,
    NameAscending,
    Author
}
