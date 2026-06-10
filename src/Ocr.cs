using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Cutter;

/// <summary>OCR local con el motor integrado de Windows (offline).</summary>
public static class Ocr
{
    public static async Task<string> RecognizeAsync(byte[] png)
    {
        using var stream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(stream))
        {
            writer.WriteBytes(png);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

        var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                     ?? OcrEngine.TryCreateFromLanguage(new Language("es"))
                     ?? OcrEngine.TryCreateFromLanguage(new Language("en"));

        if (engine is null)
            throw new InvalidOperationException(
                "No hay motor OCR disponible. Añade un idioma con OCR en " +
                "Configuración de Windows > Hora e idioma > Idioma.");

        var result = await engine.RecognizeAsync(bitmap);
        return result.Text;
    }
}
