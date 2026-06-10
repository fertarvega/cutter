using System.Windows;

namespace Cutter;

/// <summary>
/// Tema claro/oscuro usando el ThemeMode nativo de WPF (tema Fluent de
/// .NET 9+). Tematiza correctamente TODOS los controles (incluidos TextBox y
/// PasswordBox) y la barra de título, cosa que los estilos manuales no logran
/// con el tema Fluent.
/// </summary>
public static class Theme
{
    public static AppTheme Current { get; private set; } = AppTheme.Dark;

    public static void Apply(AppTheme theme)
    {
        Current = theme;
#pragma warning disable WPF0001 // ThemeMode marcado como experimental
        Application.Current.ThemeMode = theme == AppTheme.Dark ? ThemeMode.Dark : ThemeMode.Light;
#pragma warning restore WPF0001
    }
}
