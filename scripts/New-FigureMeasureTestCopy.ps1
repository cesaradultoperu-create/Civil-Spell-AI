param([string]$DrawingPath, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (-not $DrawingPath) { $DrawingPath = Join-Path $repo 'artifacts\validation\drawing3d-object-selection\1107.001.00.14-200-14-P-011-test.dwg' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo 'Test\Drawing3D\Pruebas-Medidas-20261002' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$drawing = Join-Path $OutputDirectory '1107-P-011-MEDIDAS.dwg'
if (Test-Path -LiteralPath $drawing) { throw 'La copia ya existe. Use otro directorio para no sobrescribir sus pruebas.' }
Copy-Item -LiteralPath $source -Destination $drawing
$plugin = Join-Path $repo 'artifacts\validation\figure-measures\candidate-debug\CivilSpellAI.dll'
$run = Join-Path $repo ('artifacts\validation\figure-measures\prepare-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$prepare = @'
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"__PLUGIN__"
(setq target (ssget "_C" '(23.4 -2.7) '(27.0 -1.0) '((410 . "Model"))))
(sssetfirst nil target)
AISPELLPREPARESELECTEDFIGURECOPY
(setq target (ssget "_C" '(23.4 -2.7) '(27.0 -1.0) '((410 . "Model"))))
(sssetfirst nil target)
AISPELLPREPAREMEASUREDCOPY
(command "_.ZOOM" "_Window" '(49 -0.8) '(60 4))
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
AISPELLVERIFYMEASUREDCOPY
_.QUIT
_Y
'@
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
foreach ($stage in @('prepare', 'verify')) {
    $script = Join-Path $run ($stage + '.scr')
    $contents = if ($stage -eq 'prepare') { $prepare } else { $verify }
    [IO.File]::WriteAllText($script, $contents.Replace('__PLUGIN__', $plugin.Replace('\', '/')) + [Environment]::NewLine, [Text.Encoding]::ASCII)
    $profile = Join-Path $run ($stage + '-profile')
    New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output = & $console /i $drawing /s $script /isolate ('FigureMeasureCopy-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code = $LASTEXITCODE
    $log = ($output -join [Environment]::NewLine) -replace "`0", ''
    $logPath = Join-Path $run ($stage + '.log')
    [IO.File]::WriteAllText($logPath, $log)
    $marker = if ($stage -eq 'prepare') { 'MEASURED_FIGURE_PREPARED' } else { 'FIGURE_MEASURES_SAVE_REOPEN_OK' }
    if ($code -ne 0 -or $log -notmatch $marker) { throw "Falló $stage. Registro: $logPath" }
}
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Cambió la fuente.' }
Write-Output "PASS: copia con dos detalles editables, hatch y medidas verificados tras guardar y reabrir. $drawing"
