param([string]$DrawingPath, [string]$RunDirectory, [string]$PluginPath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $DrawingPath) { $DrawingPath = Join-Path $repo 'artifacts\validation\drawing3d-object-selection\1107.001.00.14-200-14-P-011-test.dwg' }
if (-not $RunDirectory) { $RunDirectory = Join-Path $repo ('artifacts\validation\figure3d\run-' + [Guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$RunDirectory = (Resolve-Path -LiteralPath $RunDirectory).Path
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
$hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$drawing = Join-Path $RunDirectory 'fixture.dwg'
if (Test-Path -LiteralPath $drawing) { throw 'El fixture ya existe. Use un directorio nuevo.' }
Copy-Item -LiteralPath $source -Destination $drawing
$plugin = if ($PluginPath) { (Resolve-Path -LiteralPath $PluginPath).Path } else { Join-Path $repo 'bin\x64\Debug\CivilSpellAI.dll' }
$prepare = @'
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"__PLUGIN__"
AISPELLTESTFIGURE3DLEFT
(setq target (ssget "_C" '(23.4 -2.7) '(27.0 -1.0) '((410 . "Model"))))
(sssetfirst nil target)
AISPELLTESTFIGURE3D
(sssetfirst nil target)
AISPELLPREPAREFIGURE3D
AISPELLFIGURE3DUNDOSETUP
AISPELLFIGURE3DUNDOCHANGE
_.U
AISPELLFIGURE3DUNDOCHECK
(command "_.VPOINT" '(1 -1 1))
(command "_.ZOOM" "_Object" (ssget "_X" '((2 . "CIVILSPELLAI_3D_*"))) "")
(command "_.ZOOM" "0.8x")
(setvar "SECURELOAD" 1)
_.QSAVE
_.QUIT
'@
$verify = @'
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"__PLUGIN__"
AISPELLVERIFYFIGURE3D
_.QUIT
_Y
'@
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
foreach ($stage in @('prepare', 'verify')) {
    $script = Join-Path $RunDirectory ($stage + '.scr')
    $contents = if ($stage -eq 'prepare') { $prepare } else { $verify }
    [IO.File]::WriteAllText($script, $contents.Replace('__PLUGIN__', $plugin.Replace('\', '/')) + [Environment]::NewLine, [Text.Encoding]::ASCII)
    $profile = Join-Path $RunDirectory ($stage + '-profile')
    New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output = & $console /i $drawing /s $script /isolate ('Figure3D-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code = $LASTEXITCODE
    $log = ($output -join [Environment]::NewLine) -replace "`0", ''
    $logPath = Join-Path $RunDirectory ($stage + '.log')
    [IO.File]::WriteAllText($logPath, $log)
    $markers = if ($stage -eq 'prepare') { @('FIGURE3D_LEFT_REFERENCE_PREVIEW_OK','FIGURE3D_PREVIEW_SOLID_MATCH_OK','FIGURE3D_TOP_FRONT_REFERENCE_OK','FIGURE3D_SOLID_DIMENSIONS_COPY_OK','FIGURE3D_ROLLBACK_OK','FIGURE3D_PREPARED','FIGURE3D_SINGLE_UNDO_OK') } else { @('FIGURE3D_SAVE_REOPEN_OK','FIGURE3D_PREVIEW_SOLID_MATCH_OK') }
    foreach ($marker in $markers) {
        if ($log -notmatch ('(?m)^\s*' + $marker + '\s*$')) { throw "Falta $marker. Registro: $logPath" }
    }
    if ($code -ne 0 -or $log -match 'FIGURE3D_FAILED') { throw "Falló $stage. Registro: $logPath" }
}
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $hash) { throw 'Cambió el dibujo fuente.' }
Write-Output "PASS: TOP/FRONT, sólido nativo, volumen, cotas XY/XZ, copias independientes, giro, rollback, una U y persistencia. $drawing"
