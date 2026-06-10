using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Drawing = System.Drawing;
using Shapes = System.Windows.Shapes;

namespace Cutter;

/// <summary>
/// Overlay a pantalla completa (del monitor bajo el cursor) para arrastrar y
/// seleccionar la región a capturar. Devuelve la región en píxeles físicos.
/// Esc cancela.
/// </summary>
public sealed class RegionSelectWindow : Window
{
    private readonly Drawing.Rectangle _monitor; // px físicos
    private readonly Canvas _canvas = new();
    private readonly Shapes.Rectangle _sel = new()
    {
        Stroke = Brushes.Red,
        StrokeThickness = 2,
        Fill = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0x00, 0x00)),
        Visibility = Visibility.Collapsed
    };

    private System.Windows.Point _start;
    private bool _dragging;

    public Drawing.Rectangle? Selection { get; private set; }

    private RegionSelectWindow(Drawing.Rectangle monitor)
    {
        _monitor = monitor;
        Title = "Cutter — seleccionar región";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00));
        ShowInTaskbar = false;
        Topmost = true;
        Cursor = Cursors.Cross;

        var hint = new TextBlock
        {
            Text = "Arrastra para seleccionar · Esc para cancelar",
            Foreground = Brushes.White,
            FontSize = 14,
            Margin = new Thickness(14),
            HorizontalAlignment = HAlign.Center,
            VerticalAlignment = VAlign.Top
        };

        _canvas.Children.Add(_sel);
        var grid = new Grid();
        grid.Children.Add(_canvas);
        grid.Children.Add(hint);
        Content = grid;

        MouseLeftButtonDown += OnDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnUp;
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Cancel(); };
        SourceInitialized += OnSourceInitialized;
    }

    /// <summary>Muestra el overlay y devuelve la región elegida (px físicos) o null.</summary>
    public static Drawing.Rectangle? Pick()
    {
        var w = new RegionSelectWindow(ScreenCapture.ActiveMonitorBounds());
        return w.ShowDialog() == true ? w.Selection : null;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Posicionar exactamente sobre el monitor en píxeles físicos.
        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST,
            _monitor.X, _monitor.Y, _monitor.Width, _monitor.Height,
            NativeMethods.SWP_SHOWWINDOW);
        Activate();
        Focus();
    }

    private void Cancel()
    {
        Selection = null;
        DialogResult = false;
    }

    private void OnDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(_canvas);
        _dragging = true;
        Canvas.SetLeft(_sel, _start.X);
        Canvas.SetTop(_sel, _start.Y);
        _sel.Width = 0;
        _sel.Height = 0;
        _sel.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void OnMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(_canvas);
        Canvas.SetLeft(_sel, Math.Min(p.X, _start.X));
        Canvas.SetTop(_sel, Math.Min(p.Y, _start.Y));
        _sel.Width = Math.Abs(p.X - _start.X);
        _sel.Height = Math.Abs(p.Y - _start.Y);
    }

    private void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();

        double x = Canvas.GetLeft(_sel), y = Canvas.GetTop(_sel);
        double w = _sel.Width, h = _sel.Height;
        if (w < 5 || h < 5) { Cancel(); return; }

        // DIP -> px físicos, con la escala real de ESTE monitor.
        var dpi = VisualTreeHelper.GetDpi(this);
        int px = _monitor.X + (int)Math.Round(x * dpi.DpiScaleX);
        int py = _monitor.Y + (int)Math.Round(y * dpi.DpiScaleY);
        int pw = (int)Math.Round(w * dpi.DpiScaleX);
        int ph = (int)Math.Round(h * dpi.DpiScaleY);

        Selection = new Drawing.Rectangle(px, py, pw, ph);
        DialogResult = true;
    }
}
