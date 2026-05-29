Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

public class ScreenCapture
{
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void CaptureWindowByProcess(string processName, string filePath)
    {
        Process[] procs = Process.GetProcessesByName(processName);
        if (procs.Length == 0) {
            Console.WriteLine("Process not found");
            return;
        }

        IntPtr hWnd = procs[0].MainWindowHandle;
        if (hWnd == IntPtr.Zero) {
            Console.WriteLine("Window handle not found");
            return;
        }

        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(1000); // Wait for window to focus

        RECT rect;
        GetWindowRect(hWnd, out rect);
        
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0) return;

        using (Bitmap bmp = new Bitmap(width, height))
        {
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            bmp.Save(filePath, ImageFormat.Png);
        }
    }
}
"@ -ReferencedAssemblies System.Drawing, System.Windows.Forms

[ScreenCapture]::CaptureWindowByProcess("KrakenLauncher", "c:\Users\Leandro\source\repos\NebulaLauncher\window_debug2.png")
