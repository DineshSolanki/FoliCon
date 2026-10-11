#nullable enable
using System.Windows;
using FoliCon.Views;
using Prism.Mvvm;

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
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => new object());

            var view = new AboutBox();
            var size = new Size(540, 400);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            Assert.NotNull(view);
        });
    }
}
