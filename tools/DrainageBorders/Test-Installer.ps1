param([string]$PackageDirectory, [string]$TestDirectory)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
if (-not $PackageDirectory) { $PackageDirectory = Join-Path $repo 'artifacts\distribution\DrenBordes-0.1.1-Civil3D2024' }
if (-not $TestDirectory) { $TestDirectory = Join-Path $repo ('artifacts\validation\dren-installer\' + [Guid]::NewGuid().ToString('N')) }
[void][IO.Directory]::CreateDirectory($TestDirectory)
$logs = New-Object Collections.Generic.List[string]
function Invoke-Installer([string]$Package, [string]$Root, [bool]$ExpectSuccess = $true) {
    $previousErrorAction = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Package 'Instalar-DrenBordes.ps1') -ApplicationPluginsRoot $Root 2>&1
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previousErrorAction }
    $script:logs.Add(($output -join [Environment]::NewLine))
    if (($code -eq 0) -ne $ExpectSuccess) { throw "Resultado inesperado del instalador: $code. $output" }
}
$root = Join-Path $TestDirectory 'ApplicationPlugins'
Invoke-Installer $PackageDirectory $root
$installed = Join-Path $root 'DrenBordes.bundle'
$manifest = Get-Content -LiteralPath (Join-Path $PackageDirectory 'BUNDLE-SHA256.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $manifest) {
    if ((Get-FileHash -LiteralPath (Join-Path $installed $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) { throw 'Instalacion diferente del paquete validado.' }
}
Invoke-Installer $PackageDirectory $root
if (@(Get-ChildItem -LiteralPath $root -Filter 'DrenBordes.backup-*').Count -ne 0) { throw 'Reinstalar la misma version produjo un respaldo innecesario.' }
$help = Join-Path $installed 'Contents\Resources\AYUDA.html'
Add-Content -LiteralPath $help -Value '<!-- prueba de actualizacion -->' -Encoding UTF8
Invoke-Installer $PackageDirectory $root
$backups = @(Get-ChildItem -LiteralPath $root -Filter 'DrenBordes.backup-*')
if ($backups.Count -ne 1) { throw 'La actualizacion no preservo exactamente un respaldo.' }
$backupHelp = Join-Path $backups[0].FullName 'Contents\Resources\AYUDA.html'
if ([IO.File]::ReadAllText($backupHelp) -notmatch 'prueba de actualizacion') { throw 'El respaldo no contiene la version anterior.' }
$tampered = Join-Path $TestDirectory 'PaqueteAlterado'
[void][IO.Directory]::CreateDirectory($tampered)
foreach ($name in @('DrenBordes.bundle','BUNDLE-SHA256.json','Instalar-DrenBordes.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PackageDirectory $name) -Destination $tampered -Recurse
}
$tamperDll = Join-Path $tampered 'DrenBordes.bundle\Contents\Windows\2024\DrenBordes.2024.dll'
$bytes = [IO.File]::ReadAllBytes($tamperDll)
$bytes[0] = $bytes[0] -bxor 1
[IO.File]::WriteAllBytes($tamperDll, $bytes)
$badRoot = Join-Path $TestDirectory 'NoDebeInstalarse'
Invoke-Installer $tampered $badRoot $false
if (Test-Path -LiteralPath $badRoot) { throw 'Un paquete alterado llego a crear el destino de instalacion.' }
[xml]$xml = Get-Content -LiteralPath (Join-Path $PackageDirectory 'DrenBordes.bundle\PackageContents.xml') -Raw -Encoding UTF8
$runtime = $xml.ApplicationPackage.Components.RuntimeRequirements
$component = $xml.ApplicationPackage.Components.ComponentEntry
if ($runtime.Platform -ne 'Civil3D' -or $runtime.SeriesMin -ne 'R24.3' -or $runtime.SeriesMax -ne 'R24.3') { throw 'Plataforma/version incorrectas.' }
if ($component.LoadOnAutoCADStartup -ne 'True' -or $component.Commands.Command.Global -ne 'DRENBORDES') { throw 'Carga automatica o registro de comando incorrectos.' }
$module = Join-Path (Join-Path $PackageDirectory 'DrenBordes.bundle') $component.ModuleName
if (-not (Test-Path -LiteralPath $module)) { throw 'El manifiesto referencia un DLL inexistente.' }
$codeCopy = Join-Path $TestDirectory 'CodigoFuente'
Copy-Item -LiteralPath (Join-Path $PackageDirectory 'CodigoFuente') -Destination $codeCopy -Recurse
$buildOutput = & dotnet build (Join-Path $codeCopy 'DrenBordes.csproj') -c Release --ignore-failed-sources -v:minimal 2>&1
$logs.Add(($buildOutput -join [Environment]::NewLine))
if ($LASTEXITCODE -ne 0) { throw "El proyecto independiente no compila. $buildOutput" }
$summary = 'PASS: instalacion temporal, hashes de archivos, reinstalacion sin duplicados, actualizacion con respaldo, rechazo de DLL alterado, manifiesto Civil 3D 2024 y compilacion independiente con paquetes locales.'
$logs.Add($summary)
[IO.File]::WriteAllText((Join-Path $TestDirectory 'installer-validation.log'), ($logs -join "`r`n`r`n"), [Text.Encoding]::UTF8)
Copy-Item -LiteralPath (Join-Path $TestDirectory 'installer-validation.log') -Destination (Join-Path $PackageDirectory 'Validacion\installer-validation.log')
Write-Output $summary
Write-Output $TestDirectory
