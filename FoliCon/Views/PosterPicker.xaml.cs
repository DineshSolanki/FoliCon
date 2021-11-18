namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for PosterPicker
    /// </summary>
    public partial class PosterPicker : UserControl
    {
        private bool _autoScroll = true;
        public PosterPicker()
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
}
