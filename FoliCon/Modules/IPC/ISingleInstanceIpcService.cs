#nullable enable
using System;
using System.Threading.Tasks;

namespace FoliCon.Modules.IPC;

/// <summary>
/// Service managing single-instance process synchronization and hardened local IPC.
/// </summary>
public interface ISingleInstanceIpcService : IDisposable
{
    /// <summary>
    /// Checks whether this process is the first instance running in the current Windows logon session.
    /// </summary>
    bool CheckIsFirstInstance();

    /// <summary>
    /// Sends command line arguments to the running first instance via session-isolated named pipe.
    /// </summary>
    Task<bool> SendArgsToFirstInstanceAsync(string[] args, int timeoutMs = 3000);

    /// <summary>
    /// Starts the background named pipe server listening for arguments from subsequent instances.
    /// </summary>
    void StartServer(Action<string> onMessageReceived);
}
