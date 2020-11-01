using HandyControl.Controls;

namespace FoliCon.Modules
{
    public class AppConfig : GlobalDataHelper<AppConfig>
    {
        public string DevClientId { get; set; }
        public string DevClientSecret { get; set; }
        public string TmdbKey { get; set; }
        public string IgdbClientId { get; set; }
        public string IgdbClientSecret { get; set; }
    }
}