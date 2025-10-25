using System.Collections.Concurrent;

namespace FoliCon.Modules.utils;

/// <summary>
/// A dedicated STA thread-based renderer that processes rendering tasks sequentially on a single STA thread.
/// This avoids WPF's PackagePart race condition while maintaining high throughput through async queueing.
/// </summary>
[Localizable(false)]
public sealed class StaRenderer : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly Lazy<StaRenderer> Instance = new(() => new StaRenderer());
    
    private readonly Thread _staThread;
    private readonly BlockingCollection<RenderTask> _taskQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public static StaRenderer Default => Instance.Value;

    private StaRenderer()
    {
        _taskQueue = new BlockingCollection<RenderTask>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _staThread = new Thread(ProcessQueue)
        {
            Name = "WPF-STA-Renderer",
            IsBackground = true
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        
        Logger.Info("StaRenderer initialized with dedicated STA thread");
    }

    /// <summary>
    /// Queues a render operation to be executed on the dedicated STA thread.
    /// Multiple callers can enqueue simultaneously - they will be processed sequentially on the STA thread.
    /// </summary>
    public Task<T> EnqueueRender<T>(Func<T> renderFunc)
    {
        ThrowIfDisposed();
        
        var task = new RenderTask<T>(renderFunc);
        _taskQueue.Add(task);
        Logger.Debug("Render task enqueued, queue depth: {QueueCount}", _taskQueue.Count);
        
        return task.Task;
    }

    private void ProcessQueue()
    {
        Logger.Debug("STA renderer thread started");
        
        try
        {
            foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                try
                {
                    task.Execute();
                }
                catch (Exception ex)
                {
                    Logger.ForErrorEvent()
                        .Message("Error processing render task on STA thread")
                        .Exception(ex)
                        .Log();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("STA renderer thread cancelled");
        }
        
        Logger.Debug("STA renderer thread stopped");
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(StaRenderer));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellationTokenSource.Cancel();
        _taskQueue.CompleteAdding();
        
        if (!_staThread.Join(TimeSpan.FromSeconds(5)))
        {
            Logger.Warn("STA renderer thread did not terminate gracefully");
        }
        
        _taskQueue.Dispose();
        _cancellationTokenSource.Dispose();
        
        Logger.Info("StaRenderer disposed");
    }

    private abstract class RenderTask
    {
        public abstract void Execute();
    }

    private class RenderTask<T>(Func<T> renderFunc) : RenderTask
    {
        private readonly TaskCompletionSource<T> _tcs = new();

        public Task<T> Task => _tcs.Task;

        public override void Execute()
        {
            try
            {
                var result = renderFunc();
                _tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }
    }
}

