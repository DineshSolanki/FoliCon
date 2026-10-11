#nullable enable
using System.Windows;
using FoliCon.Views;
using Prism.Mvvm;

namespace FoliconTest;

/// <summary>
/// Smoke test for the SubfolderProcessing view to guarantee that XAML parsing,
/// DataGrid columns, styles, and resources instantiate without exception.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public class SubfolderProcessingViewSmokeTests
{
    [Fact]
    public void SubfolderProcessing_InstantiatesAndLoadsWithoutException()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => new object());

            var view = new SubfolderProcessing();
            var size = new Size(760, 620);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            Assert.NotNull(view);
        });
    }
}
