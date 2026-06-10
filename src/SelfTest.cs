using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace Cutter;

/// <summary>
/// Pruebas headless de la lógica delicada (ensamblado de GIF y OCR).
/// Se ejecuta con "Cutter.exe --selftest". Escribe el resultado en
/// %TEMP%\cutter_selftest.log porque una app WinExe no tiene consola.
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        var log = new StringBuilder();
        bool ok = true;

        ok &= Check(log, "GIF assemble/parse", TestGif);
        ok &= Check(log, "OCR Windows", TestOcr);

        string path = Path.Combine(Path.GetTempPath(), "cutter_selftest.log");
        log.AppendLine(ok ? "RESULT: ALL PASS" : "RESULT: FAIL");
        File.WriteAllText(path, log.ToString());
        return ok ? 0 : 1;
    }

    private static bool Check(StringBuilder log, string name, Func<string> test)
    {
        try
        {
            string detail = test();
            log.AppendLine($"[PASS] {name} — {detail}");
            return true;
        }
        catch (Exception ex)
        {
            log.AppendLine($"[FAIL] {name} — {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private static string TestGif()
    {
        var frames = new List<Bitmap>();
        foreach (var c in new[] { Color.FromArgb(255,200,30,30), Color.FromArgb(255,30,200,30), Color.FromArgb(255,30,30,200) })
        {
            var bmp = new Bitmap(200, 100);
            using (var g = Graphics.FromImage(bmp))
                g.Clear(System.Drawing.Color.FromArgb(c.R, c.G, c.B));
            frames.Add(bmp);
        }

        byte[] gif = GifBuilder.Build(frames, 100);
        foreach (var f in frames) f.Dispose();

        if (gif.Length < 20) throw new Exception("GIF demasiado pequeño");
        if (!(gif[0] == (byte)'G' && gif[1] == (byte)'I' && gif[2] == (byte)'F'))
            throw new Exception("Sin firma GIF");

        using var ms = new MemoryStream(gif);
        var decoder = new GifBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count != 3)
            throw new Exception($"Esperaba 3 frames, obtuve {decoder.Frames.Count}");
        if (decoder.Frames[0].PixelWidth != 200)
            throw new Exception($"Ancho inesperado {decoder.Frames[0].PixelWidth}");

        return $"{gif.Length} bytes, 3 frames, 200x100, válido";
    }

    private static string TestOcr()
    {
        const string expected = "HOLA";
        var bmp = new Bitmap(640, 200);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.White);
            using var font = new Font("Arial", 60, System.Drawing.FontStyle.Bold);
            g.DrawString("HOLA MUNDO 123", font, System.Drawing.Brushes.Black, 20, 50);
        }
        byte[] png = ScreenCapture.ToPng(bmp);
        bmp.Dispose();

        string text = Ocr.RecognizeAsync(png).GetAwaiter().GetResult();
        string norm = text.ToUpperInvariant().Replace(" ", "");
        if (!norm.Contains(expected))
            throw new Exception($"OCR no reconoció '{expected}'. Devolvió: '{text}'");

        return $"reconoció: '{text.Trim()}'";
    }
}
