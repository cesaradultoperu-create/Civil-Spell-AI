$ErrorActionPreference = 'Stop'
if (Get-Process acad -ErrorAction SilentlyContinue) {
    throw 'Cierre AutoCAD/Civil 3D antes de actualizar.'
}
$workspace = Split-Path -Parent $PSScriptRoot
$source = Join-Path $workspace 'bin\x64\Release\CivilSpellAI.dll'
$target = 'C:\Program Files\Autodesk\ApplicationPlugins\CivilSpellAI.bundle\Contents\Windows\2024\CivilSpellAI.dll'
$backup = Join-Path $workspace ('artifacts\CivilSpellAI-2024-respaldo-admin-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.dll')
Copy-Item -LiteralPath $target -Destination $backup
Copy-Item -LiteralPath $source -Destination $target -Force
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
    throw 'La DLL instalada no coincide con la compilada.'
}
