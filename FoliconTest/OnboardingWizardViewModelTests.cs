#nullable enable
using FoliCon.Properties.Langs;
using FoliCon.ViewModels;

namespace FoliconTest;

public class OnboardingWizardViewModelTests
{
    [Fact]
    public void InitialState_StartsAtWelcomeStep()
    {
        var vm = new OnboardingWizardViewModel();

        Assert.Equal(0, vm.CurrentStep);
        Assert.True(vm.IsWelcomeStep);
        Assert.False(vm.IsTmdbStep);
        Assert.False(vm.IsIgdbStep);
        Assert.False(vm.IsDeviantArtStep);
        Assert.False(vm.IsSummaryStep);
        Assert.False(vm.CanGoBack);
        Assert.False(vm.CanGoNext);
        Assert.False(vm.CanSkip);
    }

    [Fact]
    public void StepProgression_ReachesSummaryStep_AndHasCorrectFlags()
    {
        var vm = new OnboardingWizardViewModel();

        // Step 0 -> Step 1 (TMDB)
        vm.NextCommand.Execute();
        Assert.Equal(1, vm.CurrentStep);
        Assert.True(vm.IsTmdbStep);
        Assert.True(vm.CanGoBack);
        Assert.True(vm.CanGoNext);
        Assert.True(vm.CanSkip);
        Assert.Equal(Lang.OnboardingNext, vm.NextButtonText);

        // Step 1 -> Step 2 (IGDB)
        vm.NextCommand.Execute();
        Assert.Equal(2, vm.CurrentStep);
        Assert.True(vm.IsIgdbStep);
        Assert.True(vm.CanGoBack);
        Assert.True(vm.CanGoNext);
        Assert.True(vm.CanSkip);
        Assert.Equal(Lang.OnboardingNext, vm.NextButtonText);

        // Step 2 -> Step 3 (DeviantArt)
        vm.NextCommand.Execute();
        Assert.Equal(3, vm.CurrentStep);
        Assert.True(vm.IsDeviantArtStep);
        Assert.True(vm.CanGoBack);
        Assert.True(vm.CanGoNext);
        Assert.True(vm.CanSkip);
        Assert.Equal(Lang.OnboardingNext, vm.NextButtonText);

        // Step 3 -> Step 4 (Summary)
        vm.NextCommand.Execute();
        Assert.Equal(4, vm.CurrentStep);
        Assert.True(vm.IsSummaryStep);
        Assert.True(vm.CanGoBack);
        Assert.True(vm.CanGoNext);
        Assert.False(vm.CanSkip);
        Assert.Equal(Lang.OnboardingFinish, vm.NextButtonText);

        // Back from Step 4 -> Step 3
        vm.BackCommand.Execute();
        Assert.Equal(3, vm.CurrentStep);
        Assert.True(vm.IsDeviantArtStep);
        Assert.True(vm.CanSkip);
    }

    [Fact]
    public void SkipCommand_AdvancesToSummary_AndStopsAtSummary()
    {
        var vm = new OnboardingWizardViewModel
        {
            CurrentStep = 1
        };

        Assert.Equal(1, vm.CurrentStep);

        vm.SkipCommand.Execute();
        Assert.Equal(2, vm.CurrentStep);

        vm.SkipCommand.Execute();
        Assert.Equal(3, vm.CurrentStep);

        vm.SkipCommand.Execute();
        Assert.Equal(4, vm.CurrentStep);
        Assert.True(vm.IsSummaryStep);
        Assert.False(vm.CanSkip);

        // Attempting to skip on summary does not advance beyond step 4
        vm.SkipCommand.Execute();
        Assert.Equal(4, vm.CurrentStep);
    }
}
