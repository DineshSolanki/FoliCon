namespace FoliCon.Modules.utils;

public static class DataUtils
{
    public static int GetResultCount(bool isPickedById, dynamic result, string searchMode)
    {
        return isPickedById ? result != null ? 1 : 0 : searchMode == "Game" ? result.Length : result.TotalResults;
    }
}