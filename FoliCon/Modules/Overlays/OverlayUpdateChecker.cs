namespace FoliCon.Modules.Overlays;

/// <summary>
/// Checks for overlay updates on app start. Non-blocking — runs in the background
/// and marks updates available in the repository service for UI display.
///
/// The compare-and-mark logic itself lives in
/// <see cref="IOverlayRepositoryService.SyncAvailableUpdates"/> (also invoked after every
/// fresh catalog fetch); this class only owns the startup trigger and its failure isolation.
/// </summary>
public class OverlayUpdateChecker(IOverlayRepositoryService repositoryService)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Checks for updates for all installed (non-built-in) overlays.
    /// Non-blocking; swallows exceptions to avoid disrupting app startup.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        try
        {
            Logger.Info("Checking for overlay updates...");

            var catalog = await repositoryService.FetchCatalogAsync();
            if (catalog.Overlays.Count == 0)
            {
                Logger.Debug("Catalog is empty, skipping update check");
                return;
            }

            // FetchCatalogAsync already ran SyncAvailableUpdates; just report.
            var updateCount = catalog.Overlays.Count(o => repositoryService.IsUpdateAvailable(o.Id));
            Logger.Info("Update check complete. {Count} updates available.", updateCount);
        }
        catch (Exception ex)
        {
            // Non-fatal — app continues normally without update info
            Logger.Warn(ex, "Overlay update check failed (non-fatal)");
        }
    }
}
