namespace FoliCon.Modules.Configuration;

public static class Services
{
    public static readonly Tracker Tracker = new();
    public static readonly HttpClient HttpC = new();
    public static readonly AppConfig Settings = GlobalDataHelper.Load<AppConfig>();
}