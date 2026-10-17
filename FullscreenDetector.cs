using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace L14KeyboardLight
{
    public static class FullscreenDetector
    {
        private const int EdgeTolerance = 2;
        private const int GwlStyle = -16;
        private const long WsCaption = 0x00C00000L;
        private const uint MonitorDefaultToNearest = 2;
        private const int DwmwaExtendedFrameBounds = 9;
        private const int DwmwaCloaked = 14;

        // Program must establish per-monitor DPI awareness before calling this class.
        // DWM frame bounds are physical screen pixels, unlike DPI-virtualized bounds.
        public static bool IsForegroundFullscreen()
        {
            ForegroundInfo info;
            return TryReadForeground(out info) && IsFullscreen(info);
        }

        public static string DescribeForeground()
        {
            ForegroundInfo info;
            if (!TryReadForeground(out info))
                return "Foreground window unavailable, changed during inspection, or has invalid bounds.";

            return String.Format(CultureInfo.InvariantCulture,
                "hwnd=0x{0:X}, process={1}, class={2}, window={3}, monitor={4}, visible={5}, minimized={6}, cloaked={7}, shell={8}, zoomed={9}, caption={10}, fullscreen={11}",
                info.Handle.ToInt64(), info.ProcessName, info.ClassName,
                info.Window, info.Monitor, info.Visible, info.Minimized,
                info.Cloaked, info.Shell, info.Zoomed, info.HasCaption, IsFullscreen(info));
        }

        public static bool IsFullscreenGeometry(Rectangle window, Rectangle monitor,
            bool visible, bool minimized, bool cloaked, bool shell, bool zoomed, bool hasCaption)
        {
            if (!visible || minimized || cloaked || shell || (zoomed && hasCaption))
                return false;
            if (window.Width <= 0 || window.Height <= 0 || monitor.Width <= 0 || monitor.Height <= 0)
                return false;

            // Compare every edge. Equal size alone fails for an offset window;
            // containment alone mistakes oversized/spanning windows for fullscreen.
            // Long arithmetic also avoids overflow at extreme virtual-screen coordinates.
            return Near(window.Left, monitor.Left)
                && Near(window.Top, monitor.Top)
                && Near((long)window.Left + window.Width, (long)monitor.Left + monitor.Width)
                && Near((long)window.Top + window.Height, (long)monitor.Top + monitor.Height);
        }

        private static bool Near(long first, long second)
        {
            return Math.Abs(first - second) <= EdgeTolerance;
        }

        private static bool IsFullscreen(ForegroundInfo info)
        {
            return IsFullscreenGeometry(info.Window, info.Monitor, info.Visible,
                info.Minimized, info.Cloaked, info.Shell, info.Zoomed, info.HasCaption);
        }

        private static bool TryReadForeground(out ForegroundInfo info)
        {
            info = new ForegroundInfo();
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero)
                return false;

            info.Handle = window;
            info.Visible = IsWindowVisible(window);
            info.Minimized = IsIconic(window);
            info.Zoomed = IsZoomed(window);
            long style = IntPtr.Size == 8
                ? GetWindowLongPtr64(window, GwlStyle).ToInt64()
                : GetWindowLong32(window, GwlStyle);
            info.HasCaption = (style & WsCaption) == WsCaption;
            StringBuilder className = new StringBuilder(256);
            GetClassName(window, className, className.Capacity);
            info.ClassName = className.ToString();
            info.ProcessName = ReadProcessName(window);
            info.Shell = IsShellOrLockWindow(window, info.ClassName, info.ProcessName);
            info.Cloaked = IsCloaked(window);

            NativeRect bounds;
            if (!TryGetFrameBounds(window, out bounds) || !TryConvertRectangle(bounds, out info.Window))
                return false;

            IntPtr monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            MonitorInfo monitorInfo = new MonitorInfo();
            monitorInfo.Size = Marshal.SizeOf(typeof(MonitorInfo));
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo)
                || !TryConvertRectangle(monitorInfo.Monitor, out info.Monitor))
                return false;

            // Do not apply a result for a window that lost focus while being read.
            return GetForegroundWindow() == window && IsWindow(window);
        }

        private static bool TryGetFrameBounds(IntPtr window, out NativeRect bounds)
        {
            bounds = new NativeRect();
            try
            {
                if (DwmGetWindowRect(window, DwmwaExtendedFrameBounds, out bounds,
                    Marshal.SizeOf(typeof(NativeRect))) == 0
                    && bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                    return true;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            return GetWindowRect(window, out bounds);
        }

        private static bool IsCloaked(IntPtr window)
        {
            try
            {
                int cloaked;
                return DwmGetWindowInt(window, DwmwaCloaked, out cloaked, sizeof(int)) == 0
                    && cloaked != 0;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        private static bool TryConvertRectangle(NativeRect value, out Rectangle rectangle)
        {
            long width = (long)value.Right - value.Left;
            long height = (long)value.Bottom - value.Top;
            rectangle = Rectangle.Empty;
            if (width <= 0 || height <= 0 || width > Int32.MaxValue || height > Int32.MaxValue)
                return false;
            rectangle = new Rectangle(value.Left, value.Top, (int)width, (int)height);
            return true;
        }

        private static string ReadProcessName(IntPtr window)
        {
            uint processId;
            GetWindowThreadProcessId(window, out processId);
            if (processId == 0 || processId > Int32.MaxValue)
                return "(unavailable)";
            try
            {
                using (Process process = Process.GetProcessById((int)processId))
                    return process.ProcessName;
            }
            catch (ArgumentException) { return "(unavailable)"; }
            catch (InvalidOperationException) { return "(unavailable)"; }
            catch (Win32Exception) { return "(unavailable)"; }
        }

        private static bool IsShellOrLockWindow(IntPtr window, string className, string processName)
        {
            return window == GetDesktopWindow() || window == GetShellWindow()
                || EqualsIgnoreCase(className, "Progman")
                || EqualsIgnoreCase(className, "WorkerW")
                || EqualsIgnoreCase(className, "Shell_TrayWnd")
                || EqualsIgnoreCase(className, "Shell_SecondaryTrayWnd")
                || EqualsIgnoreCase(className, "LogonUI")
                || EqualsIgnoreCase(className, "LockScreen")
                || EqualsIgnoreCase(processName, "LogonUI")
                || EqualsIgnoreCase(processName, "LockApp");
        }

        private static bool EqualsIgnoreCase(string first, string second)
        {
            return String.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private struct ForegroundInfo
        {
            public IntPtr Handle;
            public string ClassName;
            public string ProcessName;
            public Rectangle Window;
            public Rectangle Monitor;
            public bool Visible;
            public bool Minimized;
            public bool Cloaked;
            public bool Shell;
            public bool Zoomed;
            public bool HasCaption;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect WorkArea;
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder name, int maximum);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr window, out NativeRect bounds);
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        private static extern int DwmGetWindowRect(IntPtr window, int attribute, out NativeRect value, int size);
        [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
        private static extern int DwmGetWindowInt(IntPtr window, int attribute, out int value, int size);
    }
}
