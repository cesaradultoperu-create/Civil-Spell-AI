param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$Target2023,
    [ValidateSet('2023', '2024')][string]$HostYear = '2024'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$project = if ($Target2023) { 'CivilSpellAI.Overlap2023.csproj' } else { 'CivilSpellAI.csproj' }
$plugin = if ($Target2023) { Join-Path $workspace "bin\Overlap2023\$Configuration\CivilSpellAI.TramosDWG.dll" }
    else { Join-Path $workspace "bin\x64\$Configuration\CivilSpellAI.dll" }
$console = Join-Path $env:ProgramFiles "Autodesk\AutoCAD $HostYear\accoreconsole.exe"
$template = @('enu', 'esp', 'esn') | ForEach-Object {
    Join-Path $env:LOCALAPPDATA "Autodesk\C3D $HostYear\$_\Template\AutoCAD Template\acad.dwt"
} | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $template -or -not (Test-Path -LiteralPath $console)) {
    throw "Se requiere AutoCAD/Civil 3D $HostYear con acad.dwt para esta prueba."
}
& dotnet build (Join-Path $workspace $project) --no-restore -c $Configuration -p:Platform=x64 -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion.' }
$run = Join-Path $workspace ('artifacts\validation\overlap-correction-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($run) | Out-Null
$drawing = Join-Path $run 'fixture.dwg'
Copy-Item -LiteralPath $template -Destination $drawing
$script = Join-Path $run 'lifecycle.scr'
$content = [System.IO.File]::ReadAllText((Join-Path $workspace 'Test\test-overlap-correction-lifecycle.scr'))
if ($Target2023) {
    $content = $content.Replace('REVISARPOLILINEAS', 'REVISARTRAMOSDWG').Replace('RESTAURARSOLAPES', 'RESTAURARSOLAPESTRAMOSDWG')
}
if ($Configuration -eq 'Release') {
    $content = $content -replace '(?m)^AISPELLTEST(OVERLAP|SAFECLEANUP|CORRECTION)\r?\n', ''
}
[System.IO.File]::WriteAllText($script, $content.Replace('__PLUGIN_PATH__', $plugin.Replace('\', '/')), [System.Text.Encoding]::ASCII)
$profile = Join-Path $run 'profile'
[System.IO.Directory]::CreateDirectory($profile) | Out-Null
$output = & $console /i $drawing /s $script /isolate ('Overlap-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
$code = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $run 'console.log'
[System.IO.File]::WriteAllText($logPath, $log)
$required = @('CORRECTION_DECLINED_OK', 'CORRECTION_TAILS_OK', 'CORRECTION_SELECTION_BOUNDARY_OK',
    'CORRECTION_EDITED_RESTORE_BLOCKED_OK', 'CORRECTION_LOCKED_RESTORE_BLOCKED_OK',
    'CORRECTION_RESTORE_EXACT_OK', 'CORRECTION_UNDO_EXACT_OK',
    'CORRECTION_REAPPLY_AFTER_UNDO_OK', 'CORRECTION_FINAL_RESTORE_OK',
    'CORRECTION_FULL_DUPLICATE_OK', 'CORRECTION_FULL_DUPLICATE_RESTORE_OK', 'CORRECTION_LIFECYCLE_DONE')
if ($Configuration -eq 'Debug') {
    $required += @('OVERLAP_TEST_OK', 'SAFE_CLEANUP_TEST_OK', 'CORRECTION_GEOMETRY_TEST_OK')
}
foreach ($marker in $required) {
    # A marker printed by princ occupies its own line; an echoed Lisp expression is not evidence.
    if ($log -notmatch ('(?m)^\s*' + [regex]::Escape($marker) + '\s*$')) {
        throw "No se verifico $marker. Registro: $logPath"
    }
}
if ($code -ne 0 -or $log -match '(?m)^\s*CORRECTION_\w+_FAIL\s*$') {
    throw "Fallo la regresion. Registro: $logPath"
}
if ($Target2023 -and $log -notmatch '(?m)^CIVILSPELLAI TRAMOS DWG 2023/2024 - revision 1\.3\.3\.2') {
    throw "No se verifico que los comandos ejecutados pertenecen a la DLL 2023. Registro: $logPath"
}
Write-Host "PASS: $($required.Count) verificaciones. Registro: $logPath"
