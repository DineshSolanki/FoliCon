using NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules;

//Taken from-https://stackoverflow.com/a/16722767/8076598
public static class StaTask
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static Task<T> Start<T>(Func<T> func)
    {
        Logger.Debug("Starting STA task");
        var tcs = new TaskCompletionSource<T>();
        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception e)
            {
                Logger.ForErrorEvent().Message("Error in STA task")
                    .Property("message", e.Message)
                    .Exception(e)
                    .Log();
                tcs.SetException(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Logger.Debug("STA task started");
        return tcs.Task;
    }

    public static Task StartStaTask(Action func)
    {
        var tcs = new TaskCompletionSource<object>();
        var thread = new Thread(() =>
        {
            try
            {
                func();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
}