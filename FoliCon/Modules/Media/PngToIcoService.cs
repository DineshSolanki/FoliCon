namespace FoliCon.Modules.Media;

public static class PngToIcoService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static void Convert(Bitmap bitmap, string icoPath)
    {
        Logger.Debug("Converting png to ico, path: {IcoPath}", icoPath);
        var mIcon = new MultiIcon();
        mIcon.Add("Untitled").CreateFrom(bitmap, IconOutputFormat.Vista);
        mIcon.SelectedIndex = 0;
        mIcon.Save(icoPath, MultiIconFormat.ICO);
        Logger.Debug("Converted png to ico, path: {IcoPath}", icoPath);
    }
}