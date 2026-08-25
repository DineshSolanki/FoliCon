using System.Windows;
using System.Windows.Media;
using Size = System.Windows.Size;

namespace FoliCon.Models.Data;

public abstract class PosterIconBase : UserControl
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Size WindowRenderSize = new(256, 256);
    protected PosterIconBase()
    {
    }

    protected PosterIconBase(object dataContext)
    {
        DataContext = dataContext;
    }

    public Bitmap RenderToBitmap() => RenderToBitmap(1.0);

    /// <summary>
    /// Renders at <paramref name="scale"/> times the 256×256 design size.
    ///
    /// A LayoutTransform scales the whole visual tree — images, text, and margins alike —
    /// so the designer canvas gets one bitmap pixel per screen pixel instead of upscaling a
    /// 256px frame with visible blockiness or aliasing. The default 1.0 path is untouched:
    /// exported icons and golden-image parity must stay byte-stable at 256×256.
    /// </summary>
    public Bitmap RenderToBitmap(double scale)
    {
        if (Math.Abs(scale - 1.0) <= double.Epsilon)
        {
            return RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
        }

        var width = WindowRenderSize.Width * scale;
        var height = WindowRenderSize.Height * scale;

        // Applied around Measure/Arrange so the tree lays out directly at the target
        // size rather than being rendered small and stretched by the bitmap target.
        LayoutTransform = new ScaleTransform(scale, scale);

        try
        {
            Measure(new Size(width, height));
            Arrange(new Rect(0, 0, width, height));

            var rtb = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Round(width)), Math.Max(1, (int)Math.Round(height)),
                96, 96, PixelFormats.Default);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            rtb.Render(this);

            return RenderTargetBitmapTo32BppArgb(rtb);
        }
        finally
        {
            // The control may be reused; never leak a zoomed transform into it.
            LayoutTransform = Transform.Identity;
        }
    }

    private RenderTargetBitmap AsRenderTargetBitmap()
    {
        Measure(WindowRenderSize);
        Arrange(new Rect(WindowRenderSize));
        var rtb = new RenderTargetBitmap((int)WindowRenderSize.Width, (int)WindowRenderSize.Height, 96, 96, PixelFormats.Default);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        rtb.Render(this);

        return rtb;
    }

    public static Bitmap RenderTargetBitmapTo32BppArgb(BitmapSource rtb)
    {
        Logger.Trace("Converting RenderTargetBitmap to 32BppArgb");
        var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(stream);
        Logger.Trace("RenderTargetBitmap converted to 32BppArgb");
        return new Bitmap(stream);
    }
}
