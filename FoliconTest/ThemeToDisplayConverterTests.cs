#nullable enable
using System.Globalization;
using FoliCon.Models.Enums;
using FoliCon.Modules.Convertor;
using FoliCon.Properties.Langs;

namespace FoliconTest;

public class ThemeToDisplayConverterTests
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
    [InlineData(FoliconThemes.Cinema35mm, "Cinema 35mm")]
    public void ThemeToDisplayConverter_ConvertsAllThemesAccurately(FoliconThemes theme, string expected)
    {
        var converter = new ThemeToDisplayConverter();
        var result = converter.Convert(theme, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}
