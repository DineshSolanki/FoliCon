using Microsoft.Web.WebView2.Core;

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
            var detailedErrorMessage = $"Failed to start process at path: {path}. Exception: {e.Message}";
            
            Logger.Error(e, detailedErrorMessage);
            throw new InvalidOperationException(detailedErrorMessage, e);
        }
    }

    public static void CheckWebView2()
    {
        try
        {
            Logger.Info("Checking WebView2 Runtime availability");
            var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString(string.Empty);
            Logger.Info($"WebView2 Runtime version {availableBrowserVersionString} is available");
        }
        catch (WebView2RuntimeNotFoundException exception)
        {
            Logger.ForErrorEvent().Message("WebView2 Runtime is not installed.").Exception(exception).Log();
            
            var result = MessageBox.Show(CustomMessageBox.Ask(Lang.WebView2DownloadConfirmation,
                Lang.WebView2DownloadConfirmationHeader));
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            StartProcess("https://go.microsoft.com/fwlink/p/?LinkId=2124703");
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

    public static async Task RefreshIconCacheAsync()
    {
        try
        {
            Logger.Debug("Refreshing Icon Cache");

            var isWow64Redirected = Kernel32.Wow64DisableWow64FsRedirection(out _);

            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var exePath = Path.Combine(systemFolder, "ie4uinit.exe");

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = "-ClearIconCache",
                    WindowStyle = ProcessWindowStyle.Normal
                };

                process.Start();
                await process.WaitForExitAsync();
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

    private static bool? IfNotAdminRestartAsAdmin()
    {
        if (ApplicationHelper.IsAdministrator())
        {
            Logger.Info("Application is running as Administrator");
            return null;
        }
        if (MessageBox.Show(CustomMessageBox.Ask(Lang.RestartAsAdmin,
                Lang.ExceptionOccurred)) != MessageBoxResult.Yes)
        {
            return false;
        }

        StartAppAsAdmin();
        return true;
    }

    private static void StartAppAsAdmin()
    {
        Logger.Info("Starting Application as Administrator");
        
        try
        {
            ApplicationHelper.Restart(true);
        }
        catch (Exception ex)
        {
            Logger.ForErrorEvent().Message("Failed to start process with elevated rights {Message}",
                ex.Message).Exception(ex).Log();
        }
    }

    public static void AddToContextMenu()
    {
        Logger.Info("Modifying Context Menu");
        if (IfNotAdminRestartAsAdmin() == false)
        {
            return;
        }

        if (Services.Settings.IsExplorerIntegrated && Services.Settings.ContextEntryName != Lang.CreateIconsWithFoliCon)
        {
            Logger.Info("Removing Old Context Menu Entry");
            RemoveFromContextMenu();
        }

        Services.Settings.ContextEntryName = Lang.CreateIconsWithFoliCon;
        Services.Settings.IsExplorerIntegrated = true;
        Services.Settings.Save();

        RegisterContextMenuOptions(["Professional", "Movie", "TV", "Game", "Auto"]);

        Logger.Info("Context Menu Modified");
    }

    private static void RegisterContextMenuOptions(List<string> modes)
    {
        foreach (var mode in modes)
        {
            var modeName = mode == "Auto" ? MediaTypes.Mtv : mode;
            var command = $"""{Process.GetCurrentProcess().MainModule?.FileName}" --path "%1" --mode {modeName}""";
            RegisterContextMenuOption(mode, command);
        }
    }

    private static void RegisterContextMenuOption(string modeName, string command)
    {
        ApplicationHelper.RegisterCascadeContextMenuToDirectory(Lang.CreateIconsWithFoliCon, LangProvider.GetLang(modeName), command);
        ApplicationHelper.RegisterCascadeContextMenuToBackground(Lang.CreateIconsWithFoliCon, LangProvider.GetLang(modeName), command.Replace("%1", "%V"));
    }

    public static void RemoveFromContextMenu()
    {
        if (IfNotAdminRestartAsAdmin() == false)
        {
            return;
        }

        Services.Settings.IsExplorerIntegrated = false;
        Services.Settings.Save();
        ApplicationHelper.UnRegisterCascadeContextMenuFromDirectory(Services.Settings.ContextEntryName, "");
        ApplicationHelper.UnRegisterCascadeContextMenuFromBackground(Services.Settings.ContextEntryName, "");
    }
}