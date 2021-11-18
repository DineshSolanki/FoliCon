namespace FoliCon.Views;

/// <summary>
/// Interaction logic for ProSearchResult
/// </summary>
public partial class ProSearchResult
{
    private bool _autoScroll = true;

    public ProSearchResult()
    {
        InitializeComponent();
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange == 0)
        {
                
            _autoScroll = ScrollViewer.VerticalOffset == ScrollViewer.ScrollableHeight;
        }


        if (_autoScroll && e.ExtentHeightChange != 0)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
        }
    }
}