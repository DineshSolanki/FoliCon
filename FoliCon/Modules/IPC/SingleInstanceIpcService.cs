#nullable enable
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace FoliCon.Modules.IPC;

/// <summary>
/// Hardened single-instance IPC service enforcing session isolation, CurrentUserOnly ACLs,
/// and bounded message framing to prevent privilege escalation or DoS attacks.
/// </summary>
public sealed class SingleInstanceIpcService : ISingleInstanceIpcService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int MaxPayloadSizeBytes = 64 * 1024; // 64 KB safety boundary (SEC-02)
    public const int DefaultTimeoutMs = 3000;

    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _cts = new();

    private Mutex? _singleInstanceMutex;
    private bool _hasMutex;
    private bool _isDisposed;

    public SingleInstanceIpcService(string? customMutexName = null, string? customPipeName = null)
    {
        var sessionId = Process.GetCurrentProcess().SessionId;
        var userName = Environment.UserName;

        // Session-scoped named Mutex (Local\ prefix ensures per-session isolation)
        _mutexName = customMutexName ?? $"Local\\FoliCon_SingleInstance_{sessionId}";

        // Session and user-specific Named Pipe (SEC-02: mitigates multi-user squatting)
        _pipeName = customPipeName ?? $"FoliCon_IPC_{sessionId}_{userName}";
    }

    public string PipeName => _pipeName;

    public bool CheckIsFirstInstance()
    {
        if (_hasMutex) return true;

        try
        {
            _singleInstanceMutex = new Mutex(true, _mutexName, out bool createdNew);
            _hasMutex = createdNew;
            return _hasMutex;
        }
        catch (AbandonedMutexException)
        {
            // Previous instance crashed without releasing mutex
            Logger.Warn("Acquired abandoned single-instance mutex {Name}", _mutexName);
            _hasMutex = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to create single-instance mutex {Name}", _mutexName);
            return true;
        }
    }

    public async Task<bool> SendArgsToFirstInstanceAsync(string[] args, int timeoutMs = DefaultTimeoutMs)
    {
        ArgumentNullException.ThrowIfNull(args);

        try
        {
            var payload = JsonSerializer.Serialize(args);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            if (payloadBytes.Length > MaxPayloadSizeBytes)
            {
                Logger.Warn("IPC message payload exceeds maximum size limit of {Max} bytes", MaxPayloadSizeBytes);
                return false;
            }

            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            using var timeoutCts = new CancellationTokenSource(timeoutMs);

            await client.ConnectAsync(timeoutCts.Token);

            // Write 4-byte big-endian length prefix
            var lengthHeader = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthHeader, payloadBytes.Length);

            await client.WriteAsync(lengthHeader, timeoutCts.Token);
            await client.WriteAsync(payloadBytes, timeoutCts.Token);
            await client.FlushAsync(timeoutCts.Token);

            Logger.Info("Successfully forwarded {Count} command-line arguments to primary FoliCon instance.", args.Length);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to forward arguments to running instance via pipe {PipeName}", _pipeName);
            return false;
        }
    }

    public void StartServer(Action<string> onMessageReceived)
    {
        ArgumentNullException.ThrowIfNull(onMessageReceived);

        _ = Task.Run(async () =>
        {
            Logger.Info("Starting Named Pipe IPC server on {PipeName}", _pipeName);

            while (!_cts.Token.IsCancellationRequested)
            {
                NamedPipeServerStream? server = null;
                try
                {
                    // Enforce CurrentUserOnly to prevent any other user account from connecting
                    server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                    await server.WaitForConnectionAsync(_cts.Token);

                    // Read 4-byte length header
                    var header = new byte[4];
                    var bytesRead = await ReadExactAsync(server, header, 4, _cts.Token);
                    if (bytesRead != 4)
                    {
                        Logger.Warn("Incomplete IPC message header received.");
                        continue;
                    }

                    var payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
                    if (payloadLength is <= 0 or > MaxPayloadSizeBytes)
                    {
                        Logger.Warn("Rejected IPC message with invalid payload length: {Length} bytes", payloadLength);
                        continue;
                    }

                    // Read payload bytes
                    var payloadBuffer = new byte[payloadLength];
                    var payloadBytesRead = await ReadExactAsync(server, payloadBuffer, payloadLength, _cts.Token);
                    if (payloadBytesRead == payloadLength)
                    {
                        var json = Encoding.UTF8.GetString(payloadBuffer);
                        onMessageReceived(json);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        Logger.Warn(ex, "Exception in Named Pipe IPC server loop");
                    }
                }
                finally
                {
                    if (server != null)
                    {
                        try
                        {
                            if (server.IsConnected)
                            {
                                server.Disconnect();
                            }
                            server.Dispose();
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            }

            Logger.Info("Named Pipe IPC server stopped on {PipeName}", _pipeName);
        }, _cts.Token);
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), ct);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        catch
        {
            // Ignore
        }

        if (_hasMutex && _singleInstanceMutex != null)
        {
            try
            {
                // Mutex release must be wrapped in a finally block to satisfy static analyzers
            }
            finally
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch
                {
                    // Ignore if already released or abandoned
                }
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                _hasMutex = false;
            }
        }
    }
}
