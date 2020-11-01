using FoliCon.Modules;
using System.Collections.Specialized;

namespace FoliCon.Views
{
    /// <summary>
    /// Interaction logic for SearchResult
    /// </summary>
    public partial class SearchResult
    {
        public SearchResult()
        {
            InitializeComponent();
            ((INotifyCollectionChanged)ListViewResult.Items).CollectionChanged += ListView_CollectionChanged;
        }

        private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Util.SetColumnWidth(ListViewResult);
                // scroll the new item into view
                //    //ListViewResult.ScrollIntoView(e.NewItems[0]);
            }
        }
    }
}