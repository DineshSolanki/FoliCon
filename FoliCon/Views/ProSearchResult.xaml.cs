namespace FoliCon.Views;

/// <summary>
/// Interaction logic for ProSearchResult
/// </summary>
public partial class ProSearchResult
{
    private bool _autoScroll = true;
    private const double Tolerance = 0.001;
    private const double Epsilon = 1E-10;

    public ProSearchResult()
    {
        InitializeComponent();
    }

    private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (Math.Abs(e.ExtentHeightChange) > Epsilon)
        {
            _autoScroll = Math.Abs(ScrollViewer.VerticalOffset - ScrollViewer.ScrollableHeight) < Tolerance;
        }


        if (_autoScroll && Math.Abs(e.ExtentHeightChange) > Epsilon)
        {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.ExtentHeight);
        }
    }
}