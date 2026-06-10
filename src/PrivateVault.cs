using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cutter;

/// <summary>
/// Bóveda cifrada = la "carpeta con contraseña".
/// Windows no tiene carpetas protegidas por clave reales, así que cada
/// archivo privado se cifra con AES-256-GCM. La clave se deriva de la
/// contraseña con PBKDF2 (SHA-256). Nada se guarda en claro.
///
/// Formato de archivo .enc:
///   [12 bytes nonce][16 bytes tag][ciphertext]
/// </summary>
public sealed class PrivateVault
{
    private const int Iterations = 210_000;
    private const string Verifier = "CUTTER_VAULT_OK";

    private static readonly string MetaPath = Path.Combine(Storage.PrivateDir, "vault.meta");

    private byte[]? _key; // clave de sesión (en memoria mientras la app vive)

    public static PrivateVault Instance { get; } = new();

    public bool IsConfigured => File.Exists(MetaPath);
    public bool IsUnlocked => _key is not null;

    private sealed record Meta(string Salt, int Iter, string VNonce, string VCipher);

    /// <summary>Crea la bóveda por primera vez con una contraseña.</summary>
    public void Create(string password)
    {
        Storage.EnsureDirs();
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] key = Derive(password, salt);

        (byte[] nonce, byte[] cipherWithTag) = Encrypt(key, Encoding.UTF8.GetBytes(Verifier));

        var meta = new Meta(
            Convert.ToBase64String(salt),
            Iterations,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipherWithTag));

        File.WriteAllText(MetaPath, JsonSerializer.Serialize(meta));
        _key = key;
    }

    /// <summary>Desbloquea con la contraseña. Devuelve false si es incorrecta.</summary>
    public bool Unlock(string password)
    {
        if (!IsConfigured) return false;
        var meta = JsonSerializer.Deserialize<Meta>(File.ReadAllText(MetaPath))!;
        byte[] salt = Convert.FromBase64String(meta.Salt);
        byte[] key = Derive(password, salt, meta.Iter);

        try
        {
            byte[] plain = Decrypt(
                key,
                Convert.FromBase64String(meta.VNonce),
                Convert.FromBase64String(meta.VCipher));
            if (Encoding.UTF8.GetString(plain) == Verifier)
            {
                _key = key;
                return true;
            }
        }
        catch (CryptographicException) { /* tag inválido => contraseña mala */ }
        return false;
    }

    public void Lock() => _key = null;

    /// <summary>Guarda datos cifrados en la bóveda. Devuelve la ruta del .enc.</summary>
    public string Save(byte[] data, string originalExtension)
    {
        if (_key is null) throw new InvalidOperationException("Bóveda bloqueada.");
        Storage.EnsureDirs();

        (byte[] nonce, byte[] cipherWithTag) = Encrypt(_key, data);
        string name = $"{Storage.Stamp()}{originalExtension}.enc";
        string path = Path.Combine(Storage.PrivateDir, name);

        using var fs = File.Create(path);
        fs.Write(nonce);
        fs.Write(cipherWithTag);
        return path;
    }

    /// <summary>Descifra un .enc de la bóveda (para volver a verlo).</summary>
    public byte[] Open(string encPath)
    {
        if (_key is null) throw new InvalidOperationException("Bóveda bloqueada.");
        byte[] all = File.ReadAllBytes(encPath);
        byte[] nonce = all[..12];
        byte[] cipherWithTag = all[12..];
        return Decrypt(_key, nonce, cipherWithTag);
    }

    // ---- primitivas ----

    private static byte[] Derive(string password, byte[] salt, int iter = Iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iter, HashAlgorithmName.SHA256, 32);

    private static (byte[] nonce, byte[] cipherWithTag) Encrypt(byte[] key, byte[] plain)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        byte[] cipherWithTag = new byte[16 + cipher.Length];
        tag.CopyTo(cipherWithTag, 0);
        cipher.CopyTo(cipherWithTag, 16);
        return (nonce, cipherWithTag);
    }

    private static byte[] Decrypt(byte[] key, byte[] nonce, byte[] cipherWithTag)
    {
        byte[] tag = cipherWithTag[..16];
        byte[] cipher = cipherWithTag[16..];
        byte[] plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }
}
