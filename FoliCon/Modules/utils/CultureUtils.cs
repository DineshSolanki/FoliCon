using FoliCon.Models.Enums;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class CultureUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static CultureInfo GetCultureInfoByLanguage(Languages language)
    {
        Logger.Debug("Getting CultureInfo by Language: {Language}", language);

        var languageCodes = new Dictionary<Languages, string>
        {
            { Languages.English, "en-US" },
            { Languages.Spanish, "es-MX" },
            { Languages.Arabic, "ar-SA" },
            { Languages.Russian, "ru-RU" },
            { Languages.Hindi, "hi-IN" }
        };

        var langCode = languageCodes.GetValueOrDefault(language, "en-US");
        ConfigHelper.Instance.SetLang(langCode.Split("-")[0]);

        return new CultureInfo(langCode);
    }
}