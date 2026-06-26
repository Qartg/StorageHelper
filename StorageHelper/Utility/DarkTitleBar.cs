using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StorageHelper.Utility
{

    public class DarkTitleBar
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyDarkBar(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;

            int value = 1;
            int result = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, 4);

            if (result != 0)
                result = DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, 4);
        }
    }
}
