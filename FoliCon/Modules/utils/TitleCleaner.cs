using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

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
    
    private static string NormalizeTitle(string title)
    {
        return title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');
    }
    
    private static string CleanTitle(string title)
    {
        // \s* --Remove any whitespace which would be left at the end after this substitution
        // \(? --Remove optional bracket starting (720p)
        // (\d{4}) --Remove year from movie
        // (420)|(720)|(1080) resolutions
        // (year|resolutions) find at least one main token to remove
        // p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
        // .* --Remove all trailing information after having found year or resolution as junk usually follows
        
        var cleanTitle = Regex.Replace(title, "\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, @"\[.*\]", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, " {2,}", " ", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return string.IsNullOrWhiteSpace(cleanTitle) ? title : cleanTitle;
    }
    
    private static string RemoveReplaceUnicodeCharacters(string title)
    {
        title = UnicodeToNonUnicode.Aggregate(title, (current, pair) => current.Replace(pair.Key, pair.Value));
        
        // Remove other remaining unicode characters
        return Regex.Replace(title, @"[^\u0000-\u007F]+", string.Empty);
    }
}