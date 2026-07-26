#nullable enable
using System.IO;
using System.Windows;
using FoliCon.Models.Data;
using FoliCon.Modules.Overlays;
using FoliCon.Modules.Overlays.Designer;
using FoliCon.ViewModels;
using FoliCon.Views;
using Prism.Dialogs;
using Prism.Mvvm;

namespace FoliconTest;

/// <summary>
/// Smoke test for the OverlayDesigner view.
///
/// The designer's markup carries the constructs a compile cannot check: MultiBindings that
/// compose a localized format string with a bound value, and Style setters whose Value is a
/// resource binding. Those only fail when the BAML is actually loaded and laid out, so this
/// exercises both the first-run picker and the editor surface with a real document.
/// </summary>
[Collection(XamlLoadingCollection.Name)]
public class OverlayDesignerViewSmokeTests : IDisposable
{
    private readonly WpfTestHost _host = new();
    private readonly string _tempDir;
    private readonly StubDesignerOverlayProvider _provider = new();

    public OverlayDesignerViewSmokeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FoliconDesignerView_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _host.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void DesignerView_LoadsAndLaysOutBothTheePickerAndTheEditor()
    {
        _host.Invoke(() =>
        {
            var viewModel = new OverlayDesignerViewModel(
                new DialogCloseListener(),
                _provider,
                new OverlayTemplateProvider(_provider),
                new OverlayPackageLoader(),
                // A long debounce keeps background renders out of the layout pass.
                new OverlayDesignerPreviewRenderer(TimeSpan.FromSeconds(30)),
                new OverlayDraftStore(Path.Combine(_tempDir, "drafts")));

            // The ViewModel renders template thumbnails on the process-wide StaRenderer that the
            // parity tests also queue work on. Disposing it stops that loop and tears down its
            // preview renderer, so this test cannot leave renders running into another class.
            try
            {
                ViewModelLocationProvider.SetDefaultViewModelFactory(_ => viewModel);

                var view = new OverlayDesigner { DataContext = viewModel };
                var size = new Size(1180, 760);

                // First-run picker: template cards and the drafts list.
                viewModel.OnDialogOpened(new DialogParameters());
                Layout(view, size);

                // Editor surface: element rail, property panels, validation list, action bar.
                viewModel.LoadPackage(WriteOverlayPackage());
                Assert.True(viewModel.HasDocument);
                Layout(view, size);

                // The submission panel is collapsed until an export succeeds, so its bindings —
                // including the one-sentence "copy the folder into {path}" — never lay out above.
                Assert.False(string.IsNullOrWhiteSpace(viewModel.SubmissionTargetPath));
            }
            finally
            {
                viewModel.Dispose();
            }
        });
    }

    private static void Layout(FrameworkElement view, Size size)
    {
        view.Measure(size);
        view.Arrange(new Rect(size));
        view.UpdateLayout();
    }

    /// <summary>Writes a minimal but valid package the designer can open.</summary>
    private string WriteOverlayPackage()
    {
        var folder = Path.Combine(_tempDir, "sample");
        Directory.CreateDirectory(folder);

        var definition = new PosterOverlayDefinition
        {
            Id = "sample",
            DisplayName = "Sample",
            Author = "Tester",
            OverlayVersion = "1.0.0",
            Tags = ["alpha"],
            DesignWidth = 256,
            DesignHeight = 256,
            RenderWidth = 256,
            RenderHeight = 256,
            RootMargin = "0,0,0,0",
            LayerOrder = ["poster", "rating", "title"],
            Poster = new PosterConfig { Margin = "0,0,0,0", ClipRadius = "0" },
            Rating = new RatingConfig
            {
                ShieldMargin = "0,0,0,0",
                TextMargin = "0,0,0,0",
                FontSize = 25,
                FontFamily = "Segoe UI"
            },
            Title = new TitleConfig { IsVisible = true, RotationOrigin = "0.5,0.5" }
        };

        var path = Path.Combine(folder, "overlay.json");
        File.WriteAllText(path,
            Newtonsoft.Json.JsonConvert.SerializeObject(definition, Newtonsoft.Json.Formatting.Indented));
        return path;
    }

    /// <summary>Keeps the ViewModel away from the user's real %AppData% overlay folder.</summary>
    private sealed class StubDesignerOverlayProvider : IOverlayProvider
    {
        private readonly List<PosterOverlayDefinition> _overlays =
        [
            new()
            {
                Id = "stub-template",
                DisplayName = "Stub Template",
                Author = "FoliCon",
                OverlayVersion = "1.0.0",
                IsBuiltIn = true,
                Poster = new PosterConfig { Margin = "10,10,10,10", ClipRadius = "0" },
                Rating = new RatingConfig(),
                Title = new TitleConfig()
            }
        ];

        public IReadOnlyList<PosterOverlayDefinition> GetAllOverlays() => _overlays;

        public IReadOnlyList<PosterOverlayDefinition> GetUserOverlays() => [];

        public PosterOverlayDefinition? GetOverlayById(string id) =>
            _overlays.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

        public PosterOverlayDefinition ResolveActiveOverlayOrDefault(string? activeOverlayId) => _overlays[0];

        public bool IsOverlayInstalled(string id) => GetOverlayById(id) != null;

        public string GetOverlayFolderPath(string id) => Path.Combine(Path.GetTempPath(), id);

        public void Refresh()
        {
        }
    }
}
