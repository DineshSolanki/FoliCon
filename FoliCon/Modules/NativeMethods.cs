using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace FoliCon.Modules
{
    public static class NativeMethods
    {
        [DllImport("gdi32")]
        public static extern int DeleteObject(IntPtr o);
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int wEventId, int uFlags, int dwItem1, int dwItem2);
    }
}
