using FoliCon.Modules.utils;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for SearchResult
/// </summary>
public partial class SearchResult
{
    private GridViewColumnHeader _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;

    public SearchResult()
    {
        InitializeComponent();
        ((INotifyCollectionChanged)ListViewResult.Items).CollectionChanged += ListView_CollectionChanged;
    }

    private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UiUtils.SetColumnWidth(ListViewResult);
            // scroll the new item into view
            //    //ListViewResult.ScrollIntoView(e.NewItems[0]);
        }
    }

    private void ListViewResult_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader headerClicked) return;
        if (headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
        var direction = headerClicked != _lastHeaderClicked
            ? ListSortDirection.Ascending
            : _lastDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        var header = headerClicked.Column.Header as string;
        Sort(header, direction);

        headerClicked.Column.HeaderTemplate = direction == ListSortDirection.Ascending
            ? Application.Current.Resources["HeaderTemplateArrowUp"] as DataTemplate
            : Application.Current.Resources["HeaderTemplateArrowDown"] as DataTemplate;

        // Remove arrow from previously sorted header
        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
        {
            _lastHeaderClicked.Column.HeaderTemplate = null;
        }


        _lastHeaderClicked = headerClicked;
        _lastDirection = direction;
    }

    private void Sort(string sortBy, ListSortDirection direction)
    {
        var dataView =
            CollectionViewSource.GetDefaultView(ListViewResult.ItemsSource);

            dataView.SortDescriptions.Clear();
            var sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WebBox.Browser.Dispose();
        }
}