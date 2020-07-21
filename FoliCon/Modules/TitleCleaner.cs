using System.Text.RegularExpressions;

namespace FoliCon.Modules
{
    internal static class TitleCleaner
    {
        public static string Clean(string title)
        {
            string normalizedTitle = title.Replace('-', ' ').Replace('_', ' ').Replace('.', ' ');

            // \s* --Remove any whitespace which would be left at the end after this substitution
            // \(? --Remove optional bracket starting (720p)
            // (\d{4}) --Remove year from movie
            // (420)|(720)|(1080) resolutions
            // (year|resolutions) find at least one main token to remove
            // p?i? \)? --Not needed. To emphasize removal of 1080i, closing bracket etc, but not needed due to the last part
            // .* --Remove all trailing information after having found year or resolution as junk usually follows
            string cleanTitle = Regex.Replace(normalizedTitle, "\\s*\\(?((\\d{4})|(420)|(720)|(1080))p?i?\\)?.*", "", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                return normalizedTitle;
            }
            else
            {
                return cleanTitle;
            }
        }
    }
}