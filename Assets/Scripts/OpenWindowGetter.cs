using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using HWND = System.IntPtr;
using System.Collections.Generic;
using System;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;        // x position of upper-left corner
    public int Top;         // y position of upper-left corner
    public int Right;       // x position of lower-right corner
    public int Bottom;      // y position of lower-right corner
}

public class WindowInfo
{
    public WindowInfo(string _name, WINDOWINFO _info)
    {
        name = _name;
        info = _info;
    }
    public string name;
    public WINDOWINFO info;
}

[StructLayout(LayoutKind.Sequential)]
public struct WINDOWINFO
{
    public uint cbSize;
    public RECT rcWindow;
    public RECT rcClient;
    public uint dwStyle;
    public uint dwExStyle;
    public uint dwWindowStatus;
    public uint cxWindowBorders;
    public uint cyWindowBorders;
    public ushort atomWindowType;
    public ushort wCreatorVersion;
}

/// <summary>Contains functionality to get all the open windows.</summary>
public static class OpenWindowGetter
{
    private static Dictionary<HWND, WindowInfo> m_windows;
    public static Dictionary<HWND, WindowInfo> Windows { get { return m_windows; } }

    /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
    /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
    public static IDictionary<HWND, WindowInfo> GetOpenWindows()
    {
        HWND shellWindow = GetShellWindow();
        Dictionary<HWND, WindowInfo> windows = new Dictionary<HWND, WindowInfo>();

        EnumWindows(delegate (HWND hWnd, int lParam)
        {
            if (hWnd == shellWindow) return true;
            if (!IsWindowVisible(hWnd)) return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0) return true;

            StringBuilder builder = new StringBuilder(length);
            GetWindowText(hWnd, builder, length + 1);

            RECT rc;
            GetWindowRect(hWnd, out rc);

            WINDOWINFO windowInfo = new WINDOWINFO();
            windowInfo.cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
            GetWindowInfo(hWnd, ref windowInfo);

            windows[hWnd] = new WindowInfo(builder.ToString(), windowInfo);
            return true;

        }, 0);

        m_windows = windows;

        return windows;
    }

    public static WindowInfo GetWindow(HWND hWnd)
    {
        HWND shellWindow = GetShellWindow();

        if (hWnd == shellWindow) return null;
        if (!IsWindowVisible(hWnd)) return null;

        int length = GetWindowTextLength(hWnd);
        if (length == 0) return null;

        StringBuilder builder = new StringBuilder(length);
        GetWindowText(hWnd, builder, length + 1);

        RECT rc;
        GetWindowRect(hWnd, out rc);

        WINDOWINFO windowInfo = new WINDOWINFO();
        windowInfo.cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
        GetWindowInfo(hWnd, ref windowInfo);

        return new WindowInfo(builder.ToString(), windowInfo);
    }

    private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

    [DllImport("USER32.DLL")]
    private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

    [DllImport("USER32.DLL")]
    private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("USER32.DLL")]
    private static extern int GetWindowTextLength(HWND hWnd);

    [DllImport("USER32.DLL")]
    private static extern bool IsWindowVisible(HWND hWnd);

    [DllImport("USER32.DLL")]
    private static extern IntPtr GetShellWindow();

    [DllImport("USER32.DLL")]
    private static extern bool GetWindowRect(HWND hWnd, out RECT lpRect);

    [DllImport("USER32.DLL")]
    private static extern bool GetWindowInfo(HWND hwnd, ref WINDOWINFO pwi);
}