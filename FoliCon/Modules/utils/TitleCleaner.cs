namespace FoliCon.Modules.utils;

[Localizable(false)]
internal static class TitleCleaner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<string, string> UnicodeToNonUnicode = new()
    {
        { "\uA789", ":" },
        { "\u2236", ":" }
    };
    public static string Clean(string title)
    {
        var normalizedTitle = NormalizeTitle(title);
        normalizedTitle = RemoveReplaceUnicodeCharacters(normalizedTitle);
        var cleanTitle = CleanTitle(normalizedTitle);

        Logger.Debug("Cleaned title: {Clean}, Original title: {Title}", cleanTitle, title);
        return cleanTitle;
    }
    
    public static ParsedTitle CleanAndParse(string title)
    {
        var (normalizedTitle, idType, showId, year) = ExtractShowIdAndYear(title);
        normalizedTitle = Clean(normalizedTitle);
        return new ParsedTitle(normalizedTitle, idType, showId, year);
    }
    
    private static string NormalizeTitle(string title)
    {
        return title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');
    }
    
    private static string CleanTitle(string title)
    {
        /*\s* --Remove any whitespace which would be left at the end after this substitution
        \(? --Remove optional bracket starting (720p)
        (\d{4}) --Remove year from movie
        (420)|(720)|(1080) resolutions
        (year|resolutions) find at least one main token to remove
        p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
        .* --Remove all trailing information after having found year or resolution as junk usually follows*/
        
        var cleanTitle = Regex.Replace(title, "\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, @"\[.*\]", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, " {2,}", " ", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return string.IsNullOrWhiteSpace(cleanTitle) ? title.Trim() : cleanTitle.Trim();
    }
    
    private static string RemoveReplaceUnicodeCharacters(string title)
    {
        title = UnicodeToNonUnicode.Aggregate(title, (current, pair) => current.Replace(pair.Key, pair.Value));
        
        // Remove other remaining unicode characters
        return Regex.Replace(title, @"[^\u0000-\u007F]+", string.Empty);
    }
    
    private static (string, IdType, string, int) ExtractShowIdAndYear(string title)
    {
        const string showIdPattern = @"\{(tvdb|tmdb)-(\d+)\}";
        const string yearPattern = @"\((\d{4})\)";

        var showIdMatch = Regex.Match(title, showIdPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var yearMatch = Regex.Match(title, yearPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var showIdType = IdType.None;
        var showId = "0";

        if (showIdMatch.Success)
        {
            showIdType = Enum.TryParse(showIdMatch.Groups[1].Value, true, out IdType parsedShowIdType) ? parsedShowIdType : IdType.None;
            showId = showIdMatch.Groups[2].Value;
            title = Regex.Replace(title, showIdPattern, "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        if (!yearMatch.Success)
        {
            return (title, showIdType, showId, 0);
        }
        var year = Convert.ToInt32(yearMatch.Groups[1].Value);
        title = Regex.Replace(title, yearPattern, "", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return (title, showIdType, showId, year);
    }
}