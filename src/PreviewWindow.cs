using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cutter;

public enum CaptureKind { Image, Gif }

/// <summary>
/// Ventana que se abre tras cada captura. Permite guardar normal, copiar,
/// hacer OCR (solo imágenes) y guardar como privada (cifrada).
/// </summary>
public sealed class PreviewWindow : Window
{
    private readonly byte[] _bytes;        // PNG o GIF según el tipo
    private readonly CaptureKind _kind;
    private readonly string _ext;
    private readonly BitmapSource _preview;

    private readonly TextBox _ocrBox;
    private readonly TextBlock _status;
    private readonly Image _img;
    private GifPlayer? _gif;

    public PreviewWindow(BitmapSource preview, byte[] bytes, CaptureKind kind)
    {
        _preview = preview;
        _bytes = bytes;
        _kind = kind;
        _ext = kind == CaptureKind.Gif ? ".gif" : ".png";

        Title = kind == CaptureKind.Gif ? "Cutter — GIF" : "Cutter — Captura";
        Width = 900;
        Height = 680;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Vista previa
        _img = new Image
        {
            Source = preview,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HAlign.Center,
            VerticalAlignment = VAlign.Center
        };
        var scroll = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
            Child = _img
        };
        Grid.SetRow(scroll, 0);
        grid.Children.Add(scroll);

        // Caja de texto OCR (oculta hasta usarse)
        _ocrBox = new TextBox
        {
            Visibility = Visibility.Collapsed,
            Height = 130,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            IsReadOnly = false,
            FontFamily = new FontFamily("Consolas")
        };
        Grid.SetRow(_ocrBox, 1);
        grid.Children.Add(_ocrBox);

        // Estado
        _status = new TextBlock { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
        Grid.SetRow(_status, 2);
        grid.Children.Add(_status);

        // Botones
        var bar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HAlign.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        bar.Children.Add(MakeButton("Guardar", OnSave));
        if (kind == CaptureKind.Image)
        {
            bar.Children.Add(MakeButton("Copiar", OnCopy));
            bar.Children.Add(MakeButton("OCR → privado", OnOcr));
        }
        bar.Children.Add(MakeButton("Guardar privada 🔒", OnSavePrivate));
        bar.Children.Add(MakeButton("Cerrar", (_, _) => Close()));
        Grid.SetRow(bar, 3);
        grid.Children.Add(bar);

        Content = grid;

        if (kind == CaptureKind.Gif)
        {
            _gif = new GifPlayer(_img);
            _gif.Play(_bytes);
        }

        Closed += (_, _) => _gif?.Stop();
    }

    private static Button MakeButton(string text, RoutedEventHandler onClick)
    {
        var b = new Button
        {
            Content = text,
            MinWidth = 120,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(10, 6, 10, 6)
        };
        b.Click += onClick;
        return b;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Storage.EnsureDirs();
        string path = Storage.PublicPath(_ext);
        File.WriteAllBytes(path, _bytes);
        _status.Text = $"Guardado en: {path}";
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        Clipboard.SetImage(_preview);
        _status.Text = "Copiado al portapapeles.";
    }

    private void OnSavePrivate(object sender, RoutedEventArgs e)
    {
        if (!PasswordDialog.EnsureUnlocked(this))
        {
            _status.Text = "Cancelado: carpeta privada bloqueada.";
            return;
        }
        string path = PrivateVault.Instance.Save(_bytes, _ext);
        _status.Text = $"Guardado cifrado en: {path}";
    }

    private async void OnOcr(object sender, RoutedEventArgs e)
    {
        _status.Text = "Reconociendo texto…";
        string text;
        try
        {
            text = await Ocr.RecognizeAsync(_bytes);
        }
        catch (Exception ex)
        {
            _status.Text = "Error OCR: " + ex.Message;
            return;
        }

        _ocrBox.Visibility = Visibility.Visible;
        _ocrBox.Text = string.IsNullOrWhiteSpace(text) ? "(sin texto detectado)" : text;

        if (string.IsNullOrWhiteSpace(text))
        {
            _status.Text = "No se detectó texto.";
            return;
        }

        // El texto reconocido va cifrado a la carpeta privada.
        if (!PasswordDialog.EnsureUnlocked(this))
        {
            _status.Text = "Texto reconocido. Guardado privado cancelado (sin contraseña).";
            return;
        }
        byte[] txt = System.Text.Encoding.UTF8.GetBytes(_ocrBox.Text);
        string path = PrivateVault.Instance.Save(txt, ".txt");
        _status.Text = $"Texto reconocido y guardado cifrado en: {path}";
    }
}
