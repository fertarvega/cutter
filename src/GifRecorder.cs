using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;

namespace Cutter;

/// <summary>Graba frames de una región de pantalla a intervalos fijos.</summary>
public sealed class GifRecorder
{
    public const int FrameDelayMs = 100; // ~10 fps
    private const int MaxSeconds = 30;    // tope de seguridad

    private readonly DispatcherTimer _timer;
    private readonly List<Bitmap> _frames = new();
    private Rectangle _area;
    private Stopwatch _watch = new();

    public bool IsRecording { get; private set; }

    public GifRecorder()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Send)
        {
            Interval = TimeSpan.FromMilliseconds(FrameDelayMs)
        };
        _timer.Tick += OnTick;
    }

    public void Start(Rectangle area)
    {
        Stop();                 // limpia cualquier grabación previa
        ClearFrames();
        _area = area;
        _watch = Stopwatch.StartNew();
        IsRecording = true;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_watch.Elapsed.TotalSeconds > MaxSeconds)
        {
            _timer.Stop();
            IsRecording = false;
            return;
        }
        _frames.Add(ScreenCapture.Grab(_area));
    }

    /// <summary>Detiene y devuelve los frames capturados.</summary>
    public IReadOnlyList<Bitmap> StopAndCollect()
    {
        _timer.Stop();
        IsRecording = false;
        return _frames.ToList();
    }

    private void Stop()
    {
        _timer.Stop();
        IsRecording = false;
    }

    private void ClearFrames()
    {
        foreach (var f in _frames) f.Dispose();
        _frames.Clear();
    }
}
