namespace FoliCon.Modules.utils;

public static class UiUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// Set Column width of list view to fit content
    /// </summary>
    /// <param name="listView"> list view to change width</param>
    public static void SetColumnWidth(ListView listView)
    {
        if (listView.View is not GridView gridView)
        {
            return;
        }

        foreach (var column in gridView.Columns)
        {
            if (double.IsNaN(column.Width))
            {
                column.Width = column.ActualWidth;
            }

            column.Width = double.NaN;
        }
    }

    public static Visibility BooleanToVisibility(bool value)
    {
        return value ? Visibility.Visible : Visibility.Hidden;
    }

    public static void ShowImageBrowser(string imageLocation)
    {
        Logger.Trace("Opening Image {Image}", imageLocation);
        var browser = new ImageBrowser(imageLocation)
        {
            ShowTitle = false,
            IsFullScreen = true,
            AllowsTransparency = true
        };
        browser.Show();
    }
}