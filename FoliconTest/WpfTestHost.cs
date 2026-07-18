using System.Windows;
using System.Windows.Threading;

namespace FoliconTest;

/// <summary>
/// Provides a dedicated STA thread with a WPF Application and Dispatcher
/// for rendering compiled XAML views in tests. Sets Application.BaseUri
/// to the FoliCon assembly so that relative pack URIs in compiled XAML
/// (e.g. /Resources/poster_mockups/...) resolve correctly.
/// </summary>
internal sealed class WpfTestHost : IDisposable
{
    private readonly Thread _staThread;
    private Dispatcher _dispatcher = null!;
    private readonly ManualResetEventSlim _ready = new(false);

    public WpfTestHost()
    {
        _staThread = new Thread(() =>
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

            _dispatcher = Dispatcher.CurrentDispatcher;
            _ready.Set();
            Dispatcher.Run();
        })
        {
            Name = "WPF-Test-Host",
            IsBackground = true
        };
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        _ready.Wait();
    }

    /// <summary>
    /// Execute a function on the STA thread and return the result.
    /// </summary>
    public T Invoke<T>(Func<T> func)
    {
        return _dispatcher!.Invoke(func, DispatcherPriority.Send);
    }

    /// <summary>
    /// Execute an action on the STA thread.
    /// </summary>
    public void Invoke(Action action)
    {
        _dispatcher!.Invoke(action, DispatcherPriority.Send);
    }

    public void Dispose()
    {
        _dispatcher?.InvokeShutdown();
        _staThread.Join(5000);
        _ready.Dispose();
    }
}
