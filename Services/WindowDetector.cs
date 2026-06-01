using System.Diagnostics;
using System.Windows;
using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public sealed class WindowDetector
{
    public IReadOnlyList<WindowSnapshot> GetVisibleWindows()
    {
        var windows = new List<WindowSnapshot>();
        var shellWindow = NativeMethods.GetShellWindow();
        var desktopWindow = NativeMethods.GetDesktopWindow();
        var currentProcessId = Environment.ProcessId;

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (hWnd == IntPtr.Zero || hWnd == shellWindow || hWnd == desktopWindow)
            {
                return true;
            }

            if (!NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd))
            {
                return true;
            }

            var title = NativeMethods.GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == currentProcessId)
            {
                return true;
            }

            if (!NativeMethods.TryGetVisibleWindowBounds(hWnd, out var rect))
            {
                return true;
            }

            var scale = NativeMethods.GetScaleForWindow(hWnd);
            var pixelWidth = rect.Right - rect.Left;
            var pixelHeight = rect.Bottom - rect.Top;
            var left = rect.Left / scale;
            var top = rect.Top / scale;
            var width = pixelWidth / scale;
            var height = pixelHeight / scale;

            if (width < 80 || height < 60 || left <= -30000 || top <= -30000)
            {
                return true;
            }

            var processName = GetProcessName((int)processId);
            var bounds = new Rect(left, top, width, height);
            var pixelBounds = new Rect(rect.Left, rect.Top, pixelWidth, pixelHeight);

            windows.Add(new WindowSnapshot(
                hWnd,
                title,
                processName,
                bounds,
                pixelBounds));

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "Unknown";
        }
    }
}
