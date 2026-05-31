using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
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
        ApplyMaskAsset();

        if (!IsVisible)
        {
            Show();
        }
    }

    private void ApplyMaskAsset()
    {
        var settings = MaskSettingsStore.Load();
        if (settings.MaskKind == "custom")
        {
            var source = MaskAssetProvider.Shared.GetCurrentFrameSource();
            if (source is not null)
            {
                MaskBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 246, 235));
                MaskImage.Source = source;
                MaskImage.Visibility = Visibility.Visible;
                DefaultTextPanel.Visibility = Visibility.Collapsed;

                return;
            }
        }

        MaskImage.Source = null;
        MaskImage.Visibility = Visibility.Collapsed;
        DefaultTextPanel.Visibility = Visibility.Visible;
        PresetMessageText.Text = string.IsNullOrWhiteSpace(settings.Message)
            ? "不可以偷看哦"
            : settings.Message;
        ApplyPresetBrush(settings.PresetId);
    }

    private void ApplyPresetBrush(string presetId)
    {
        MaskBorder.Background = presetId switch
        {
            "pink" => new LinearGradientBrush(System.Windows.Media.Color.FromRgb(255, 222, 231), System.Windows.Media.Color.FromRgb(255, 247, 214), 35),
            "blue" => new LinearGradientBrush(System.Windows.Media.Color.FromRgb(23, 53, 88), System.Windows.Media.Color.FromRgb(36, 120, 128), 25),
            "warning" => new LinearGradientBrush(System.Windows.Media.Color.FromRgb(32, 32, 32), System.Windows.Media.Color.FromRgb(96, 68, 22), 35),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(232, 24, 32, 42))
        };
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
