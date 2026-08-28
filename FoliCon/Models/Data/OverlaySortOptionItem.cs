namespace FoliCon.Models.Data;

/// <summary>
/// One entry in the store's sort-order dropdown: the value the sort runs on, paired with
/// the text shown to the user. See <see cref="OverlayStatusFilterOption"/> for why the two
/// are kept apart.
/// </summary>
public sealed record OverlaySortOptionItem(OverlaySortOption Value, string DisplayText);
