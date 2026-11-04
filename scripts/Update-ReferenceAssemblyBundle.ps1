param([string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
try {
    if (Get-Process acad -ErrorAction SilentlyContinue) {
        throw 'Guarde sus dibujos y cierre Civil 3D/AutoCAD antes de actualizar.'
    }
    if (-not $PackageDirectory) { $PackageDirectory = Join-Path $PSScriptRoot 'CivilSpellAI-1.3.7.0' }
    $package = (Resolve-Path -LiteralPath $PackageDirectory).Path
    $bundle = Join-Path $package 'CivilSpellAI.bundle'
    $expected = @{
        '2024' = '57C196F43914C6C44E7B52A98FB6D2F4CC36FB27E54377AFFA28B4C7DE051E6D'
        '2025' = 'BDB338987C25E9EF96BF2E5096F83275211BC5081113A609EF97B763297D13CB'
        '2026' = 'FD17F321D10E5CE2500DAF56DC874D48F3476FC9C3DE157826D5DB1CC81D0E74'
    }
    foreach ($year in $expected.Keys) {
        $dll = Join-Path $bundle "Contents\Windows\$year\CivilSpellAI.dll"
        if ((Get-FileHash -LiteralPath $dll).Hash -ne $expected[$year]) {
            throw "La DLL $year no coincide con el Release validado del 12/10/2026."
        }
    }
    [xml]$manifest = Get-Content -Raw -LiteralPath (Join-Path $bundle 'PackageContents.xml')
    if ($manifest.ApplicationPackage.AppVersion -ne '1.3.7.0') { throw 'Version del paquete incorrecta.' }
    foreach ($component in $manifest.ApplicationPackage.Components) {
        foreach ($command in @('CREARFIGURA3D','CREARCOMPONENTE3D','EDITARFIGURA3D')) {
            if ($command -notin @($component.ComponentEntry.Commands.Command.Global)) {
                throw "Falta el comando $command en el manifiesto."
            }
        }
    }
    $target = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins\CivilSpellAI.bundle'
    if (-not (Test-Path -LiteralPath $target -PathType Container)) { throw 'No se encontro la instalacion administrada existente.' }
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        $arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + $PSCommandPath + '" -PackageDirectory "' + $package + '"'
        $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments -Verb RunAs -WindowStyle Hidden -Wait -PassThru
        exit $process.ExitCode
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'Manage-CivilSpellAI.ps1') -Action Update -Scope AllUsers
    if ($LASTEXITCODE -ne 0) { throw 'No se completo la actualizacion del bundle. Revise el mensaje anterior.' }
    Write-Host 'Actualizacion completada. Abra Referencia-TOP-FRONT-3D.dwg y pruebe CREARFIGURA3D / EDITARFIGURA3D.'
} catch {
    Write-Error $_.Exception.Message -ErrorAction Continue
    exit 1
}
