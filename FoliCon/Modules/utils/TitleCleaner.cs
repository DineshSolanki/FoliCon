namespace FoliCon.Modules.utils;

[Localizable(false)]
public static partial class TitleCleaner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<string, string> UnicodeToNonUnicode = new()
    {
        { "\uA789", ":" },
        { "\u2236", ":" }
    };
    [GeneratedRegex("\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex QualityAndResolutionFormatRegex();

    [GeneratedRegex(@"\s*\(?(?:420|720|1080|2160)p?i?\)?.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ResolutionOnlyFormatRegex();

    [GeneratedRegex(@"\[.*?\]", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex EnclosedInBracketsRegex();

    [GeneratedRegex(@"\s*\([^)]*(?:repack|game\s*pass|edition|steam|rip|fitgirl|dodi|gog)[^)]*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex NonYearParentheticalRegex();

    [GeneratedRegex(@"(?i)\s*[-_~.]*\s*(?:Windows Store|PlayStation Store|Microsoft Store|EA Play|Nintendo eShop|Steam|Humble Bundle|Rockstar Games Launcher|Ubisoft Connect|GOG|Battle\.net|itch\.io|Xbox Game Pass)\s+Edition\b", RegexOptions.Compiled, "en-US")]
    private static partial Regex StoreEditionRegex();

    [GeneratedRegex(@"(?i)\s*[-_~.]*\s*(?:Win64|Win32|x64|x86|PCGame|PC)\b", RegexOptions.Compiled, "en-US")]
    private static partial Regex ArchitectureRegex();

    [GeneratedRegex(@"(?i)(?:(?:\s+|[-_~.]+)\bv(?:ersion)?\s*\d+(?:\.\d+)*\b|(?:\s+|[-_~.]+)\b\d+\.\d+(?:\.\d+)*\s*$)", RegexOptions.Compiled, "en-US")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"(?i)(?:[-_~.]+|\s+)\s*(?:Repack|FitGirl|DODI|CODEX|SKIDROW|PLAZA|PROPHET|HOODLUM|EMPRESS|CorePack|RAZOR1911|FLT|REVOLT|DEFAULTR|ElAmigos|KaOsKrew|KaosKrew|POSTMORTEM|TiNYiSO|HI2U|HYBRiD|SteamRIP|Multiup|SiMPLEX|DARKZER0|CPY|FAIRLIGHT|DUNE|ALI213|3DM|TENOKE|VREX|black_box|Frosted|RG Mechanics|R\.G\. Mechanics|Chovka|Darck|Hexadrive|Chronos|DMGAME|P2P)(?:\s*Repack)?\s*$", RegexOptions.Compiled, "en-US")]
    private static partial Regex TrailingSceneGroupRegex();

    [GeneratedRegex(" {2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MultipleSpacesRegex();
    [GeneratedRegex(@"[^\u0000-\u007F]+")]
    private static partial Regex NonAsciiRegex();
    [GeneratedRegex(@"\{(tvdb|tmdb)-(\d+)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex ShowIdRegex();
    [GeneratedRegex(@"\((\d{4})\)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex YearRegex();

    public static string Clean(string title, string? mediaType = null)
    {
        var normalizedTitle = RemoveReplaceUnicodeCharacters(title);
        var cleanTitle = CleanTitle(normalizedTitle, mediaType);

        Logger.Debug("Cleaned title: {Clean}, Original title: {Title}", cleanTitle, title);
        return cleanTitle;
    }

    public static ParsedTitle CleanAndParse(string title, string? mediaType = null)
    {
        var (extractedTitle, idType, showId, year) = ExtractShowIdAndYear(title);
        var cleanTitle = Clean(extractedTitle, mediaType);
        return new ParsedTitle(cleanTitle, idType, showId, year);
    }

    private static string NormalizeTitle(string title) => title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ').Replace('~', ' ');

    private static string CleanTitle(string title, string? mediaType = null)
    {
        var cleanTitle = title;

        cleanTitle = EnclosedInBracketsRegex().Replace(cleanTitle, "");
        cleanTitle = NonYearParentheticalRegex().Replace(cleanTitle, "");

        if (mediaType == MediaTypes.game)
        {
            cleanTitle = ResolutionOnlyFormatRegex().Replace(cleanTitle, "");
        }
        else
        {
            cleanTitle = QualityAndResolutionFormatRegex().Replace(cleanTitle, "");
        }

        cleanTitle = StoreEditionRegex().Replace(cleanTitle, "");
        cleanTitle = ArchitectureRegex().Replace(cleanTitle, "");
        cleanTitle = VersionRegex().Replace(cleanTitle, "");

        while (true)
        {
            var lenBefore = cleanTitle.Length;
            cleanTitle = TrailingSceneGroupRegex().Replace(cleanTitle, "");
            if (cleanTitle.Length == lenBefore)
            {
                break;
            }
        }

        cleanTitle = NormalizeTitle(cleanTitle);
        cleanTitle = MultipleSpacesRegex().Replace(cleanTitle, " ");

        return string.IsNullOrWhiteSpace(cleanTitle) ? title.Trim() : cleanTitle.Trim();
    }

    private static string RemoveReplaceUnicodeCharacters(string title)
    {
        title = UnicodeToNonUnicode.Aggregate(title, (current, pair) => current.Replace(pair.Key, pair.Value));

        // Remove other remaining unicode characters
        return NonAsciiRegex().Replace(title, string.Empty);
    }

    private static (string, IdType, string, int) ExtractShowIdAndYear(string title)
    {
        var showIdMatch = ShowIdRegex().Match(title);
        var yearMatch = YearRegex().Match(title);

        var showIdType = IdType.None;
        var showId = "0";

        if (showIdMatch.Success)
        {
            showIdType = Enum.TryParse(showIdMatch.Groups[1].Value, true, out IdType parsedShowIdType) ? parsedShowIdType : IdType.None;
            showId = showIdMatch.Groups[2].Value;
            title = ShowIdRegex().Replace(title, "");
        }

        if (!yearMatch.Success)
        {
            return (title, showIdType, showId, 0);
        }
        var year = Convert.ToInt32(yearMatch.Groups[1].Value);
        title = YearRegex().Replace(title, "");

        return (title, showIdType, showId, year);
    }
}
