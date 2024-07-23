using FoliCon.Models.Enums;
using FoliCon.Modules.utils;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private GridViewColumnHeader _lastHeaderClicked;
    private ListSortDirection _lastDirection = ListSortDirection.Ascending;
    public MainWindow()
    {
        InitializeComponent();
        ((INotifyCollectionChanged)FinalList.Items).CollectionChanged += ListView_CollectionChanged;
    }

    private void ListView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            UiUtils.SetColumnWidth(FinalList);
        }
    }

    private void CmbLanguage_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (CmbLanguage.SelectedItem is null)
        {
            return;
        }

        var selectedLanguage = (Languages)CmbLanguage.SelectedValue;
        var cultureInfo = CultureUtils.GetCultureInfoByLanguage(selectedLanguage);
        LangProvider.Culture = cultureInfo;
        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Kernel32.SetThreadUILanguage((ushort)Thread.CurrentThread.CurrentUICulture.LCID);
        if (FinalList is not null)
        {
            UiUtils.SetColumnWidth(FinalList);
        }

    }

    private void FinalList_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader headerClicked)
        {
            return;
        }

        if (headerClicked.Role == GridViewColumnHeaderRole.Padding)
        {
            return;
        }

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
        var dataView =
            CollectionViewSource.GetDefaultView(FinalList.ItemsSource);

        dataView.SortDescriptions.Clear();
        var sd = new SortDescription(sortBy, direction);
        dataView.SortDescriptions.Add(sd);
        dataView.Refresh();
    }

    private void MainWindow_OnClosed(object sender, EventArgs e)
    {
        FileUtils.DeleteFoliConTempDeviationDirectory();
    }
}