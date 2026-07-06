using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AbyssOverlay;

public static class Win32Interop
{
    public const int HotkeyIdSelect = 1;
    public const int HotkeyIdAnalyze = 2;
    public const int HotkeyIdQuit = 3;

    public const int ModAlt = 0x0001;
    public const int ModControl = 0x0002;

    public const int WmHotkey = 0x0312;

    public const int GwlExstyle = -20;
    public const int WsExLayered = 0x00080000;
    public const int WsExTransparent = 0x00000020;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public static void EnableClickThrough(Window window, bool enable)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var exStyle = GetWindowLong(hwnd, GwlExstyle);
        if (enable)
        {
            exStyle |= WsExLayered | WsExTransparent;
        }
        else
        {
            exStyle |= WsExLayered;
            exStyle &= ~WsExTransparent;
        }
        SetWindowLong(hwnd, GwlExstyle, exStyle);
    }
}
