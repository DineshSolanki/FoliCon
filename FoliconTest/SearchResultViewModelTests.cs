#nullable enable
using FoliCon.ViewModels;

namespace FoliconTest;

public class SearchResultViewModelTests
{
    [Fact]
    public void InitialState_IsCorrect()
    {
        var vm = new SearchResultViewModel(null!);

        Assert.False(vm.IsBusy);
        Assert.False(vm.IsSearchFocused);
        Assert.Equal("Default", vm.CurrentSort);
        Assert.NotNull(vm.ResultListViewData);
        Assert.NotNull(vm.ResultListViewData.Data);
        Assert.Empty(vm.ResultListViewData.Data);
        Assert.NotNull(vm.SearchAgainCommand);
        Assert.NotNull(vm.SkipCommand);
        Assert.NotNull(vm.SkipAllCommand);
        Assert.NotNull(vm.PickCommand);
        Assert.NotNull(vm.SortCommand);
    }

    [Fact]
    public async Task ExecuteFallbackSearchAsync_WhenExceptionOccurs_CatchesAndFocusesSearchInput()
    {
        var vm = new SearchResultViewModel(null!);
        Assert.False(vm.IsSearchFocused);

        // StartSearch will throw NullReferenceException because _tmdbObject is null.
        // ExecuteFallbackSearchAsync should catch, log, and surface by setting IsSearchFocused = true.
        var ex = await Record.ExceptionAsync(() => vm.ExecuteFallbackSearchAsync("Inception"));

        Assert.Null(ex);
        Assert.True(vm.IsSearchFocused);
    }
}
