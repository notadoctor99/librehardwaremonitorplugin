namespace NotADoctor99.LibreHardwareMonitorPlugin
{
    using System;
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern Boolean SetForegroundWindow(IntPtr hWnd);
    }
}
