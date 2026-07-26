using System.Windows;
using System.Windows.Threading;

namespace FoliconTest;

/// <summary>
/// Provides a dedicated STA thread with a WPF Application and Dispatcher
/// for rendering compiled XAML views in tests. Sets Application.BaseUri
/// to the FoliCon assembly so that relative pack URIs in compiled XAML
/// (e.g. /Resources/poster_mockups/...) resolve correctly.
/// WPF allows only one Application per AppDomain, so a single dispatcher
/// thread is shared by every WpfTestHost instance and outlives them all.
/// </summary>
internal sealed class WpfTestHost : IDisposable
{
    private static readonly Lazy<Dispatcher> SharedDispatcher = new(StartDispatcherThread, LazyThreadSafetyMode.ExecutionAndPublication);

    private static Dispatcher StartDispatcherThread()
    {
        Dispatcher dispatcher = null!;
        using var ready = new ManualResetEventSlim(false);
        var staThread = new Thread(() =>
        {
            // Creating Application sets Application.Current (required for relative pack URIs).
            // Set BaseUri to FoliCon assembly so relative URIs like /Resources/... resolve
            // against FoliCon.dll resources, not the test runner assembly.
            if (Application.Current == null)
            {
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.GetType().GetProperty("BaseUri",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(app, new Uri("pack://application:,,,/FoliCon;component/"));
            }

            dispatcher = Dispatcher.CurrentDispatcher;
            // ReSharper disable once AccessToDisposedClosure - Set() runs before ready is disposed
            ready.Set();
            Dispatcher.Run();
        })
        {
            Name = "WPF-Test-Host",
            IsBackground = true
        };
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        ready.Wait();
        return dispatcher;
    }

    /// <summary>
    /// Execute a function on the STA thread and return the result.
    /// </summary>
    public T Invoke<T>(Func<T> func) => SharedDispatcher.Value.Invoke(func, DispatcherPriority.Send);

    /// <summary>
    /// Execute an action on the STA thread.
    /// </summary>
    public void Invoke(Action action) => SharedDispatcher.Value.Invoke(action, DispatcherPriority.Send);

    public void Dispose()
    {
        // Intentionally empty: the shared dispatcher thread must survive for
        // later tests and dies with the process (IsBackground = true).
    }
}
