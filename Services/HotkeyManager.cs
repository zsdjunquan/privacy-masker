using System.Windows;
using System.Windows.Interop;

namespace PrivacyMasker.Services;

public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 901;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkP = 0x50;

    private readonly Window _window;
    private readonly Action _onHotkey;
    private HwndSource? _source;
    private IntPtr _handle;

    public HotkeyManager(Window window, Action onHotkey)
    {
        _window = window;
        _onHotkey = onHotkey;
    }

    public void Register()
    {
        _handle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
        NativeMethods.RegisterHotKey(_handle, HotkeyId, ModControl | ModAlt, VkP);
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_handle, HotkeyId);
        }

        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _onHotkey();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
