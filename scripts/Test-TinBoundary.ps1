param([switch]$TryCivilSurfaces, [string]$PreviousPlugin)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$run = Join-Path $repo ('artifacts\validation\tin-boundary\run-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$testOutput = Join-Path $run 'plugin'
dotnet build (Join-Path $repo 'CivilSpellAI.BordeTin3D.csproj') --configuration Release --no-restore /p:IncludeNativeTests=true /p:AssemblyName=CivilSpellAI.BordeTin3D.Tests "/p:OutputPath=$testOutput"
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion de la prueba nativa.' }
$plugin = (Join-Path $testOutput 'CivilSpellAI.BordeTin3D.Tests.dll').Replace('\','/')
$template = Join-Path $env:LOCALAPPDATA 'Autodesk\C3D 2024\enu\Template\AutoCAD Template\acad.dwt'
if (-not (Test-Path -LiteralPath $template)) { throw "No se encuentra la plantilla: $template" }
$drawing = Join-Path $run 'BordeTin3D-Prueba.dwg'
Copy-Item -LiteralPath $template -Destination $drawing
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
$previousLoad = ''
if ($PreviousPlugin) {
    $previousPath = (Resolve-Path -LiteralPath $PreviousPlugin).Path.Replace('\','/')
    $previousLoad = "_.NETLOAD`n`"$previousPath`"`n"
}
foreach ($stage in @('prepare','verify')) {
    $commands = if ($stage -eq 'prepare') {
@'
AISPELLTESTTINNATIVE
_.U
AISPELLVERIFYTINUNDONE
AISPELLTESTTINNATIVE
_.U
_.REDO
AISPELLVERIFYTINNATIVE
_.QSAVE
'@
    } else { 'AISPELLVERIFYTINNATIVE' }
    if ($TryCivilSurfaces -and $stage -eq 'verify') { $commands += @'

AISPELLTESTTINSURFACES
(command "_.UCS" "_Origin" '(3 4 0))
BORDETIN3D
PickPoints
Visibles
0.001
-2,-3
2,1
2,1

_.U
_.REDO
(command "_.UCS" "_World")
BORDETIN3D
Seleccion
_ALL

0.001
_.U
AISPELLVERIFYTINCOMMANDUNDO
BORDETIN3D
Seleccion
_ALL

0.001
AISPELLVERIFYTINCOMMANDS
'@ }
    $script = Join-Path $run ($stage + '.scr')
    $contents = @"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
${previousLoad}_.NETLOAD
"$plugin"
$commands
_.QUIT
_Y

"@
    [IO.File]::WriteAllText($script, $contents, [Text.Encoding]::ASCII)
    $profile = Join-Path $run ($stage + '-profile')
    New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output = & $console /i $drawing /s $script /isolate ('TinBoundary-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code = $LASTEXITCODE
    $log = ($output -join [Environment]::NewLine) -replace "`0", ''
    $logPath = Join-Path $run ($stage + '.log')
    [IO.File]::WriteAllText($logPath, $log)
    $markers = if ($stage -eq 'prepare') { @('TINBOUNDARY_NATIVE_WRITE_OK','TINBOUNDARY_UNDO_OK','TINBOUNDARY_REOPEN_OK') } else { @('TINBOUNDARY_REOPEN_OK') }
    foreach ($marker in $markers) { if ($log -notmatch ('(?m)^\s*' + $marker + '\s*$')) { throw "Falta $marker. Registro: $logPath" } }
    if ($code -ne 0 -or $log -match 'TINBOUNDARY_FAILED') { throw "Fallo $stage. Registro: $logPath" }
    if ($TryCivilSurfaces -and $stage -eq 'verify') {
        if ($log -match '(?m)^\s*TINBOUNDARY_REAL_SURFACES_OK\s*$') { Write-Output 'PASS: Extraccion de cuatro superficies TIN reales, recinto vacio y limpieza de objetos temporales.' }
        else { throw "Fallo la extraccion real de TIN. Registro: $logPath" }
        if ($log -notmatch '(?m)^\s*TINBOUNDARY_INTERACTIVE_COMMANDS_OK\s*$') { throw "Fallaron los flujos interactivos. Registro: $logPath" }
        if ($log -notmatch '(?m)^\s*TINBOUNDARY_COMMAND_UNDO_OK\s*$') { throw "Fallo Deshacer del comando real. Registro: $logPath" }
        Write-Output 'PASS: BORDETIN3D PickPoints (UCS desplazado, punto invalido y repetido) y Seleccion con TIN reales.'
    }
}
Write-Output "PASS: Polilinea 3D nativa, cotas, capa, cierre, Undo/Redo y reapertura. $drawing"
