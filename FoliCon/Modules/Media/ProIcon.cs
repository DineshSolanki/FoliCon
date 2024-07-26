using FoliCon.Models.Data;
using FoliCon.Modules.utils;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.Media;

public class ProIcon(string filePath)
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Bitmap RenderToBitmap()
    {
        Logger.Debug("Rendering icon to bitmap");
        return PosterIconBase.RenderTargetBitmapTo32BppArgb(AsRenderTargetBitmap());
    }

    private BitmapSource AsRenderTargetBitmap()
    {
        using var img = new Bitmap(filePath);
        using var icon = new Bitmap(img, 256, 256);
        Logger.Debug("Icon resized to 256x256, filePath: {FilePath}", filePath);
        return ImageUtils.LoadBitmap(icon);
    }
}