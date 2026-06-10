using Microsoft.Win32;

namespace Cutter;

/// <summary>
/// Ajuste de Windows 11: "Usar el botón Impr Pant para abrir la captura".
/// Mientras esté activo, el SO abre la Herramienta de recortes con Impr Pant
/// antes de que nuestra app pueda usarla. Se controla con el valor de registro
/// HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled (1=activo).
/// </summary>
public static class WindowsSettings
{
    private const string KeyPath = @"Control Panel\Keyboard";
    private const string ValueName = "PrintScreenKeyForSnippingEnabled";

    /// <summary>true si la Herramienta de recortes se queda Impr Pant.</summary>
    public static bool SnippingToolOwnsPrintScreen()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        object? v = key?.GetValue(ValueName);
        // Por defecto en Win11 está activo (no existe o = 1).
        if (v is null) return true;
        return v.ToString() != "0";
    }

    /// <summary>Libera Impr Pant para que la use Cutter. Devuelve true si cambió algo.</summary>
    public static bool ReleasePrintScreen()
    {
        if (!SnippingToolOwnsPrintScreen()) return false;
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(ValueName, 0, RegistryValueKind.DWord);
        return true;
    }

    /// <summary>Restaura el comportamiento original de Windows.</summary>
    public static void RestorePrintScreen()
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(ValueName, 1, RegistryValueKind.DWord);
    }
}
