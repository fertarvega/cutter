# Cutter

Herramienta de captura para Windows: capturas y GIFs con OCR local y
carpeta privada cifrada que se sincroniza a OneDrive.

## Qué hace

- **Impr Pant (una vez)** → abre un overlay para **arrastrar y seleccionar la
  región** a capturar; luego abre la ventana de previsualización.
- **Impr Pant (dos veces seguidas)** → empieza la grabación de un **GIF** de la
  región que selecciones. **Doble Impr Pant otra vez** lo para (máx 30 s).
  Mientras graba, aparece una insignia **● REC** junto a la región (fuera del
  GIF). El GIF respeta los tiempos reales entre fotogramas y va en bucle.
- La ventana de previsualización ofrece:
  - **Guardar** → PNG/GIF en `Imágenes\Cutter` (se sube a OneDrive).
  - **Copiar** → imagen al portapapeles.
  - **OCR → privado** → extrae el texto con el motor local de Windows y lo
    guarda **cifrado** en la carpeta privada.
  - **Guardar privada 🔒** → cifra la imagen/GIF (AES-256-GCM) en
    `Imágenes\Cutter\Privado`.
- Icono en la bandeja con menú (capturar región, GIF, ver/bloquear bóveda,
  abrir carpeta, **Configuración**, devolver Impr Pant a Windows, salir).
  Doble clic en el icono = abrir carpeta privada.
- **Tema oscuro** por defecto (claro opcional en Configuración, se recuerda).
- Hay **visor de carpeta pública y privada** (mismo visor): listar, previsualizar
  imagen/GIF/texto, abrir con la app del sistema y hacer **OCR de una imagen
  guardada** más tarde (botón "OCR → texto"). En privado el texto se guarda
  cifrado; en pública, en claro.

> El doble Impr Pant añade ~350 ms de espera a la captura simple (hace falta
> para distinguir una pulsación de dos).

## Carpeta "con contraseña"

Windows no tiene carpetas protegidas por clave de verdad. La privacidad se
consigue **cifrando cada archivo** con AES-256-GCM. La clave se deriva de tu
contraseña con PBKDF2-SHA256 (210 000 iteraciones). Los `.enc` se sincronizan
a OneDrive ya cifrados.

> Si pierdes la contraseña, los archivos **no se pueden recuperar**. No hay
> puerta trasera.

La primera vez que guardes algo privado se te pedirá crear la contraseña.
"Bloquear carpeta privada" en la bandeja olvida la clave hasta volver a entrar.

## Contenido protegido (Netflix / Crunchyroll / Udemy)

Esas apps salen **en negro** a propósito: usan DRM (Widevine / PlayReady) que
marca el vídeo como no capturable. Cutter **no** intenta saltarse esa
protección. Funciona con todo lo demás (escritorio, juegos, webs sin DRM, etc.).

## Instalación rápida (recomendado)

Requiere **.NET 10 SDK** en Windows. Desde la carpeta del proyecto:

```powershell
# Poner Cutter como herramienta de captura por defecto
pwsh -ExecutionPolicy Bypass -File scripts\install.ps1
```

`install.ps1` hace todo automático:
1. Compila Cutter en Release y lo copia a `%LOCALAPPDATA%\Programs\Cutter`.
2. Crea un acceso directo en Inicio (arranca con Windows).
3. Le quita **Impr Pant** a la Herramienta de recortes de Windows.
4. Arranca Cutter.

Para **quitarlo** y devolver Impr Pant a Windows:

```powershell
pwsh -ExecutionPolicy Bypass -File scripts\uninstall.ps1
```

`uninstall.ps1` cierra la app, quita el arranque automático, restaura el
ajuste de Windows y borra la instalación. **No toca** tus capturas de
`Imágenes\Cutter`. Tras desinstalar, cierra sesión y vuelve a entrar para que
Impr Pant abra de nuevo el recortes.

> Si no tienes `pwsh` (PowerShell 7), usa `powershell` en su lugar.

## Compilar y ejecutar a mano

```powershell
dotnet build
dotnet run
```

El ejecutable queda en `bin\Debug\net10.0-windows10.0.19041.0\Cutter.exe`.

### Pruebas internas

```powershell
.\bin\Debug\net10.0-windows10.0.19041.0\Cutter.exe --selftest
# resultado en %TEMP%\cutter_selftest.log
```

## Stack

| Pieza | Tecnología |
|---|---|
| UI / app | C# .NET 10, WPF + WinForms (bandeja) |
| OCR | `Windows.Media.Ocr` (offline, integrado en Windows) |
| Hotkeys | Hook de teclado de bajo nivel `WH_KEYBOARD_LL` |
| Región | Overlay WPF posicionado en píxeles físicos (`SetWindowPos`), DPI por monitor |
| GIF | frames vía GDI+, ensamblado manual con bucle y retardo real por fotograma |
| Cifrado | AES-256-GCM + PBKDF2-SHA256 |

## Limitaciones / siguientes pasos

- El GIF graba a ~12 fps. Falta poder elegir los fps.
- **Impr Pant** se captura con un hook de teclado de bajo nivel, así que
  funciona aunque Windows 11 la tenga reservada para la Herramienta de recortes
  (Cutter la intercepta y suprime). Excepción: si la ventana en primer plano se
  ejecuta como **administrador** y Cutter no, el hook no recibe la tecla (UIPI);
  ahí usa el menú de la bandeja o ejecuta Cutter como administrador.
