using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WinForms = System.Windows.Forms;

namespace Cutter;

public static class ScreenCapture
{
    /// <summary>Rectángulo (en píxeles físicos) del monitor donde está el cursor.</summary>
    public static Rectangle ActiveMonitorBounds()
    {
        var pos = WinForms.Cursor.Position;
        var screen = WinForms.Screen.FromPoint(pos);
        return screen.Bounds;
    }

    /// <summary>Captura una región de pantalla (píxeles físicos) a un Bitmap.</summary>
    public static Bitmap Grab(Rectangle area)
    {
        var bmp = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(area.Location, System.Drawing.Point.Empty, area.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }

    public static byte[] ToPng(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public static BitmapSource ToBitmapSource(Bitmap bmp)
    {
        IntPtr h = bmp.GetHbitmap();
        try
        {
            var src = Imaging.CreateBitmapSourceFromHBitmap(
                h, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            src.Freeze();
            return src;
        }
        finally
        {
            NativeMethods.DeleteObject(h);
        }
    }
}
