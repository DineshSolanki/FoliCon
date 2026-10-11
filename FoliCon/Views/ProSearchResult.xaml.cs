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

    private void ListBoxItem_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.ListBoxItem item && item.DataContext != null)
        {
            item.IsSelected = true;
            item.Focus();
        }
    }
}