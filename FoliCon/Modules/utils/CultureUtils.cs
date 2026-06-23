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
        var pureLangCode = langCode.Split("-")[0];

        // FIX: Defer the HandyControl initialization to prevent the startup deadlock.
        // This allows the method to return the CultureInfo immediately to your view model
        // without waiting for the uninitialized Dispatcher thread loop to finish.
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                // Now safe to initialize the static HandyControl instance
                ConfigHelper.Instance.SetLang(pureLangCode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Deferred internal HandyControl language configuration failed.");
            }
        }), DispatcherPriority.Background);

        return new CultureInfo(langCode);
    }
}
