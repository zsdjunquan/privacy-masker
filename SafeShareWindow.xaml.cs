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

    public SafeShareWindow()
    {
        InitializeComponent();

        _frameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
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
        _frameTimer.Stop();
    }

    private void SafeShareWindow_StateChanged(object? sender, EventArgs e)
    {
        _frameTimer.IsEnabled = WindowState != WindowState.Minimized;
    }

    private void RenderFrame()
    {
        try
        {
            PreviewImage.Source = _renderer.Render(out var protectedWindowCount, GetExcludedWindowBounds());
            StatusText.Text = protectedWindowCount > 0
                ? $"安全共享中，已遮罩 {protectedWindowCount} 个窗口"
                : "安全共享中";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"采集失败：{ex.Message}";
        }
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
        var targetScreen = FormsScreen.AllScreens.FirstOrDefault(screen => !screen.Primary)
            ?? FormsScreen.PrimaryScreen
            ?? FormsScreen.AllScreens.First();

        Left = targetScreen.WorkingArea.Left + 40;
        Top = targetScreen.WorkingArea.Top + 40;
        Width = Math.Min(1120, Math.Max(720, targetScreen.WorkingArea.Width - 80));
        Height = Math.Min(720, Math.Max(460, targetScreen.WorkingArea.Height - 80));
    }
}
