param(
    [Parameter(Mandatory = $true)][string]$DrawingPath,
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
function Get-SharedFileHash([string]$Path) {
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '') }
    finally { $algorithm.Dispose(); $stream.Dispose() }
}
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot ('Test\Drawing3D\Pruebas-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '\1107-P-011-PRUEBAS.dwg')
}
$destination = [System.IO.Path]::GetFullPath($OutputPath)
if (-not $destination.StartsWith($repoRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'La copia de prueba debe guardarse dentro del repositorio.'
}
if (Test-Path -LiteralPath $destination) { throw "La copia ya existe: $destination" }
$sourceHash = Get-SharedFileHash $source
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
$plugin = Join-Path $repoRoot 'bin\x64\Debug\CivilSpellAI.dll'
if (-not (Test-Path -LiteralPath $console)) { throw 'Se requiere AutoCAD/Civil 3D 2024.' }
& dotnet build (Join-Path $repoRoot 'CivilSpellAI.csproj') --no-restore -c Debug -p:Platform=x64 -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion.' }
$folder = Split-Path -Parent $destination
[System.IO.Directory]::CreateDirectory($folder) | Out-Null
$inputStream = [System.IO.File]::Open($source, [System.IO.FileMode]::Open,
    [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $outputStream = [System.IO.File]::Open($destination, [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
} finally { $inputStream.Dispose() }
if ((Get-SharedFileHash $destination) -ne $sourceHash) { throw 'La copia no coincide con la fuente.' }
(Get-Item -LiteralPath $destination).IsReadOnly = $false
$runPath = Join-Path $repoRoot ('artifacts\validation\section-copy-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($runPath) | Out-Null
$script = Join-Path $runPath 'prepare.scr'
$scriptText = @'
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"__PLUGIN_PATH__"
AISPELLTESTSECTIONOBJECTS
AISPELLPREPARESECTIONCOPY
AISPELLVALIDATEPARAMETRICBLOCK
(setvar "TILEMODE" 1)
(setq testObjects (ssget "_X" '((8 . "CIVILSPELLAI_PRUEBAS") (410 . "Model"))))
(if testObjects (progn (command "_.ZOOM" "_Object" testObjects "") (command "_.ZOOM" "0.85x")))
(setvar "SECURELOAD" 1)
_.QSAVE
_.QUIT
'@
[System.IO.File]::WriteAllText($script,
    $scriptText.Replace('__PLUGIN_PATH__', $plugin.Replace('\', '/')) + [Environment]::NewLine,
    [System.Text.Encoding]::ASCII)
$profile = Join-Path $runPath 'profile'
[System.IO.Directory]::CreateDirectory($profile) | Out-Null
$output = & $console /i $destination /s $script /isolate ('SectionCopy-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
$code = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $runPath 'console.log'
[System.IO.File]::WriteAllText($logPath, $log)
if ($code -ne 0 -or $log -notmatch '(?m)^\s*PARAMETRIC_SECTION_OBJECT_SELECTION_OK\s*$' -or
    $log -notmatch '(?m)^\s*PARAMETRIC_TEST_COPY_PREPARED count=[1-9][0-9]*\s*$' -or
    $log -match '(?m)^\s*PARAMETRIC_BLOCK_INVALID\b') {
    throw "Fallo la preparacion de la copia. Registro: $logPath"
}
if ((Get-SharedFileHash $source) -ne $sourceHash) {
    throw 'El archivo fuente cambio durante la prueba.'
}
Write-Host "COPIA_PREPARADA: $destination"
Write-Host "ORIGINAL_SIN_CAMBIOS_SHA256: $sourceHash"
Write-Host "REGISTRO: $logPath"
