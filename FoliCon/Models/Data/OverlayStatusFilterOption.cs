namespace FoliCon.Models.Data;

/// <summary>
/// One entry in the store's installation-status dropdown: the value the filter runs on,
/// paired with the text shown to the user.
///
/// Keeping the two apart is the point — <see cref="Value"/> drives filtering and never
/// changes, while <see cref="DisplayText"/> is free to be translated.
/// </summary>
public sealed record OverlayStatusFilterOption(OverlayStatusFilter Value, string DisplayText);
