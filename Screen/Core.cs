using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Screen {
    internal static class DwmWindowAttributes {
        public static int ExtendedFrameBounds = 0x9;   // Bounds without aero margins
    }

    internal static class WinAPI {
        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int GetClassName(IntPtr handle, StringBuilder builder, int capacity);

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern IntPtr FindWindow(string formClass, string formTitle);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll",SetLastError = true)]
        public static extern int GetWindowInfo(IntPtr hwnd, ref Structure.WindowInfo pwi);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out Structure.Rect pvAttribute, int cbAttribute);
    }

    internal static class Structure {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width {get {return Right - Left;}}
            public int Height {get {return Bottom - Top;}}
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowInfo {
            public int   cbSize;
            public Rect  rcWindow;
            public Rect  rcClient;
            public int   dwStyle;
            public int   dwExStyle;
            public int   dwWindowStatus;
            public uint  cxWindowBorders;
            public uint  cyWindowBorders;
            public short atomWindowType;
            public short wCreatorVersion;
        }
    }
}
