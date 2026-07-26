#nullable enable
namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Test-preview inputs. Kept outside <see cref="OverlayDesignerDocument"/> so changing the
/// sample poster or rating never marks the overlay dirty.
/// </summary>
public sealed class OverlayPreviewContext
{
    /// <summary>Absolute path to the sample poster, or null for the bundled dummy.</summary>
    public string? PosterPath { get; set; }

    public string Rating { get; set; } = "7.8";

    public string MediaTitle { get; set; } = "Made with ♥ by FoliCon";

    public bool ShowRating { get; set; } = true;

    public bool ShowMockup { get; set; } = true;

    public string RatingVisibility => ShowRating ? "visible" : "hidden";

    public string MockupVisibility => ShowMockup ? "visible" : "hidden";

    public OverlayPreviewContext Clone() => new()
    {
        PosterPath = PosterPath,
        Rating = Rating,
        MediaTitle = MediaTitle,
        ShowRating = ShowRating,
        ShowMockup = ShowMockup
    };
}

/// <summary>
/// Renders designer snapshots through the production <see cref="DynamicPosterIcon"/> pipeline
/// on the shared STA renderer.
///
/// Editing generates changes far faster than WPF can render them, so requests are debounced and
/// versioned: only the newest request survives the debounce window, and a result that arrives
/// after a newer request started is discarded rather than flashing stale output onto the canvas.
///
/// The global <see cref="OverlayPreviewCache"/> is deliberately not used — a draft changes on
/// every keystroke and would poison a cache keyed by overlay ID and version.
/// </summary>
public sealed class OverlayDesignerPreviewRenderer : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Long enough to coalesce a burst of drag updates, short enough that the canvas still
    /// feels live. Roughly one frame at 30fps plus scheduling slack.
    /// </summary>
    public static readonly TimeSpan DefaultDebounce = TimeSpan.FromMilliseconds(80);

    private readonly TimeSpan _debounce;
    private readonly object _gate = new();

    private CancellationTokenSource? _pending;
    private long _requestVersion;
    private bool _disposed;

    public OverlayDesignerPreviewRenderer(TimeSpan? debounce = null) =>
        _debounce = debounce ?? DefaultDebounce;

    /// <summary>Raised on the STA render completion path when a fresh frame is ready.</summary>
    public event EventHandler<OverlayPreviewRenderedEventArgs>? Rendered;

    /// <summary>Raised when a render attempt fails. The canvas keeps its previous frame.</summary>
    public event EventHandler<OverlayPreviewFailedEventArgs>? Failed;

    /// <summary>Number of frames actually published. Test/diagnostic hook.</summary>
    public long PublishedFrameCount { get; private set; }

    /// <summary>Number of completed renders discarded because a newer request superseded them.</summary>
    public long DiscardedFrameCount { get; private set; }

    /// <summary>
    /// Requests a preview of <paramref name="definition"/>. Supersedes any pending request.
    /// Returns immediately; results arrive via <see cref="Rendered"/> or <see cref="Failed"/>.
    /// </summary>
    public void RequestRender(PosterOverlayDefinition definition, OverlayPreviewContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        if (_disposed)
        {
            return;
        }

        CancellationTokenSource cts;
        long version;

        lock (_gate)
        {
            // Cancel the request still inside its debounce window; only the newest wins.
            _pending?.Cancel();
            _pending?.Dispose();

            cts = new CancellationTokenSource();
            _pending = cts;
            version = ++_requestVersion;
        }

        // Snapshot the context so later UI edits can't mutate what this frame renders.
        _ = RenderAfterDebounceAsync(definition, context.Clone(), version, cts.Token);
    }

    /// <summary>
    /// Renders immediately, bypassing the debounce. Used for the initial frame and for export
    /// preview generation, where there is no burst to coalesce.
    /// </summary>
    public async Task<BitmapSource?> RenderNowAsync(PosterOverlayDefinition definition, OverlayPreviewContext context)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            return await RenderOnStaAsync(definition, context.Clone());
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Immediate preview render failed for overlay '{Id}'", definition.Id);
            return null;
        }
    }

    private async Task RenderAfterDebounceAsync(
        PosterOverlayDefinition definition,
        OverlayPreviewContext context,
        long version,
        CancellationToken token)
    {
        try
        {
            await Task.Delay(_debounce, token);
        }
        catch (OperationCanceledException)
        {
            return; // Superseded before the render even started.
        }

        BitmapSource bitmap;
        try
        {
            bitmap = await RenderOnStaAsync(definition, context);
        }
        catch (Exception ex)
        {
            if (IsCurrent(version))
            {
                Logger.Warn(ex, "Preview render failed for overlay '{Id}'", definition.Id);
                Failed?.Invoke(this, new OverlayPreviewFailedEventArgs(ex));
            }
            return;
        }

        // Rendering is queued behind other STA work, so a newer request may have started
        // while this one waited. Publishing now would flash a stale frame.
        if (!IsCurrent(version))
        {
            DiscardedFrameCount++;
            return;
        }

        PublishedFrameCount++;
        Rendered?.Invoke(this, new OverlayPreviewRenderedEventArgs(bitmap));
    }

    private static async Task<BitmapSource> RenderOnStaAsync(PosterOverlayDefinition definition, OverlayPreviewContext context)
    {
        return await StaRenderer.Default.EnqueueRender(() =>
        {
            // PosterIcon owns WPF image resources and must be built on the STA thread.
            using var posterIcon = CreatePosterIcon(context);
            var icon = new DynamicPosterIcon(definition, posterIcon);
            using var bitmap = icon.RenderToBitmap();
            return ToFrozenBitmap(bitmap);
        });
    }

    private static PosterIcon CreatePosterIcon(OverlayPreviewContext context)
    {
        var path = context.PosterPath;

        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            return new PosterIcon(path, context.Rating, context.RatingVisibility, context.MockupVisibility, context.MediaTitle);
        }

        // Default constructor loads the bundled posterDummy.png.
        return new PosterIcon
        {
            Rating = context.Rating,
            RatingVisibility = context.RatingVisibility,
            MockupVisibility = context.MockupVisibility,
            MediaTitle = context.MediaTitle
        };
    }

    /// <summary>
    /// Converts the renderer's <see cref="Bitmap"/> into a frozen <see cref="BitmapSource"/>
    /// so it can cross from the STA render thread to the UI thread safely.
    /// </summary>
    private static BitmapSource ToFrozenBitmap(Bitmap bitmap)
    {
        // Not disposed: BitmapImage reads the stream lazily during EndInit, and freezing
        // makes the result self-contained.
        var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private bool IsCurrent(long version)
    {
        lock (_gate)
        {
            return version == _requestVersion && !_disposed;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _pending?.Cancel();
            _pending?.Dispose();
            _pending = null;
        }
    }
}

public sealed class OverlayPreviewRenderedEventArgs(BitmapSource image) : EventArgs
{
    public BitmapSource Image { get; } = image;
}

public sealed class OverlayPreviewFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
