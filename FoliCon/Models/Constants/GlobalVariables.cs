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
            "Windows11" => IconOverlay.Windows11,
            _ => IconOverlay.Alternate
        };
    }

    public const string MediaInfoFile = "info.folicon";

    private static string IconOverlayTypeString
    {
        get
        {
            var data = Services.Tracker.Store.GetData("PosterIconConfigViewModel");
            return data.TryGetValue("p.IconOverlay", out var value) ? value.ToString() : IconOverlay.Liaher.ToString();
        }
    }
}