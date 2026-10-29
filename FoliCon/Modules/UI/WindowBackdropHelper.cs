#nullable enable
using Window = System.Windows.Window;

namespace FoliCon.Modules.UI;

/// <summary>
/// Helper for managing window backdrop behavior, synchronizing DWM immersive dark mode across themes,
/// and suppressing duplicate native DWM caption buttons.
/// </summary>
public static class WindowBackdropHelper
{
    private const int gwlStyle = -16;
    private const int wsSysmenu = 0x00080000;

    private const uint swpNosize = 0x0001;
    private const uint swpNomove = 0x0002;
    private const uint swpNozorder = 0x0004;
    private const uint swpNoactivate = 0x0010;
    private const uint swpFramechanged = 0x0020;

    private const int dwmwaUseImmersiveDarkModeBefore20H1 = 19;
    private const int dwmwaUseImmersiveDarkMode = 20;
    private const int dwmwaSystembackdropType = 38;
    private const int dwmsbtMainwindow = 2; // Mica
    private const uint wmNcactivate = 0x0086;

    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>
    /// Sets the DWM immersive dark mode attribute on the HWND and immediately forces DWM
    /// to recompute and repaint the non-client title bar frame and backdrop material.
    /// </summary>
    public static void SetImmersiveDarkMode(IntPtr handle, bool isDark)
    {
        if (handle == IntPtr.Zero) return;

        try
        {
            // Sync HandyControl's internal Mica dark mode state if available
            try
            {
                if (isDark)
                {
                    handle.ApplyDarkMode();
                }
                else
                {
                    handle.RemoveDarkMode();
                }
            }
            catch
            {
                // Fall back to direct DWM P/Invoke below
            }

            // Direct DWMWA_USE_IMMERSIVE_DARK_MODE call for Windows 10 (19/20) & Windows 11 (20)
            var val = isDark ? 1 : 0;
            DwmSetWindowAttribute(handle, dwmwaUseImmersiveDarkMode, ref val, sizeof(int));
            DwmSetWindowAttribute(handle, dwmwaUseImmersiveDarkModeBefore20H1, ref val, sizeof(int));

            // Re-apply DWMWA_SYSTEMBACKDROP_TYPE = DWMSBT_MAINWINDOW (2) for Windows 11 Mica
            var backdrop = dwmsbtMainwindow;
            DwmSetWindowAttribute(handle, dwmwaSystembackdropType, ref backdrop, sizeof(int));

            // Force DWM to redraw the non-client frame and backdrop material by pulsing activation
            var fg = GetForegroundWindow();
            var isActive = fg == handle;
            SendMessage(handle, wmNcactivate, isActive ? IntPtr.Zero : 1, IntPtr.Zero);
            SendMessage(handle, wmNcactivate, isActive ? 1 : IntPtr.Zero, IntPtr.Zero);

            // Force DWM to redraw the window frame and non-client title bar immediately
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                swpNomove | swpNosize | swpNozorder | swpNoactivate | swpFramechanged);

            // Re-suppress native caption buttons to prevent DWM from painting duplicate buttons
            SuppressNativeCaptionButtons(handle);
        }
        catch
        {
            // Fail safely on non-DWM or non-Windows platforms
        }
    }

    /// <summary>
    /// Updates DWM immersive dark mode for the specified WPF Window. If the HWND is not yet
    /// initialized, hooks SourceInitialized to ensure dark mode is applied once the HWND exists.
    /// </summary>
    public static void UpdateImmersiveDarkMode(Window window, bool isDark)
    {

        try
        {
            if (isDark)
            {
                window.ApplyDarkMode();
            }
            else
            {
                window.RemoveDarkMode();
            }
        }
        catch
        {
            // ignored
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            SetImmersiveDarkMode(handle, isDark);
            window.InvalidateVisual();
        }
        else
        {
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                window.SourceInitialized -= handler;
                var hwnd = new WindowInteropHelper(window).Handle;
                SetImmersiveDarkMode(hwnd, isDark);
                window.InvalidateVisual();
            };
            window.SourceInitialized += handler;
        }
    }

    /// <summary>
    /// Updates DWM immersive dark mode across all currently open windows in the application.
    /// Safely marshals to the UI dispatcher if invoked from a non-UI thread.
    /// </summary>
    public static void UpdateAllOpenWindows(bool isDark)
    {
        if (Application.Current == null) return;

        if (Application.Current.Dispatcher.CheckAccess())
        {
            UpdateWindows();
        }
        else
        {
            Application.Current.Dispatcher.BeginInvoke(UpdateWindows);
        }
        return;

        void UpdateWindows()
        {
            try
            {
                var windows = Application.Current.Windows.OfType<Window>().ToArray();
                foreach (var window in windows)
                {
                    UpdateImmersiveDarkMode(window, isDark);
                }
            }
            catch
            {
                // Fail safely
            }
        }
    }

    /// <summary>
    /// Suppresses native Windows DWM caption buttons (minimize, maximize, close) that Windows 11
    /// automatically renders when Mica/Acrylic backdrop is applied to a window with WS_SYSMENU.
    /// This ensures only HandyControl's styled WPF caption buttons are visible and interactive.
    /// </summary>
    public static void SuppressNativeCaptionButtons(Window? window)
    {
        if (window == null) return;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            SuppressNativeCaptionButtons(handle);
        }
        else
        {
            window.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                SuppressNativeCaptionButtons(hwnd);
            };
        }
    }

    /// <summary>
    /// Strips WS_SYSMENU from the HWND style to prevent DWM from painting native caption buttons.
    /// </summary>
    public static void SuppressNativeCaptionButtons(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;

        try
        {
            var changed = false;
            if (IntPtr.Size == 8)
            {
                var style = GetWindowLongPtr64(handle, gwlStyle).ToInt64();
                if ((style & wsSysmenu) != 0)
                {
                    SetWindowLongPtr64(handle, gwlStyle, new IntPtr(style & ~wsSysmenu));
                    changed = true;
                }
            }
            else
            {
                var style = GetWindowLong32(handle, gwlStyle);
                if ((style & wsSysmenu) != 0)
                {
                    SetWindowLong32(handle, gwlStyle, style & ~wsSysmenu);
                    changed = true;
                }
            }

            if (changed)
            {
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                    swpNomove | swpNosize | swpNozorder | swpNoactivate | swpFramechanged);
            }
        }
        catch
        {
            // Fail safely on non-standard platforms
        }
    }
}
