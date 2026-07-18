namespace FoliCon.Models.Constants;

[Localizable(false)]
internal static class GlobalVariables
{
    private static IOverlayProvider? _overlayProvider;

    /// <summary>
    /// Gets or creates the static overlay provider instance.
    /// </summary>
    public static IOverlayProvider OverlayProvider => _overlayProvider ??= new OverlayProvider();

    /// <summary>
    /// Returns the active overlay string ID from the persisted tracker setting.
    /// </summary>
    public static string ActiveOverlayId
    {
        get
        {
            var data = Services.Tracker.Store.GetData("PosterIconConfigViewModel");
            if (!data.TryGetValue("p.IconOverlay", out var value)) return OverlayConstants.DefaultOverlayId;
            var strValue = value.ToString();
            return strValue switch
            {
                "Legacy" => "legacy",
                "Alternate" => "alternate",
                "Liaher" => "liaher",
                "Faelpessoal" => "faelpessoal",
                "FaelpessoalHorizontal" => "faelpessoal-horizontal",
                "Windows11" => "windows11",
                _ => strValue
            };
        }
    }

    /// <summary>
    /// Returns the active overlay definition.
    /// </summary>
    public static PosterOverlayDefinition GetActiveOverlay() => OverlayProvider.ResolveActiveOverlayOrDefault(ActiveOverlayId);

    public const string mediaInfoFile = "info.folicon";
}
