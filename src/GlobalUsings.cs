// Con UseWPF + UseWindowsForms + System.Drawing hay muchos tipos con el
// mismo nombre en varios frameworks. Estos alias fijan cuál usamos por
// defecto en todo el proyecto.
global using Application = System.Windows.Application;
global using Button = System.Windows.Controls.Button;
global using TextBox = System.Windows.Controls.TextBox;
global using MessageBox = System.Windows.MessageBox;
global using Image = System.Windows.Controls.Image;
global using Color = System.Windows.Media.Color;
global using FontFamily = System.Windows.Media.FontFamily;
global using Orientation = System.Windows.Controls.Orientation;
global using Clipboard = System.Windows.Clipboard;
global using ListBox = System.Windows.Controls.ListBox;
global using HAlign = System.Windows.HorizontalAlignment;
global using VAlign = System.Windows.VerticalAlignment;
