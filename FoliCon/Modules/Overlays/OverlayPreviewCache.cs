using Microsoft.Extensions.Caching.Memory;

#nullable enable
namespace FoliCon.Modules.Overlays;

/// <summary>
/// STA-safe rendered preview caching for the Previewer dialog.
/// All WPF control creation and rendering goes through StaRenderer.Default.EnqueueRender().
/// Cached BitmapSource instances are frozen before storage for cross-thread UI access.
///
/// Storage is a size-limited <see cref="MemoryCache"/> so the cache cannot grow without
/// bound as poster/rating combinations accumulate; least-recently-used entries are
/// evicted automatically instead of leaking for the lifetime of the app.
/// </summary>
public static class OverlayPreviewCache
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Maximum number of rendered previews held. Each entry is a small icon-sized bitmap,
    /// so a few hundred entries stay well under a few tens of MB.
    /// </summary>
    private const int maxEntries = 256;

    /// <summary>
    /// Renders at 2x the 256x256 display size so the Previewer's Image control (which the OS
    /// may additionally scale for display DPI) downsamples instead of upscaling a 1:1 bitmap,
    /// avoiding the blurry/blocky look of stretching a low-res render.
    /// </summary>
    private const double previewRenderScale = 2.0;

    private static readonly MemoryCache Cache = new(new MemoryCacheOptions
    {
        SizeLimit = maxEntries,
        CompactionPercentage = 0.25
    });

    public static async Task<List<OverlayPreviewItem>> GetPreviewsAsync(
        IOverlayProvider provider,
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility,
        string? mediaTitle = null)
    {
        var allOverlays = provider.GetAllOverlays();
        var result = new List<OverlayPreviewItem>(allOverlays.Count);

        foreach (var overlay in allOverlays)
        {
            var cacheKey = BuildCacheKey(overlay.Id, overlay.OverlayVersion, posterPath, rating, ratingVisibility, mockupVisibility, mediaTitle);

            if (Cache.TryGetValue(cacheKey, out BitmapSource? cached) && cached != null)
            {
                result.Add(new OverlayPreviewItem(overlay.Id, overlay.DisplayName, cached));
                continue;
            }

            try
            {
                var bitmap = await RenderPreviewAsync(overlay, posterPath, rating, ratingVisibility, mockupVisibility, mediaTitle);
                Cache.Set(cacheKey, bitmap, new MemoryCacheEntryOptions { Size = 1 });
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
            if (!((string)key).StartsWith(overlayId, StringComparison.Ordinal))
            {
                continue;
            }

            Cache.Remove(key);
            removed++;
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
        string mockupVisibility,
        string? mediaTitle)
    {
        return await StaRenderer.Default.EnqueueRender(() =>
        {
            // Create PosterIcon on the STA thread (WPF objects require STA)
            var posterIcon = CreatePosterIcon(posterPath, rating, ratingVisibility, mockupVisibility, mediaTitle);

            var dynamicIcon = new DynamicPosterIcon(overlay, posterIcon);
            using var bitmap = dynamicIcon.RenderToBitmap(previewRenderScale);

            return ConvertToBitmapSource(bitmap);
        });
    }

    private static PosterIcon CreatePosterIcon(
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility,
        string? mediaTitle)
    {
        var resolvedPath = posterPath ??= FileUtils.GetResourcePath("posterDummy.png");

        if (File.Exists(resolvedPath))
        {
            return string.IsNullOrEmpty(mediaTitle)
                ? new PosterIcon(resolvedPath, rating, ratingVisibility, mockupVisibility)
                : new PosterIcon(resolvedPath, rating, ratingVisibility, mockupVisibility, mediaTitle);
        }

        // Fallback to default constructor (loads posterDummy.png)
        return new PosterIcon();
    }

    private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var stride = width * 4;
        var pixels = new byte[stride * height];

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
        bitmap.UnlockBits(bitmapData);

        // GDI+ Format32bppArgb is non-premultiplied ARGB (byte order: B,G,R,A).
        // WPF's Pbgra32 expects premultiplied BGRA. Premultiply in-place.
        PremultiplyBgra(pixels);

        var bitmapSource = BitmapSource.Create(
            width, height, 96, 96, PixelFormats.Pbgra32, null, pixels, stride);
        bitmapSource.Freeze();
        return bitmapSource;
    }

    private static void PremultiplyBgra(byte[] pixels)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var a = pixels[i + 3];
            if (a == 0)
            {
                pixels[i] = 0;
                pixels[i + 1] = 0;
                pixels[i + 2] = 0;
            }
            else if (a < 255)
            {
                pixels[i] = (byte)(pixels[i] * a / 255);
                pixels[i + 1] = (byte)(pixels[i + 1] * a / 255);
                pixels[i + 2] = (byte)(pixels[i + 2] * a / 255);
            }
        }
    }

    private static string BuildCacheKey(
        string overlayId,
        string overlayVersion,
        string? posterPath,
        string rating,
        string ratingVisibility,
        string mockupVisibility,
        string? mediaTitle) =>
        $"{overlayId}_{overlayVersion}_{posterPath}_{rating}_{ratingVisibility}_{mockupVisibility}_{mediaTitle}";
}

/// <summary>
/// A single preview item: overlay metadata + rendered frozen BitmapSource.
/// </summary>
public sealed class OverlayPreviewItem(string overlayId, string displayName, BitmapSource previewImage)
{
    public string OverlayId { get; } = overlayId;
    public string DisplayName { get; } = displayName;
    public BitmapSource PreviewImage { get; } = previewImage;
}
