#nullable enable
using System.Globalization;
using FoliCon.Properties.Langs;

namespace FoliconTest;

public class ThemeLocalizationTests
{
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

    [Theory]
    [InlineData("ar", "سينما 35 مم")]
    [InlineData("es", "Cine 35mm")]
    [InlineData("hi", "सिनेमा 35mm")]
    [InlineData("ja", "シネマ 35mm")]
    [InlineData("pt", "Cinema 35mm")]
    [InlineData("ru", "Синема 35мм")]
    [InlineData("zh", "35毫米胶片")]
    public void AllLocales_HaveIdiomaticCinema35mmTheme(string cultureCode, string expectedTheme)
    {
        var culture = new CultureInfo(cultureCode);
        var themeString = Lang.ResourceManager.GetString(nameof(Lang.ThemeCinema35mm), culture);

        Assert.Equal(expectedTheme, themeString);
    }
}
