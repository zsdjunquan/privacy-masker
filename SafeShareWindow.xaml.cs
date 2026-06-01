using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using PrivacyMasker.Services;
using FormsScreen = System.Windows.Forms.Screen;

namespace PrivacyMasker;

public partial class SafeShareWindow : Window
{
    private readonly DispatcherTimer _frameTimer;
    private readonly SafeFrameRenderer _renderer = new();
    private bool _isRenderingFrame;
    private bool _isClosing;

    public SafeShareWindow()
    {
        InitializeComponent();

        _frameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _frameTimer.Tick += (_, _) => RenderFrame();

        Loaded += SafeShareWindow_Loaded;
        Closing += SafeShareWindow_Closing;
        StateChanged += SafeShareWindow_StateChanged;
    }

    private void SafeShareWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MoveToUsefulScreen();
        _frameTimer.Start();
        RenderFrame();
    }

    private void SafeShareWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _isClosing = true;
        _frameTimer.Stop();
    }

    private void SafeShareWindow_StateChanged(object? sender, EventArgs e)
    {
        _frameTimer.IsEnabled = WindowState != WindowState.Minimized;
    }

    private void RenderFrame()
    {
        if (_isRenderingFrame || _isClosing || WindowState == WindowState.Minimized)
        {
            return;
        }

        _isRenderingFrame = true;

        // 采集和遮罩绘制比较重，放到后台线程，避免拖动安全共享窗口时卡住界面。
        var excludedWindowBounds = GetExcludedWindowBounds();
        _ = Task.Run(() =>
        {
            try
            {
                var frame = _renderer.Render(out var protectedWindowCount, excludedWindowBounds);
                Dispatcher.BeginInvoke(() =>
                {
                    if (_isClosing)
                    {
                        return;
                    }

                    PreviewImage.Source = frame;
                    StatusText.Text = protectedWindowCount > 0
                        ? $"安全共享中，已遮罩 {protectedWindowCount} 个窗口，30 FPS"
                        : "安全共享中，30 FPS";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    if (!_isClosing)
                    {
                        StatusText.Text = $"采集失败：{ex.Message}";
                    }
                });
            }
            finally
            {
                Dispatcher.BeginInvoke(() => _isRenderingFrame = false);
            }
        });
    }

    private IReadOnlyCollection<Rect> GetExcludedWindowBounds()
    {
        var bounds = new List<Rect>();
        AddWindowBounds(this, bounds);

        if (Owner is not null)
        {
            AddWindowBounds(Owner, bounds);
        }

        return bounds;
    }

    private static void AddWindowBounds(Window window, ICollection<Rect> bounds)
    {
        if (!window.IsVisible || window.WindowState == WindowState.Minimized)
        {
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        if (NativeMethods.TryGetVisibleWindowBounds(handle, out var rect))
        {
            bounds.Add(new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top));
        }
    }

    private void MoveToUsefulScreen()
    {
        var secondaryScreen = FormsScreen.AllScreens.FirstOrDefault(screen => !screen.Primary);
        var targetScreen = secondaryScreen
            ?? FormsScreen.PrimaryScreen
            ?? FormsScreen.AllScreens.First();

        if (secondaryScreen is not null)
        {
            // 有第二块屏幕时，安全共享窗口铺满第二屏，减少会议软件二次缩放造成的模糊。
            Left = targetScreen.WorkingArea.Left;
            Top = targetScreen.WorkingArea.Top;
            Width = targetScreen.WorkingArea.Width;
            Height = targetScreen.WorkingArea.Height;
            WindowState = WindowState.Maximized;
            return;
        }

        // 单屏时保留操作空间，避免安全共享窗口把自己和主面板完全盖住。
        var maxWidth = Math.Max(320, targetScreen.WorkingArea.Width - 40);
        var maxHeight = Math.Max(240, targetScreen.WorkingArea.Height - 40);
        var minWidth = Math.Min(720, maxWidth);
        var minHeight = Math.Min(460, maxHeight);
        var width = Math.Clamp(targetScreen.WorkingArea.Width - 120, minWidth, maxWidth);
        var height = Math.Clamp(targetScreen.WorkingArea.Height - 120, minHeight, maxHeight);

        Left = targetScreen.WorkingArea.Left + (targetScreen.WorkingArea.Width - width) / 2d;
        Top = targetScreen.WorkingArea.Top + (targetScreen.WorkingArea.Height - height) / 2d;
        Width = width;
        Height = height;
    }
}
