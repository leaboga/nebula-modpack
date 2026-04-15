using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace KrakenLauncher.Services
{
    public static class ModernUIHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_BORDER_COLOR = 34;

        // Backdrop Types
        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Tabbed

        public static void ApplyMica(Window window)
        {
            if (Environment.OSVersion.Version.Build < 22000) return; // Only Win11+

            var windowInteropHelper = new WindowInteropHelper(window);
            IntPtr hwnd = windowInteropHelper.Handle;

            // Set Background to Transparent in XAML first!
            window.Background = System.Windows.Media.Brushes.Transparent;

            int backdropType = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

            // Optional: Dark mode caption
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
        }

        public static void ApplyAcrylic(Window window)
        {
            if (Environment.OSVersion.Version.Build < 22000) return;

            var windowInteropHelper = new WindowInteropHelper(window);
            IntPtr hwnd = windowInteropHelper.Handle;

            window.Background = System.Windows.Media.Brushes.Transparent;

            int backdropType = DWMSBT_TRANSIENTWINDOW;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            
            int darkMode = 1;
            DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
        }
        
        public static void SetDarkTitleBar(Window window)
        {
             if (Environment.OSVersion.Version.Major < 10) return;
             
             var windowInteropHelper = new WindowInteropHelper(window);
             IntPtr hwnd = windowInteropHelper.Handle;
             
             int darkMode = 1;
             DwmSetWindowAttribute(hwnd, 20, ref darkMode, sizeof(int));
        }
    }
}
