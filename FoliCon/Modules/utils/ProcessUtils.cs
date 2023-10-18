using FoliCon.Modules.Configuration;
using FoliCon.Modules.UI;
using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class ProcessUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    /// <summary>
    /// Starts Process associated with given path.
    /// </summary>
    /// <param name="path">If the path is a URL, it opens the URL in the default browser. If the path is a file or folder path, it will be started.</param>
    public static void StartProcess(string path)
    {
        try
        {
            var processInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true
            };

            Logger.Debug($"Starting Process: {path}");
            Process.Start(processInfo);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to start process: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Terminates Explorer.exe Process.
    /// </summary>
    private static void KillExplorer()
    {
        Logger.Debug("Killing Explorer.exe");
        var taskKill = new ProcessStartInfo("taskkill", "/F /IM explorer.exe")
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        };
        using var process = new Process();
        process.StartInfo = taskKill;
        process.Start();
        process.WaitForExit();
        Logger.Debug("Explorer.exe Killed");
    }

    /// <summary>
    /// Terminates and Restart Explorer.exe process.
    /// </summary>
    public static void RestartExplorer()
    {
        KillExplorer();
        Process.Start("explorer.exe");
        Logger.Debug("Explorer.exe Restarted");
    }

    public static void RefreshIconCache()
    {
        try
        {
            Logger.Debug("Refreshing Icon Cache");

            var isWow64Redirected = Kernel32.Wow64DisableWow64FsRedirection(out _);

            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var exePath = Path.Combine(systemFolder, "ie4uinit.exe");

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    FileName = exePath,
                    Arguments = "-ClearIconCache",
                    WindowStyle = ProcessWindowStyle.Normal
                };

                process.Start();
                process.WaitForExit();
            }

            Logger.Debug("Icon Cache Refreshed");

            if (isWow64Redirected)
            {
                Kernel32.Wow64EnableWow64FsRedirection(true);
            }
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message($"Failed to refresh the icon cache due to error: {ex.Message}")
                .Exception(ex)
                .Log();
        }
    }

    public static IDictionary<string, string> GetCmdArgs()
    {
        Logger.Info("Getting Command Line Arguments");
        IDictionary<string, string> arguments = new Dictionary<string, string>();
        var args = Environment.GetCommandLineArgs();

        for (var index = 1; index < args.Length; index += 2)
        {
            var arg = args[index].Replace("--", "");
            arguments.Add(arg, args[index + 1]);
        }
        Logger.Info("Command Line Arguments: {@Arguments}", arguments);
        return arguments;
    }

    public static bool? IfNotAdminRestartAsAdmin()
    {
        if (ApplicationHelper.IsAdministrator())
        {
            Logger.Info("Application is running as Administrator");
            return null;
        }
        if (MessageBox.Show(CustomMessageBox.Ask(LangProvider.GetLang("RestartAsAdmin"),
                LangProvider.GetLang("Error"))) != MessageBoxResult.Yes) return false;

        StartAppAsAdmin();
        return true;
    }

    private static void StartAppAsAdmin()
    {
        Logger.Info("Starting Application as Administrator");

        var appPath = Path.ChangeExtension(AppPath, "exe");

        if (string.IsNullOrWhiteSpace(appPath))
        {
            Logger.Error("AppPath: {AppPath} is not valid", appPath);
            return;
        }

        var elevated = new ProcessStartInfo(appPath)
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            using var process = Process.Start(elevated);
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Failed to start process with elevated rights {Message}",
                ex.Message).Exception(ex).Log();
        }

        Environment.Exit(0);
    }

    public static void AddToContextMenu()
    {
        Logger.Info("Modifying Context Menu");
        if (IfNotAdminRestartAsAdmin() == false) return;

        if (Services.Settings.IsExplorerIntegrated && Services.Settings.ContextEntryName != LangProvider.GetLang("CreateIconsWithFoliCon"))
        {
            Logger.Info("Removing Old Context Menu Entry");
            RemoveFromContextMenu();
        }

        Services.Settings.ContextEntryName = LangProvider.GetLang("CreateIconsWithFoliCon");
        Services.Settings.IsExplorerIntegrated = true;
        Services.Settings.Save();

        RegisterContextMenuOptions(new List<string> { "Professional", "Movie", "TV", "Game", "Auto" });

        Logger.Info("Context Menu Modified");
    }

    private static void RegisterContextMenuOptions(List<string> modes)
    {
        foreach (var mode in modes)
        {
            var modeName = mode == "Auto" ? "Auto (Movies & TV Shows)" : mode;
            var command = $"""{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode {modeName}""";
            RegisterContextMenuOption(mode, command);
        }
    }

    private static void RegisterContextMenuOption(string modeName, string command)
    {
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang(modeName), command);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(LangProvider.GetLang("CreateIconsWithFoliCon"), LangProvider.GetLang(modeName), command.Replace("%1", "%V"));
    }

    public static void RemoveFromContextMenu()
    {
        if (IfNotAdminRestartAsAdmin() == false) return;
        Services.Settings.IsExplorerIntegrated = false;
        Services.Settings.Save();
        ApplicationHelper.UnRegisterCascadeContextMenuFromDirectory(Services.Settings.ContextEntryName, "");
        ApplicationHelper.UnRegisterCascadeContextMenuFromBackground(Services.Settings.ContextEntryName, "");

        //Growl.InfoGlobal("Merge Subtitle option removed from context menu!");
    }

    private static string AppPath { get; } = ApplicationHelper.GetExecutablePathNative();
}