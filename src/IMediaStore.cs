using System.IO;
using System.Text;

namespace Cutter;

/// <summary>
/// Origen de archivos para el visor. Abstrae si la carpeta es pública (sin
/// cifrar) o privada (cifrada), de modo que una misma ventana sirva a ambas.
/// </summary>
public interface IMediaStore
{
    string Title { get; }
    string Kind { get; }        // "privado" / "público" (para los mensajes)
    bool CanExport { get; }

    IEnumerable<string> List();           // rutas completas de archivos
    string DisplayName(string path);      // nombre a mostrar / nombre real
    string InnerExt(string path);         // extensión real del contenido (.png/.gif/.txt)
    byte[] Read(string path);             // bytes ya en claro
    void Delete(string path);
    string SaveText(string text);         // guarda texto OCR, devuelve la ruta
}

/// <summary>Carpeta privada: archivos .enc cifrados con la bóveda.</summary>
public sealed class PrivateStore : IMediaStore
{
    public string Title => "Cutter — Carpeta privada 🔒";
    public string Kind => "privado";
    public bool CanExport => true;

    public IEnumerable<string> List() =>
        Directory.Exists(Storage.PrivateDir)
            ? Directory.EnumerateFiles(Storage.PrivateDir, "*.enc")
            : [];

    public string DisplayName(string path) => Path.GetFileName(path);

    // Quita el .enc final y luego toma la extensión real.
    public string InnerExt(string path) =>
        Path.GetExtension(Path.GetFileNameWithoutExtension(path)).ToLowerInvariant();

    public byte[] Read(string path) => PrivateVault.Instance.Open(path);

    public void Delete(string path) => File.Delete(path);

    public string SaveText(string text) =>
        PrivateVault.Instance.Save(Encoding.UTF8.GetBytes(text), ".txt");
}

/// <summary>Carpeta pública: capturas normales (sin cifrar) de Imágenes\Cutter.</summary>
public sealed class PublicStore : IMediaStore
{
    private static readonly string[] Exts = { ".png", ".gif", ".jpg", ".jpeg", ".bmp", ".txt" };

    public string Title => "Cutter — Carpeta pública";
    public string Kind => "público";
    public bool CanExport => false; // ya están sin cifrar en disco

    public IEnumerable<string> List()
    {
        if (!Directory.Exists(Storage.PublicDir)) return [];
        return Directory.EnumerateFiles(Storage.PublicDir) // solo nivel superior (excluye Privado)
            .Where(f => Exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderByDescending(f => f);
    }

    public string DisplayName(string path) => Path.GetFileName(path);

    public string InnerExt(string path) => Path.GetExtension(path).ToLowerInvariant();

    public byte[] Read(string path) => File.ReadAllBytes(path);

    public void Delete(string path) => File.Delete(path);

    public string SaveText(string text)
    {
        Storage.EnsureDirs();
        string path = Storage.PublicPath(".txt");
        File.WriteAllText(path, text, Encoding.UTF8);
        return path;
    }
}
