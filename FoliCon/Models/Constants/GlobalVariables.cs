namespace FoliCon.Models.Constants;

internal static class GlobalVariables
{
    
    public static IconOverlay IconOverlayType()
    {
        return new PosterIconConfigViewModel().IconOverlay switch
        {
            "Legacy" => IconOverlay.Legacy,
            "Alternate" => IconOverlay.Alternate,
            "Liaher" => IconOverlay.Liaher,
            "Faelpessoal" => IconOverlay.Faelpessoal,
            "FaelpessoalHorizontal" => IconOverlay.FaelpessoalHorizontal,
            _ => IconOverlay.Alternate
        };
    }

    public static readonly string MediaInfoFile = "info.folicon";
}