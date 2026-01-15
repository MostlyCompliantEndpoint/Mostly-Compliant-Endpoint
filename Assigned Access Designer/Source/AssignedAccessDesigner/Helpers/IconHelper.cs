
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace AssignedAccessDesigner.Helpers
{
    public static class IconHelper
    {
        // --- Win32 fallback: Send WM_SETICON with HICON ---
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInstance, string lpName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        // uType values
        private const uint IMAGE_ICON = 1;
        // fuLoad flags
        private const uint LR_LOADFROMFILE = 0x0010;
        private const uint LR_DEFAULTSIZE = 0x0040;

        /// <summary>
        /// Sets the window icon for both AppWindow (title bar/taskbar) and Win32 (WM_SETICON) fallback.
        /// </summary>
        /// <param name="window">Your WinUI 3 Window (e.g., MainWindow).</param>
        /// <param name="icoPath">Relative or absolute path to the .ico. Example: "Assets\\AppIcon.ico"</param>
        public static void ApplyIcon(Window window, string icoPath)
        {
            if (window is null) throw new ArgumentNullException(nameof(window));
            if (string.IsNullOrWhiteSpace(icoPath)) throw new ArgumentNullException(nameof(icoPath));

            // Resolve to absolute path (works for packaged & unpackaged)
            var fullPath = ResolveToAbsolutePath(icoPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Icon file not found: {fullPath}");

            // 1) AppWindow.SetIcon — preferred path for Windows 11 / Windows App SDK
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                appWindow?.SetIcon(fullPath);
            }
            catch
            {
                // Swallow and continue to Win32 fallback
            }

            // 2) Win32 fallback — ensures both small and big icons are set for the HWND
            try
            {
                var hwnd = WindowNative.GetWindowHandle(window);

                // BIG icon (typically used by taskbar / Alt-Tab)
                var hIconBig = LoadImage(IntPtr.Zero, fullPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
                if (hIconBig != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hIconBig);
                }

                // SMALL icon (title bar)
                var hIconSmall = LoadImage(IntPtr.Zero, fullPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
                if (hIconSmall != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hIconSmall);
                }
            }
            catch
            {
                // If Win32 fallback fails, we leave the default icon
            }
        }

        private static string ResolveToAbsolutePath(string path)
        {
            // Absolute path already
            if (Path.IsPathRooted(path))
                return path;

            // For packaged apps, Environment.CurrentDirectory points to install location
            // For unpackaged, it is usually the working directory; we can also use AppContext.BaseDirectory
            var baseDir = AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(baseDir, path));
        }
    }
}