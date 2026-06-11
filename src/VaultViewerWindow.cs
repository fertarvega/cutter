using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cutter;

/// <summary>
/// Visor de archivos de Cutter. Funciona con la carpeta pública (sin cifrar) o
/// la privada (cifrada) según el <see cref="IMediaStore"/> que reciba: lista,
/// previsualiza (imagen/GIF/texto), abre con la app del sistema, hace OCR de
/// imágenes y, en privado, exporta descifrado.
/// </summary>
public sealed class MediaViewerWindow : Window
{
    private readonly IMediaStore _store;
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
    private readonly GifPlayer _gif;

    private MediaViewerWindow(IMediaStore store)
    {
        _store = store;
        _gif = new GifPlayer(_image);
        Title = store.Title;
        Width = 960;
        Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var root = new Grid { Margin = new Thickness(10) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetRow(_list, 0);
        Grid.SetColumn(_list, 0);
        _list.SelectionChanged += (_, _) => Preview();
        root.Children.Add(_list);

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

        Grid.SetRow(_status, 1);
        Grid.SetColumn(_status, 0);
        Grid.SetColumnSpan(_status, 2);
        root.Children.Add(_status);

        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HAlign.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        bar.Children.Add(MakeButton("Refrescar", (_, _) => Refresh()));
        bar.Children.Add(MakeButton("Abrir con app del sistema", (_, _) => OpenExternal()));
        bar.Children.Add(MakeButton("OCR → texto", (_, _) => RunOcr()));
        if (_store.CanExport)
            bar.Children.Add(MakeButton("Exportar descifrado…", (_, _) => Export()));
        bar.Children.Add(MakeButton("Eliminar", (_, _) => DeleteSelected()));
        bar.Children.Add(MakeButton("Cerrar", (_, _) => Close()));
        Grid.SetRow(bar, 2);
        Grid.SetColumn(bar, 0);
        Grid.SetColumnSpan(bar, 2);
        root.Children.Add(bar);

        Content = root;
        Closed += (_, _) => { _gif.Stop(); CleanupTemp(); };
    }

    /// <summary>Abre el visor de la carpeta privada (pide contraseña). null si se cancela.</summary>
    public static MediaViewerWindow? OpenPrivate(Window? owner = null)
    {
        if (!PrivateVault.Instance.IsConfigured)
        {
            MessageBox.Show("Aún no hay carpeta privada. Guarda algo como privado primero.",
                "Cutter", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }
        if (!PasswordDialog.EnsureUnlocked(owner)) return null;
        return Show(new PrivateStore(), owner);
    }

    /// <summary>Abre el visor de la carpeta pública (sin contraseña).</summary>
    public static MediaViewerWindow OpenPublic(Window? owner = null) => Show(new PublicStore(), owner);

    private static MediaViewerWindow Show(IMediaStore store, Window? owner)
    {
        var w = new MediaViewerWindow(store);
        if (owner is not null) w.Owner = owner;
        w.Refresh();
        w.Show();
        w.Activate();
        return w;
    }

    private static Button MakeButton(string text, RoutedEventHandler onClick)
    {
        var b = new Button { Content = text, MinWidth = 120, Margin = new Thickness(6, 0, 0, 0), Padding = new Thickness(10, 6, 10, 6) };
        b.Click += onClick;
        return b;
    }

    /// <summary>Elemento de la lista: guarda la ruta completa, muestra el nombre.</summary>
    private sealed record Item(string Path, string Name)
    {
        public override string ToString() => Name;
    }

    private void Refresh()
    {
        _list.Items.Clear();
        foreach (var f in _store.List())
            _list.Items.Add(new Item(f, _store.DisplayName(f)));
        _status.Text = $"{_list.Items.Count} archivo(s) {_store.Kind}(s).";
    }

    private string? SelectedPath() => (_list.SelectedItem as Item)?.Path;

    private void Preview()
    {
        string? path = SelectedPath();
        if (path is null) return;

        try
        {
            _gif.Stop();
            byte[] data = _store.Read(path);
            string ext = _store.InnerExt(path);

            if (ext == ".txt")
            {
                _text.Text = System.Text.Encoding.UTF8.GetString(data);
                _text.Visibility = Visibility.Visible;
                _image.Source = null;
            }
            else if (ext == ".gif")
            {
                _gif.Play(data);
                _text.Visibility = Visibility.Collapsed;
            }
            else
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
            _status.Text = _store.DisplayName(path);
        }
        catch (Exception ex)
        {
            _status.Text = "Error al cargar: " + ex.Message;
        }
    }

    private void OpenExternal()
    {
        string? path = SelectedPath();
        if (path is null) { _status.Text = "Selecciona un archivo."; return; }

        try
        {
            byte[] data = _store.Read(path);
            string dir = Path.Combine(Path.GetTempPath(), "CutterView");
            Directory.CreateDirectory(dir);
            // nombre legible (sin el .enc en privado)
            string name = Path.GetFileNameWithoutExtension(_store.DisplayName(path)) + _store.InnerExt(path);
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

    /// <summary>
    /// OCR sobre la imagen seleccionada y guarda el texto en la misma carpeta
    /// (cifrado en privado, en claro en pública). Útil si no se hizo OCR al
    /// capturar.
    /// </summary>
    private async void RunOcr()
    {
        string? path = SelectedPath();
        if (path is null) { _status.Text = "Selecciona un archivo."; return; }

        string ext = _store.InnerExt(path);
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".bmp"))
        {
            _status.Text = "OCR solo disponible para imágenes.";
            return;
        }

        _status.Text = "Reconociendo texto…";
        string text;
        try
        {
            text = await Ocr.RecognizeAsync(_store.Read(path));
        }
        catch (Exception ex)
        {
            _status.Text = "Error OCR: " + ex.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _status.Text = "No se detectó texto en la imagen.";
            return;
        }

        _gif.Stop();
        _image.Source = null;
        _text.Text = text;
        _text.Visibility = Visibility.Visible;

        string saved = _store.SaveText(text);
        Refresh();
        _status.Text = $"Texto reconocido y guardado ({_store.Kind}): {Path.GetFileName(saved)}";
    }

    private void Export()
    {
        string? path = SelectedPath();
        if (path is null) { _status.Text = "Selecciona un archivo."; return; }

        string suggested = Path.GetFileNameWithoutExtension(_store.DisplayName(path));
        var dlg = new Microsoft.Win32.SaveFileDialog { FileName = suggested };
        if (dlg.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllBytes(dlg.FileName, _store.Read(path));
            _status.Text = "Exportado SIN cifrar a: " + dlg.FileName;
        }
        catch (Exception ex)
        {
            _status.Text = "Error al exportar: " + ex.Message;
        }
    }

    private void DeleteSelected()
    {
        string? path = SelectedPath();
        if (path is null) { _status.Text = "Selecciona un archivo."; return; }

        if (MessageBox.Show("¿Eliminar definitivamente este archivo?", "Cutter",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _gif.Stop();
        _store.Delete(path);
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
