using FoliCon.ViewModels;

namespace FoliCon.Models
{
    internal static class GlobalVariables
    {
        public static bool SkipAll = false;

        public static IconOverlay IconOverlayType = new PosterIconConfigViewModel().IconOverlay switch
        {
            "Legacy" => IconOverlay.Legacy,
            "Alternate" => IconOverlay.Alternate,
            _ => IconOverlay.Alternate
        };
    }
}