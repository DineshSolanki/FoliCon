#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace FoliCon.Modules.Protocol;

/// <summary>
/// Registers the folicon:// protocol scheme in HKCU with argument escaping and safe-open flags.
/// </summary>
public class ProtocolRegistrationService : IProtocolRegistrationService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string ProtocolScheme = "folicon";
    public const string ProtocolClassesSubKey = @"Software\Classes\folicon";

    private readonly RegistryKey? _rootKeyOverride;

    public ProtocolRegistrationService(RegistryKey? rootKeyOverride = null)
    {
        _rootKeyOverride = rootKeyOverride;
    }

    private RegistryKey GetRootKey() => _rootKeyOverride ?? Registry.CurrentUser;

    public bool IsRegistered(string? customAppPath = null)
    {
        try
        {
            var appPath = customAppPath ?? GetCurrentAppPath();
            using var baseKey = GetRootKey().OpenSubKey(ProtocolClassesSubKey);
            if (baseKey == null) return false;

            var urlProtocol = baseKey.GetValue("URL Protocol");
            if (urlProtocol == null) return false;

            using var commandKey = baseKey.OpenSubKey(@"shell\open\command");
            if (commandKey == null) return false;

            var commandValue = commandKey.GetValue(string.Empty) as string;
            if (string.IsNullOrEmpty(commandValue)) return false;

            return commandValue.Contains(appPath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to verify folicon:// protocol registration state.");
            return false;
        }
    }

    public bool RegisterProtocol(string? customAppPath = null)
    {
        try
        {
            var appPath = customAppPath ?? GetCurrentAppPath();
            if (string.IsNullOrWhiteSpace(appPath) || !File.Exists(appPath))
            {
                Logger.Warn("Cannot register protocol: executable path '{Path}' does not exist.", appPath);
                return false;
            }

            using var root = GetRootKey();
            using var foliconKey = root.CreateSubKey(ProtocolClassesSubKey);
            if (foliconKey == null) return false;

            foliconKey.SetValue(string.Empty, "URL:FoliCon Protocol");
            foliconKey.SetValue("URL Protocol", string.Empty);
            foliconKey.SetValue("EditFlags", 0x00200000, RegistryValueKind.DWord); // FTA_OpenIsSafe

            using (var iconKey = foliconKey.CreateSubKey("DefaultIcon"))
            {
                iconKey.SetValue(string.Empty, $"\"{appPath}\",0");
            }

            using (var commandKey = foliconKey.CreateSubKey(@"shell\open\command"))
            {
                // CRITICAL SECURITY (SEC-04): Strictly quote %1 to prevent CLI argument injection
                commandKey.SetValue(string.Empty, $"\"{appPath}\" \"%1\"");
            }

            Logger.Info("Successfully registered folicon:// protocol handler in HKCU.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to register folicon:// protocol handler.");
            return false;
        }
    }

    public bool UnregisterProtocol()
    {
        try
        {
            using var root = GetRootKey();
            root.DeleteSubKeyTree(ProtocolClassesSubKey, throwOnMissingSubKey: false);
            Logger.Info("Successfully unregistered folicon:// protocol handler.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to unregister folicon:// protocol handler.");
            return false;
        }
    }

    public static string GetCurrentAppPath()
    {
        var mainModule = Process.GetCurrentProcess().MainModule;
        return mainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "FoliCon.exe");
    }
}
