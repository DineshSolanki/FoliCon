using System.Runtime.CompilerServices;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace FoliCon.Modules.UI;

public static class ScrollViewerBehavior
{
    private const double tolerance = 1.01;

    /// <summary>Per-instance auto-scroll state — avoids cross-view contamination.</summary>
    private static readonly ConditionalWeakTable<ScrollViewer, StrongBox<bool>> AutoScrollState = new();

    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached("AutoScroll", typeof(bool), typeof(ScrollViewerBehavior),
            new PropertyMetadata(false, AutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj) => (bool)obj.GetValue(AutoScrollProperty);

    public static void SetAutoScroll(DependencyObject obj, bool value) => obj.SetValue(AutoScrollProperty, value);

    private static void AutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (e.NewValue is true)
        {
            AutoScrollState.GetOrCreateValue(scrollViewer)?.Value = true;
            scrollViewer.ScrollChanged -= OnScrollChanged;
            scrollViewer.ScrollChanged += OnScrollChanged;
        }
        else
        {
            AutoScrollState.Remove(scrollViewer);
            scrollViewer.ScrollChanged -= OnScrollChanged;
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs ea)
    {
        var scrollViewer = (ScrollViewer)sender;
        var state = AutoScrollState.GetOrCreateValue(scrollViewer);

        if (Math.Abs(ea.ExtentHeightChange) <= double.Epsilon)
        {
            state?.Value = Math.Abs(scrollViewer.VerticalOffset - scrollViewer.ScrollableHeight) <= tolerance;
            return;
        }

        if (state is not { Value: true })
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
    }
}
