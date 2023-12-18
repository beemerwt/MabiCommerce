﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class WinApi
{
    private const uint WINEVENT_OUTOFCONTEXT = 0;
    private const uint EVENT_SYSTEM_FOREGROUND = 3;

    public delegate void ForegroundWindowChangedDelegate();
    delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }

    private IntPtr hWnd = IntPtr.Zero;
    private IntPtr m_hhook = IntPtr.Zero;

    private WinEventDelegate? dele = null;
    private ForegroundWindowChangedDelegate? m_onForegroundWindowChanged = null;

    public Rectangle ClientBounds;
    public event ForegroundWindowChangedDelegate OnForegroundWindowChanged
    {
        add
        {
            m_onForegroundWindowChanged += value;
        }
        remove
        {
            m_onForegroundWindowChanged -= value;
        }
    }

    private WinApi()
	{
        dele = new WinEventDelegate(ForegroundWindowChanged);
        m_hhook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
        ClientBounds = new(0, 0, 0, 0);
    }

	~WinApi()
	{
        if (m_hhook != IntPtr.Zero)
            UnhookWinEvent(m_hhook);
    }

	public static WinApi Instance { get; private set; } = new WinApi();
    public static bool IsMabiActive { get { return Instance.hWnd != IntPtr.Zero; } }

    private string? GetActiveWindowTitle(IntPtr hwnd)
    {
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);

        if (GetWindowText(hwnd, Buff, nChars) > 0)
            return Buff.ToString();

        return null;
    }

    private void ForegroundWindowChanged(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;
        string title = GetActiveWindowTitle(hwnd) ?? "";

        if (title.CompareTo("Mabinogi") != 0)
            return;

        hWnd = hwnd;

        GetWindowRect(new HandleRef(this, hwnd), out RECT rect);
        ClientBounds = new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

        m_onForegroundWindowChanged?.Invoke();
    }
}