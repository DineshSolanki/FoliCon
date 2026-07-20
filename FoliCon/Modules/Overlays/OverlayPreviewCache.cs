using System.Collections.Concurrent;

#nullable enable
namespace FoliCon.Modules.Overlays;

/// <summary>
/// STA-safe rendered preview caching for the Previewer dialog.
/// All WPF control creation and rendering goes through StaRenderer.Default.EnqueueRender().
/// Cached BitmapSource instances are frozen before storage for cross-thread UI access.
/// </summary>
[Localizable(false)]
public static class OverlayPreviewCache
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly ConcurrentDictionary<string, BitmapSource> Cache = new();

    /// <summary>
    /// Gets all overlay previews, using cached versions when available.
    /// Renders missing previews via StaRenderer on the dedicated STA thread.
    /// </summary>
    /// <param name="provider">Overlay provider to enumerate overlays.</param>
    /// <param name="posterPath">Path to poster image file, or null for default.</param>
    /// <param name="rating">Current rating value.</param>
    /// <param name="ratingVisibility">Rating visibility string.</param>
    /// <param name="mockupVisibility">Mockup visibility string.</param>
    /// <returns>List of preview items (overlay ID + frozen BitmapSource).</returns>
    public static async Task<List<OverlayPreviewItem>> GetPreviewsAsync(
        IOverlayProvider provider,
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility)
    {
        var allOverlays = provider.GetAllOverlays();
        var result = new List<OverlayPreviewItem>(allOverlays.Count);

        foreach (var overlay in allOverlays)
        {
            var cacheKey = BuildCacheKey(overlay.Id, overlay.OverlayVersion, posterPath, rating, ratingVisibility, mockupVisibility);

            if (Cache.TryGetValue(cacheKey, out var cached))
            {
                result.Add(new OverlayPreviewItem(overlay.Id, overlay.DisplayName, cached));
                continue;
            }

            try
            {
                var bitmap = await RenderPreviewAsync(overlay, posterPath, rating, ratingVisibility, mockupVisibility);
                Cache[cacheKey] = bitmap;
                result.Add(new OverlayPreviewItem(overlay.Id, overlay.DisplayName, bitmap));
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to render preview for overlay '{Id}'", overlay.Id);
            }
        }

        Logger.Debug("Preview cache: {Cached}/{Total} cached hits", result.Count, allOverlays.Count);
        return result;
    }

    /// <summary>
    /// Invalidates all cached entries. Called when poster image, rating, or visibility changes.
    /// </summary>
    public static void InvalidateAll()
    {
        Cache.Clear();
        Logger.Debug("Preview cache invalidated");
    }

    /// <summary>
    /// Invalidates cached entries for a specific overlay (e.g., after definition update).
    /// </summary>
    public static void Invalidate(string overlayId)
    {
        var removed = 0;
        foreach (var key in Cache.Keys)
        {
            if (key.StartsWith(overlayId, StringComparison.Ordinal) && Cache.TryRemove(key, out _))
            {
                removed++;
            }
        }
        if (removed > 0)
        {
            Logger.Debug("Invalidated {Count} cache entries for overlay '{Id}'", removed, overlayId);
        }
    }

    /// <summary>
    /// Returns the number of cached entries.
    /// </summary>
    public static int Count => Cache.Count;

    private static async Task<BitmapSource> RenderPreviewAsync(
        PosterOverlayDefinition overlay,
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility)
    {
        return await StaRenderer.Default.EnqueueRender(() =>
        {
            // Create PosterIcon on the STA thread (WPF objects require STA)
            var posterIcon = CreatePosterIcon(posterPath, rating, ratingVisibility, mockupVisibility);

            var dynamicIcon = new DynamicPosterIcon(overlay, posterIcon);
            using var bitmap = dynamicIcon.RenderToBitmap();

            var bitmapSource = ConvertToBitmapSource(bitmap);
            bitmapSource.Freeze();
            return bitmapSource;
        });
    }

    private static PosterIcon CreatePosterIcon(
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility)
    {
        var resolvedPath = posterPath ??= FileUtils.GetResourcePath("posterDummy.png");

        if (File.Exists(resolvedPath))
        {
            return new PosterIcon(resolvedPath, rating, ratingVisibility, mockupVisibility);
        }

        // Fallback to default constructor (loads posterDummy.png)
        return new PosterIcon();
    }

    private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
    {
        // Note: stream is intentionally NOT disposed — BitmapImage reads lazily
        // and the BitmapImage will be frozen, making it self-contained.
        var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        return bitmapImage;
    }

    private static string BuildCacheKey(
        string overlayId,
        string overlayVersion,
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility)
    {
        return $"{overlayId}_{overlayVersion}_{posterPath}_{rating}_{ratingVisibility}_{mockupVisibility}";
    }
}

/// <summary>
/// A single preview item: overlay metadata + rendered frozen BitmapSource.
/// </summary>
public sealed class OverlayPreviewItem
{
    public string OverlayId { get; }
    public string DisplayName { get; }
    public BitmapSource PreviewImage { get; }

    public OverlayPreviewItem(string overlayId, string displayName, BitmapSource previewImage)
    {
        OverlayId = overlayId;
        DisplayName = displayName;
        PreviewImage = previewImage;
    }
}
