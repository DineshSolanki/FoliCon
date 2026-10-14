#nullable enable
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using FoliCon.Modules.UI;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class RowNumberHelperTests
{
    [Fact]
    public void UpdateRowNumbers_AssignsCorrectSequentialRowNumbers()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var listBox = new ListBox
            {
                Width = 200,
                Height = 400
            };

            var items = new ObservableCollection<string>
            {
                "Movie A",
                "Movie B",
                "Movie C",
                "Movie D"
            };

            listBox.ItemsSource = items;
            RowNumberHelper.SetShowRowNumbers(listBox, true);

            var window = new Window { Content = listBox, Width = 300, Height = 500 };
            window.Show();

            listBox.UpdateLayout();
            RowNumberHelper.UpdateRowNumbers(listBox);

            var item0 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(0);
            var item1 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(1);
            var item2 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(2);
            var item3 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(3);

            Assert.Equal("#1", RowNumberHelper.GetRowIndex(item0));
            Assert.Equal("#2", RowNumberHelper.GetRowIndex(item1));
            Assert.Equal("#3", RowNumberHelper.GetRowIndex(item2));
            Assert.Equal("#4", RowNumberHelper.GetRowIndex(item3));

            window.Close();
        });
    }

    [Fact]
    public void UpdateRowNumbers_AccuratelyReassignsIndices_WhenItemIsMovedToTop()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var listBox = new ListBox
            {
                Width = 200,
                Height = 400
            };

            var items = new ObservableCollection<string>
            {
                "Agents of S.H.I.E.L.D.",
                "Aharen-san",
                "Airplane",
                "Alien Resurrection"
            };

            listBox.ItemsSource = items;
            RowNumberHelper.SetShowRowNumbers(listBox, true);

            var window = new Window { Content = listBox, Width = 300, Height = 500 };
            window.Show();

            listBox.UpdateLayout();
            RowNumberHelper.UpdateRowNumbers(listBox);

            // Reorder item 3 ("Alien Resurrection") to index 0 (top)
            items.Move(3, 0);

            listBox.UpdateLayout();
            RowNumberHelper.UpdateRowNumbers(listBox);

            var item0 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(0);
            var item1 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(1);
            var item2 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(2);
            var item3 = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(3);

            // First item (moved Alien Resurrection) MUST be #1, NOT #10000
            Assert.Equal("#1", RowNumberHelper.GetRowIndex(item0));
            Assert.Equal("Alien Resurrection", item0.Content);

            // Second item (Agents of S.H.I.E.L.D.) MUST be #2
            Assert.Equal("#2", RowNumberHelper.GetRowIndex(item1));
            Assert.Equal("Agents of S.H.I.E.L.D.", item1.Content);

            // Subsequent items must follow sequentially
            Assert.Equal("#3", RowNumberHelper.GetRowIndex(item2));
            Assert.Equal("#4", RowNumberHelper.GetRowIndex(item3));

            window.Close();
        });
    }
}
