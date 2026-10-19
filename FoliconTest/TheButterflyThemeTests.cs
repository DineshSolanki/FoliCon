#nullable enable
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FoliCon.Models.Enums;
using FoliCon.Modules.Convertor;
using FoliCon.Properties.Langs;
using FoliCon.ViewModels;
using HandyControl.Themes;

namespace FoliconTest;

[Collection(XamlLoadingCollection.name)]
public class TheButterflyThemeTests
{
    [Fact]
    public void ThemeToDisplayConverter_ConvertsTheButterflyToExpectedText()
    {
        var converter = new ThemeToDisplayConverter();
        var result = converter.Convert(FoliconThemes.TheButterfly, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(Lang.ThemeTheButterfly, result);
        Assert.Equal("The Butterfly", result);
    }

    [Theory]
    [InlineData(FoliconThemes.Light, "Light")]
    [InlineData(FoliconThemes.Dark, "Dark")]
    [InlineData(FoliconThemes.System, "System")]
    [InlineData(FoliconThemes.TheButterfly, "The Butterfly")]
    [InlineData(FoliconThemes.TokyoNeon, "Tokyo Neon")]
    [InlineData(FoliconThemes.MicaDarkObsidian, "Mica Dark Obsidian")]
    [InlineData(FoliconThemes.NordicPine, "Nordic Pine")]
    public void ThemeToDisplayConverter_ConvertsAllThemesAccurately(FoliconThemes theme, string expected)
    {
        var converter = new ThemeToDisplayConverter();
        var result = converter.Convert(theme, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

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
    public void ApplyAppTheme_TogglesTheButterflyThemeSuccessfully()
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

    [Fact]
    public void AboutBox_UsesUpdatedTagline()
    {
        Assert.Equal("YOUR MEDIA. YOUR LIBRARY. REIMAGINED.", Lang.AboutTagline);
    }

    [Theory]
    [InlineData("ar", "الفراشة", "وسائطك. مكتبتك. برؤية جديدة.")]
    [InlineData("es", "La Mariposa", "TUS MEDIOS. TU BIBLIOTECA. REINVENTADOS.")]
    [InlineData("hi", "तितली", "आपका मीडिया। आपकी लाइब्रेरी। नए अंदाज़ में।")]
    [InlineData("ja", "バタフライ", "あなたのメディア。あなたのライブラリ。新しく生まれ変わる。")]
    [InlineData("pt", "A Borboleta", "A SUA MÍDIA. A SUA BIBLIOTECA. REINVENTADAS.")]
    [InlineData("ru", "Бабочка", "ВАШИ МЕДИА. ВАША БИБЛИОТЕКА. ПО-НОВОМУ.")]
    [InlineData("zh", "蝴蝶", "你的媒体。你的媒体库。全新构想。")]
    public void AllLocales_HaveIdiomaticButterflyThemeAndTagline(string cultureCode, string expectedTheme, string expectedTagline)
    {
        var culture = new CultureInfo(cultureCode);
        var themeString = Lang.ResourceManager.GetString(nameof(Lang.ThemeTheButterfly), culture);
        var taglineString = Lang.ResourceManager.GetString(nameof(Lang.AboutTagline), culture);

        Assert.Equal(expectedTheme, themeString);
        Assert.Equal(expectedTagline, taglineString);
    }

    [Theory]
    [InlineData("ar", "طوكيو نيون")]
    [InlineData("es", "Tokio Neón")]
    [InlineData("hi", "टोक्यो नियॉन")]
    [InlineData("ja", "東京ネオン")]
    [InlineData("pt", "Tóquio Neon")]
    [InlineData("ru", "Токио Неон")]
    [InlineData("zh", "东京霓虹")]
    public void AllLocales_HaveIdiomaticTokyoNeonTheme(string cultureCode, string expectedTheme)
    {
        var culture = new CultureInfo(cultureCode);
        var themeString = Lang.ResourceManager.GetString(nameof(Lang.ThemeTokyoNeon), culture);

        Assert.Equal(expectedTheme, themeString);
    }

    [Theory]
    [InlineData("ar", "الصنوبر الشمالي")]
    [InlineData("es", "Pino Nórdico")]
    [InlineData("hi", "नॉर्डिक पाइन")]
    [InlineData("ja", "ノルディックパイン")]
    [InlineData("pt", "Pinho Nórdico")]
    [InlineData("ru", "Северная сосна")]
    [InlineData("zh", "北欧松林")]
    public void AllLocales_HaveIdiomaticNordicPineTheme(string cultureCode, string expectedTheme)
    {
        var culture = new CultureInfo(cultureCode);
        var themeString = Lang.ResourceManager.GetString(nameof(Lang.ThemeNordicPine), culture);

        Assert.Equal(expectedTheme, themeString);
    }

    [Fact]
    public void TransparentMark_CreatesValidTransparentIco()
    {
        var markPath = Path.GetFullPath(@"..\..\..\..\FoliCon\Resources\icons\folicon Icon.png");
        Assert.True(File.Exists(markPath));
        var tempIcoPath = Path.Combine(Path.GetTempPath(), $"folicon_test_{Guid.NewGuid():N}.ico");
        try
        {
            using var bmp = new System.Drawing.Bitmap(markPath);
            FoliCon.Modules.Media.PngToIcoService.Convert(bmp, tempIcoPath);
            Assert.True(File.Exists(tempIcoPath));
            var fi = new FileInfo(tempIcoPath);
            Assert.True(fi.Length > 0);
        }
        finally
        {
            if (File.Exists(tempIcoPath))
            {
                File.Delete(tempIcoPath);
            }
        }
    }
}
