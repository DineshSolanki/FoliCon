#nullable enable
using System.IO;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays.Designer;

namespace FoliconTest;

/// <summary>
/// Tests for <see cref="OverlayDesignerPreviewRenderer"/> — debouncing, stale-frame
/// suppression, and STA-safe frozen output.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class OverlayDesignerPreviewRendererTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly List<OverlayDesignerPreviewRenderer> _renderers = [];

    public void Dispose()
    {
        foreach (var renderer in _renderers)
        {
            renderer.Dispose();
        }

        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    private OverlayDesignerPreviewRenderer NewRenderer(TimeSpan? debounce = null)
    {
        var renderer = new OverlayDesignerPreviewRenderer(debounce);
        _renderers.Add(renderer);
        return renderer;
    }

    #region Rendering

    [Fact]
    public async Task RenderNowAsync_ProducesAFrozen256Bitmap()
    {
        var image = await OverlayDesignerPreviewRenderer.RenderNowAsync(CreateDefinition(), new OverlayPreviewContext());

        Assert.NotNull(image);
        Assert.Equal(256, image.PixelWidth);
        Assert.Equal(256, image.PixelHeight);

        // Must be frozen to cross from the STA render thread to the UI thread.
        Assert.True(image.IsFrozen);
    }

    [Fact]
    public async Task RequestRender_RaisesRenderedWithAFrame()
    {
        var renderer = NewRenderer(TimeSpan.FromMilliseconds(20));
        var completion = new TaskCompletionSource<OverlayPreviewRenderedEventArgs>();
        renderer.Rendered += (_, e) => completion.TrySetResult(e);

        renderer.RequestRender(CreateDefinition(), new OverlayPreviewContext());

        var result = await WaitFor(completion.Task);
        Assert.NotNull(result.Image);
        Assert.True(result.Image.IsFrozen);
    }

    [Fact]
    public async Task PreviewContextChanges_AlterTheRenderedOutput()
    {
        var renderer = NewRenderer();
        var definition = CreateDefinition();

        var withRating = await OverlayDesignerPreviewRenderer.RenderNowAsync(definition,
            new OverlayPreviewContext { ShowRating = true, Rating = "9.9" });
        var withoutRating = await OverlayDesignerPreviewRenderer.RenderNowAsync(definition,
            new OverlayPreviewContext { ShowRating = false });

        Assert.NotNull(withRating);
        Assert.NotNull(withoutRating);
        Assert.NotEqual(ToBytes(withRating), ToBytes(withoutRating));
    }

    #endregion

    #region Debounce and staleness

    [Fact]
    public async Task RapidRequests_PublishOnlyTheFinalFrame()
    {
        // A drag fires far faster than WPF can render; only the last state should reach the canvas.
        // The debounce is long relative to the request interval so the burst is unambiguously
        // still in flight when the last request lands.
        var renderer = NewRenderer(TimeSpan.FromMilliseconds(250));
        var rendered = 0;
        var firstFrame = new TaskCompletionSource();
        renderer.Rendered += (_, _) =>
        {
            Interlocked.Increment(ref rendered);
            firstFrame.TrySetResult();
        };

        for (var i = 0; i < 12; i++)
        {
            renderer.RequestRender(CreateDefinition(), new OverlayPreviewContext());
            await Task.Delay(5);
        }

        // Wait on the signal rather than a fixed sleep: the shared STA render thread is
        // contended by other tests, so a wall-clock deadline makes this flaky under load.
        await WaitFor(firstFrame.Task);

        // Give any wrongly-surviving superseded frame a chance to arrive and be counted.
        await Task.Delay(300);

        Assert.Equal(1, rendered);
        Assert.Equal(1, renderer.PublishedFrameCount);
    }

    [Fact]
    public async Task SupersededRequests_NeverPublish()
    {
        var renderer = NewRenderer(TimeSpan.FromMilliseconds(50));
        var completion = new TaskCompletionSource();
        renderer.Rendered += (_, _) => completion.TrySetResult();

        renderer.RequestRender(CreateDefinition("first"), new OverlayPreviewContext());
        renderer.RequestRender(CreateDefinition("second"), new OverlayPreviewContext());
        renderer.RequestRender(CreateDefinition("third"), new OverlayPreviewContext());

        await WaitFor(completion.Task);
        await Task.Delay(200);

        Assert.Equal(1, renderer.PublishedFrameCount);
    }

    [Fact]
    public async Task DisposeDuringDebounce_PublishesNothing()
    {
        var renderer = new OverlayDesignerPreviewRenderer(TimeSpan.FromMilliseconds(200));
        var rendered = 0;
        renderer.Rendered += (_, _) => Interlocked.Increment(ref rendered);

        renderer.RequestRender(CreateDefinition(), new OverlayPreviewContext());
        renderer.Dispose();

        await Task.Delay(400);

        Assert.Equal(0, rendered);
    }

    [Fact]
    public void RequestRender_AfterDispose_IsIgnored()
    {
        var renderer = new OverlayDesignerPreviewRenderer();
        renderer.Dispose();

        // Must not throw — the dialog can close while an edit is still in flight.
        var ex = Record.Exception(() => renderer.RequestRender(CreateDefinition(), new OverlayPreviewContext()));
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var renderer = new OverlayDesignerPreviewRenderer();

        var ex = Record.Exception(() =>
        {
            renderer.Dispose();
            renderer.Dispose();
        });
        Assert.Null(ex);
    }

    #endregion

    #region Failure handling

    [Fact]
    public async Task RenderNowAsync_InvalidAsset_ReturnsNullRatherThanThrowing()
    {
        var renderer = NewRenderer();
        var definition = CreateDefinition();
        definition.BaseLayer = new LayerDefinition
        {
            ImagePath = "does-not-exist.png",
            Margin = "0,0,0,0"
        };
        definition.OverlayFolderPath = Path.Combine(Path.GetTempPath(), "no-such-folder-xyz");

        var image = await OverlayDesignerPreviewRenderer.RenderNowAsync(definition, new OverlayPreviewContext());

        // DynamicPosterIcon skips layers it cannot load, so this still renders.
        // The contract that matters is that it never throws into the UI thread.
        Assert.True(image == null || image.IsFrozen);
    }

    #endregion

    #region Preview context

    [Fact]
    public void Clone_CopiesEveryField()
    {
        var original = new OverlayPreviewContext
        {
            PosterPath = @"C:\posters\sample.png",
            Rating = "8.4",
            MediaTitle = "Title",
            ShowRating = false,
            ShowMockup = false
        };

        var clone = original.Clone();

        Assert.Equal(original.PosterPath, clone.PosterPath);
        Assert.Equal(original.Rating, clone.Rating);
        Assert.Equal(original.MediaTitle, clone.MediaTitle);
        Assert.Equal(original.ShowRating, clone.ShowRating);
        Assert.Equal(original.ShowMockup, clone.ShowMockup);
    }

    [Fact]
    public void Clone_IsIndependentOfTheOriginal()
    {
        // The renderer snapshots the context so later UI edits can't mutate an in-flight frame.
        var original = new OverlayPreviewContext { Rating = "1.0" };
        var clone = original.Clone();

        original.Rating = "9.9";

        Assert.Equal("1.0", clone.Rating);
    }

    [Theory]
    [InlineData(true, "visible")]
    [InlineData(false, "hidden")]
    public void VisibilityFlags_MapToRendererStrings(bool show, string expected)
    {
        var context = new OverlayPreviewContext { ShowRating = show, ShowMockup = show };

        Assert.Equal(expected, context.RatingVisibility);
        Assert.Equal(expected, context.MockupVisibility);
    }

    #endregion

    private static async Task<T> WaitFor<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(task, completed);
        return await task;
    }

    private static async Task WaitFor(Task task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(task, completed);
    }

    private static byte[] ToBytes(System.Windows.Media.Imaging.BitmapSource source)
    {
        var stride = source.PixelWidth * 4;
        var buffer = new byte[stride * source.PixelHeight];
        source.CopyPixels(buffer, stride, 0);
        return buffer;
    }

    private static PosterOverlayDefinition CreateDefinition(string id = "preview-test") => new()
    {
        SchemaVersion = 1,
        Id = id,
        DisplayName = "Preview Test",
        OverlayVersion = "1.0.0",
        DesignWidth = 265,
        DesignHeight = 256,
        RootMargin = "0,0,0,-11",
        RenderWidth = 256,
        RenderHeight = 256,
        LayerOrder = ["poster", "rating"],
        Poster = new PosterConfig { Margin = "20,20,20,20", ClipRadius = "0" },
        Rating = new RatingConfig
        {
            ShieldMargin = "160,97,6,5",
            TextMargin = "189,30,21,24",
            FontSize = 25,
            FontFamily = "Castellar",
            TextWidth = 55,
            TextHeight = 46
        },
        Title = new TitleConfig { IsVisible = false, RotationOrigin = "0.5,0.5" }
    };
}
