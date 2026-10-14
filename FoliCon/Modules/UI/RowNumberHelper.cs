#nullable enable
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using ScrollViewer = System.Windows.Controls.ScrollViewer;

namespace FoliCon.Modules.UI;

/// <summary>
/// Attached helper that assigns accurate, 1-based row numbers (e.g. "#1", "#2")
/// to ListBoxItem containers in virtualized ListBoxes, updating properly on
/// drag/drop reordering, collection changes, and scrolling.
/// </summary>
public static class RowNumberHelper
{
    public static readonly DependencyProperty ShowRowNumbersProperty =
        DependencyProperty.RegisterAttached(
            "ShowRowNumbers",
            typeof(bool),
            typeof(RowNumberHelper),
            new PropertyMetadata(false, OnShowRowNumbersChanged));

    public static readonly DependencyProperty RowIndexProperty =
        DependencyProperty.RegisterAttached(
            "RowIndex",
            typeof(string),
            typeof(RowNumberHelper),
            new FrameworkPropertyMetadata(string.Empty));

    public static bool GetShowRowNumbers(DependencyObject obj) => (bool)obj.GetValue(ShowRowNumbersProperty);
    public static void SetShowRowNumbers(DependencyObject obj, bool value) => obj.SetValue(ShowRowNumbersProperty, value);

    public static string GetRowIndex(DependencyObject obj) => (string)obj.GetValue(RowIndexProperty);
    public static void SetRowIndex(DependencyObject obj, string value) => obj.SetValue(RowIndexProperty, value);

    private static readonly DependencyProperty CollectionHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(RowNumberHelper),
            new PropertyMetadata(null));

    private static void OnShowRowNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            listBox.Loaded += ListBox_Loaded;
            if (listBox.IsLoaded)
            {
                Attach(listBox);
                UpdateRowNumbers(listBox);
            }
        }
        else
        {
            listBox.Loaded -= ListBox_Loaded;
            Detach(listBox);
        }
    }

    private static void ListBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            Attach(listBox);
            UpdateRowNumbers(listBox);
        }
    }

    private static void Attach(ListBox listBox)
    {
        // Update whenever item containers finish generating / recycling
        listBox.ItemContainerGenerator.StatusChanged -= Generator_StatusChanged;
        listBox.ItemContainerGenerator.StatusChanged += Generator_StatusChanged;

        void Generator_StatusChanged(object? s, EventArgs e)
        {
            if (listBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                UpdateRowNumbers(listBox);
            }
        }

        // Update on scrolling
        listBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, (ScrollChangedEventHandler)ListBox_ScrollChanged);
        listBox.AddHandler(ScrollViewer.ScrollChangedEvent, (ScrollChangedEventHandler)ListBox_ScrollChanged);

        void ListBox_ScrollChanged(object s, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0 || e.ExtentHeightChange != 0)
            {
                UpdateRowNumbers(listBox);
            }
        }

        HookCollection(listBox);

        // Hook ItemsSource property changes
        var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        dpd?.RemoveValueChanged(listBox, OnItemsSourceChanged);
        dpd?.AddValueChanged(listBox, OnItemsSourceChanged);

        void OnItemsSourceChanged(object? s, EventArgs e)
        {
            HookCollection(listBox);
            listBox.Dispatcher.InvokeAsync(() => UpdateRowNumbers(listBox), DispatcherPriority.Loaded);
        }
    }

    private static void HookCollection(ListBox listBox)
    {
        // Unhook previous collection handler if any
        if (listBox.GetValue(CollectionHandlerProperty) is NotifyCollectionChangedEventHandler oldHandler &&
            listBox.ItemsSource is INotifyCollectionChanged oldNotify)
        {
            oldNotify.CollectionChanged -= oldHandler;
        }

        if (listBox.ItemsSource is INotifyCollectionChanged notify)
        {
            void CollectionChangedHandler(object? s, NotifyCollectionChangedEventArgs e)
            {
                // Dispatch after layout settles to guarantee container positions are updated
                listBox.Dispatcher.InvokeAsync(() => UpdateRowNumbers(listBox), DispatcherPriority.Loaded);
            }

            listBox.SetValue(CollectionHandlerProperty, (NotifyCollectionChangedEventHandler)CollectionChangedHandler);
            notify.CollectionChanged += CollectionChangedHandler;
        }
    }

    private static void Detach(ListBox listBox)
    {
        if (listBox.GetValue(CollectionHandlerProperty) is NotifyCollectionChangedEventHandler handler &&
            listBox.ItemsSource is INotifyCollectionChanged notify)
        {
            notify.CollectionChanged -= handler;
            listBox.ClearValue(CollectionHandlerProperty);
        }
    }

    public static void UpdateRowNumbers(ListBox listBox)
    {
        if (listBox == null || listBox.Items.Count == 0)
        {
            return;
        }

        var generator = listBox.ItemContainerGenerator;
        if (generator.Status != GeneratorStatus.ContainersGenerated)
        {
            return;
        }

        var scrollViewer = FindDescendant<ScrollViewer>(listBox);
        int start = 0;
        int end = listBox.Items.Count;

        if (scrollViewer != null && scrollViewer.ViewportHeight > 0)
        {
            int firstVisible = (int)Math.Floor(scrollViewer.VerticalOffset);
            int viewportCount = (int)Math.Ceiling(scrollViewer.ViewportHeight);
            start = Math.Max(0, firstVisible - 4);
            end = Math.Min(listBox.Items.Count, firstVisible + viewportCount + 6);
        }

        for (int i = start; i < end; i++)
        {
            if (generator.ContainerFromIndex(i) is ListBoxItem container)
            {
                var expected = $"#{i + 1}";
                if (GetRowIndex(container) != expected)
                {
                    SetRowIndex(container, expected);
                }
            }
        }
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindDescendant<T>(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
