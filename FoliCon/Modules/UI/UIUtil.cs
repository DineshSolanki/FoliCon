using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.UI;

public static class UiUtil
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static void ShowImageBrowser(string imageLocation)
    {
        Logger.Trace("Opening Image {Image}", imageLocation);
        var browser = new ImageBrowser(imageLocation)
        {
            ShowTitle = false,
            IsFullScreen = true
        };
        browser.Show();
    }
}