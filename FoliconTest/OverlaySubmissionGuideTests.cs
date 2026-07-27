#nullable enable
using System.Net.Http;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlaySubmissionGuide"/>: the pre-submission catalog check and
/// the URLs the guided steps rely on.
/// </summary>
public class OverlaySubmissionGuideTests
{
    #region Catalog clash check

    [Fact]
    public async Task Check_UnusedId_ReportsNewOverlay()
    {
        var guide = new OverlaySubmissionGuide(new StubRepositoryService());

        var result = await guide.CheckAsync("brand-new", "1.0.0");

        Assert.Equal(OverlaySubmissionConflict.None, result.Conflict);
        Assert.True(result.CanProceed);
        Assert.Null(result.ExistingVersion);
    }

    [Fact]
    public async Task Check_HigherVersion_IsAValidUpdate()
    {
        var guide = new OverlaySubmissionGuide(StubWith("existing", "1.0.0"));

        var result = await guide.CheckAsync("existing", "1.1.0");

        Assert.Equal(OverlaySubmissionConflict.UpdateToExisting, result.Conflict);
        Assert.True(result.CanProceed);
        Assert.Equal("1.0.0", result.ExistingVersion);
    }

    [Fact]
    public async Task Check_SameVersion_IsBlocked()
    {
        // Submitting without bumping the version would be rejected upstream; telling the
        // author now saves them building a pull request that cannot land.
        var guide = new OverlaySubmissionGuide(StubWith("existing", "1.0.0"));

        var result = await guide.CheckAsync("existing", "1.0.0");

        Assert.Equal(OverlaySubmissionConflict.VersionNotIncremented, result.Conflict);
        Assert.False(result.CanProceed);
    }

    [Fact]
    public async Task Check_LowerVersion_IsBlocked()
    {
        var guide = new OverlaySubmissionGuide(StubWith("existing", "2.0.0"));

        var result = await guide.CheckAsync("existing", "1.0.0");

        Assert.Equal(OverlaySubmissionConflict.VersionNotIncremented, result.Conflict);
        Assert.False(result.CanProceed);
    }

    [Fact]
    public async Task Check_IdComparisonIsCaseInsensitive()
    {
        var guide = new OverlaySubmissionGuide(StubWith("existing", "1.0.0"));

        var result = await guide.CheckAsync("EXISTING", "1.0.0");

        Assert.Equal(OverlaySubmissionConflict.VersionNotIncremented, result.Conflict);
    }

    [Fact]
    public async Task Check_WhenCatalogIsUnreachable_DoesNotBlockSubmission()
    {
        // Being offline should not stop someone preparing a contribution.
        var guide = new OverlaySubmissionGuide(new ThrowingRepositoryService());

        var result = await guide.CheckAsync("anything", "1.0.0");

        Assert.Equal(OverlaySubmissionConflict.CatalogUnavailable, result.Conflict);
        Assert.True(result.CanProceed);
    }

    [Fact]
    public async Task Check_WithoutAnId_ExplainsWhatIsMissing()
    {
        var guide = new OverlaySubmissionGuide(new StubRepositoryService());

        var result = await guide.CheckAsync("", "1.0.0");

        Assert.Contains("ID", result.Message);
    }

    [Fact]
    public async Task Check_MessageNamesTheOverlayAndVersion()
    {
        var guide = new OverlaySubmissionGuide(StubWith("cinema", "2.0.0"));

        var result = await guide.CheckAsync("cinema", "1.0.0");

        Assert.Contains("cinema", result.Message);
        Assert.Contains("2.0.0", result.Message);
    }

    #endregion

    #region URLs

    [Fact]
    public void SubmissionUrls_PointAtTheCommunityRepository()
    {
        Assert.StartsWith("https://github.com/DineshSolanki/FoliCon-Overlays", OverlaySubmissionGuide.RepositoryUrl);
        Assert.StartsWith(OverlaySubmissionGuide.RepositoryUrl, OverlaySubmissionGuide.ForkUrl);
        Assert.StartsWith(OverlaySubmissionGuide.RepositoryUrl, OverlaySubmissionGuide.PullRequestUrl);
        Assert.StartsWith(OverlaySubmissionGuide.RepositoryUrl, OverlaySubmissionGuide.ContributingGuideUrl);
    }

    [Fact]
    public void ForkUrl_IsTheForkEndpoint()
    {
        Assert.EndsWith("/fork", OverlaySubmissionGuide.ForkUrl);
    }

    [Fact]
    public void ContributingGuideUrl_PointsAtTheAuthoringGuide()
    {
        Assert.EndsWith("CREATING-OVERLAYS.md", OverlaySubmissionGuide.ContributingGuideUrl);
    }

    [Fact]
    public void TargetPath_MatchesTheRepositoryLayout()
    {
        // The catalog CI globs overlays/*/manifest.json; anywhere else is invisible to it.
        Assert.Equal("overlays/my-overlay/", OverlaySubmissionGuide.TargetPathInRepository("my-overlay"));
    }

    #endregion

    /// <summary>Reuses the shared stub from <see cref="OverlayStoreViewModelTests"/>.</summary>
#pragma warning disable CA1859 // StubRepositoryService is internal to another test class
    private static IOverlayRepositoryService StubWith(string id, string version) =>
#pragma warning restore CA1859
        new StubRepositoryService(
        [
            new OverlayCatalogEntry { Id = id, DisplayName = id, OverlayVersion = version }
        ]);

    /// <summary>Simulates the store being unreachable.</summary>
    private sealed class ThrowingRepositoryService : StubRepositoryService
    {
        public override Task<OverlayCatalog> FetchCatalogAsync(CancellationToken ct) =>
            throw new HttpRequestException("offline"); // ct is required by the interface
    }
}
