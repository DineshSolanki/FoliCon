using FoliCon.Models.Data;

namespace FoliCon.Modules.Configuration;

public class AppConfig : GlobalDataHelper
{
    public string DevClientId { get; set; }
    public string DevClientSecret { get; set; }
    public string TmdbKey { get; set; }
    public string IgdbClientId { get; set; }
    public string IgdbClientSecret { get; set; }
    public string ContextEntryName { get; set; } = "Create icons with FoliCon";
    public bool IsExplorerIntegrated { get; set; }
    public override string FileName { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"FoliConConfig.json");
    public override JsonSerializerOptions JsonSerializerOptions { get; set; }
    public override int FileVersion { get; set; }

    public bool SubfolderProcessingEnabled { get; set; }
    
    public ObservableCollection<Pattern> Patterns { get; set; }
}