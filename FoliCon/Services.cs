namespace FoliCon;

public static class Services
{
    public static readonly Tracker Tracker = new();
    public static readonly HttpClient HttpC = new();
    public static AppConfig Settings = GlobalDataHelper.Load<AppConfig>();
}