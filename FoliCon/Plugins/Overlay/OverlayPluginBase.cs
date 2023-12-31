namespace FoliCon.Plugins.Overlay;

public abstract class OverlayPluginBase : UserControl
{
    public virtual string OverlayName { get; }
    
    public virtual string PluginIconSource { get; set; }
    
    public virtual FoliCon.Models.Data.PosterIcon PluginParameters { get; private set; }
    
    public virtual Bitmap RenderOverlay(FoliCon.Models.Data.PosterIcon parameters)
    {
        DataContext = parameters;
        PluginParameters = parameters;
        return RenderToBitmap();
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

    private static Bitmap RenderTargetBitmapTo32BppArgb(BitmapSource rtb)
    {
        var stream = new MemoryStream();
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        encoder.Save(stream);
        return new Bitmap(stream); //png;
    }
}