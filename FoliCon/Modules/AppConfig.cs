using HandyControl.Tools;
using System.Text.Json;

namespace FoliCon.Modules
{
    public class AppConfig : GlobalDataHelper
    {
        public string DevClientId { get; set; }
        public string DevClientSecret { get; set; }
        public string TmdbKey { get; set; }
        public string IgdbClientId { get; set; }
        public string IgdbClientSecret { get; set; }
        public override string FileName { get; set; }
        public override JsonSerializerOptions JsonSerializerOptions { get; set; }
        public override int FileVersion { get; set; }
    }
}