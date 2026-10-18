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
    public void ThemeToDisplayConverter_ConvertsAllThemesAccurately(FoliconThemes theme, string expected)
    {
        var converter = new ThemeToDisplayConverter();
        var result = converter.Convert(theme, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
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
