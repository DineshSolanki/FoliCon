#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FoliCon.Views;
using Prism.Mvvm;

namespace FoliconTest;

/// <summary>
/// Smoke test for the Change Poster Icon Overlay view.
///
/// The overlay tiles are built from a DataTemplate whose accessible name is a MultiBinding over
/// a localized format string. A template's markup is not parsed until it is instantiated, so a
/// binding WPF rejects there compiles cleanly and only fails when the dialog is opened. This
/// forces that parse.
///
/// The ViewModel is deliberately not constructed: it reaches for the persistence tracker and the
/// global overlay provider, and this test is about the markup, not the data.
/// </summary>
[Collection(XamlLoadingCollection.Name)]
public class PosterIconConfigViewSmokeTests
{
    [Fact]
    public void PosterIconConfigView_LoadsAndItsOverlayTemplateParses()
    {
        using var host = new WpfTestHost();
        host.Invoke(() =>
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => new object());

            var view = new PosterIconConfig();
            var size = new Size(1266, 600);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            var items = FindTemplatedItemsControl(view);
            Assert.NotNull(items);

            // LoadContent() runs TemplateContent.ParseXaml() — the code path that rejects a
            // markup extension WPF does not accept in the position it was used.
            Assert.NotNull(items!.ItemTemplate.LoadContent());
        });
    }

    private static ItemsControl? FindTemplatedItemsControl(DependencyObject root)
    {
        if (root is ItemsControl { ItemTemplate: not null } found)
        {
            return found;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var result = FindTemplatedItemsControl(VisualTreeHelper.GetChild(root, i));
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
