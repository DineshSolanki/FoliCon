using FoliCon.Models.Enums;
using FoliCon.ViewModels;

namespace FoliCon.Models.Constants;

internal static class GlobalVariables
{
    public static bool SkipAll;

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

    public static string MediaInfoFile = "info.folicon";
}