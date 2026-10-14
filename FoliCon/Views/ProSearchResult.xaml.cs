namespace FoliCon.Views;

/// <summary>
/// Interaction logic for ProSearchResult
/// </summary>
public partial class ProSearchResult
{

    public ProSearchResult()
    {
        InitializeComponent();
    }

    private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem { DataContext: not null } item)
        {
            return;
        }

        item.IsSelected = true;
        item.Focus();
    }
}
