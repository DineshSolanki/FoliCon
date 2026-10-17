#nullable enable
using FoliCon.ViewModels;

namespace FoliconTest;

public class CustomIconControlViewModelTests
{
    [Fact]
    public void InitialState_IsCorrect()
    {
        var vm = new CustomIconControlViewModel();

        Assert.False(vm.IsBusy);
        Assert.False(vm.StopSearch);
        Assert.False(vm.IsUndoEnable);
        Assert.Equal(0, vm.Index);
        Assert.Equal(0, vm.TotalIcons);
        Assert.Empty(vm.Directories);
        Assert.Empty(vm.Icons);
        Assert.NotNull(vm.Apply);
        Assert.NotNull(vm.StopSearchCommand);
        Assert.NotNull(vm.UndoIcons);
        Assert.NotNull(vm.ClearFoldersCommand);
        Assert.NotNull(vm.ClearIconsCommand);
    }

    [Fact]
    public void IsUndoEnable_CanBeToggled()
    {
        var vm = new CustomIconControlViewModel();
        Assert.False(vm.IsUndoEnable);

        vm.IsUndoEnable = true;
        Assert.True(vm.IsUndoEnable);

        vm.IsUndoEnable = false;
        Assert.False(vm.IsUndoEnable);
    }

    [Fact]
    public void StopSearchCommand_SetsStopSearchFlag()
    {
        var vm = new CustomIconControlViewModel();
        Assert.False(vm.StopSearch);

        vm.StopSearchCommand.Execute();

        Assert.True(vm.StopSearch);
    }

    [Fact]
    public void OnDialogClosed_ExecutesWithoutException()
    {
        var vm = new CustomIconControlViewModel();
        var ex = Record.Exception(() => vm.OnDialogClosed());

        Assert.Null(ex);
    }

    [Fact]
    public void IsBusy_CanBeToggled()
    {
        var vm = new CustomIconControlViewModel();
        Assert.False(vm.IsBusy);

        vm.IsBusy = true;
        Assert.True(vm.IsBusy);

        vm.IsBusy = false;
        Assert.False(vm.IsBusy);
    }
}
