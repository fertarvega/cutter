using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Cutter;

/// <summary>
/// Ensambla un GIF animado a partir de varios frames.
///
/// WPF tiene GifBitmapEncoder pero ignora el retardo entre frames y el
/// bucle, así que el GIF saldría a toda velocidad y sin repetir. Para
/// arreglarlo, codificamos cada frame por separado (WPF cuantiza a 256
/// colores y nos da los datos LZW ya comprimidos), extraemos su paleta y
/// su bloque de imagen, y montamos a mano el GIF final con su extensión
/// de bucle (NETSCAPE2.0) y un retardo por frame.
/// </summary>
public static class GifBuilder
{
    /// <summary>Retardo uniforme para todos los frames.</summary>
    public static byte[] Build(IReadOnlyList<Bitmap> frames, int delayMs)
    {
        var delays = new int[frames.Count];
        Array.Fill(delays, delayMs);
        return Build(frames, delays);
    }

    /// <param name="delaysMs">Retardo por frame en milisegundos (mismo tamaño que frames).</param>
    public static byte[] Build(IReadOnlyList<Bitmap> frames, IReadOnlyList<int> delaysMs)
    {
        if (frames.Count == 0) throw new ArgumentException("Sin frames.");

        ushort width = (ushort)frames[0].Width;
        ushort height = (ushort)frames[0].Height;

        using var ms = new MemoryStream();

        // --- Cabecera + Logical Screen Descriptor (sin tabla de color global) ---
        ms.Write("GIF89a"u8);
        WriteU16(ms, width);
        WriteU16(ms, height);
        ms.WriteByte(0x00); // packed: sin GCT
        ms.WriteByte(0x00); // índice de fondo
        ms.WriteByte(0x00); // aspect ratio

        // --- Extensión de aplicación NETSCAPE2.0: bucle infinito ---
        ms.Write([0x21, 0xFF, 0x0B]);
        ms.Write("NETSCAPE2.0"u8);
        ms.Write([0x03, 0x01, 0x00, 0x00, 0x00]);

        for (int i = 0; i < frames.Count; i++)
        {
            var f = ParseSingleFrameGif(EncodeSingleGif(frames[i]));
            ushort delayCs = (ushort)Math.Max(2, delaysMs[i] / 10); // GIF usa centisegundos

            // Graphic Control Extension (retardo, sin transparencia, no descartar)
            ms.Write([(byte)0x21, (byte)0xF9, (byte)0x04, (byte)0x04]);
            WriteU16(ms, delayCs);
            ms.Write([(byte)0x00, (byte)0x00]);

            // Image Descriptor con tabla de color local
            ms.WriteByte(0x2C);
            WriteU16(ms, 0); // left
            WriteU16(ms, 0); // top
            WriteU16(ms, width);
            WriteU16(ms, height);
            ms.WriteByte((byte)(0x80 | f.PaletteSizeField)); // LCT flag + tamaño

            ms.Write(f.Palette);
            ms.WriteByte(f.MinCodeSize);
            ms.Write(f.LzwData);
        }

        ms.WriteByte(0x3B); // trailer
        return ms.ToArray();
    }

    private static byte[] EncodeSingleGif(Bitmap bmp)
    {
        var src = ScreenCapture.ToBitmapSource(bmp);
        var enc = new GifBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(src));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }

    private readonly record struct Frame(byte[] Palette, byte PaletteSizeField, byte MinCodeSize, byte[] LzwData);

    private static Frame ParseSingleFrameGif(byte[] d)
    {
        int pos = 6; // saltar "GIF89a"
        // Logical Screen Descriptor
        byte packed = d[pos + 4];
        pos += 7;

        byte[] palette = [];
        byte sizeField = 0;
        bool gct = (packed & 0x80) != 0;
        if (gct)
        {
            sizeField = (byte)(packed & 0x07);
            int n = 3 * (1 << (sizeField + 1));
            palette = d[pos..(pos + n)];
            pos += n;
        }

        // Saltar bloques de extensión hasta el Image Descriptor
        while (d[pos] == 0x21)
        {
            pos += 2; // 0x21 + label
            pos = SkipSubBlocks(d, pos);
        }

        // Image Descriptor (0x2C + 9 bytes)
        byte imgPacked = d[pos + 9];
        pos += 10;
        if ((imgPacked & 0x80) != 0) // tabla local (no esperada, pero por si acaso)
        {
            sizeField = (byte)(imgPacked & 0x07);
            int n = 3 * (1 << (sizeField + 1));
            palette = d[pos..(pos + n)];
            pos += n;
        }

        byte minCodeSize = d[pos];
        pos++;
        int start = pos;
        pos = SkipSubBlocks(d, pos);
        byte[] lzw = d[start..pos]; // incluye el terminador 0x00

        return new Frame(palette, sizeField, minCodeSize, lzw);
    }

    /// <summary>Avanza sobre sub-bloques y devuelve la posición tras el terminador 0x00.</summary>
    private static int SkipSubBlocks(byte[] d, int pos)
    {
        while (d[pos] != 0x00)
            pos += d[pos] + 1;
        return pos + 1;
    }

    private static void WriteU16(Stream s, ushort v)
    {
        s.WriteByte((byte)(v & 0xFF));
        s.WriteByte((byte)(v >> 8));
    }
}
