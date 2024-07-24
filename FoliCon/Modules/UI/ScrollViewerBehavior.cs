namespace FoliCon.Modules.UI;

using System.Windows;
using System.Windows.Controls;

public static class ScrollViewerBehavior
{
    private const double Tolerance = 1.01;
    private static bool _autoScroll = true;
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ScrollViewerBehavior), 
            new PropertyMetadata(false, AutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollProperty);
    }

    public static void SetAutoScroll(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollProperty, value);
    }

    private static void AutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer && e.NewValue is true)
        {
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
    }
    
    private static void OnScrollChanged(object sender, ScrollChangedEventArgs ea)
    {
        var scrollViewer = sender as ScrollViewer;

        if (Math.Abs(ea.ExtentHeightChange) <= double.Epsilon)
        {
            _autoScroll = Math.Abs(scrollViewer!.VerticalOffset - scrollViewer.ScrollableHeight) <= Tolerance;
            return;
        }

        if (!_autoScroll)
        {
            return;
        }

        scrollViewer!.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
    }
}