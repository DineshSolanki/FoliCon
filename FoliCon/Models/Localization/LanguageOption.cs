using System.Collections.Generic;
using FoliCon.Models.Enums;

namespace FoliCon.Models.Localization;

public sealed class LanguageOption
{
    public Languages Language { get; }
    public string Code { get; }
    public string DisplayName { get; }
    public string NativeName { get; }
    public string Tooltip { get; }

    public LanguageOption(Languages language, string code, string displayName, string nativeName, string cultureCode)
    {
        Language = language;
        Code = code;
        DisplayName = displayName;
        NativeName = nativeName;
        Tooltip = $"{displayName} ({cultureCode})";
    }

    public static readonly IReadOnlyList<LanguageOption> All =
    [
        new(Languages.English, "EN", "English", "English", "en-US"),
        new(Languages.Spanish, "ES", "Español", "Español", "es-MX"),
        new(Languages.Arabic, "AR", "العربية", "العربية", "ar-SA"),
        new(Languages.Russian, "RU", "Русский", "Русский", "ru-RU"),
        new(Languages.Hindi, "HI", "हिन्दी", "हिन्दी", "hi-IN"),
        new(Languages.Portuguese, "PT", "Português", "Português", "pt-PT"),
    ];

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj)
    {
        return obj is LanguageOption other && Language == other.Language;
    }

    public override int GetHashCode() => (int)Language;
}
