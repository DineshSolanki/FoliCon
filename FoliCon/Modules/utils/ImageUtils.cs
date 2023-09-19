using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class ImageUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    /// <summary>
    /// Converts Bitmap to BitmapSource
    /// </summary>
    /// <param name="source">Bitmap object</param>
    /// <returns></returns>
    public static BitmapSource LoadBitmap(Bitmap source)
    {
        Logger.Trace("Converting Bitmap to BitmapSource");
        var ip = source.GetHbitmap();
        BitmapSource bs;

        try
        {
            bs = Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(ip);
            // _ = NativeMethods.DeleteObject(ip);
        }

        Logger.Trace("Bitmap Converted to BitmapSource");
        return bs;
    }
}