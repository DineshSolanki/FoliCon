#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// How an exported overlay's ID compares to what is already in the store catalog.
/// </summary>
public enum OverlaySubmissionConflict
{
    /// <summary>ID is unused; this would be a new overlay.</summary>
    None,

    /// <summary>ID exists and this version is higher — a valid update.</summary>
    UpdateToExisting,

    /// <summary>ID exists at the same or a higher version; submitting would be rejected.</summary>
    VersionNotIncremented,

    /// <summary>The catalog could not be reached, so no check was possible.</summary>
    CatalogUnavailable
}

/// <summary>
/// Result of checking an overlay against the live catalog before submission.
/// </summary>
[Localizable(false)]
public sealed class OverlaySubmissionCheck(
    OverlaySubmissionConflict conflict,
    string? existingVersion,
    string message)
{
    public OverlaySubmissionConflict Conflict { get; } = conflict;

    /// <summary>Version already published under this ID, when there is one.</summary>
    public string? ExistingVersion { get; } = existingVersion;

    /// <summary>Plain-language explanation for the author.</summary>
    public string Message { get; } = message;

    /// <summary>False only when submitting would certainly be rejected.</summary>
    public bool CanProceed => Conflict != OverlaySubmissionConflict.VersionNotIncremented;
}

/// <summary>
/// Supports the guided manual submission flow.
///
/// Release 3 does not automate pull requests — that needs a registered GitHub OAuth app and
/// its own security review, so it is deferred to Release 3.1. What this does instead is remove
/// the guesswork: it checks the overlay against the live catalog before the author invests
/// effort in a pull request, and builds the exact URLs the submission steps need.
/// </summary>
[Localizable(false)]
public class OverlaySubmissionGuide(IOverlayRepositoryService repositoryService)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IOverlayRepositoryService _repositoryService = repositoryService
        ?? throw new ArgumentNullException(nameof(repositoryService));

    [SuppressMessage("Sonar", "S1075:URIs should not be hardcoded",
        Justification = "Canonical community overlay repository.")]
    private const string repositoryUrl = "https://github.com/DineshSolanki/FoliCon-Overlays";

    public static string RepositoryUrl => repositoryUrl;

    public static string ForkUrl => $"{repositoryUrl}/fork";

    /// <summary>Compare view GitHub opens when starting a pull request.</summary>
    public static string PullRequestUrl => $"{repositoryUrl}/compare";

    public static string ContributingGuideUrl => $"{repositoryUrl}/blob/main/CREATING-OVERLAYS.md";

    /// <summary>Where the author's folder must be copied inside their fork.</summary>
    public static string TargetPathInRepository(string overlayId) => $"overlays/{overlayId}/";

    /// <summary>
    /// Checks the overlay ID and version against the published catalog.
    ///
    /// A network failure is reported as <see cref="OverlaySubmissionConflict.CatalogUnavailable"/>
    /// and does not block submission — being offline should not stop someone preparing a
    /// contribution.
    /// </summary>
    public virtual async Task<OverlaySubmissionCheck> CheckAsync(string overlayId, string overlayVersion)
    {
        if (string.IsNullOrWhiteSpace(overlayId))
        {
            return new OverlaySubmissionCheck(
                OverlaySubmissionConflict.None, null, "The overlay needs an ID before it can be submitted.");
        }

        try
        {
            var catalog = await _repositoryService.FetchCatalogAsync();
            var existing = catalog.Overlays.FirstOrDefault(o =>
                string.Equals(o.Id, overlayId, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                return new OverlaySubmissionCheck(
                    OverlaySubmissionConflict.None, null,
                    $"'{overlayId}' is not in the store yet — this would be a new overlay.");
            }

            if (OverlayConstants.TryCompareVersions(overlayVersion, existing.OverlayVersion, out var isNewer) && isNewer)
            {
                return new OverlaySubmissionCheck(
                    OverlaySubmissionConflict.UpdateToExisting, existing.OverlayVersion,
                    $"'{overlayId}' is already in the store at v{existing.OverlayVersion}. " +
                    $"Submitting v{overlayVersion} would update it.");
            }

            return new OverlaySubmissionCheck(
                OverlaySubmissionConflict.VersionNotIncremented, existing.OverlayVersion,
                $"'{overlayId}' is already in the store at v{existing.OverlayVersion}. " +
                $"Raise the version above that before submitting.");
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not check the catalog for overlay '{Id}'", overlayId);
            return new OverlaySubmissionCheck(
                OverlaySubmissionConflict.CatalogUnavailable, null,
                "Could not reach the store to check for a name clash. You can still submit.");
        }
    }
}
