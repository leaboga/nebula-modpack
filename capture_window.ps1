Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public class ScreenCapture
{
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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

    public static void CaptureWindow(string windowName, string filePath)
    {
        IntPtr hWnd = FindWindow(null, windowName);
        if (hWnd == IntPtr.Zero) {
            Console.WriteLine("Window not found");
            return;
        }

        SetForegroundWindow(hWnd);
        System.Threading.Thread.Sleep(500); // Wait for window to focus

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

[ScreenCapture]::CaptureWindow("KRAKEN Launcher", "c:\Users\Leandro\source\repos\NebulaLauncher\window_debug.png")
