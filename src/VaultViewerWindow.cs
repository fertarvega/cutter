using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cutter;

/// <summary>
/// Visor de la carpeta privada. Desbloquea la bóveda, lista los .enc y
/// permite previsualizar (imagen/texto), abrir con la app del sistema o
/// exportar el archivo descifrado.
/// </summary>
public sealed class VaultViewerWindow : Window
{
    private readonly ListBox _list = new() { MinWidth = 240 };
    private readonly Image _image = new() { Stretch = Stretch.Uniform };
    private readonly TextBox _text = new()
    {
        IsReadOnly = true,
        TextWrapping = TextWrapping.Wrap,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        FontFamily = new FontFamily("Consolas"),
        Visibility = Visibility.Collapsed
    };
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap };
    private readonly List<string> _tempFiles = new();

    private VaultViewerWindow()
    {
        Title = "Cutter — Carpeta privada 🔒";
        Width = 960;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var root = new Grid { Margin = new Thickness(10) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Lista a la izquierda
        Grid.SetRow(_list, 0);
        Grid.SetColumn(_list, 0);
        _list.SelectionChanged += (_, _) => Preview();
        root.Children.Add(_list);

        // Previsualización a la derecha (imagen o texto)
        var previewHost = new Grid { Margin = new Thickness(10, 0, 0, 0) };
        var imgBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
            Child = _image
        };
        previewHost.Children.Add(imgBorder);
        previewHost.Children.Add(_text);
        Grid.SetRow(previewHost, 0);
        Grid.SetColumn(previewHost, 1);
        root.Children.Add(previewHost);

        // Estado
        Grid.SetRow(_status, 1);
        Grid.SetColumn(_status, 0);
        Grid.SetColumnSpan(_status, 2);
        root.Children.Add(_status);

        // Botones
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HAlign.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        bar.Children.Add(MakeButton("Abrir con app del sistema", (_, _) => OpenExternal()));
        bar.Children.Add(MakeButton("Exportar descifrado…", (_, _) => Export()));
        bar.Children.Add(MakeButton("Eliminar", (_, _) => DeleteSelected()));
        bar.Children.Add(MakeButton("Cerrar", (_, _) => Close()));
        Grid.SetRow(bar, 2);
        Grid.SetColumn(bar, 0);
        Grid.SetColumnSpan(bar, 2);
        root.Children.Add(bar);

        Content = root;
        Closed += (_, _) => CleanupTemp();
    }

    /// <summary>Abre el visor tras pedir la contraseña. No-op si no hay bóveda o se cancela.</summary>
    public static void Open(Window? owner = null)
    {
        if (!PrivateVault.Instance.IsConfigured)
        {
            MessageBox.Show("Aún no hay carpeta privada. Guarda algo como privado primero.",
                "Cutter", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!PasswordDialog.EnsureUnlocked(owner)) return;

        var w = new VaultViewerWindow();
        if (owner is not null) w.Owner = owner;
        w.Refresh();
        w.Show();
    }

    private static Button MakeButton(string text, RoutedEventHandler onClick)
    {
        var b = new Button { Content = text, MinWidth = 120, Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 6, 10, 6) };
        b.Click += onClick;
        return b;
    }

    private void Refresh()
    {
        _list.Items.Clear();
        if (!Directory.Exists(Storage.PrivateDir)) return;
        foreach (var f in Directory.EnumerateFiles(Storage.PrivateDir, "*.enc"))
            _list.Items.Add(Path.GetFileName(f));
        _status.Text = $"{_list.Items.Count} archivo(s) privado(s).";
    }

    private string? SelectedPath()
    {
        if (_list.SelectedItem is not string name) return null;
        return Path.Combine(Storage.PrivateDir, name);
    }

    /// <summary>Extensión real del contenido (quita el .enc final).</summary>
    private static string InnerExt(string encPath) =>
        Path.GetExtension(Path.GetFileNameWithoutExtension(encPath)).ToLowerInvariant();

    private void Preview()
    {
        string? enc = SelectedPath();
        if (enc is null) return;

        try
        {
            byte[] data = PrivateVault.Instance.Open(enc);
            string ext = InnerExt(enc);

            if (ext == ".txt")
            {
                _text.Text = System.Text.Encoding.UTF8.GetString(data);
                _text.Visibility = Visibility.Visible;
                _image.Source = null;
            }
            else // imagen o gif: muestra el (primer) fotograma
            {
                var bmp = new BitmapImage();
                using (var ms = new MemoryStream(data))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                }
                bmp.Freeze();
                _image.Source = bmp;
                _text.Visibility = Visibility.Collapsed;
            }
            _status.Text = $"{Path.GetFileName(enc)} — descifrado en memoria.";
        }
        catch (Exception ex)
        {
            _status.Text = "Error al descifrar: " + ex.Message;
        }
    }

    private void OpenExternal()
    {
        string? enc = SelectedPath();
        if (enc is null) { _status.Text = "Selecciona un archivo."; return; }

        try
        {
            byte[] data = PrivateVault.Instance.Open(enc);
            string dir = Path.Combine(Path.GetTempPath(), "CutterView");
            Directory.CreateDirectory(dir);
            string name = Path.GetFileNameWithoutExtension(enc); // quita .enc
            string tmp = Path.Combine(dir, name);
            File.WriteAllBytes(tmp, data);
            _tempFiles.Add(tmp);

            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(tmp) { UseShellExecute = true });
            _status.Text = "Abierto con la app del sistema (copia temporal, se borra al cerrar el visor).";
        }
        catch (Exception ex)
        {
            _status.Text = "Error al abrir: " + ex.Message;
        }
    }

    private void Export()
    {
        string? enc = SelectedPath();
        if (enc is null) { _status.Text = "Selecciona un archivo."; return; }

        string suggested = Path.GetFileNameWithoutExtension(enc);
        var dlg = new Microsoft.Win32.SaveFileDialog { FileName = suggested };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllBytes(dlg.FileName, PrivateVault.Instance.Open(enc));
            _status.Text = "Exportado SIN cifrar a: " + dlg.FileName;
        }
        catch (Exception ex)
        {
            _status.Text = "Error al exportar: " + ex.Message;
        }
    }

    private void DeleteSelected()
    {
        string? enc = SelectedPath();
        if (enc is null) { _status.Text = "Selecciona un archivo."; return; }

        if (MessageBox.Show("¿Eliminar definitivamente este archivo privado?", "Cutter",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        File.Delete(enc);
        _image.Source = null;
        _text.Visibility = Visibility.Collapsed;
        Refresh();
    }

    private void CleanupTemp()
    {
        foreach (var f in _tempFiles)
            try { File.Delete(f); } catch { }
        _tempFiles.Clear();
    }
}
