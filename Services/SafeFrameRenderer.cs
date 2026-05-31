using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Media.Imaging;
using PrivacyMasker.Models;

namespace PrivacyMasker.Services;

public sealed class SafeFrameRenderer
{
    private readonly ScreenCaptureService _screenCaptureService = new();
    private readonly WindowDetector _windowDetector = new();
    private readonly AppRuleEngine _ruleEngine = new();

    public BitmapSource Render(out int protectedWindowCount)
    {
        using var frame = _screenCaptureService.CapturePrimaryScreen();
        var screenBounds = _screenCaptureService.PrimaryScreenBounds;
        var protectedWindows = _windowDetector
            .GetVisibleWindows()
            .Where(_ruleEngine.ShouldProtect)
            .ToList();

        protectedWindowCount = protectedWindows.Count;
        DrawMasks(frame, screenBounds, protectedWindows);

        return ScreenCaptureService.ToBitmapSource(frame);
    }

    private static void DrawMasks(Bitmap frame, Rectangle screenBounds, IReadOnlyCollection<WindowSnapshot> windows)
    {
        using var graphics = Graphics.FromImage(frame);
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var maskBrush = new SolidBrush(Color.FromArgb(232, 24, 32, 42));
        using var borderPen = new Pen(Color.FromArgb(220, 46, 125, 107), 3);
        using var textBrush = new SolidBrush(Color.White);
        using var smallTextBrush = new SolidBrush(Color.FromArgb(220, 225, 232, 238));
        using var titleFont = new Font("Microsoft YaHei UI", 24, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font("Microsoft YaHei UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);

        foreach (var window in windows)
        {
            var mask = ToFrameRectangle(window.PixelBounds, screenBounds);
            if (mask.Width <= 0 || mask.Height <= 0)
            {
                continue;
            }

            graphics.FillRectangle(maskBrush, mask);
            graphics.DrawRectangle(borderPen, mask);
            DrawCenteredText(graphics, mask, window, titleFont, subtitleFont, textBrush, smallTextBrush);
        }
    }

    private static Rectangle ToFrameRectangle(System.Windows.Rect windowBounds, Rectangle screenBounds)
    {
        var left = Math.Max((int)Math.Round(windowBounds.Left - screenBounds.Left), 0);
        var top = Math.Max((int)Math.Round(windowBounds.Top - screenBounds.Top), 0);
        var right = Math.Min((int)Math.Round(windowBounds.Right - screenBounds.Left), screenBounds.Width);
        var bottom = Math.Min((int)Math.Round(windowBounds.Bottom - screenBounds.Top), screenBounds.Height);

        return Rectangle.FromLTRB(left, top, Math.Max(left, right), Math.Max(top, bottom));
    }

    private static void DrawCenteredText(
        Graphics graphics,
        Rectangle mask,
        WindowSnapshot window,
        Font titleFont,
        Font subtitleFont,
        Brush textBrush,
        Brush smallTextBrush)
    {
        if (mask.Width < 180 || mask.Height < 120)
        {
            return;
        }

        const string title = "隐私遮罩";
        var subtitle = window.ProcessName;
        var titleSize = graphics.MeasureString(title, titleFont);
        var subtitleSize = graphics.MeasureString(subtitle, subtitleFont);
        var centerX = mask.Left + mask.Width / 2f;
        var centerY = mask.Top + mask.Height / 2f;

        graphics.DrawString(title, titleFont, textBrush, centerX - titleSize.Width / 2f, centerY - titleSize.Height);
        graphics.DrawString(subtitle, subtitleFont, smallTextBrush, centerX - subtitleSize.Width / 2f, centerY + 8);
    }
}
