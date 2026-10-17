#nullable enable
using FoliCon.Modules.Overlays;
using FoliCon.ViewModels;
using System.Windows.Media;

namespace FoliconTest;

public class PreviewerViewModelTests
{
    [Fact]
    public void CanvasTheme_ChangesUpdatesCanvasBrush()
    {
        var vm = new PreviewerViewModel();
        Assert.Equal("Theme", vm.CanvasTheme);

        vm.SelectCanvasThemeCommand.Execute("Dark");
        Assert.Equal("Dark", vm.CanvasTheme);
        Assert.IsType<SolidColorBrush>(vm.CanvasBrush);

        vm.SelectCanvasThemeCommand.Execute("Light");
        Assert.Equal("Light", vm.CanvasTheme);

        vm.SelectCanvasThemeCommand.Execute("Folder");
        Assert.Equal("Folder", vm.CanvasTheme);

        vm.SelectCanvasThemeCommand.Execute("Theme");
        Assert.Equal("Theme", vm.CanvasTheme);
    }

    [Fact]
    public void InspectItem_CanOpenAndClose()
    {
        var vm = new PreviewerViewModel();
        Assert.False(vm.IsInspecting);
        Assert.Null(vm.InspectedItem);

        var dummyBmp = new System.Windows.Media.Imaging.BitmapImage();
        var item = new OverlayPreviewItem("test-overlay", "Test Overlay", dummyBmp);

        vm.InspectItemCommand.Execute(item);
        Assert.True(vm.IsInspecting);
        Assert.Same(item, vm.InspectedItem);

        vm.CloseInspectCommand.Execute();
        Assert.False(vm.IsInspecting);
        Assert.Null(vm.InspectedItem);
    }

    [Fact]
    public void ResetPoster_ClearsSelectedPoster()
    {
        var vm = new PreviewerViewModel();
        vm.SelectedPosterPath = @"C:\Test\MyPoster.jpg";
        Assert.True(vm.HasCustomPoster);
        Assert.Equal("MyPoster.jpg", vm.PosterFileName);

        vm.ResetPosterCommand.Execute();
        Assert.False(vm.HasCustomPoster);
        Assert.Equal("Sample Poster", vm.PosterFileName);
    }
}
