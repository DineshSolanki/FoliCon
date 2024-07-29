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

    public Bitmap RenderToBitmap()
    {
        var rtb = AsRenderTargetBitmap();
        return RenderTargetBitmapTo32BppArgb(rtb);
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
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(stream);
        Logger.Trace("RenderTargetBitmap converted to 32BppArgb");
        return new Bitmap(stream);
    }
}