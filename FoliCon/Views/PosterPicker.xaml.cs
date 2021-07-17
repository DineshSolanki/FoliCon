using System.Windows.Controls;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for PosterPicker
    /// </summary>
    public partial class PosterPicker : UserControl
    {
        private bool AutoScroll = true;
        public PosterPicker()
        {
            InitializeComponent();
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
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
