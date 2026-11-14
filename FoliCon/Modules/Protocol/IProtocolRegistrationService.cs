#nullable enable
namespace FoliCon.Modules.Protocol;

/// <summary>
/// Service managing Windows Registry registration for the folicon:// protocol handler.
/// </summary>
public interface IProtocolRegistrationService
{
    /// <summary>
    /// Returns true if the folicon:// protocol scheme is currently registered in HKCU for the current application.
    /// </summary>
    bool IsRegistered(string? customAppPath = null);

    /// <summary>
    /// Registers or updates the folicon:// URI scheme in HKCU with quoted command template escaping.
    /// </summary>
    bool RegisterProtocol(string? customAppPath = null);

    /// <summary>
    /// Unregisters the folicon:// URI scheme from HKCU.
    /// </summary>
    bool UnregisterProtocol();
}
