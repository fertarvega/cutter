using System.Drawing;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace Cutter;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--selftest")
        {
            Environment.Exit(SelfTest.Run());
            return;
        }
        var app = new App();
        app.Run();
    }
}

public sealed class App : Application
{
    private WinForms.NotifyIcon _tray = null!;
    private readonly GifRecorder _recorder = new();
    private readonly KeyboardHook _hook = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Storage.EnsureDirs();

        try { System.IO.File.Delete(Log.Path); } catch { }
        Log.Write($"Cutter iniciado. PID={Environment.ProcessId}, admin={IsAdmin()}.");

        // Quitarle Impr Pant a la Herramienta de recortes (si la tiene).
        bool changed = WindowsSettings.ReleasePrintScreen();
        Log.Write(changed
            ? "Impr Pant estaba asignada a recortes; ahora liberada para Cutter."
            : "Impr Pant ya estaba libre.");

        SetupTray();
        InstallHook();
    }

    private void SetupTray()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Capturar pantalla", null, (_, _) => DoScreenshot());
        menu.Items.Add("Grabar / parar GIF", null, (_, _) => ToggleGif());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Ver carpeta privada 🔒", null, (_, _) => VaultViewerWindow.Open());
        menu.Items.Add("Bloquear carpeta privada", null, (_, _) =>
        {
            PrivateVault.Instance.Lock();
            Notify("Cutter", "Carpeta privada bloqueada.");
        });
        menu.Items.Add("Abrir carpeta Cutter", null, (_, _) =>
            System.Diagnostics.Process.Start("explorer.exe", Storage.Root));
        menu.Items.Add("Devolver Impr Pant a Windows", null, (_, _) =>
        {
            WindowsSettings.RestorePrintScreen();
            Notify("Cutter", "Impr Pant vuelve a abrir la Herramienta de recortes de Windows.");
        });
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitApp());

        _tray = new WinForms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Cutter — Impr Pant: captura · Ctrl+Impr Pant: GIF",
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => DoScreenshot();
    }

    private void InstallHook()
    {
        // El hook se dispara en el hilo de UI; diferimos el trabajo pesado con
        // BeginInvoke para devolver el control al SO de inmediato.
        _hook.ScreenshotRequested += () => Dispatcher.BeginInvoke(new Action(DoScreenshot));
        _hook.GifToggleRequested += () => Dispatcher.BeginInvoke(new Action(ToggleGif));
        try
        {
            _hook.Install();
        }
        catch (Exception ex)
        {
            Notify("Cutter", "No se pudo capturar Impr Pant: " + ex.Message +
                             " Usa el menú del icono de la bandeja.");
        }
    }

    private void DoScreenshot()
    {
        var area = ScreenCapture.ActiveMonitorBounds();
        using var bmp = ScreenCapture.Grab(area);
        byte[] png = ScreenCapture.ToPng(bmp);
        var preview = ScreenCapture.ToBitmapSource(bmp);
        new PreviewWindow(preview, png, CaptureKind.Image).Show();
    }

    private void ToggleGif()
    {
        if (!_recorder.IsRecording)
        {
            var area = ScreenCapture.ActiveMonitorBounds();
            _recorder.Start(area);
            Notify("Cutter", "Grabando GIF… pulsa Ctrl+Impr Pant para parar (máx 30 s).");
            return;
        }

        var frames = _recorder.StopAndCollect();
        if (frames.Count == 0)
        {
            Notify("Cutter", "GIF vacío (muy corto).");
            return;
        }

        byte[] gif = GifBuilder.Build(frames, GifRecorder.FrameDelayMs);
        var preview = ScreenCapture.ToBitmapSource(frames[0]);
        new PreviewWindow(preview, gif, CaptureKind.Gif).Show();

        foreach (var f in frames) f.Dispose();
    }

    private static bool IsAdmin()
    {
        using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
        return new System.Security.Principal.WindowsPrincipal(id)
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    private void Notify(string title, string text) =>
        _tray.ShowBalloonTip(4000, title, text, WinForms.ToolTipIcon.Info);

    private void ExitApp()
    {
        _hook.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        Shutdown();
    }
}
