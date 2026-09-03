param(
    [string]$OutputPath,

    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = Join-Path $repoRoot "artifacts\testing"

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $artifactsRoot "CivilSpellAI-Pilot-Fixture-1.1.5.dwg"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath))
{
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$artifactsFull = [System.IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'

if (-not $OutputPath.StartsWith(
    $artifactsFull,
    [System.StringComparison]::OrdinalIgnoreCase))
{
    throw "El fixture debe generarse dentro de: $artifactsRoot"
}

if ([System.IO.Path]::GetExtension($OutputPath) -ne ".dwg")
{
    throw "El fixture debe usar la extensión .dwg."
}

if ((Test-Path -LiteralPath $OutputPath) -and -not $Force)
{
    throw "El fixture ya existe. Use -Force para reemplazar únicamente este archivo: $OutputPath"
}

$consolePath = Join-Path $env:ProgramFiles "Autodesk\AutoCAD 2024\accoreconsole.exe"
$templatePath = Join-Path $env:LOCALAPPDATA "Autodesk\C3D 2024\enu\Template\AutoCAD Template\acad.dwt"

if (-not (Test-Path -LiteralPath $consolePath -PathType Leaf))
{
    throw "No se encontró accoreconsole.exe de AutoCAD 2024."
}

if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf))
{
    throw "No se encontró la plantilla acad.dwt de Civil 3D 2024."
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

if (Test-Path -LiteralPath $OutputPath)
{
    Remove-Item -LiteralPath $OutputPath -Force
}

$scriptPath = Join-Path $env:TEMP (
    "civilspell-fixture-" + [Guid]::NewGuid().ToString("N") + ".scr")
$lispOutputPath = $OutputPath.Replace("\", "\\")
$scriptLines = @(
    '(setvar "CMDECHO" 0)',
    '(setvar "FILEDIA" 0)',
    '(entmakex (list ''(0 . "TEXT") ''(100 . "AcDbEntity") ''(8 . "0") ''(100 . "AcDbText") (cons 10 (list 0.0 0.0 0.0)) ''(40 . 2.5) ''(1 . "LA ESTRUTURAA EN COTA 25 m") ''(7 . "Standard") ''(50 . 0.0)))',
    '(entmakex (list ''(0 . "MTEXT") ''(100 . "AcDbEntity") ''(8 . "0") ''(100 . "AcDbMText") (cons 10 (list 0.0 8.0 0.0)) ''(40 . 2.5) ''(41 . 80.0) ''(71 . 1) ''(1 . "LA UBCACION DEL PROYECTOO") ''(7 . "Standard")))',
    '(entmakex (list ''(0 . "TEXT") ''(100 . "AcDbEntity") ''(8 . "0") ''(100 . "AcDbText") (cons 10 (list 0.0 16.0 0.0)) ''(40 . 2.5) ''(1 . "THE EXISTENT SURFCE AT STATION 1+250.00") ''(7 . "Standard") ''(50 . 0.0)))',
    '(entmakex (list ''(0 . "MTEXT") ''(100 . "AcDbEntity") ''(8 . "0") ''(100 . "AcDbMText") (cons 10 (list 0.0 24.0 0.0)) ''(40 . 2.5) ''(41 . 80.0) ''(71 . 1) ''(1 . "{\\C1;PERFIL LONGITUDINAL}\\PSTA 1+250.00 - TUBERIA 300 mm") ''(7 . "Standard")))',
    '(setq civilspell-fixture-selection (ssget "_X" ''((0 . "TEXT,MTEXT"))))',
    '(setq civilspell-fixture-count (if civilspell-fixture-selection (sslength civilspell-fixture-selection) 0))',
    '(princ (strcat "\nCIVILSPELLAI_FIXTURE_ENTITY_COUNT=" (itoa civilspell-fixture-count)))',
    '(command "_.ZOOM" "_E")',
    ('(command "_.SAVEAS" "2018" "' + $lispOutputPath + '")'),
    '(princ "\nCIVILSPELLAI_FIXTURE_CREATED")'
)

try
{
    [System.IO.File]::WriteAllLines(
        $scriptPath,
        $scriptLines,
        [System.Text.Encoding]::ASCII)
    $consoleOutput = & $consolePath `
        /i $templatePath `
        /s $scriptPath `
        /l en-US 2>&1
    $consoleExitCode = $LASTEXITCODE
    $consoleText = (($consoleOutput -join [Environment]::NewLine) -replace "`0", "")
    Write-Host $consoleText

    if ($consoleExitCode -ne 0)
    {
        throw "accoreconsole terminó con código $consoleExitCode."
    }

    if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf))
    {
        throw "AutoCAD no generó el fixture esperado: $OutputPath"
    }

    if ($consoleText.IndexOf(
        "CIVILSPELLAI_FIXTURE_ENTITY_COUNT=4",
        [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "El fixture no contiene exactamente cuatro entidades TEXT/MTEXT."
    }
}
finally
{
    if (Test-Path -LiteralPath $scriptPath)
    {
        Remove-Item -LiteralPath $scriptPath -Force
    }
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $OutputPath).Hash
Write-Host ""
Write-Host "FIXTURE PILOTO CORRECTO"
Write-Host "DWG: $OutputPath"
Write-Host "SHA-256: $hash"
