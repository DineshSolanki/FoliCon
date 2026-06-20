using STJ = System.Text.Json.Serialization;

namespace FoliCon.Modules.Configuration;

[Localizable(false)]
public class AppConfig : GlobalDataHelper
{
    // DeviantArt OAuth tokens (replaces DevClientId/DevClientSecret from client_credentials flow)
    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string DeviantArtAccessToken { get; set; }

    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string DeviantArtRefreshToken { get; set; }

    public DateTime DeviantArtTokenExpiresAt { get; set; }

    // DeviantArt custom credentials (user-provided)
    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string DeviantArtClientId { get; set; }

    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string DeviantArtClientSecret { get; set; }

    public bool DeviantArtWatchEnabled { get; set; }

    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string TmdbKey { get; set; }

    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string IgdbClientId { get; set; }

    [STJ.JsonConverter(typeof(DpapiEncryptingConverter))]
    public string IgdbClientSecret { get; set; }

    public bool OnboardingCompleted { get; set; }

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
