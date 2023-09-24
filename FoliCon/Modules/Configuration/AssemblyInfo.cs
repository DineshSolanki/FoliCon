namespace FoliCon.Modules.Configuration;

internal static class AssemblyInfo
{
    public static string GetVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
    
    public static string GetVersionWithoutBuild()
    {
        var fullVersion = GetVersion();
        var versionParts = fullVersion.Split('.');
        var versionWithoutBuild = string.Join(".", versionParts.Take(3));
        
        return $"Folicon v{versionWithoutBuild}";
    }
}