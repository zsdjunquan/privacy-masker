using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using PrivacyMasker.Models;
using PrivacyMasker.Services;

namespace PrivacyMasker;

public partial class MainWindow : Window
{
    private static readonly MaskPresetOption[] PresetOptions =
    [
        new("dark", "默认深色"),
        new("pink", "粉色提示"),
        new("blue", "蓝色保密"),
        new("warning", "警示条纹")
    ];

    private readonly ObservableCollection<WindowSnapshot> _protectedWindows = [];
    private readonly DispatcherTimer _scanTimer;
    private readonly WindowDetector _windowDetector = new();
    private readonly AppRuleEngine _ruleEngine = new();
    private readonly OverlayManager _overlayManager = new();
    private MaskSettings _maskSettings = MaskSettingsStore.Load();
    private HotkeyManager? _hotkeyManager;
    private TrayController? _trayController;
    private SafeShareWindow? _safeShareWindow;
    private bool _isProtectionEnabled;
    private bool _showLocalMask = true;
    private bool _isUpdatingMaskUi;

    public MainWindow()
    {
        InitializeComponent();

        // Bind after XAML loading so the initial IsChecked value cannot fire before all controls exist.
        ShowLocalMaskCheckBox.Checked += ShowLocalMaskCheckBox_Changed;
        ShowLocalMaskCheckBox.Unchecked += ShowLocalMaskCheckBox_Changed;
        ProtectedWindowsList.ItemsSource = _protectedWindows;
        MaskPresetComboBox.ItemsSource = PresetOptions;
        MaskPresetComboBox.SelectionChanged += MaskPresetComboBox_SelectionChanged;
        MaskMessageTextBox.Text = _maskSettings.Message;
        MaskMessageTextBox.LostFocus += MaskMessageTextBox_LostFocus;
        UpdateMaskAssetText();

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

    private void ChooseMaskAssetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择遮罩图片",
            Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _maskSettings.MaskKind = "custom";
        _maskSettings.AssetPath = dialog.FileName;
        MaskSettingsStore.Save(_maskSettings);
        UpdateMaskAssetText();
    }

    private void ClearMaskAssetButton_Click(object sender, RoutedEventArgs e)
    {
        _maskSettings.MaskKind = "preset";
        _maskSettings.PresetId = "dark";
        _maskSettings.AssetPath = null;
        MaskSettingsStore.Save(_maskSettings);
        UpdateMaskAssetText();
    }

    private void MaskPresetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingMaskUi)
        {
            return;
        }

        if (MaskPresetComboBox.SelectedItem is not MaskPresetOption option)
        {
            return;
        }

        _maskSettings.MaskKind = "preset";
        _maskSettings.PresetId = option.Id;
        MaskSettingsStore.Save(_maskSettings);
        UpdateMaskAssetText();
    }

    private void MaskMessageTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _maskSettings.Message = string.IsNullOrWhiteSpace(MaskMessageTextBox.Text)
            ? "不可以偷看哦"
            : MaskMessageTextBox.Text.Trim();
        MaskMessageTextBox.Text = _maskSettings.Message;
        MaskSettingsStore.Save(_maskSettings);
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
        if (SafeShareButton is not null)
        {
            SafeShareButton.Content = _safeShareWindow is { IsVisible: true }
                ? "显示安全共享窗口"
                : "打开安全共享窗口";
        }
        _trayController?.Update(_isProtectionEnabled);
    }

    private void UpdateMaskAssetText()
    {
        _isUpdatingMaskUi = true;
        try
        {
            var preset = PresetOptions.First(option => option.Id == NormalizePresetId(_maskSettings.PresetId));
            MaskPresetComboBox.SelectedItem = preset;
            MaskAssetText.Text = _maskSettings.MaskKind == "custom" && !string.IsNullOrWhiteSpace(_maskSettings.AssetPath)
                ? $"当前：{Path.GetFileName(_maskSettings.AssetPath)}"
                : $"当前：{preset.Name}";
        }
        finally
        {
            _isUpdatingMaskUi = false;
        }
    }

    private static string NormalizePresetId(string? presetId)
    {
        return PresetOptions.Any(option => option.Id == presetId) ? presetId! : "dark";
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
