using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using PrivacyMasker.Models;
using PrivacyMasker.Services;

namespace PrivacyMasker;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<WindowSnapshot> _protectedWindows = [];
    private readonly DispatcherTimer _scanTimer;
    private readonly WindowDetector _windowDetector = new();
    private readonly AppRuleEngine _ruleEngine = new();
    private readonly OverlayManager _overlayManager = new();
    private HotkeyManager? _hotkeyManager;
    private TrayController? _trayController;
    private SafeShareWindow? _safeShareWindow;
    private bool _isProtectionEnabled;
    private bool _showLocalMask = true;

    public MainWindow()
    {
        InitializeComponent();
        ProtectedWindowsList.ItemsSource = _protectedWindows;

        _scanTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _scanTimer.Tick += (_, _) => RefreshProtection();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hotkeyManager = new HotkeyManager(this, ToggleProtection);
        _hotkeyManager.Register();

        _trayController = new TrayController(
            ToggleProtection,
            () => Dispatcher.Invoke(ShowFromTray),
            () => Dispatcher.Invoke(Close));
        _trayController.Update(_isProtectionEnabled);
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _scanTimer.Stop();
        _overlayManager.Clear();
        _safeShareWindow?.Close();
        _hotkeyManager?.Dispose();
        _trayController?.Dispose();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleProtection();
    }

    private void ShowLocalMaskCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _showLocalMask = ShowLocalMaskCheckBox.IsChecked == true;

        if (!_showLocalMask)
        {
            _overlayManager.Clear();
        }

        RefreshProtection();
        UpdateStatus();
    }

    private void SafeShareButton_Click(object sender, RoutedEventArgs e)
    {
        if (_safeShareWindow is { IsVisible: true })
        {
            _safeShareWindow.Activate();
            return;
        }

        _safeShareWindow = new SafeShareWindow
        {
            Owner = this
        };
        _safeShareWindow.Closed += (_, _) =>
        {
            _safeShareWindow = null;
            UpdateStatus();
        };
        _safeShareWindow.Show();
        UpdateStatus();
    }

    private void ToggleProtection()
    {
        _isProtectionEnabled = !_isProtectionEnabled;

        if (_isProtectionEnabled)
        {
            _scanTimer.Start();
            RefreshProtection();
        }
        else
        {
            _scanTimer.Stop();
            _overlayManager.Clear();
            _protectedWindows.Clear();
        }

        UpdateStatus();
    }

    private void RefreshProtection()
    {
        if (!_isProtectionEnabled)
        {
            return;
        }

        var windows = _windowDetector.GetVisibleWindows();
        var protectedWindows = windows
            .Where(window => _ruleEngine.ShouldProtect(window))
            .ToList();

        if (_showLocalMask)
        {
            _overlayManager.Sync(protectedWindows);
        }
        else
        {
            _overlayManager.Clear();
        }

        SyncProtectedList(protectedWindows);
        UpdateStatus();
    }

    private void SyncProtectedList(IReadOnlyList<WindowSnapshot> windows)
    {
        _protectedWindows.Clear();
        foreach (var window in windows.OrderBy(window => window.ProcessName).ThenBy(window => window.Title))
        {
            _protectedWindows.Add(window);
        }
    }

    private void UpdateStatus()
    {
        StatusTitle.Text = _isProtectionEnabled ? "隐私模式已开启" : "隐私模式已关闭";
        StatusSubtitle.Text = GetStatusSubtitle();
        ToggleButton.Content = _isProtectionEnabled ? "关闭保护" : "开启保护";
        ToggleButton.Background = _isProtectionEnabled
            ? System.Windows.Media.Brushes.DarkSlateGray
            : (System.Windows.Media.Brush)FindResource("AccentBrush");
        ProtectedCountText.Text = $"{_protectedWindows.Count} 个";
        SafeShareButton.Content = _safeShareWindow is { IsVisible: true }
            ? "显示安全共享窗口"
            : "打开安全共享窗口";
        _trayController?.Update(_isProtectionEnabled);
    }

    private string GetStatusSubtitle()
    {
        if (!_isProtectionEnabled)
        {
            return "开启后会自动扫描微信、QQ 窗口。快捷键：Ctrl + Alt + P";
        }

        return _showLocalMask
            ? "正在扫描微信、QQ 窗口，并在本机覆盖遮罩。普通桌面共享时，对方也会看到遮罩。"
            : "正在扫描微信、QQ 窗口，但本机不显示遮罩。注意：普通桌面共享里对方也不会看到遮罩。";
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
