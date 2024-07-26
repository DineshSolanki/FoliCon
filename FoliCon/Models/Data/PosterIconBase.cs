using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Models.Data;

public abstract class PosterIconBase : UserControl
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    protected PosterIconBase()
    {
    }

    protected PosterIconBase(object dataContext)
    {
        DataContext = dataContext;
    }

    public Bitmap RenderToBitmap()
    {
        return RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
    }

    private RenderTargetBitmap AsRenderTargetBitmap()
    {
        var size = new System.Windows.Size(256, 256);
        Measure(size);
        Arrange(new Rect(size));

        var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Default);
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