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
    
    public ObservableCollection<Pattern> Patterns { get; set; } =
        [new Pattern("^[0-9]{1,2}x[0-9]{1,2}", false, true), new Pattern("S[0-9]{1,2}E[0-9]", false, true),
            new Pattern("Season [0-9]{1,2} Episode [0-9]{1,2}", false, true), new Pattern("\\S+", true, true)];
}