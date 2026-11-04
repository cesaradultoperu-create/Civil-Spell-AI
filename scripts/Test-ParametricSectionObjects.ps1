param([string]$DrawingPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
$plugin = Join-Path $repoRoot 'bin\x64\Debug\CivilSpellAI.dll'
if ([string]::IsNullOrWhiteSpace($DrawingPath)) {
    $DrawingPath = Join-Path $repoRoot 'Test\Drawing3D\Drawing3D-test.dwg'
}
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
if (-not (Test-Path -LiteralPath $console)) { throw 'Se requiere AutoCAD/Civil 3D 2024.' }
& dotnet build (Join-Path $repoRoot 'CivilSpellAI.csproj') --no-restore -c Debug -p:Platform=x64 -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion.' }

$runPath = Join-Path $repoRoot ('artifacts\validation\section-objects-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($runPath) | Out-Null
$drawing = Join-Path $runPath 'fixture.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$script = Join-Path $runPath 'selection.scr'
$scriptText = @'
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"__PLUGIN_PATH__"
AISPELLTESTSECTIONFILE
AISPELLTESTSECTIONOBJECTS
AISPELLTESTSECTION
_.QUIT
_Y
'@
[System.IO.File]::WriteAllText($script,
    $scriptText.Replace('__PLUGIN_PATH__', $plugin.Replace('\', '/')) + [Environment]::NewLine,
    [System.Text.Encoding]::ASCII)
$profile = Join-Path $runPath 'profile'
[System.IO.Directory]::CreateDirectory($profile) | Out-Null
$output = & $console /i $drawing /s $script /isolate ('SectionObjects-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
$code = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $runPath 'console.log'
[System.IO.File]::WriteAllText($logPath, $log)
foreach ($marker in @('PARAMETRIC_SECTION_OBJECT_SELECTION_OK', 'PARAMETRIC_SECTION_SMOKE_OK')) {
    if ($log -notmatch ('(?m)^\s*' + [regex]::Escape($marker) + '\s*$')) {
        throw "No se verifico $marker. Registro: $logPath"
    }
}
if ($code -ne 0) { throw "AutoCAD termino con codigo $code. Registro: $logPath" }
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $sourceHash) {
    throw 'El archivo fuente cambio durante la prueba.'
}
$recognized = [regex]::Matches($log, '(?m)^\s*SECCION_RECONOCIDA tipo=').Count
Write-Host "PASS: seleccion desde bloque, cota y hatch; creacion de secciones y muros."
Write-Host "Grupos reconocidos en el DWG original: $recognized. Registro: $logPath"
