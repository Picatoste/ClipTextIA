using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ClipTextIA
{

    public class ActiveWindowInfoService : IActiveWindowInfoService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public string GetActiveWindowProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            GetWindowThreadProcessId(hwnd, out uint pid);

            try
            {
                var process = Process.GetProcessById((int)pid);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetActiveWindowTitle()
        {
            IntPtr hwnd = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}
