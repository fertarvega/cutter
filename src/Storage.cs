using System.IO;

namespace Cutter;

/// <summary>
/// Rutas de guardado. La carpeta "Imágenes" (MyPictures) suele estar
/// redirigida a OneDrive cuando la copia de seguridad está activa, así
/// que todo lo que se guarde aquí se sincroniza solo.
/// </summary>
public static class Storage
{
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Cutter");

    /// <summary>Capturas e imágenes normales (sin cifrar).</summary>
    public static string PublicDir => Root;

    /// <summary>Carpeta "con contraseña": archivos cifrados (.enc).</summary>
    public static string PrivateDir => Path.Combine(Root, "Privado");

    public static void EnsureDirs()
    {
        Directory.CreateDirectory(PublicDir);
        Directory.CreateDirectory(PrivateDir);
    }

    public static string PublicPath(string ext) =>
        Path.Combine(PublicDir, Stamp() + ext);

    public static string Stamp() => $"Cutter_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
}
