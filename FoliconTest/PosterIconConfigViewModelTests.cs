#nullable enable
using FoliCon.ViewModels;
using Prism.Dialogs;
using System.Windows.Data;

namespace FoliconTest;

public class PosterIconConfigViewModelTests
{
    private sealed class TestDialogService : IDialogService
    {
        public string? LastShownDialogName { get; private set; }

        public void ShowDialog(string name, IDialogParameters parameters, DialogCallback callback)
        {
            _ = parameters;
            _ = callback;
            LastShownDialogName = name;
        }
    }

    [Fact]
    public void PosterIconConfigViewModel_InitializesWithOverlays()
    {
        var dialogService = new TestDialogService();
        var vm = new PosterIconConfigViewModel(dialogService);

        Assert.NotEmpty(vm.AvailableOverlays);
        Assert.NotNull(vm.Title);
        Assert.NotNull(vm.IconOverlay);
    }

    [Fact]
    public void IconOverlayChangedCommand_UpdatesSelectedOverlayAndActiveState()
    {
        var dialogService = new TestDialogService();
        var vm = new PosterIconConfigViewModel(dialogService);

        vm.IconOverlayChangedCommand.Execute("alternate");

        Assert.Equal("alternate", vm.IconOverlay);
        var alternateItem = vm.AvailableOverlays.FirstOrDefault(o => o.OverlayId == "alternate");
        if (alternateItem != null)
        {
            Assert.True(alternateItem.IsActive);
        }

        var otherItem = vm.AvailableOverlays.FirstOrDefault(o => o.OverlayId != "alternate");
        if (otherItem != null)
        {
            Assert.False(otherItem.IsActive);
        }
    }

    [Fact]
    public void SearchFilter_FiltersAvailableOverlays()
    {
        var dialogService = new TestDialogService();
        var vm = new PosterIconConfigViewModel(dialogService);

        var view = CollectionViewSource.GetDefaultView(vm.AvailableOverlays);
        Assert.NotNull(view);

        // Search for a specific known built-in overlay
        vm.SearchFilter = "dvd";
        var filteredList = view.Cast<OverlayItemViewModel>().ToList();
        Assert.All(filteredList, item =>
            Assert.True(
                item.DisplayName.Contains("dvd", StringComparison.OrdinalIgnoreCase) ||
                item.OverlayId.Contains("dvd", StringComparison.OrdinalIgnoreCase) ||
                item.Tags.Any(t => t.Contains("dvd", StringComparison.OrdinalIgnoreCase))));

        // Clear filter
        vm.SearchFilter = string.Empty;
        var fullList = view.Cast<OverlayItemViewModel>().ToList();
        Assert.Equal(vm.AvailableOverlays.Count, fullList.Count);
    }

    [Fact]
    public void CloseCommand_CanExecuteAndClosesDialog()
    {
        var dialogService = new TestDialogService();
        var vm = new PosterIconConfigViewModel(dialogService);

        Assert.True(vm.CloseCommand.CanExecute());
        Assert.True(vm.CanCloseDialog());
    }

    [Fact]
    public void StoreAndDesignerCommands_InvokeDialogService()
    {
        var dialogService = new TestDialogService();
        var vm = new PosterIconConfigViewModel(dialogService);

        vm.BrowseOverlayStoreCommand.Execute();
        Assert.Equal("OverlayStore", dialogService.LastShownDialogName);

        vm.CreateOverlayCommand.Execute();
        Assert.Equal("OverlayDesigner", dialogService.LastShownDialogName);
    }
}
