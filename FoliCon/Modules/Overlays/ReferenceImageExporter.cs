using System.Drawing;
using System.IO;
using FoliCon.Models.Data;

namespace FoliCon.Modules.Overlays;

/// <summary>
/// One-shot utility to export reference PNGs from the compiled XAML views.
/// These PNGs are used as golden images for DynamicPosterIcon parity tests.
///
/// USAGE: Call ReferenceImageExporter.ExportToTestProject() from App startup,
/// run the app once, then remove the call.
/// </summary>
[Localizable(false)]
public static class ReferenceImageExporter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly (string Id, Func<object, PosterIconBase> Factory)[] OldViews =
    [
        ("legacy", dc => new Views.PosterIcon(dc)),
        ("alternate", dc => new Views.PosterIconAlt(dc)),
        ("liaher", dc => new Views.PosterIconLiaher(dc)),
        ("faelpessoal", dc => new Views.PosterIconFaelpessoal(dc)),
        ("faelpessoal-horizontal", dc => new Views.PosterIconFaelpessoalHorizontal(dc)),
        ("windows11", dc => new Views.PosterIconWindows11(dc)),
    ];

    /// <summary>
    /// Renders all 6 old compiled XAML views and saves them as PNGs
    /// to FoliconTest/Resources/ReferenceOverlays/.
    /// Call this once from App startup, run the app, then remove the call.
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

        foreach (var (id, factory) in OldViews)
        {
            try
            {
                using var posterIcon = new PosterIcon();
                var view = factory(posterIcon);
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
            OldViews.Length, outputDir);
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
