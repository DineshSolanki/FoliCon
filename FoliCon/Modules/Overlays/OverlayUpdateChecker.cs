namespace FoliCon.Modules.Overlays;

/// <summary>
/// Checks for overlay updates on app start. Non-blocking — runs in the background
/// and marks updates available in the repository service for UI display.
/// </summary>
[Localizable(false)]
public class OverlayUpdateChecker(IOverlayRepositoryService repositoryService, IOverlayProvider overlayProvider)
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

            var installed = overlayProvider.GetUserOverlays();
            var updateCount = 0;

            foreach (var overlay in installed)
            {
                var catalogEntry = catalog.Overlays.FirstOrDefault(o =>
                    string.Equals(o.Id, overlay.Id, StringComparison.OrdinalIgnoreCase));

                if (catalogEntry == null)
                {
                    continue;
                }

                if (!OverlayConstants.TryCompareVersions(catalogEntry.OverlayVersion, overlay.OverlayVersion,
                        out var isNewer) || !isNewer)
                {
                    continue;
                }
                updateCount++;
                Logger.Info("Update available for '{Id}': {Installed} → {Available}",
                    overlay.Id, overlay.OverlayVersion, catalogEntry.OverlayVersion);
                repositoryService.MarkUpdateAvailable(overlay.Id, catalogEntry.OverlayVersion);
            }

            Logger.Info("Update check complete. {Count} updates available.", updateCount);
        }
        catch (Exception ex)
        {
            // Non-fatal — app continues normally without update info
            Logger.Warn(ex, "Overlay update check failed (non-fatal)");
        }
    }

}
