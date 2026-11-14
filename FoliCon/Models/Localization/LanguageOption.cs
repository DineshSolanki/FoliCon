namespace FoliCon.Models.Localization;

public sealed class LanguageOption
{
    public Languages Language { get; }
    public string Code { get; }
    public string NativeName { get; }
    public string EnglishName { get; }
    public string CultureCode { get; }

    /// <summary>
    /// Displays the native language endonym (e.g. "Español", "Русский", "हिन्दी").
    /// </summary>
    public string DisplayName => NativeName;

    /// <summary>
    /// Displays the native language endonym alongside the English name for international clarity.
    /// E.g. "Español (Spanish)" or "English".
    /// </summary>
    public string FullDisplayName => EnglishName == NativeName ? NativeName : $"{NativeName} ({EnglishName})";

    public string Tooltip => EnglishName == NativeName ? $"{NativeName} [{CultureCode}]" : $"{NativeName} — {EnglishName} [{CultureCode}]";

    public LanguageOption(Languages language, string code, string nativeName, string englishName, string cultureCode)
    {
        Language = language;
        Code = code;
        NativeName = nativeName;
        EnglishName = englishName;
        CultureCode = cultureCode;
    }

    public static readonly IReadOnlyList<LanguageOption> All =
    [
        new(Languages.English, "EN", "English", "English", "en-US"),
        new(Languages.Spanish, "ES", "Español", "Spanish", "es-MX"),
        new(Languages.Arabic, "AR", "العربية", "Arabic", "ar-SA"),
        new(Languages.Russian, "RU", "Русский", "Russian", "ru-RU"),
        new(Languages.Hindi, "HI", "हिन्दी", "Hindi", "hi-IN"),
        new(Languages.Portuguese, "PT", "Português", "Portuguese", "pt-PT"),
    ];

    public override string ToString() => FullDisplayName;

    public override bool Equals(object? obj)
    {
        return obj is LanguageOption other && Language == other.Language;
    }

    public override int GetHashCode() => (int)Language;
}
