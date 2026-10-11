#nullable enable
using System.Windows;
using FoliCon.Views;
using Prism.Mvvm;

namespace FoliconTest;

/// <summary>
/// Smoke test for the Previewer view to guarantee that XAML parsing,
/// data templates, styles, and resources instantiate without exception.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class PreviewerViewSmokeTests
{
    [Fact]
    public void Previewer_InstantiatesAndLoadsWithoutException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => new object());

            var view = new Previewer();
            var size = new Size(1120, 750);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            Assert.NotNull(view);
        });
    }
}
