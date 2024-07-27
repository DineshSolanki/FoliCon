using System.Windows.Controls.Primitives;

namespace FoliCon.Modules.UI;

public class ListViewClickSortBehavior : Behavior<ListView>
{
    private GridViewColumnHeader _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;

    protected override void OnAttached()
    {
        base.OnAttached();
        
        AssociatedObject.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnClick));
        ((INotifyCollectionChanged) AssociatedObject.Items).CollectionChanged += ListView_CollectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        AssociatedObject.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnClick));
        ((INotifyCollectionChanged) AssociatedObject.Items).CollectionChanged -= ListView_CollectionChanged;
    }
    
    private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UiUtils.SetColumnWidth(AssociatedObject);
        }
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader headerClicked) return;
        if (headerClicked.Role == GridViewColumnHeaderRole.Padding) return;

        ListSortDirection direction;
        if (headerClicked != _lastHeaderClicked)
        {
            direction = ListSortDirection.Ascending;
        }
        else
        {
            direction = _lastDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }

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
        var dataView = CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
        dataView.SortDescriptions.Clear();
        var sd = new SortDescription(sortBy, direction);
        dataView.SortDescriptions.Add(sd);
        dataView.Refresh();
    }
}