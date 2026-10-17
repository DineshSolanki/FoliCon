#nullable enable
using System.Windows.Controls.Primitives;
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

    private sealed class ListBoxAttachment
    {
        public EventHandler? GeneratorStatusChanged { get; set; }
        public ScrollChangedEventHandler? ScrollChanged { get; set; }
        public EventHandler? ItemsSourceChanged { get; set; }
        public NotifyCollectionChangedEventHandler? CollectionChanged { get; set; }
        public INotifyCollectionChanged? SubscribedCollection { get; set; }
    }

    private static readonly DependencyProperty AttachmentProperty =
        DependencyProperty.RegisterAttached(
            "Attachment",
            typeof(ListBoxAttachment),
            typeof(RowNumberHelper),
            new PropertyMetadata(null));

    private static void OnShowRowNumbersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
        {
            return;
        }

        listBox.Loaded -= ListBox_Loaded;

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
        Detach(listBox);

        var attachment = new ListBoxAttachment();
        listBox.SetValue(AttachmentProperty, attachment);

        attachment.GeneratorStatusChanged = (_, _) =>
        {
            if (listBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                UpdateRowNumbers(listBox);
            }
        };
        listBox.ItemContainerGenerator.StatusChanged += attachment.GeneratorStatusChanged;

        attachment.ScrollChanged = (_, e) =>
        {
            if (e.VerticalChange != 0 || e.ExtentHeightChange != 0)
            {
                UpdateRowNumbers(listBox);
            }
        };
        listBox.AddHandler(ScrollViewer.ScrollChangedEvent, attachment.ScrollChanged);

        var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        if (dpd != null)
        {
            attachment.ItemsSourceChanged = (_, _) =>
            {
                HookCollection(listBox, attachment);
                listBox.Dispatcher.InvokeAsync(() => UpdateRowNumbers(listBox), DispatcherPriority.Loaded);
            };
            dpd.AddValueChanged(listBox, attachment.ItemsSourceChanged);
        }

        HookCollection(listBox, attachment);
    }

    private static void HookCollection(ListBox listBox, ListBoxAttachment attachment)
    {
        if (attachment.SubscribedCollection != null && attachment.CollectionChanged != null)
        {
            attachment.SubscribedCollection.CollectionChanged -= attachment.CollectionChanged;
            attachment.SubscribedCollection = null;
            attachment.CollectionChanged = null;
        }

        if (listBox.ItemsSource is INotifyCollectionChanged notify)
        {
            attachment.CollectionChanged = (_, _) =>
            {
                listBox.Dispatcher.InvokeAsync(() => UpdateRowNumbers(listBox), DispatcherPriority.Loaded);
            };
            attachment.SubscribedCollection = notify;
            notify.CollectionChanged += attachment.CollectionChanged;
        }
    }

    private static void Detach(ListBox listBox)
    {
        if (listBox.GetValue(AttachmentProperty) is not ListBoxAttachment attachment)
        {
            return;
        }

        if (attachment.GeneratorStatusChanged != null)
        {
            listBox.ItemContainerGenerator.StatusChanged -= attachment.GeneratorStatusChanged;
        }

        if (attachment.ScrollChanged != null)
        {
            listBox.RemoveHandler(ScrollViewer.ScrollChangedEvent, attachment.ScrollChanged);
        }

        if (attachment.ItemsSourceChanged != null)
        {
            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
            dpd?.RemoveValueChanged(listBox, attachment.ItemsSourceChanged);
        }

        if (attachment.SubscribedCollection != null && attachment.CollectionChanged != null)
        {
            attachment.SubscribedCollection.CollectionChanged -= attachment.CollectionChanged;
        }

        listBox.ClearValue(AttachmentProperty);
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
