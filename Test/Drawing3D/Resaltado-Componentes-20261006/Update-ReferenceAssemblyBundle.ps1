param([string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
try {
    if (Get-Process acad -ErrorAction SilentlyContinue) {
        throw 'Guarde sus dibujos y cierre Civil 3D/AutoCAD antes de actualizar.'
    }
    if (-not $PackageDirectory) { $PackageDirectory = Join-Path $PSScriptRoot 'CivilSpellAI-1.3.6.0' }
    $package = (Resolve-Path -LiteralPath $PackageDirectory).Path
    $bundle = Join-Path $package 'CivilSpellAI.bundle'
    $expected = @{
        '2024' = '755BD17D34417D2C1601DB8C132837ACA324D8DAB798C1830C2DFEB6EF9FDE26'
        '2025' = '998101371676330A56CE4E2A0D8DAB395A9FEDD1E1CCAC74F3952891EC59ECB3'
        '2026' = '0D0BB73D09C1720ABF3FD43FAEDFE20E59C570F065F15D9F47F55E06A25C3B82'
    }
    foreach ($year in $expected.Keys) {
        $dll = Join-Path $bundle "Contents\Windows\$year\CivilSpellAI.dll"
        if ((Get-FileHash -LiteralPath $dll).Hash -ne $expected[$year]) {
            throw "La DLL $year no coincide con el Release validado del 06/10/2026."
        }
    }
    [xml]$manifest = Get-Content -Raw -LiteralPath (Join-Path $bundle 'PackageContents.xml')
    if ($manifest.ApplicationPackage.AppVersion -ne '1.3.6.0') { throw 'Version del paquete incorrecta.' }
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
