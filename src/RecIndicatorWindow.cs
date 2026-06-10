using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Drawing = System.Drawing;

namespace Cutter;

/// <summary>
/// Insignia "● REC" mientras se graba un GIF. Se coloca FUERA de la región
/// grabada (justo encima, o debajo si no cabe) para que no salga en el GIF.
/// Es click-through y no roba el foco.
/// </summary>
public sealed class RecIndicatorWindow : Window
{
    private const int BadgeW = 210;
    private const int BadgeH = 30;

    private readonly Drawing.Rectangle _badge;

    public RecIndicatorWindow(Drawing.Rectangle region)
    {
        var monitor = ScreenCapture.ActiveMonitorBounds();
        int x = region.X;
        int y = region.Y - BadgeH - 2;
        if (y < monitor.Y) y = region.Y + region.Height + 2; // no cabe arriba -> abajo
        _badge = new Drawing.Rectangle(x, y, BadgeW, BadgeH);

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        IsHitTestVisible = false;
        Focusable = false;

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x10, 0x10, 0x10)),
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock
            {
                Text = "● REC — doble Impr Pant para parar",
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VAlign.Center,
                HorizontalAlignment = HAlign.Center
            }
        };

        SourceInitialized += OnInit;
    }

    private void OnInit(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        ex |= NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED
              | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, ex);

        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST,
            _badge.X, _badge.Y, _badge.Width, _badge.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }
}
