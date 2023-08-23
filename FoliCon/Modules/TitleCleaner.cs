using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules;

internal static class TitleCleaner
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static string Clean(string title)
    {
        var normalizedTitle = title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');

        // \s* --Remove any whitespace which would be left at the end after this substitution
        // \(? --Remove optional bracket starting (720p)
        // (\d{4}) --Remove year from movie
        // (420)|(720)|(1080) resolutions
        // (year|resolutions) find at least one main token to remove
        // p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
        // .* --Remove all trailing information after having found year or resolution as junk usually follows
        var cleanTitle = Regex.Replace(normalizedTitle, "\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, @"\[.*\]", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        cleanTitle = Regex.Replace(cleanTitle, " {2,}", " ", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var clean = string.IsNullOrWhiteSpace(cleanTitle) ? normalizedTitle : cleanTitle;
        
        Logger.Debug("Cleaned title: {Clean}, Original title: {Title}", clean, title);
        return clean;
    }
}