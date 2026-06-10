using System.Windows;
using System.Windows.Controls;

namespace Cutter;

/// <summary>Pide la contraseña de la bóveda (crear la 1ª vez o desbloquear).</summary>
public sealed class PasswordDialog : Window
{
    private readonly PasswordBox _pwd = new() { Margin = new Thickness(0, 4, 0, 8), MinWidth = 260 };
    private readonly PasswordBox _confirm = new() { Margin = new Thickness(0, 4, 0, 8), MinWidth = 260 };
    private readonly TextBlock _error = new() { Foreground = System.Windows.Media.Brushes.IndianRed };

    public string Password => _pwd.Password;

    private PasswordDialog(bool creating)
    {
        Title = creating ? "Crear contraseña de la carpeta privada" : "Desbloquear carpeta privada";
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = creating
                ? "Esta contraseña cifra tus capturas privadas (AES-256).\nSi la pierdes, NO se pueden recuperar."
                : "Introduce la contraseña de tu carpeta privada.",
            Margin = new Thickness(0, 0, 0, 10)
        });

        panel.Children.Add(new TextBlock { Text = "Contraseña" });
        panel.Children.Add(_pwd);

        if (creating)
        {
            panel.Children.Add(new TextBlock { Text = "Repetir contraseña" });
            panel.Children.Add(_confirm);
        }

        panel.Children.Add(_error);

        var ok = new Button { Content = "Aceptar", Width = 90, IsDefault = true, Margin = new Thickness(0, 8, 8, 0) };
        var cancel = new Button { Content = "Cancelar", Width = 90, IsCancel = true, Margin = new Thickness(0, 8, 0, 0) };
        ok.Click += (_, _) =>
        {
            if (_pwd.Password.Length < 4)
            {
                _error.Text = "Mínimo 4 caracteres.";
                return;
            }
            if (creating && _pwd.Password != _confirm.Password)
            {
                _error.Text = "Las contraseñas no coinciden.";
                return;
            }
            DialogResult = true;
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HAlign.Right
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        Content = panel;
        Loaded += (_, _) => _pwd.Focus();
    }

    /// <summary>
    /// Garantiza que la bóveda esté desbloqueada. Crea contraseña la 1ª vez.
    /// Devuelve true si quedó desbloqueada.
    /// </summary>
    public static bool EnsureUnlocked(Window? owner = null)
    {
        var vault = PrivateVault.Instance;
        if (vault.IsUnlocked) return true;

        bool creating = !vault.IsConfigured;
        var dlg = new PasswordDialog(creating);
        if (owner is not null) dlg.Owner = owner;

        while (dlg.ShowDialog() == true)
        {
            if (creating)
            {
                vault.Create(dlg.Password);
                return true;
            }
            if (vault.Unlock(dlg.Password)) return true;

            dlg._error.Text = "Contraseña incorrecta.";
            dlg._pwd.Clear();
        }
        return false;
    }
}
