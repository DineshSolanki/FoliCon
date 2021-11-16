namespace FoliCon.Modules
{
    internal static class AssemblyInfo
    {
        public static string GetVersion()
        {
            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
        }
    }
}
