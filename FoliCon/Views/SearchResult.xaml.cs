namespace FoliCon.Views;

/// <summary>
/// Interaction logic for SearchResult
/// </summary>
public partial class SearchResult
{
    public SearchResult()
    {
        InitializeComponent();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        WebBox.Browser.Dispose();
    }
}