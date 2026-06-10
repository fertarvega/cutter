# Cutter

Herramienta de captura para Windows: capturas y GIFs con OCR local y
carpeta privada cifrada que se sincroniza a OneDrive.

## Qué hace

- **Impr Pant** → captura el monitor donde está el cursor y abre una ventana.
- **Ctrl + Impr Pant** → empieza/para la grabación de un **GIF** (máx 30 s).
- La ventana de previsualización ofrece:
  - **Guardar** → PNG/GIF en `Imágenes\Cutter` (se sube a OneDrive).
  - **Copiar** → imagen al portapapeles.
  - **OCR → privado** → extrae el texto con el motor local de Windows y lo
    guarda **cifrado** en la carpeta privada.
  - **Guardar privada 🔒** → cifra la imagen/GIF (AES-256-GCM) en
    `Imágenes\Cutter\Privado`.
- Icono en la bandeja con menú (capturar, GIF, bloquear bóveda, abrir carpeta, salir).
  Doble clic en el icono = captura.

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
| Hotkeys | `RegisterHotKey` (Win32) |
| GIF | frames vía GDI+, ensamblado manual con bucle y retardo |
| Cifrado | AES-256-GCM + PBKDF2-SHA256 |

## Limitaciones / siguientes pasos

- La captura toma el **monitor completo** bajo el cursor. Falta un selector de
  región (overlay arrastrable) — es la mejora principal pendiente.
- El GIF graba a ~10 fps el monitor completo. Falta poder elegir región y fps.
- Falta un visor para volver a abrir y descifrar archivos de la bóveda dentro
  de la app (ahora se descifran con la lógica de `PrivateVault.Open`).
- **Impr Pant** se captura con un hook de teclado de bajo nivel, así que
  funciona aunque Windows 11 la tenga reservada para la Herramienta de recortes
  (Cutter la intercepta y suprime). Excepción: si la ventana en primer plano se
  ejecuta como **administrador** y Cutter no, el hook no recibe la tecla (UIPI);
  ahí usa el menú de la bandeja o ejecuta Cutter como administrador.
