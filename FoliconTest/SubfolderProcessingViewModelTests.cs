#nullable enable
using FoliCon.Models.Data;
using FoliCon.ViewModels;

namespace FoliconTest;

public class SubfolderProcessingViewModelTests
{
    [Fact]
    public void AddPattern_ValidRegex_AppendsToPatternsList()
    {
        var vm = new SubfolderProcessingViewModel();
        var initialCount = vm.PatternsList.Count;

        vm.AddCommand.Execute(@"^Season \d+$");
        Assert.Equal(initialCount + 1, vm.PatternsList.Count);

        var added = vm.PatternsList.Last();
        Assert.Equal(@"^Season \d+$", added.Regex);
        Assert.True(added.IsEnabled);
        Assert.False(added.IsReadOnly);

        // Clean up
        vm.RemoveCommand.Execute(added);
        Assert.Equal(initialCount, vm.PatternsList.Count);
    }

    [Fact]
    public void RemovePattern_RemovesSpecifiedPattern()
    {
        var vm = new SubfolderProcessingViewModel();
        var pattern = new Pattern(@"^TestExtra$", true, false);
        vm.PatternsList.Add(pattern);

        Assert.Contains(pattern, vm.PatternsList);

        vm.RemoveCommand.Execute(pattern);
        Assert.DoesNotContain(pattern, vm.PatternsList);
    }

    [Fact]
    public void RemovePattern_ReadOnlyPattern_IsNotRemoved()
    {
        var vm = new SubfolderProcessingViewModel();
        var readOnlyPattern = vm.PatternsList.FirstOrDefault(p => p.IsReadOnly);
        Assert.NotNull(readOnlyPattern);

        var initialCount = vm.PatternsList.Count;
        vm.RemoveCommand.Execute(readOnlyPattern);

        Assert.Equal(initialCount, vm.PatternsList.Count);
        Assert.Contains(readOnlyPattern, vm.PatternsList);
    }
}
