using System.Runtime.InteropServices;

namespace Cutter;

/// <summary>
/// Hook global de teclado de bajo nivel. Necesario porque Windows 11
/// reserva Impr Pant para la Herramienta de recortes, así que RegisterHotKey
/// no la recibe. El hook intercepta la tecla antes que el SO y la suprime
/// (devuelve 1) para que no abra el recortes.
///
/// Impr Pant normalmente solo genera WM_KEYUP, así que actuamos en KEYUP y
/// suprimimos también el KEYDOWN.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _proc; // referencia para que no lo recoja el GC
    private IntPtr _hook;

    public event Action? ScreenshotRequested;
    public event Action? GifToggleRequested;

    public KeyboardHook()
    {
        _proc = HookProc;
    }

    public void Install()
    {
        _hook = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _proc,
            NativeMethods.GetModuleHandle(null), 0);

        if (_hook == IntPtr.Zero)
            throw new InvalidOperationException(
                "No se pudo instalar el hook de teclado (error " + Marshal.GetLastWin32Error() + ").");

        Log.Write($"Hook instalado OK (handle={_hook}).");
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vk = Marshal.ReadInt32(lParam); // vkCode está al inicio de KBDLLHOOKSTRUCT
            int msg = (int)wParam;

            if (vk == (int)NativeMethods.VK_SNAPSHOT)
            {
                bool ctrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
                Log.Write($"Impr Pant detectada (msg=0x{msg:X}, ctrl={ctrl}) -> suprimida.");

                // Disparamos en KEYUP (Impr Pant suele no enviar KEYDOWN).
                if (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP)
                {
                    if (ctrl) GifToggleRequested?.Invoke();
                    else ScreenshotRequested?.Invoke();
                }

                // Suprimir tanto down como up para que no abra el recortes.
                return 1;
            }
        }
        return NativeMethods.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
}
