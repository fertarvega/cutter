using System.Diagnostics;
using System.Drawing;
using System.Windows.Threading;

namespace Cutter;

/// <summary>Graba fotogramas de una región a intervalos fijos, con marcas de tiempo reales.</summary>
public sealed class GifRecorder
{
    public const int FrameDelayMs = 33; // objetivo ~30 fps
    private const int MaxSeconds = 30;   // tope de seguridad

    private readonly DispatcherTimer _timer;
    private readonly List<Bitmap> _frames = new();
    private readonly List<int> _stampsMs = new();
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
        StopTimer();
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
            StopTimer();
            return;
        }
        _frames.Add(ScreenCapture.Grab(_area));
        _stampsMs.Add((int)_watch.ElapsedMilliseconds);
    }

    /// <summary>Detiene y devuelve fotogramas + retardo real por fotograma (ms).</summary>
    public (List<Bitmap> frames, List<int> delays) StopAndCollect()
    {
        StopTimer();

        var frames = _frames.ToList();
        var delays = new List<int>(frames.Count);
        for (int i = 0; i < frames.Count; i++)
        {
            int d = i < frames.Count - 1
                ? _stampsMs[i + 1] - _stampsMs[i] // tiempo hasta el siguiente
                : FrameDelayMs;                    // último: retardo nominal
            delays.Add(Math.Clamp(d, 20, 1000));
        }
        return (frames, delays);
    }

    private void StopTimer()
    {
        _timer.Stop();
        IsRecording = false;
    }

    private void ClearFrames()
    {
        foreach (var f in _frames) f.Dispose();
        _frames.Clear();
        _stampsMs.Clear();
    }
}
