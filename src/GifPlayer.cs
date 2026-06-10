using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Cutter;

/// <summary>
/// Reproduce bytes de un GIF animándolo en un control Image (cambia el
/// fotograma según el retardo real de cada uno, en bucle). Reutilizable
/// por la ventana de captura y por el visor de privados.
/// </summary>
public sealed class GifPlayer
{
    private readonly Image _target;
    private DispatcherTimer? _timer;
    private List<BitmapSource>? _frames;
    private List<int>? _delays;
    private int _i;

    public GifPlayer(Image target) => _target = target;

    /// <summary>Carga y reproduce el GIF. Si tiene 1 frame, lo deja estático. false si falla.</summary>
    public bool Play(byte[] gifBytes)
    {
        Stop();
        try
        {
            using var ms = new MemoryStream(gifBytes);
            var decoder = new GifBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) return false;

            _frames = new List<BitmapSource>(decoder.Frames.Count);
            _delays = new List<int>(decoder.Frames.Count);
            foreach (var f in decoder.Frames)
            {
                f.Freeze();
                _frames.Add(f);

                int delayMs = 33;
                if (f.Metadata is BitmapMetadata md && md.ContainsQuery("/grctlext/Delay")
                    && md.GetQuery("/grctlext/Delay") is ushort cs && cs > 0)
                    delayMs = cs * 10;
                _delays.Add(Math.Max(20, delayMs));
            }

            _i = 0;
            _target.Source = _frames[0];
            if (_frames.Count < 2) return true; // un solo frame: estático

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_delays[0]) };
            _timer.Tick += Tick;
            _timer.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_frames is null || _delays is null || _timer is null) return;
        _i = (_i + 1) % _frames.Count;
        _target.Source = _frames[_i];
        _timer.Interval = TimeSpan.FromMilliseconds(_delays[_i]);
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _frames = null;
        _delays = null;
        _i = 0;
    }
}
