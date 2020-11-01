using FoliCon.Modules;
using System.Collections.Specialized;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)FinalList.Items).CollectionChanged += ListView_CollectionChanged;
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Util.SetColumnWidth(FinalList);
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                //    // scroll the new item into view

                //    //FinalList.ScrollIntoView(e.NewItems[0]);
            }
        }
    }
}