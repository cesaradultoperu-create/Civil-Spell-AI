param([switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
try {
    $source = Join-Path $PSScriptRoot 'CivilSpellAI.dll'
    $manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Entrega.json') -Raw | ConvertFrom-Json
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $manifest.SHA256) {
        throw 'La DLL no coincide con la entrega verificada.'
    }
    $identity = [Reflection.AssemblyName]::GetAssemblyName($source)
    if ($identity.Name -ne 'CivilSpellAI') { throw 'El ensamblado no es CivilSpellAI.' }
    $target = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins\CivilSpellAI.bundle\Contents\Windows\2024\CivilSpellAI.dll'
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw 'No se encontro la instalacion existente para Civil 3D 2024.' }
    if ($VerifyOnly) {
        Write-Output "Entrega verificada: $source"
        Write-Output "Destino: $target"
        exit 0
    }
    if (Get-Process acad -ErrorAction SilentlyContinue) {
        throw 'Guarde todos sus dibujos y cierre Civil 3D antes de actualizar. Luego ejecute ACTUALIZAR.cmd.'
    }
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '"'
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
        exit $process.ExitCode
    }
    $backupDirectory = Join-Path $PSScriptRoot ('Respaldo-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $backupDirectory | Out-Null
    $backup = Join-Path $backupDirectory 'CivilSpellAI.dll'
    $beforeHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $target -Destination $backup
    if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $beforeHash) { throw 'No se pudo verificar el respaldo.' }
    try {
        Copy-Item -LiteralPath $source -Destination $target -Force
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $manifest.SHA256) { throw 'La copia instalada no coincide con la entrega.' }
    } catch {
        Copy-Item -LiteralPath $backup -Destination $target -Force
        throw
    }
    [pscustomobject]@{ Installed = $target; SHA256 = $manifest.SHA256; Backup = $backup; PreviousSHA256 = $beforeHash } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backupDirectory 'Instalacion-verificada.json') -Encoding UTF8
    Write-Output 'Actualizacion completada. Abra Civil 3D y use CREARFIGURA3D o EDITARFIGURA3D > Tuberia > Inclinacion.'
    Write-Output "Respaldo: $backup"
} catch {
    Write-Error $_.Exception.Message -ErrorAction Continue
    exit 1
}
