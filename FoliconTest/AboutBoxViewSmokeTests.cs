#nullable enable
using System.Windows;
using FoliCon.ViewModels;
using FoliCon.Views;

namespace FoliconTest;

/// <summary>
/// Smoke test for AboutBox view to guarantee that XAML parsing,
/// vector geometries, styles, and resources instantiate without exception.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class AboutBoxViewSmokeTests
{
    [Fact]
    public void AboutBox_InstantiatesAndLoadsWithoutException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var viewModel = new AboutBoxViewModel();
            var view = new AboutBox { DataContext = viewModel };
            var size = new Size(540, 400);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            Assert.False(string.IsNullOrWhiteSpace(AboutBoxViewModel.VersionTag));
            Assert.NotNull(view.Content);
        });
    }
}
