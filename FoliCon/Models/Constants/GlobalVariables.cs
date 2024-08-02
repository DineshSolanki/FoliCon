namespace FoliCon.Models.Constants;

[Localizable(false)]
internal static class GlobalVariables
{
    
    public static IconOverlay IconOverlayType()
    {
        return IconOverlayTypeString switch
        {
            "Legacy" => IconOverlay.Legacy,
            "Alternate" => IconOverlay.Alternate,
            "Liaher" => IconOverlay.Liaher,
            "Faelpessoal" => IconOverlay.Faelpessoal,
            "FaelpessoalHorizontal" => IconOverlay.FaelpessoalHorizontal,
            _ => IconOverlay.Alternate
        };
    }

    public const string MediaInfoFile = "info.folicon";

    private static string IconOverlayTypeString =>
        Services.Tracker.Store.GetData("PosterIconConfigViewModel")?["p.IconOverlay"]?.ToString() ?? IconOverlay.Liaher.ToString();
    
    public static string SeasonRegexString => @"(?i)\b(s|season)\s*(\d{1,2})\b";
}