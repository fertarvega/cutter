using System.IO;

namespace Cutter;

/// <summary>Log simple a archivo para diagnosticar (WinExe no tiene consola).</summary>
public static class Log
{
    public static readonly string Path =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cutter.log");

    private static readonly object Gate = new();

    public static void Write(string msg)
    {
        try
        {
            lock (Gate)
                File.AppendAllText(Path, $"{DateTime.Now:HH:mm:ss.fff}  {msg}{Environment.NewLine}");
        }
        catch { /* nunca romper por logging */ }
    }
}
