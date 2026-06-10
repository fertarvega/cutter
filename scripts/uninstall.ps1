<#
    uninstall.ps1 — Quita Cutter y devuelve Impr Pant a la Herramienta de
    recortes de Windows.

    Pasos automaticos:
      1. Cierra Cutter.
      2. Quita el acceso directo de arranque.
      3. Restaura Impr Pant -> Herramienta de recortes de Windows.
      4. Borra la carpeta de instalacion.

    Uso:  pwsh -ExecutionPolicy Bypass -File scripts\uninstall.ps1
#>
#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Cutter'

Write-Host '== Cutter :: desinstalacion ==' -ForegroundColor Cyan

# 1. Cerrar app
Write-Host '1/4 Cerrando Cutter...'
Get-Process Cutter -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300

# 2. Quitar accesos directos (arranque + carpeta privada)
Write-Host '2/4 Quitando accesos directos...'
$ShortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'Cutter.lnk'
if (Test-Path $ShortcutPath) { Remove-Item $ShortcutPath -Force }
$VaultLnkPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Cutter privado.lnk'
if (Test-Path $VaultLnkPath) { Remove-Item $VaultLnkPath -Force }

# 3. Restaurar Impr Pant -> Herramienta de recortes
Write-Host '3/4 Restaurando Impr Pant a Windows...'
New-ItemProperty -Path 'HKCU:\Control Panel\Keyboard' `
    -Name 'PrintScreenKeyForSnippingEnabled' -Value 1 -PropertyType DWord -Force | Out-Null

# 4. Borrar instalacion (NO toca tus capturas de Imagenes\Cutter)
Write-Host '4/4 Borrando archivos de la app...'
if (Test-Path $InstallDir) { Remove-Item $InstallDir -Recurse -Force }

Write-Host ''
Write-Host 'Listo. Cutter desinstalado y Windows restaurado.' -ForegroundColor Green
Write-Host 'Tus capturas en Imagenes\Cutter NO se han tocado.'
Write-Host 'Cierra sesion y vuelve a entrar para que Impr Pant abra el recortes de nuevo.'
