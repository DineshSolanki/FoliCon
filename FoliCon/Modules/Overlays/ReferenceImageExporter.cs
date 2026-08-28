namespace FoliCon.Modules.Overlays;

/// <summary>
/// One-shot utility to export reference PNGs from the current built-in overlay definitions.
/// These PNGs are used as golden images for DynamicPosterIcon parity tests.
///
/// USAGE: Invoke ReferenceImageExporter.ExportToTestProject() manually during development,
/// run the app once, then do not leave an application-startup call in place.
/// </summary>
[Localizable(false)]
public static class ReferenceImageExporter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string[] BuiltInOverlayIds =
        ["legacy", "alternate", "liaher", "faelpessoal", "faelpessoal-horizontal", "windows11"];

    /// <summary>
    /// Renders all built-in overlay definitions through DynamicPosterIcon and saves them as PNGs
    /// to FoliconTest/Resources/ReferenceOverlays/.
    /// Invoke this manually during development when built-in overlay golden baselines need
    /// refreshing. It must not be called from normal application startup.
    /// </summary>
    public static void ExportToTestProject()
    {
        // Resolve output path relative to the solution root
        var baseDir = AppContext.BaseDirectory;
        var solutionRoot = FindSolutionRoot(baseDir);
        if (solutionRoot == null)
        {
            Logger.Warn("Could not find solution root from {BaseDir}", baseDir);
            return;
        }

        var outputDir = Path.Combine(solutionRoot, "FoliconTest", "Resources", "ReferenceOverlays");
        Directory.CreateDirectory(outputDir);

        Logger.Info("Exporting reference overlay images to {OutputDir}", outputDir);

        var provider = new OverlayProvider();
        foreach (var id in BuiltInOverlayIds)
        {
            try
            {
                var definition = provider.GetOverlayById(id);
                if (definition == null)
                {
                    Logger.Warn("Built-in overlay definition not found for {Id}", id);
                    continue;
                }

                using var posterIcon = new PosterIcon();
                var view = new DynamicPosterIcon(definition, posterIcon);
                using var bitmap = view.RenderToBitmap();

                var filePath = Path.Combine(outputDir, $"{id}_reference.png");
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                Logger.Info("Exported {Id} -> {Path} ({Width}x{Height})", id, filePath, bitmap.Width, bitmap.Height);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to export reference image for {Id}", id);
            }
        }

        Logger.Info("Reference image export complete. {Count} images saved to {OutputDir}",
            BuiltInOverlayIds.Length, outputDir);
    }

    private static string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
