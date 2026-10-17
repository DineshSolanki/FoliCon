namespace FoliCon.Modules.Configuration;

[Localizable(false)]
internal static class AssemblyInfo
{
    private static readonly Version FallbackVersion = new(1, 0, 0);

    public static Version AppVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version
        ?? typeof(AssemblyInfo).Assembly.GetName().Version
        ?? FallbackVersion;

    public static string GetVersion() => AppVersion.ToString();

    public static string GetShortVersion() => $"{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}";

    public static string GetVersionWithoutBuild() => $"Folicon v{GetShortVersion()}";
}