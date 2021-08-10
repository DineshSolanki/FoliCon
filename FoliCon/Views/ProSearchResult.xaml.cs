namespace FoliCon.Views
{
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

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                
                _autoScroll = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
            }


            if (_autoScroll && e.ExtentHeightChange != 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }
    }
}