namespace FoliCon.Modules.Media;

[Localizable(false)]
public static class PngToIcoService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Convert(Bitmap bitmap, string icoPath)
    {
        Logger.Debug("Converting png to ico, path: {IcoPath}", icoPath);
        var sourceBitmap = bitmap;
        var disposeSource = false;
        if (bitmap.Width > 256 || bitmap.Height > 256)
        {
            var maxDim = Math.Max(bitmap.Width, bitmap.Height);
            var newWidth = Math.Max(1, (bitmap.Width * 256) / maxDim);
            var newHeight = Math.Max(1, (bitmap.Height * 256) / maxDim);
            var resized = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(bitmap, 0, 0, newWidth, newHeight);
            }
            sourceBitmap = resized;
            disposeSource = true;
        }

        try
        {
            var mIcon = new MultiIcon();
            mIcon.Add("Untitled").CreateFrom(sourceBitmap, IconOutputFormat.Vista);
            mIcon.SelectedIndex = 0;
            mIcon.Save(icoPath, MultiIconFormat.ICO);
            Logger.Debug("Converted png to ico, path: {IcoPath}", icoPath);
        }
        finally
        {
            if (disposeSource)
            {
                sourceBitmap.Dispose();
            }
        }
    }
}
