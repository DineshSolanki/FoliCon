using FoliCon.Models.Data;
using FoliCon.Models.Enums;
using NLog;

namespace FoliCon.Modules.utils;

public static class DataUtils
{
    private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static int GetResultCount(bool isPickedById, dynamic result, string searchMode)
    {
        if (isPickedById)
        {
            return result != null ? 1 : 0;
        }

        return searchMode == "Game" ? result.Length : result.TotalResults;
    }

    /// <summary>
    /// Internal helper to format the Rating string for the selected Item.
    /// The decimal points of the rating should be limited to two digits:
    /// https://github.com/DineshSolanki/FoliCon/issues/145
    /// e.g.: X.XX -> 7.15
    /// </summary>
    /// <param name="ratingInput">Rating value.</param>
    /// <returns>Formatted Rating value.</returns>
    public static string FormatRating(double ratingInput)
    {
        Logger.Debug("Start FormatRatingString() - Input received : {RatingInput}", ratingInput);
        var decimalPart = ratingInput % 1;
        var formattedRatingValue = decimalPart > 0 ? ratingInput.ToString("0.##") : ratingInput.ToString("0");
        Logger.Debug("End FormatRatingString() - Formatted Rating : {FormattedRatingValue}", formattedRatingValue);
        return formattedRatingValue;
    }
    
   public static bool ShouldUseParsedTitle(ParsedTitle parsedTitle)
   {
       return parsedTitle != null && (parsedTitle.Year != 0 || (parsedTitle.IdType != IdType.None && parsedTitle.Id != "0"));
   }
   
   public static bool IsValidRegex(string pattern)
   {
       try
       {
           Logger.Trace("Checking if the pattern {Pattern}is a valid regex.", pattern);
           _ = new Regex(pattern);
           Logger.Trace("Pattern {Pattern} is a valid regex.", pattern);
           return true;
       }
       catch (ArgumentException e)
       {
              Logger.Trace(e, "Pattern {Pattern} is not a valid regex. reason: {Reason}", pattern, e.Message);
           return false;
       }
   }
}
