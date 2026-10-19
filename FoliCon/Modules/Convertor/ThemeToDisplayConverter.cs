namespace FoliCon.Modules.Convertor;

public sealed class ThemeToDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FoliconThemes theme)
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
                _ => value.ToString() ?? string.Empty
            };
        }

        if (value is not string str)
        {
            return value?.ToString() ?? string.Empty;
        }

        if (Enum.TryParse<FoliconThemes>(str, out var parsedTheme))
        {
            return parsedTheme switch
            {
                FoliconThemes.Light => Lang.ThemeLight,
                FoliconThemes.Dark => Lang.ThemeDark,
                FoliconThemes.System => Lang.ThemeSystem,
                FoliconThemes.TheButterfly => Lang.ThemeTheButterfly,
                FoliconThemes.TokyoNeon => Lang.ThemeTokyoNeon,
                FoliconThemes.MicaDarkObsidian => Lang.ThemeMicaDarkObsidian,
                FoliconThemes.NordicPine => Lang.ThemeNordicPine,
                FoliconThemes.Cinema35mm => Lang.ThemeCinema35mm,
                _ => str
            };
        }
        return str;

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
