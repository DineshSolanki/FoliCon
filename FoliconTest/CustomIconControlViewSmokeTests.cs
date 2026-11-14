#nullable enable
using System.Windows;
using FoliCon.Views;

namespace FoliconTest;

/// <summary>
/// Smoke test for the CustomIconControl view to guarantee that XAML parsing,
/// data templates, styles, and resources instantiate without exception.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class CustomIconControlViewSmokeTests
{
    [Fact]
    public void CustomIconControl_InstantiatesAndLoadsWithoutException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => new object());

            var view = new CustomIconControl();
            var size = new Size(880, 660);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            Assert.NotNull(view);
        });
    }
}
