namespace FoliCon.Modules.Media;

public class ProIcon(string filePath)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Bitmap RenderToBitmap()
    {
        Logger.Debug("Rendering icon to bitmap, filePath: {FilePath}", filePath);
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.UriSource = new Uri(filePath, UriKind.Absolute);
        bi.DecodePixelWidth = 256;
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.EndInit();
        bi.Freeze();
        return PosterIconBase.RenderTargetBitmapTo32BppArgb(bi);
    }
}