using FoliCon.ViewModels;

namespace FoliCon.Models
{
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
                _ => IconOverlay.Alternate
            };
        }

        public static string MediaInfoFile = "info.folicon";
    }
}