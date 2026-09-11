using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace IconFlow;

internal static class WindowSizing
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    private static readonly IntPtr TopMost = new(-1);
    private static readonly IntPtr NotTopMost = new(-2);
    private const uint NoMove = 0x0002, NoSize = 0x0001, NoActivate = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    public static void ResizeAndCenter(Window window, int logicalWidth, int logicalHeight)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var scale = Math.Max(1d, GetDpiForWindow(handle) / 96d);
        var area = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
        var work = area.WorkArea;
        var margin = (int)Math.Round(24 * scale);
        var width = Math.Min((int)Math.Round(logicalWidth * scale), work.Width - margin * 2);
        var height = Math.Min((int)Math.Round(logicalHeight * scale), work.Height - margin * 2);
        var x = work.X + Math.Max(margin, (work.Width - width) / 2);
        var y = work.Y + Math.Max(margin, (work.Height - height) / 2);
        window.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
    }

    public static void ResizeNearCursor(Window window, int logicalWidth, int logicalHeight)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var scale = Math.Max(1d, GetDpiForWindow(handle) / 96d);
        if (!GetCursorPos(out var cursor)) { ResizeAndCenter(window, logicalWidth, logicalHeight); return; }
        var point = new Windows.Graphics.PointInt32(cursor.X, cursor.Y);
        var area = DisplayArea.GetFromPoint(point, DisplayAreaFallback.Nearest);
        var work = area.WorkArea;
        var gap = (int)Math.Round(14 * scale);
        var width = Math.Min((int)Math.Round(logicalWidth * scale), work.Width - gap * 2);
        var height = Math.Min((int)Math.Round(logicalHeight * scale), work.Height - gap * 2);
        var x = cursor.X + gap;
        var y = cursor.Y + gap;
        if (x + width > work.X + work.Width) x = cursor.X - width - gap;
        if (y + height > work.Y + work.Height) y = cursor.Y - height - gap;
        x = Math.Clamp(x, work.X + gap, work.X + work.Width - width - gap);
        y = Math.Clamp(y, work.Y + gap, work.Y + work.Height - height - gap);
        window.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
    }

    public static void SetAlwaysOnTop(Window window, bool enabled)
        => SetWindowPos(WinRT.Interop.WindowNative.GetWindowHandle(window), enabled ? TopMost : NotTopMost, 0, 0, 0, 0, NoMove | NoSize | NoActivate);
}
