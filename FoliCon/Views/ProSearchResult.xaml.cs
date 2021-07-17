namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for ProSearchResult
    /// </summary>
    public partial class ProSearchResult
    {
        private bool AutoScroll = true;

        public ProSearchResult()
        {
            InitializeComponent();
        }

        private void ScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                
                AutoScroll = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;
            }


            if (AutoScroll && e.ExtentHeightChange != 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
            }
        }
    }
}