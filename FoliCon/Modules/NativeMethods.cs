namespace FoliCon.Modules;

public static class NativeMethods
{
    [DllImport("gdi32")]
    internal static extern int DeleteObject(IntPtr o);

    [DllImport("shell32.dll")]
    internal static extern void SHChangeNotify(int wEventId, int uFlags, int dwItem1, int dwItem2);
}