using System.Windows;
using System.Windows.Interop;
using PrivacyMasker.Models;
using PrivacyMasker.Services;

namespace PrivacyMasker;

public partial class OverlayWindow : Window
{
    private bool _isClickThroughApplied;

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += OverlayWindow_SourceInitialized;
    }

    public void UpdateFor(WindowSnapshot target)
    {
        Left = target.Bounds.Left;
        Top = target.Bounds.Top;
        Width = target.Bounds.Width;
        Height = target.Bounds.Height;
        WindowTitleText.Text = target.Title;

        if (!IsVisible)
        {
            Show();
        }
    }

    private void OverlayWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (_isClickThroughApplied)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GwlExStyle).ToInt64();
        style |= NativeMethods.WsExTransparent | NativeMethods.WsExToolWindow | NativeMethods.WsExNoActivate;
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GwlExStyle, new IntPtr(style));
        _isClickThroughApplied = true;
    }
}
