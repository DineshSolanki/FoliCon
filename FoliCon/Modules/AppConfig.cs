using HandyControl.Controls;

namespace FoliCon.Modules
{
    public class AppConfig : GlobalDataHelper<AppConfig>
    {
        public string DevClientID { get; set; }
        public string DevClientSecret { get; set; }
        public string TMDBKey { get; set; }
        public string IGDBKey { get; set; }
    }
}