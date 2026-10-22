using System.Globalization;
using System.Linq;
using System.Threading;
using FoliCon.Models.Enums;
using FoliCon.Models.Localization;
using FoliCon.Modules.utils;
using FoliCon.Properties.Langs;
using FoliCon.ViewModels;
using Xunit;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class LanguageSelectionTests
{
    [Fact]
    public void LanguageOption_All_ContainsAllSixSupportedLanguages()
    {
        var options = LanguageOption.All;

        Assert.Equal(6, options.Count);
        Assert.Contains(options, o => o.Language == Languages.English && o.Code == "EN");
        Assert.Contains(options, o => o.Language == Languages.Spanish && o.Code == "ES");
        Assert.Contains(options, o => o.Language == Languages.Arabic && o.Code == "AR");
        Assert.Contains(options, o => o.Language == Languages.Russian && o.Code == "RU");
        Assert.Contains(options, o => o.Language == Languages.Hindi && o.Code == "HI");
        Assert.Contains(options, o => o.Language == Languages.Portuguese && o.Code == "PT");
    }

    [Fact]
    public void LanguageOption_EqualityAndToString_WorkCorrectly()
    {
        var en1 = LanguageOption.All.First(o => o.Language == Languages.English);
        var en2 = new LanguageOption(Languages.English, "EN", "English", "English", "en-US");
        var es = LanguageOption.All.First(o => o.Language == Languages.Spanish);

        Assert.Equal(en1, en2);
        Assert.NotEqual(en1, es);
        Assert.Equal("English", en1.ToString());
    }

    [Fact]
    public void MainWindowViewModel_InitializesAvailableLanguages_AndSyncsSelectedLanguageOption()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            try
            {
                var vm = new MainWindowViewModel(null!);

                Assert.Equal(6, vm.AvailableLanguages.Count);
                Assert.NotNull(vm.SelectedLanguageOption);
                Assert.Equal(vm.AppLanguage, vm.SelectedLanguageOption!.Language);

                // Switching SelectedLanguageOption updates AppLanguage
                var hindiOption = vm.AvailableLanguages.First(l => l.Language == Languages.Hindi);
                vm.SelectedLanguageOption = hindiOption;
                Assert.Equal(Languages.Hindi, vm.AppLanguage);

                // Switching AppLanguage updates SelectedLanguageOption
                vm.AppLanguage = Languages.Spanish;
                Assert.Equal(Languages.Spanish, vm.SelectedLanguageOption.Language);
                Assert.Equal("Español", vm.SelectedLanguageOption.DisplayName);
            }
            finally
            {
                var enCulture = CultureUtils.GetCultureInfoByLanguage(Languages.English);
                LangProvider.Culture = enCulture;
                Thread.CurrentThread.CurrentCulture = enCulture;
                Thread.CurrentThread.CurrentUICulture = enCulture;
            }
        });
    }
}
