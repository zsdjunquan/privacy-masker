using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace PrivacyMasker.Services;

public sealed class MaskAssetProvider : IDisposable
{
    public static MaskAssetProvider Shared { get; } = new();

    private readonly object _syncRoot = new();
    private Image? _image;
    private string? _loadedPath;

    private MaskAssetProvider()
    {
        MaskSettingsStore.SettingsChanged += (_, _) => Reset();
    }

    public bool HasAsset
    {
        get
        {
            EnsureLoaded();
            return _image is not null;
        }
    }

    public BitmapSource? GetCurrentFrameSource()
    {
        lock (_syncRoot)
        {
            EnsureLoadedLocked();
            if (_image is null)
            {
                return null;
            }

            using var frame = new Bitmap(_image.Width, _image.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(frame))
            {
                graphics.DrawImage(_image, 0, 0, _image.Width, _image.Height);
            }

            return ScreenCaptureService.ToBitmapSource(frame);
        }
    }

    public bool Draw(Graphics graphics, Rectangle target, float opacity)
    {
        lock (_syncRoot)
        {
            EnsureLoadedLocked();
            if (_image is null)
            {
                return false;
            }

            using var backgroundBrush = new SolidBrush(Color.FromArgb(245, 250, 246, 235));
            using var attributes = CreateOpacityAttributes(opacity);
            var destination = GetFitDestinationRectangle(_image, target);
            graphics.FillRectangle(backgroundBrush, target);
            graphics.DrawImage(
                _image,
                destination,
                0,
                0,
                _image.Width,
                _image.Height,
                GraphicsUnit.Pixel,
                attributes);
            return true;
        }
    }

    public void Dispose()
    {
        Reset();
    }

    private void EnsureLoaded()
    {
        lock (_syncRoot)
        {
            EnsureLoadedLocked();
        }
    }

    private void EnsureLoadedLocked()
    {
        var settings = MaskSettingsStore.Load();
        var path = settings.AssetPath;
        if (string.Equals(_loadedPath, path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ResetLocked();

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _loadedPath = path;
            return;
        }

        try
        {
            var assetPath = path;
            _image = Image.FromFile(assetPath);
            _loadedPath = assetPath;
        }
        catch
        {
            ResetLocked();
            _loadedPath = path;
        }
    }

    private void Reset()
    {
        lock (_syncRoot)
        {
            ResetLocked();
        }
    }

    private void ResetLocked()
    {
        _image?.Dispose();
        _image = null;
        _loadedPath = null;
    }

    private static Rectangle GetFitDestinationRectangle(Image image, Rectangle target)
    {
        var sourceAspect = image.Width / (double)image.Height;
        var targetAspect = target.Width / (double)Math.Max(1, target.Height);

        if (sourceAspect > targetAspect)
        {
            var width = target.Width;
            var height = (int)Math.Round(target.Width / sourceAspect);
            var top = target.Top + (target.Height - height) / 2;
            return new Rectangle(target.Left, top, width, height);
        }

        var fitHeight = target.Height;
        var fitWidth = (int)Math.Round(target.Height * sourceAspect);
        var left = target.Left + (target.Width - fitWidth) / 2;
        return new Rectangle(left, target.Top, fitWidth, fitHeight);
    }

    private static ImageAttributes CreateOpacityAttributes(float opacity)
    {
        var clampedOpacity = Math.Clamp(opacity, 0f, 1f);
        var matrix = new ColorMatrix
        {
            Matrix33 = clampedOpacity
        };
        var attributes = new ImageAttributes();
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        return attributes;
    }
}
