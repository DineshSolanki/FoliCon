using FoliCon.Models.Enums;
using FoliCon.Models.Themes;

namespace FoliCon.Modules.Convertor;

public sealed class ThemeToDisplayConverter : IValueConverter
{
    public static string GetLocalizedThemeName(FoliconThemes theme)
    {
        return theme switch
        {
            FoliconThemes.Light => Lang.ThemeLight,
            FoliconThemes.Dark => Lang.ThemeDark,
            FoliconThemes.System => Lang.ThemeSystem,
            FoliconThemes.TheButterfly => Lang.ThemeTheButterfly,
            FoliconThemes.TokyoNeon => Lang.ThemeTokyoNeon,
            FoliconThemes.MicaDarkObsidian => Lang.ThemeMicaDarkObsidian,
            FoliconThemes.NordicPine => Lang.ThemeNordicPine,
            FoliconThemes.Cinema35mm => Lang.ThemeCinema35mm,
            _ => theme.ToString()
        };
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ThemeOption option)
        {
            return option.DisplayName;
        }

        if (value is FoliconThemes theme)
        {
            return GetLocalizedThemeName(theme);
        }

        if (value is not string str)
        {
            return value?.ToString() ?? string.Empty;
        }

        if (Enum.TryParse<FoliconThemes>(str, out var parsedTheme))
        {
            return GetLocalizedThemeName(parsedTheme);
        }
        return str;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
