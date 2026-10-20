#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Window = System.Windows.Window;

namespace FoliCon.Modules.UI;

/// <summary>
/// Helper for managing window backdrop behavior, synchronizing DWM immersive dark mode across themes,
/// and suppressing duplicate native DWM caption buttons.
/// </summary>
public static class WindowBackdropHelper
{
    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x00080000;

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMSBT_MAINWINDOW = 2; // Mica
    private const uint WM_NCACTIVATE = 0x0086;

    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref val, sizeof(int));

            // Re-apply DWMWA_SYSTEMBACKDROP_TYPE = DWMSBT_MAINWINDOW (2) for Windows 11 Mica
            var backdrop = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

            // Force DWM to redraw the non-client frame and backdrop material by pulsing activation
            var fg = GetForegroundWindow();
            var isActive = fg == handle;
            SendMessage(handle, WM_NCACTIVATE, isActive ? IntPtr.Zero : (IntPtr)1, IntPtr.Zero);
            SendMessage(handle, WM_NCACTIVATE, isActive ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);

            // Force DWM to redraw the window frame and non-client title bar immediately
            SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

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

        if (Application.Current.Dispatcher.CheckAccess())
        {
            UpdateWindows();
        }
        else
        {
            Application.Current.Dispatcher.BeginInvoke(UpdateWindows);
        }
    }

    /// <summary>
    /// Suppresses native Windows DWM caption buttons (minimize, maximize, close) that Windows 11
    /// automatically renders when Mica/Acrylic backdrop is applied to a window with WS_SYSMENU.
    /// This ensures only HandyControl's styled WPF caption buttons are visible and interactive.
    /// </summary>
    public static void SuppressNativeCaptionButtons(Window window)
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
                var style = GetWindowLongPtr64(handle, GWL_STYLE).ToInt64();
                if ((style & WS_SYSMENU) != 0)
                {
                    SetWindowLongPtr64(handle, GWL_STYLE, new IntPtr(style & ~WS_SYSMENU));
                    changed = true;
                }
            }
            else
            {
                var style = GetWindowLong32(handle, GWL_STYLE);
                if ((style & WS_SYSMENU) != 0)
                {
                    SetWindowLong32(handle, GWL_STYLE, style & ~WS_SYSMENU);
                    changed = true;
                }
            }

            if (changed)
            {
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }
        }
        catch
        {
            // Fail safely on non-standard platforms
        }
    }
}
