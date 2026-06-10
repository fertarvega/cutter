<#
    install.ps1 — Pone Cutter como herramienta de captura por defecto.

    Pasos automaticos:
      1. Compila Cutter en Release y lo copia a %LOCALAPPDATA%\Programs\Cutter.
      2. Crea un acceso directo en Inicio para que arranque con Windows.
      3. Le quita Impr Pant a la Herramienta de recortes de Windows.
      4. Arranca Cutter.

    Uso:  pwsh -ExecutionPolicy Bypass -File scripts\install.ps1
          (o)  click derecho > Ejecutar con PowerShell
#>
#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ProjectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Csproj     = Join-Path $ProjectDir 'Cutter.csproj'
$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Cutter'
$ExePath    = Join-Path $InstallDir 'Cutter.exe'

Write-Host '== Cutter :: instalacion ==' -ForegroundColor Cyan

# 0. Cerrar instancia previa (si la hay) para no bloquear archivos
Get-Process Cutter -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

# 1. Compilar y publicar
Write-Host '1/4 Compilando (Release)...'
dotnet publish $Csproj -c Release -o $InstallDir --nologo
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion. Revisa que el SDK .NET 10 este instalado.' }

# 2. Acceso directo en la carpeta de Inicio
Write-Host '2/4 Configurando arranque con Windows...'
$StartupDir   = [Environment]::GetFolderPath('Startup')
$ShortcutPath = Join-Path $StartupDir 'Cutter.lnk'
$ws  = New-Object -ComObject WScript.Shell
$lnk = $ws.CreateShortcut($ShortcutPath)
$lnk.TargetPath       = $ExePath
$lnk.WorkingDirectory = $InstallDir
$lnk.Description       = 'Cutter - captura de pantalla con OCR'
$lnk.Save()

# 3. Liberar Impr Pant de la Herramienta de recortes
Write-Host '3/4 Asignando Impr Pant a Cutter...'
New-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' `
    -Name 'PrintScreenKeyForSnippingEnabled' -Value 0 -PropertyType DWord -Force | Out-Null

# 4. Arrancar
Write-Host '4/4 Arrancando Cutter...'
Start-Process $ExePath

Write-Host ''
Write-Host 'Listo. Cutter instalado en:' -ForegroundColor Green
Write-Host "  $InstallDir"
Write-Host 'Pulsa Impr Pant para capturar. Doble Impr Pant para GIF.'
Write-Host 'Si Impr Pant siguiera abriendo el recortes, cierra sesion y vuelve a entrar una vez.'
