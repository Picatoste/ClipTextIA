using System;
using System.Runtime.InteropServices;

namespace ClipTextIA
{
    public static class HwndSubclass
    {
        private static IntPtr _originalWndProc = IntPtr.Zero;
        private static WndProcDelegate _newWndProcDelegate;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int GWLP_WNDPROC = -4;

        public static void SetHook(IntPtr hwnd, Func<IntPtr, int, IntPtr, IntPtr, IntPtr> handler)
        {
            _newWndProcDelegate = (h, m, w, l) => handler(h, m, w, l);

            _originalWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, _newWndProcDelegate);
        }
    }
}
