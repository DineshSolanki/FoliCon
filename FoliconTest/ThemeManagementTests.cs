#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FoliCon.Models.Themes;
using FoliCon.Modules.Themes;
using System.Windows;
using System.Windows.Media;
using FoliCon.Models.Enums;
using FoliCon.ViewModels;
using HandyControl.Themes;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class ThemeManagementTests
{
    [Fact]
    public void ThemeTokyoNeon_DictionaryLoadsAndContainsRequiredBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var uri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeTokyoNeon.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            Assert.NotNull(dict);

            // Verify Tokyo Neon Cyberpunk Palette
            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.Equal(Color.FromRgb(0x0B, 0x0E, 0x17), regionBrush.Color);

            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.Equal(Color.FromRgb(0x08, 0x83, 0x95), primaryBrush.Color);

            var accentBrush = dict["AccentBrush"] as SolidColorBrush;
            Assert.NotNull(accentBrush);
            Assert.Equal(Color.FromRgb(0xFF, 0x2A, 0x85), accentBrush.Color);

            var primaryTextBrush = dict["PrimaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(primaryTextBrush);
            Assert.Equal(Color.FromRgb(0xF8, 0xFA, 0xFC), primaryTextBrush.Color);
        });
    }

    [Fact]
    public void ThemeNordicPine_DictionaryLoadsAndContainsRequiredBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var uri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeNordicPine.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            Assert.NotNull(dict);

            // Verify Nordic Pine Emerald Abyssal Palette
            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.Equal(Color.FromRgb(0x0C, 0x16, 0x13), regionBrush.Color);

            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.Equal(Color.FromRgb(0x10, 0xB9, 0x81), primaryBrush.Color);

            var accentBrush = dict["AccentBrush"] as SolidColorBrush;
            Assert.NotNull(accentBrush);
            Assert.Equal(Color.FromRgb(0x6E, 0xE7, 0xB7), accentBrush.Color);

            var primaryTextBrush = dict["PrimaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(primaryTextBrush);
            Assert.Equal(Color.FromRgb(0xEC, 0xFD, 0xF5), primaryTextBrush.Color);
        });
    }

    [Fact]
    public void ThemeCinema35mm_DictionaryLoadsAndContainsRequiredBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var uri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeCinema35mm.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            Assert.NotNull(dict);

            // Verify Cinema 35mm Golden Hour Palette
            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.Equal(Color.FromRgb(0x14, 0x12, 0x10), regionBrush.Color);

            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.Equal(Color.FromRgb(0xF5, 0x9E, 0x0B), primaryBrush.Color);

            var accentBrush = dict["AccentBrush"] as SolidColorBrush;
            Assert.NotNull(accentBrush);
            Assert.Equal(Color.FromRgb(0xFC, 0xD3, 0x4D), accentBrush.Color);

            var primaryTextBrush = dict["PrimaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(primaryTextBrush);
            Assert.Equal(Color.FromRgb(0xFF, 0xFB, 0xEB), primaryTextBrush.Color);
        });
    }

    [Fact]
    public void ThemeTheButterfly_DictionaryLoadsAndContainsRequiredBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var uri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeTheButterfly.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            Assert.NotNull(dict);

            // Verify Midnight Violet Brand Palette
            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.Equal(Color.FromRgb(0x17, 0x15, 0x2B), regionBrush.Color);

            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.Equal(Color.FromRgb(0x76, 0x51, 0xE8), primaryBrush.Color);

            var secondaryTextBrush = dict["SecondaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(secondaryTextBrush);
            Assert.Equal(Color.FromRgb(0xB6, 0x9A, 0xFF), secondaryTextBrush.Color);

            var primaryTextBrush = dict["PrimaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(primaryTextBrush);
            Assert.Equal(Color.FromRgb(0xF5, 0xF3, 0xFF), primaryTextBrush.Color);
        });
    }

    [Fact]
    public void ThemeMicaDarkObsidian_DictionaryLoadsAndContainsRequiredBrushes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var uri = new Uri("pack://application:,,,/FoliCon;component/XamlResources/ThemeMicaDarkObsidian.xaml", UriKind.Absolute);
            var dict = new ResourceDictionary { Source = uri };

            Assert.NotNull(dict);

            // Verify Mica Dark Obsidian Palette
            var regionBrush = dict["RegionBrush"] as SolidColorBrush;
            Assert.NotNull(regionBrush);
            Assert.Equal(Color.FromRgb(0x09, 0x0D, 0x16), regionBrush.Color);

            var primaryBrush = dict["PrimaryBrush"] as SolidColorBrush;
            Assert.NotNull(primaryBrush);
            Assert.Equal(Color.FromRgb(0x8B, 0x5C, 0xF6), primaryBrush.Color);

            var primaryTextBrush = dict["PrimaryTextBrush"] as SolidColorBrush;
            Assert.NotNull(primaryTextBrush);
            Assert.Equal(Color.FromRgb(0xF8, 0xFA, 0xFC), primaryTextBrush.Color);
        });
    }

    [Fact]
    public void ApplyAppTheme_TogglesAllThemesSuccessfully()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            // Apply Butterfly theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.TheButterfly);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Apply Tokyo Neon theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.TokyoNeon);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Apply Nordic Pine theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.NordicPine);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Apply Cinema 35mm theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Cinema35mm);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Apply Mica Dark Obsidian theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.MicaDarkObsidian);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Revert to System theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.System);
            Assert.True(ThemeManager.Current.UsingSystemTheme);

            // Apply Light theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Light);
            Assert.Equal(ApplicationTheme.Light, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);

            // Apply Dark theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Dark);
            Assert.Equal(ApplicationTheme.Dark, ThemeManager.Current.ApplicationTheme);
            Assert.False(ThemeManager.Current.UsingSystemTheme);
        });
    }

    [Theory]
    [InlineData(FoliconThemes.Light, false)]
    [InlineData(FoliconThemes.Dark, true)]
    [InlineData(FoliconThemes.TheButterfly, true)]
    [InlineData(FoliconThemes.TokyoNeon, true)]
    [InlineData(FoliconThemes.MicaDarkObsidian, true)]
    [InlineData(FoliconThemes.NordicPine, true)]
    [InlineData(FoliconThemes.Cinema35mm, true)]
    public void IsDarkTheme_ReturnsExpectedValueForExplicitThemes(FoliconThemes theme, bool expected)
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var result = MainWindowViewModel.IsDarkTheme(theme);
            Assert.Equal(expected, result);
        });
    }

    [Fact]
    public void ApplyAppTheme_SynchronizesWindowBackdropBrush_AcrossThemes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            // 1. Light Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Light);
            Assert.False(MainWindowViewModel.IsCurrentThemeDark());
            var lightBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(lightBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF), lightBrush.Color);

            // 2. Dark Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Dark);
            Assert.True(MainWindowViewModel.IsCurrentThemeDark());
            var darkBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(darkBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0x1C, 0x1C, 0x1C), darkBrush.Color);

            // 3. Custom Cinema 35mm Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Cinema35mm);
            Assert.True(MainWindowViewModel.IsCurrentThemeDark());
            var cinemaBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(cinemaBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0x14, 0x12, 0x10), cinemaBrush.Color);

            // 4. Custom Tokyo Neon Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.TokyoNeon);
            Assert.True(MainWindowViewModel.IsCurrentThemeDark());
            var tokyoBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(tokyoBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0x0B, 0x0E, 0x17), tokyoBrush.Color);

            // 5. Custom Mica Dark Obsidian Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.MicaDarkObsidian);
            Assert.True(MainWindowViewModel.IsCurrentThemeDark());
            var obsidianBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(obsidianBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0x09, 0x0D, 0x16), obsidianBrush.Color);

            // 6. Revert to Light Theme
            MainWindowViewModel.ApplyAppTheme(FoliconThemes.Light);
            Assert.False(MainWindowViewModel.IsCurrentThemeDark());
            var revertedLightBrush = Application.Current.Resources["WindowBackdropBrush"] as SolidColorBrush;
            Assert.NotNull(revertedLightBrush);
            Assert.Equal(Color.FromArgb(0xD9, 0xFF, 0xFF, 0xFF), revertedLightBrush.Color);
        });
    }

    [Fact]
    public void MainWindow_SnapshotRollback_RestoresOriginalTheme()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var initialSnapshot = MainWindowViewModel.CaptureThemeSnapshot();
            try
            {
                MainWindowViewModel.ApplyAppTheme(FoliconThemes.TokyoNeon);
                Assert.Equal("tokyo-neon", MainWindowViewModel.ActiveThemeId);

                // Live preview simulation: rollback snapshot
                MainWindowViewModel.RestoreThemeSnapshot(initialSnapshot);
                Assert.Equal(initialSnapshot.ActiveThemeId, MainWindowViewModel.ActiveThemeId);
            }
            finally
            {
                MainWindowViewModel.RestoreThemeSnapshot(initialSnapshot);
            }
        });
    }

    [Fact]
    public void MainWindow_HoverPreview_WhenIsPreviewingTheme_DoesNotCommitToCustomThemeId()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var initialSnapshot = MainWindowViewModel.CaptureThemeSnapshot();
            try
            {
                var parser = new HybridThemeParser();
                var fakeStorage = new FakeThemeStorageForMainWindow();
                fakeStorage.Themes.AddRange(BuiltInThemes.GetAll());
                fakeStorage.Themes.Add(new ThemeManifest
                {
                    Id = "retro-synth",
                    Name = "Retro Synth",
                    Author = "RetroDev",
                    BaseTheme = "Dark",
                    Colors = new ThemeColorPalette
                    {
                        Background = "#101010",
                        CardSurface = "#202020",
                        Primary = "#FF007F",
                        Accent = "#00F0FF",
                        PrimaryText = "#FFFFFF"
                    }
                });

                var vm = new MainWindowViewModel(null!, fakeStorage, parser);
                var communityOption = vm.AvailableThemes.FirstOrDefault(t => t.Id == "retro-synth");
                Assert.NotNull(communityOption);

                // Simulate dropdown open and hover preview
                vm.IsPreviewingTheme = true;
                vm.SelectedThemeOption = communityOption;

                // Visual theme is applied for live preview
                Assert.Equal("retro-synth", MainWindowViewModel.ActiveThemeId);
                // BUT not committed to settings yet!
                Assert.Equal(string.Empty, vm.CustomThemeId);

                // When user clicks/commits the theme:
                vm.IsPreviewingTheme = false;
                vm.ApplyThemeOption(communityOption, isPreview: false);
                Assert.Equal("retro-synth", vm.CustomThemeId);
                Assert.Equal("retro-synth", MainWindowViewModel.ActiveThemeId);
            }
            finally
            {
                MainWindowViewModel.RestoreThemeSnapshot(initialSnapshot);
            }
        });
    }

    [Fact]
    public void MainWindow_AvailableThemes_IncludesBothBuiltInAndCommunityThemes()
    {
        using var host = new WpfTestHost();
        WpfTestHost.Invoke(() =>
        {
            var initialSnapshot = MainWindowViewModel.CaptureThemeSnapshot();
            try
            {
                var parser = new HybridThemeParser();
                var fakeStorage = new FakeThemeStorageForMainWindow();
                fakeStorage.Themes.AddRange(BuiltInThemes.GetAll());
                fakeStorage.Themes.Add(new ThemeManifest
                {
                    Id = "synthwave-84",
                    Name = "Synthwave '84",
                    Author = "NeonMaster",
                    BaseTheme = "Dark",
                    Colors = new ThemeColorPalette
                    {
                        Background = "#262335",
                        CardSurface = "#2A2139",
                        Primary = "#FF7EDB",
                        Accent = "#36F9F6",
                        PrimaryText = "#FFFFFF"
                    }
                });

                var vm = new MainWindowViewModel(null!, fakeStorage, parser);
                Assert.True(vm.AvailableThemes.Count >= 9); // 8 built-in + 1 community

                var communityOption = vm.AvailableThemes.FirstOrDefault(t => t.Id == "synthwave-84");
                Assert.NotNull(communityOption);
                Assert.Equal("Synthwave '84", communityOption.DisplayName);
                Assert.False(communityOption.IsBuiltIn);

                // Select community theme in dropdown
                vm.SelectedThemeOption = communityOption;
                Assert.Equal("synthwave-84", vm.CustomThemeId);
                Assert.Equal("synthwave-84", MainWindowViewModel.ActiveThemeId);
            }
            finally
            {
                MainWindowViewModel.RestoreThemeSnapshot(initialSnapshot);
            }
        });
    }


    private sealed class FakeThemeStorageForMainWindow : IThemeStorageService
    {
        public string ThemesDirectory => Path.GetTempPath();
        public List<ThemeManifest> Themes { get; } = [];
        public IReadOnlyList<ThemeManifest> GetInstalledThemes() => Themes;
        public IReadOnlyList<ThemeManifest> GetUserThemes() => Themes;
        public ThemeManifest? GetThemeById(string themeId) => Themes.Find(t => t.Id == themeId);
        public bool IsThemeInstalled(string themeId) => Themes.Exists(t => t.Id == themeId);
        public string GetThemeFolderPath(string themeId) => Path.Combine(ThemesDirectory, themeId);
        public Task<ThemeManifest> InstallThemeFromJsonAsync(string json, byte[]? previewImageBytes = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ThemeManifest> InstallThemeFromPackageAsync(Stream packageStream, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<string> ExportThemePackageAsync(string themeId, string destinationZipPath, CancellationToken ct = default) => Task.FromResult(destinationZipPath);
        public bool DeleteTheme(string themeId) => Themes.RemoveAll(t => t.Id == themeId) > 0;
        public void Refresh() { }
    }
}
