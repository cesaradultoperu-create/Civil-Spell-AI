param([string]$SourceDrawing)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SourceDrawing) {
    $SourceDrawing = Join-Path $workspace 'artifacts\validation\overlap-corte-20261006\source.dwg'
}
if (-not (Test-Path -LiteralPath $SourceDrawing -PathType Leaf)) { throw 'Falta la copia local de !CORTE.dwg.' }
$sourceHash = (Get-FileHash -LiteralPath $SourceDrawing).Hash
& dotnet build (Join-Path $workspace 'CivilSpellAI.Overlap2023.csproj') --no-restore -c Debug -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion.' }
$run = Join-Path $workspace ('artifacts\validation\overlap-corte-tiny-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
$plugin = Join-Path $workspace 'bin\Overlap2023\Debug\CivilSpellAI.TramosDWG.dll'
$script = [IO.File]::ReadAllText((Join-Path $workspace 'Test\test-corte-tiny-cleanup.scr'))
$script = $script.Replace('__PLUGIN_PATH__', $plugin.Replace('\', '/')).Replace('__WORK_DIR__', $run.Replace('\', '/'))
$script = $script.Replace('REVISARPOLILINEAS', 'REVISARTRAMOSDWG').Replace('RESTAURARPOLILINEAS', 'RESTAURARTRAMOSDWG')
$scriptPath = Join-Path $run 'cleanup.scr'
[IO.File]::WriteAllText($scriptPath, $script, [Text.Encoding]::ASCII)
$output = & (Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe') /i $SourceDrawing /s $scriptPath /isolate ('CorteTiny-' + [Guid]::NewGuid().ToString('N')) (Join-Path $run 'profile') /l en-US 2>&1
$code = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $run 'console.log'
[IO.File]::WriteAllText($logPath, $log)
$checks = @('CORTE_MARKED_TINY_EDGE_REMOVED_OK', 'CORTE_EDGE_0013_REMOVED_OK',
    'CORTE_CLOSED_ELEVATION_NORMAL_OK', 'CORTE_MARKED_BOX_UNCHANGED_OK',
    'CORTE_CIVIL_LINE_3D_UNCHANGED_OK', 'CORTE_TARGET_IDEMPOTENT_OK',
    'SAFE_RESTORE_EXACT_OK', 'CORTE_TARGET_RESTORED_EXACT_OK',
    'CORTE_CIVIL_LINE_3D_STILL_UNCHANGED_OK', 'CORTE_TINY_CLEANUP_DONE')
foreach ($check in $checks) {
    if ($log -notmatch ('(?m)^\s*' + [regex]::Escape($check) + '\s*$')) { throw "Falta $check. Registro: $logPath" }
}
if ($code -ne 0 -or $log -match '(?m)^\s*CORTE_\w+_FAIL\s*$' -or
    $log -notmatch 'CIVILSPELLAI TRAMOS DWG 2023/2024 - revision 1\.3\.3\.2') { throw "Fallo la prueba. Registro: $logPath" }
if ((Get-FileHash -LiteralPath $SourceDrawing).Hash -ne $sourceHash) { throw 'Se modifico la copia de origen.' }
$corrected = Join-Path $run 'CORTE-tramos-corregidos-v1_3_3_2.dwg'
if (-not (Test-Path -LiteralPath $corrected)) { throw 'No se genero la copia corregida.' }
Write-Host "PASS: $($checks.Count) controles del dibujo real. Registro: $logPath"
Write-Host "Copia corregida: $corrected"
Write-Output $run
