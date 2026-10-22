#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;

namespace FoliCon.Views;

/// <summary>
/// Interaction logic for FoliConStore.xaml
/// </summary>
public partial class FoliConStore : UserControl
{
    public FoliConStore()
    {
        InitializeComponent();
    }

    private void ThemesListBox_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject depObj) return;

        var scroller = depObj as WpfScrollViewer ?? FindVisualChild<WpfScrollViewer>(depObj);
        if (scroller != null)
        {
            scroller.ScrollToVerticalOffset(scroller.VerticalOffset - (e.Delta * 0.8));
            e.Handled = true;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild) return typedChild;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
