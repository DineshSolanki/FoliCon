namespace FoliCon.Models.Enums;

/// <summary>
/// Installation-status filter for the overlay store's Discover section.
///
/// Deliberately an enum rather than the display string. Filtering used to compare the
/// selection against the English dropdown label, so translating the dropdown would have
/// silently matched nothing for every non-English user. Display text now lives only in
/// <see cref="Models.Data.OverlayStatusFilterOption"/>.
/// </summary>
public enum OverlayStatusFilter
{
    All,
    Installed,
    NotInstalled,
    UpdateAvailable
}
