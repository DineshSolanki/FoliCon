#nullable enable
using System.IO;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using FoliCon.ViewModels;
using Xunit;

namespace FoliconTest;

public class CrimsonNoirCommunityThemeTests
{
    [Fact]
    public void CrimsonNoir_StorageServiceDiscoversLocalUserTheme()
    {
        var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var themeFile = Path.Combine(appData, "FoliCon", "Themes", "crimson-noir", "theme.json");

        Assert.True(File.Exists(themeFile), $"Theme file should exist at: {themeFile}");

        var parser = new HybridThemeParser();
        var storage = new ThemeStorageService(parser);

        var manifest = storage.GetThemeById("crimson-noir");
        Assert.NotNull(manifest);
        Assert.Equal("Crimson Noir", manifest.Name);
        Assert.Equal("FoliCon Community", manifest.Author);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.False(manifest.IsBuiltIn);
        Assert.True(manifest.IsDark);
        Assert.Equal("#E11D48", manifest.Colors.Primary);
    }

    [Fact]
    public void CrimsonNoir_HybridParserSynthesizesValidWpfResourceDictionary()
    {
        var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var json = File.ReadAllText(Path.Combine(appData, "FoliCon", "Themes", "crimson-noir", "theme.json"));

        var parser = new HybridThemeParser();
        var manifest = parser.Parse(json);

        Assert.NotNull(manifest);

        WpfTestHost.Invoke(() =>
        {
            var dict = parser.SynthesizeResourceDictionary(manifest);
            Assert.NotNull(dict);

            // Assert core brushes are created and frozen
            var regionBrush = dict["RegionBrush"] as System.Windows.Media.SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.True(regionBrush.IsFrozen);
            Assert.Equal(0x0F, regionBrush.Color.R);
            Assert.Equal(0x0E, regionBrush.Color.G);
            Assert.Equal(0x11, regionBrush.Color.B);

            var primaryBrush = dict["PrimaryBrush"] as System.Windows.Media.SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.True(primaryBrush.IsFrozen);
            Assert.Equal(0xE1, primaryBrush.Color.R);
            Assert.Equal(0x1D, primaryBrush.Color.G);
            Assert.Equal(0x48, primaryBrush.Color.B);

            var heroGrad = dict["HeroCardBackgroundBrush"] as System.Windows.Media.LinearGradientBrush;
            Assert.NotNull(heroGrad);
            Assert.True(heroGrad.IsFrozen);
            Assert.Equal(2, heroGrad.GradientStops.Count);

            var actionGrad = dict["PrimaryActionGradientBrush"] as System.Windows.Media.LinearGradientBrush;
            Assert.NotNull(actionGrad);
            Assert.True(actionGrad.IsFrozen);
        });
    }

    [Fact]
    public void CrimsonNoir_CanBeAppliedToMainWindowViewModel()
    {
        var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        var storage = new ThemeStorageService(new HybridThemeParser());
        var parser = new HybridThemeParser();

        var manifest = storage.GetThemeById("crimson-noir");
        Assert.NotNull(manifest);

        WpfTestHost.Invoke(() =>
        {
            var originalSnapshot = MainWindowViewModel.CaptureThemeSnapshot();
            try
            {
                MainWindowViewModel.ApplyThemeManifest(manifest, parser);

                Assert.Equal("crimson-noir", MainWindowViewModel.ActiveThemeId);
                Assert.NotNull(MainWindowViewModel.ActiveThemeManifest);
                Assert.Equal("Crimson Noir", MainWindowViewModel.ActiveThemeManifest.Name);
            }
            finally
            {
                MainWindowViewModel.RestoreThemeSnapshot(originalSnapshot, parser);
            }
        });
    }
}
