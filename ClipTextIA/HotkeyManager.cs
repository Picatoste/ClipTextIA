using ClipTextIA;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.System;

namespace ClipTextIA
{
    public class HotkeyManager : IHotkeyManager
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private Action _callback;

        public void RegisterHotkey(VirtualKey key, HotkeyModifiers modifiers, Action callback)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App).Window);
            _callback = callback;

            RegisterHotKey(hwnd, HOTKEY_ID, (int)modifiers, (int)key);

            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                SubclassWindow(hwnd);
            });
        }

        private void SubclassWindow(IntPtr hwnd)
        {
            // Hook the window to receive WM_HOTKEY
            HwndSubclass.SetHook(hwnd, WndProc);
        }

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                _callback?.Invoke();
            }

            return IntPtr.Zero;
        }
    }
}
