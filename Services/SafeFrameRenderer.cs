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

    public BitmapSource Render(out int protectedWindowCount, IReadOnlyCollection<System.Windows.Rect>? excludedPixelBounds = null)
    {
        using var frame = _screenCaptureService.CapturePrimaryScreen();
        var screenBounds = _screenCaptureService.PrimaryScreenBounds;
        var protectedWindows = _windowDetector
            .GetVisibleWindows()
            .Where(_ruleEngine.ShouldProtect)
            .ToList();

        protectedWindowCount = protectedWindows.Count;
        DrawMasks(frame, screenBounds, protectedWindows, excludedPixelBounds ?? []);

        return ScreenCaptureService.ToBitmapSource(frame);
    }

    private static void DrawMasks(
        Bitmap frame,
        Rectangle screenBounds,
        IReadOnlyCollection<WindowSnapshot> windows,
        IReadOnlyCollection<System.Windows.Rect> excludedPixelBounds)
    {
        using var graphics = Graphics.FromImage(frame);
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        DrawExcludedRegions(graphics, screenBounds, excludedPixelBounds);

        var settings = MaskSettingsStore.Load();
        using var borderPen = new Pen(Color.FromArgb(220, 46, 125, 107), 3);
        using var textBrush = new SolidBrush(Color.White);
        using var smallTextBrush = new SolidBrush(Color.FromArgb(220, 225, 232, 238));
        using var titleFont = new Font("Microsoft YaHei UI", 36, FontStyle.Bold, GraphicsUnit.Pixel);
        using var subtitleFont = new Font("Microsoft YaHei UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);

        foreach (var window in windows)
        {
            var mask = ToFrameRectangle(window.PixelBounds, screenBounds);
            if (mask.Width <= 0 || mask.Height <= 0)
            {
                continue;
            }

            var drewCustomAsset = settings.MaskKind == "custom" &&
                MaskAssetProvider.Shared.Draw(graphics, mask, (float)settings.Opacity);
            if (!drewCustomAsset)
            {
                DrawPresetMask(graphics, mask, settings);
            }

            graphics.DrawRectangle(borderPen, mask);
            if (!drewCustomAsset)
            {
                DrawCenteredText(graphics, mask, settings, window, titleFont, subtitleFont, textBrush, smallTextBrush);
            }
        }
    }

    private static void DrawExcludedRegions(
        Graphics graphics,
        Rectangle screenBounds,
        IReadOnlyCollection<System.Windows.Rect> excludedPixelBounds)
    {
        if (excludedPixelBounds.Count == 0)
        {
            return;
        }

        using var brush = new SolidBrush(Color.FromArgb(255, 12, 16, 20));
        using var borderPen = new Pen(Color.FromArgb(160, 46, 125, 107), 2);
        using var textBrush = new SolidBrush(Color.FromArgb(220, 225, 232, 238));
        using var font = new Font("Microsoft YaHei UI", 16, FontStyle.Bold, GraphicsUnit.Pixel);

        foreach (var bounds in excludedPixelBounds)
        {
            var region = ToFrameRectangle(bounds, screenBounds);
            if (region.Width <= 0 || region.Height <= 0)
            {
                continue;
            }

            graphics.FillRectangle(brush, region);
            graphics.DrawRectangle(borderPen, region);

            if (region.Width >= 220 && region.Height >= 80)
            {
                const string text = "共享窗口预览已隐藏";
                var textSize = graphics.MeasureString(text, font);
                graphics.DrawString(
                    text,
                    font,
                    textBrush,
                    region.Left + (region.Width - textSize.Width) / 2f,
                    region.Top + (region.Height - textSize.Height) / 2f);
            }
        }
    }

    private static void DrawPresetMask(Graphics graphics, Rectangle mask, MaskSettings settings)
    {
        switch (settings.PresetId)
        {
            case "pink":
                using (var brush = new LinearGradientBrush(mask, Color.FromArgb(238, 255, 220, 232), Color.FromArgb(238, 255, 247, 210), 35f))
                {
                    graphics.FillRectangle(brush, mask);
                }
                DrawDots(graphics, mask, Color.FromArgb(120, 255, 142, 165));
                break;
            case "blue":
                using (var brush = new LinearGradientBrush(mask, Color.FromArgb(238, 23, 53, 88), Color.FromArgb(238, 36, 120, 128), 25f))
                {
                    graphics.FillRectangle(brush, mask);
                }
                DrawDots(graphics, mask, Color.FromArgb(100, 148, 217, 232));
                break;
            case "warning":
                using (var brush = new SolidBrush(Color.FromArgb(238, 31, 31, 31)))
                using (var stripeBrush = new SolidBrush(Color.FromArgb(80, 255, 190, 70)))
                {
                    graphics.FillRectangle(brush, mask);
                    for (var x = mask.Left - mask.Height; x < mask.Right; x += 72)
                    {
                        var points = new[]
                        {
                            new Point(x, mask.Bottom),
                            new Point(x + 32, mask.Bottom),
                            new Point(x + mask.Height + 32, mask.Top),
                            new Point(x + mask.Height, mask.Top)
                        };
                        graphics.FillPolygon(stripeBrush, points);
                    }
                }
                break;
            default:
                using (var brush = new SolidBrush(Color.FromArgb(232, 24, 32, 42)))
                {
                    graphics.FillRectangle(brush, mask);
                }
                break;
        }
    }

    private static void DrawDots(Graphics graphics, Rectangle mask, Color color)
    {
        using var brush = new SolidBrush(color);
        var size = Math.Max(8, Math.Min(mask.Width, mask.Height) / 18);
        graphics.FillEllipse(brush, mask.Left + mask.Width / 10, mask.Top + mask.Height / 8, size, size);
        graphics.FillEllipse(brush, mask.Right - mask.Width / 6, mask.Top + mask.Height / 5, size + 6, size + 6);
        graphics.FillEllipse(brush, mask.Left + mask.Width / 5, mask.Bottom - mask.Height / 5, size + 3, size + 3);
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
        MaskSettings settings,
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

        var title = string.IsNullOrWhiteSpace(settings.Message) ? "不可以偷看哦" : settings.Message;
        var subtitle = window.ProcessName;
        var titleSize = graphics.MeasureString(title, titleFont);
        var subtitleSize = graphics.MeasureString(subtitle, subtitleFont);
        var centerX = mask.Left + mask.Width / 2f;
        var centerY = mask.Top + mask.Height / 2f;

        graphics.DrawString(title, titleFont, textBrush, centerX - titleSize.Width / 2f, centerY - titleSize.Height);
        graphics.DrawString(subtitle, subtitleFont, smallTextBrush, centerX - subtitleSize.Width / 2f, centerY + 8);
    }
}
