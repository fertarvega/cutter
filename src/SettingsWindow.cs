using System.Windows;
using System.Windows.Controls;

namespace Cutter;

/// <summary>Configuración de Cutter. Por ahora: tema oscuro/claro.</summary>
public sealed class SettingsWindow : Window
{
    private static SettingsWindow? _open;

    private SettingsWindow()
    {
        Title = "Cutter — Configuración";
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Topmost = true;

        var panel = new StackPanel { Margin = new Thickness(18), MinWidth = 320 };
        panel.Children.Add(new TextBlock
        {
            Text = "Tema",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var dark = new RadioButton
        {
            Content = "Oscuro",
            Margin = new Thickness(0, 3, 0, 3),
            IsChecked = Theme.Current == AppTheme.Dark
        };
        var light = new RadioButton
        {
            Content = "Claro",
            Margin = new Thickness(0, 3, 0, 3),
            IsChecked = Theme.Current == AppTheme.Light
        };
        dark.Checked += (_, _) => SetTheme(AppTheme.Dark);
        light.Checked += (_, _) => SetTheme(AppTheme.Light);
        panel.Children.Add(dark);
        panel.Children.Add(light);

        var close = new Button
        {
            Content = "Cerrar",
            Width = 90,
            Margin = new Thickness(0, 16, 0, 0),
            Padding = new Thickness(10, 6, 10, 6),
            HorizontalAlignment = HAlign.Right,
            IsCancel = true
        };
        close.Click += (_, _) => Close();
        panel.Children.Add(close);

        Content = panel;
        Closed += (_, _) => _open = null;
    }

    public static void ShowSingleton()
    {
        if (_open is not null) { _open.Activate(); return; }
        _open = new SettingsWindow();
        _open.Show();
    }

    private static void SetTheme(AppTheme theme)
    {
        Theme.Apply(theme);
        var s = AppSettings.Load();
        s.Theme = theme;
        s.Save();
    }
}
