#nullable enable
using System.Windows;
using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using FoliCon.ViewModels;
using FoliCon.Views;
using Prism.Mvvm;

namespace FoliconTest;

/// <summary>
/// Smoke test for the OverlayStore view: instantiates the compiled XAML with the
/// same application-level resources FoliCon merges at startup and lays out every
/// section, so a missing StaticResource key or broken template binding fails here
/// instead of at runtime.
/// </summary>
[Collection(XamlLoadingCollection.Name)]
public class OverlayStoreViewSmokeTests
{
    [Fact]
    public async Task OverlayStoreView_LoadsAndLaysOutAllSections()
    {
        var entries = new List<OverlayCatalogEntry>
        {
            CreateEntry("one", "One", "Alice"),
            CreateEntry("two", "Two", "Bob")
        };
        var service = new StubRepositoryService(entries);
        service.MarkInstalled("one");
        service.MarkUpdateAvailable("one", "2.0.0");

        using var host = new WpfTestHost();
        var vm = host.Invoke(() => OverlayStoreViewModel.Create(service, _ => false));
        await vm.CatalogLoaded;

        host.Invoke(() =>
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory(_ => vm);

            var view = new OverlayStore { DataContext = vm };
            var size = new Size(900, 650);

            foreach (var section in Enum.GetValues<OverlayStoreSection>())
            {
                vm.CurrentSection = section;
                view.Measure(size);
                view.Arrange(new Rect(size));
                view.UpdateLayout();
            }
        });

        Assert.False(vm.HasError, vm.ErrorMessage);
    }

    private static OverlayCatalogEntry CreateEntry(string id, string name, string author) => new()
    {
        Id = id,
        DisplayName = name,
        Author = author,
        OverlayVersion = "1.0.0",
        Tags = ["tag"],
        SizeBytes = 1000,
        Description = $"Description for {name}",
        PreviewUrl = $"https://example.com/{id}/preview.png",
        OverlayBaseUrl = "https://example.com/overlays",
        OverlayPath = id
    };
}
