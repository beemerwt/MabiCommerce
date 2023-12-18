using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace MabiCommerce.Hotkey
{
    /// <summary>
    /// Simpler way to expose key modifiers
    /// </summary>
    [Flags]
    public enum HotKeyModifiers
    {
        Alt = 1,        // MOD_ALT
        Control = 2,    // MOD_CONTROL
        Shift = 4,      // MOD_SHIFT
        WindowsKey = 8, // MOD_WIN
    }

    [Flags]
    public enum FormsKeys
    {
        End = 0x23,
        Up = 0x26,
        Down = 0x28,
    }

    // --------------------------------------------------------------------------
    /// <summary>
    /// A nice generic class to register multiple hotkeys for your app
    /// </summary>
    // --------------------------------------------------------------------------
    public class HotkeyHelper : IDisposable
    {
        // Required interop declarations for working with hotkeys
        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(nint hwnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32", SetLastError = true)]
        public static extern int UnregisterHotKey(nint hwnd, int id);

        [DllImport("kernel32", SetLastError = true)]
        public static extern short GlobalAddAtom(string lpString);

        [DllImport("kernel32", SetLastError = true)]
        public static extern short GlobalDeleteAtom(short nAtom);

        public const int WM_HOTKEY = 0x0312;

        /// <summary>
        /// The unique ID to receive hotkey messages
        /// </summary>
        public short HotkeyID { get; private set; }

        /// <summary>
        /// Handle to the window listening to hotkeys
        /// </summary>
        private nint _windowHandle;

        /// <summary>
        /// Callback for hot keys
        /// </summary>
        Action<int> _onHotKeyPressed;

        // --------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        // --------------------------------------------------------------------------

        public HotkeyHelper(Window handlerWindow, Action<int> hotKeyHandler)
        {
            _onHotKeyPressed = hotKeyHandler;

            // Create a unique Id for this class in this instance
            string atomName = Thread.CurrentThread.ManagedThreadId.ToString("X8") + GetType().FullName;
            HotkeyID = GlobalAddAtom(atomName);

            // Set up the hook to listen for hot keys
            _windowHandle = new WindowInteropHelper(handlerWindow).Handle;
            var source = HwndSource.FromHwnd(_windowHandle);
            source.AddHook(HwndHook);
        }

        // --------------------------------------------------------------------------
        /// <summary>
        /// Intermediate processing of hotkeys
        /// </summary>
        // --------------------------------------------------------------------------
        private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyID)
            {
                _onHotKeyPressed?.Invoke(lParam.ToInt32());
                handled = true;
            }

            return nint.Zero;
        }

        // --------------------------------------------------------------------------
        /// <summary>
        /// Tell what key you want to listen for.  Returns an id representing
        /// this particular key combination.  Use this in your handler to
        /// disambiguate what key was pressed.
        /// </summary>
        // --------------------------------------------------------------------------
        public uint ListenForHotKey(FormsKeys key, HotKeyModifiers modifiers)
        {
            RegisterHotKey(_windowHandle, HotkeyID, (uint)modifiers, (uint)key);
            return (uint)modifiers | (uint)key << 16;
        }

        // --------------------------------------------------------------------------
        /// <summary>
        /// Stop listening for hotkeys
        /// </summary>
        // --------------------------------------------------------------------------
        private void StopListening()
        {
            if (HotkeyID == 0)
                return;

            UnregisterHotKey(_windowHandle, HotkeyID);
            // clean up the atom list
            GlobalDeleteAtom(HotkeyID);
            HotkeyID = 0;
        }

        // --------------------------------------------------------------------------
        /// <summary>
        /// Dispose
        /// </summary>
        // --------------------------------------------------------------------------
        public void Dispose()
        {
            StopListening();
        }
    }
}
