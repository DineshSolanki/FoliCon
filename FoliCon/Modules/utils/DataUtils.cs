using NLog.Fluent;
using System.Windows.Media;
using static Vanara.PInvoke.User32;

namespace FoliCon.Modules.utils;

public static class DataUtils
{
    public static int GetResultCount(bool isPickedById, dynamic result, string searchMode)
    {
        return isPickedById ? result != null ? 1 : 0 : searchMode == "Game" ? result.Length : result.TotalResults;
    }

    /// <summary>
    /// Internal helper to format the Rating string for the selected Item.
    /// The decimal points of rating should be limited to 2 digits : https://github.com/DineshSolanki/FoliCon/issues/145
    /// Eg: X.XX -> 7.15
    /// </summary>
    /// <param name="ratingInput">Rating value.</param>
    /// <returns>Formatted Rating value.</returns>
    public static string FormatRatingString(string ratingInput)
    {
        Logger.Debug($"Start FormatRatingString() - Input received : {ratingInput}.");

        //TODO: Validate the incoming string if required.
        // As the rating is pure string and the validation is already done from the server side [assumption],
        // just trimming the incoming string and formatting to the required format.
        string formattedRatingValue = string.Empty;
        int decimalIndex = ratingInput.IndexOf('.');
        if (decimalIndex >= 0)
        {
            int digitsAfterDecimal = 2;
            int endIndex = Math.Min(decimalIndex + (digitsAfterDecimal + 1), ratingInput.Length);
            string trimmedValue = ratingInput.Substring(0, endIndex);
            formattedRatingValue = trimmedValue;
        }

        //Verification.
        string pattern = @".....+"; // Pattern to match string which have 3+ characters
        if (Regex.IsMatch(formattedRatingValue, pattern))
        {
            Logger.Error("Found a rating string with 3+ characters in it.");
        }
        Logger.Debug($"End FormatRatingString() - Formatted Rating : {formattedRatingValue}.");
        return formattedRatingValue;
    }
}