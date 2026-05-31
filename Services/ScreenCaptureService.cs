using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FormsScreen = System.Windows.Forms.Screen;

namespace PrivacyMasker.Services;

public sealed class ScreenCaptureService
{
    public Rectangle PrimaryScreenBounds
    {
        get
        {
            var screen = FormsScreen.PrimaryScreen ?? FormsScreen.AllScreens.First();
            return screen.Bounds;
        }
    }

    public Bitmap CapturePrimaryScreen()
    {
        var bounds = PrimaryScreenBounds;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

        return bitmap;
    }

    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DeleteObject(handle);
        }
    }
}
