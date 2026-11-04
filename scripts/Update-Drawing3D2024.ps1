param([Parameter(Mandatory = $true)][string]$SourceDll)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
try {
    if (Get-Process acad -ErrorAction SilentlyContinue) {
        throw 'Guarde sus dibujos y cierre Civil 3D antes de actualizar. La DLL cargada no se reemplaza dentro de la misma sesion.'
    }
    $source = (Resolve-Path -LiteralPath $SourceDll).Path
    if ([System.IO.Path]::GetFileName($source) -ne 'CivilSpellAI.dll') { throw 'La DLL fuente no es CivilSpellAI.dll.' }
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -SourceDll "' + $source + '"'
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
        exit $process.ExitCode
    }
    $target = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins\CivilSpellAI.bundle\Contents\Windows\2024\CivilSpellAI.dll'
    if (-not (Test-Path -LiteralPath $target)) { throw "No se encontro la instalacion existente: $target" }
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $backupFolder = Join-Path $repoRoot 'artifacts\drawing3d-backups'
    [System.IO.Directory]::CreateDirectory($backupFolder) | Out-Null
    $backup = Join-Path $backupFolder ('CivilSpellAI-2024-' + [Guid]::NewGuid().ToString('N') + '.dll')
    Copy-Item -LiteralPath $target -Destination $backup
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) {
        Copy-Item -LiteralPath $backup -Destination $target -Force
        throw 'No se verifico la copia. Se restauro la DLL anterior.'
    }
    [System.Windows.Forms.MessageBox]::Show(
        "Drawing3D actualizado para Civil 3D 2024. Abra Civil 3D y pruebe EDITARSECCION seleccionando una figura completa y pulsando Enter.`n`nRespaldo: $backup",
        'CivilSpellAI', [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information) | Out-Null
} catch {
    [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Actualizacion Drawing3D',
        [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}
