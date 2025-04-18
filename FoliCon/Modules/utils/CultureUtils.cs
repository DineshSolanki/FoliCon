namespace FoliCon.Modules.utils;

[Localizable(false)]
public static class CultureUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<Languages, string> LanguageCodes = new()
    {
        { Languages.English, "en-US" },
        { Languages.Spanish, "es-MX" },
        { Languages.Arabic, "ar-SA" },
        { Languages.Russian, "ru-RU" },
        { Languages.Hindi, "hi-IN" },
        { Languages.Portuguese, "pt-PT" }
    };

    public static CultureInfo GetCultureInfoByLanguage(Languages language)
    {
        Logger.Debug("Getting CultureInfo by Language: {Language}", language);
        
        var langCode = LanguageCodes.GetValueOrDefault(language, "en-US");
        ConfigHelper.Instance.SetLang(langCode.Split("-")[0]);

        return new CultureInfo(langCode);
    }
}