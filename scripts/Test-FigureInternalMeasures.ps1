param([string]$DrawingPath, [string]$RunDirectory, [string]$PluginPath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $DrawingPath) { $DrawingPath = Join-Path $repo 'artifacts\validation\drawing3d-object-selection\1107.001.00.14-200-14-P-011-test.dwg' }
if (-not $RunDirectory) { $RunDirectory = Join-Path $repo ('artifacts\validation\figure-measures\run-' + [Guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$RunDirectory = (Resolve-Path -LiteralPath $RunDirectory).Path
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
$hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$drawing = Join-Path $RunDirectory 'fixture.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$plugin = if ($PluginPath) { (Resolve-Path -LiteralPath $PluginPath).Path } else { Join-Path $repo 'artifacts\validation\figure-measures\debug\CivilSpellAI.dll' }
$script = Join-Path $RunDirectory 'test.scr'
$contents = @'
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"__PLUGIN__"
AISPELLTESTFIGUREMEASURES
AISPELLTESTSELECTEDFIGURE
AISPELLTESTSECTIONOBJECTS
AISPELLFIGUREUNDOSETUP
AISPELLFIGUREUNDOCHANGE
_.U
AISPELLFIGUREUNDOCHECK
(setq target (ssget "_C" '(23.4 -2.7) '(27.0 -1.0) '((410 . "Model"))))
(sssetfirst nil target)
AISPELLTESTFIGUREMEASURESFILE
_.QUIT
_Y
'@
[IO.File]::WriteAllText($script, $contents.Replace('__PLUGIN__', $plugin.Replace('\', '/')) + [Environment]::NewLine, [Text.Encoding]::ASCII)
$profile = Join-Path $RunDirectory 'profile'
New-Item -ItemType Directory -Path $profile -Force | Out-Null
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
$output = & $console /i $drawing /s $script /isolate ('FigureMeasures-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
$exitCode = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $RunDirectory 'console.log'
[IO.File]::WriteAllText($logPath, $log)
foreach ($marker in @('FIGURE_PREVIEW_NAVIGATION_OK', 'FIGURE_INTERNAL_MEASURES_OK', 'SELECTED_FIGURE_GEOMETRY_DIMENSIONS_OK', 'PARAMETRIC_SECTION_OBJECT_SELECTION_OK', 'SELECTED_FIGURE_SINGLE_UNDO_OK', 'FIGURE_REAL_INTERNAL_MEASURES_OK')) {
    if ($log -notmatch ('(?m)^\s*' + $marker + '\s*$')) { throw "Falta $marker. Registro: $logPath" }
}
if ($exitCode -ne 0) { throw "Core Console terminó con código $exitCode. Registro: $logPath" }
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $hash) { throw 'Cambió el dibujo fuente.' }
Write-Output "PASS: medidas interiores, hatch, cotas, copias, rotación, rollback y detalle real. Registro: $logPath"
