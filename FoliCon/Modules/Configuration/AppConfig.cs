namespace FoliCon.Modules.Configuration;

[Localizable(false)]
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
    public override int FileVersion { get; set; } = 1;

    public bool SubfolderProcessingEnabled { get; set; }
    
    public ObservableCollection<Pattern> Patterns { get; set; } =
        [new Pattern(GlobalVariables.SeasonRegexString, false, true), new Pattern("\\S+", true, true)];
}