param(
    [string]$OutputPath,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactsRoot = Join-Path $repoRoot "artifacts\drawings"
$lispPath = Join-Path $repoRoot "Lisp\QuebradaDrawing.lsp"

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $artifactsRoot "Descarga_Quebrada.dwg"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath))
{
    $OutputPath = Join-Path $repoRoot $OutputPath
}

$OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$artifactsFull = [System.IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'

if (-not $OutputPath.StartsWith($artifactsFull, [System.StringComparison]::OrdinalIgnoreCase))
{
    throw "El DWG debe generarse dentro de: $artifactsRoot"
}

if ([System.IO.Path]::GetExtension($OutputPath) -ne ".dwg")
{
    throw "El archivo de salida debe tener extensión .dwg."
}

if ((Test-Path -LiteralPath $OutputPath) -and -not $Force)
{
    throw "El DWG ya existe. Use -Force para reemplazar únicamente este archivo: $OutputPath"
}

$installedRuntime = $null
foreach ($year in @("2026", "2025", "2024"))
{
    $candidateConsole = Join-Path $env:ProgramFiles "Autodesk\AutoCAD $year\accoreconsole.exe"
    if (-not (Test-Path -LiteralPath $candidateConsole -PathType Leaf))
    {
        continue
    }

    $candidateTemplates = @("enu", "esp", "esn") | ForEach-Object {
        Join-Path $env:LOCALAPPDATA "Autodesk\C3D $year\$_\Template\AutoCAD Template\acad.dwt"
    }
    $candidateTemplate = $candidateTemplates | Where-Object {
        Test-Path -LiteralPath $_ -PathType Leaf
    } | Select-Object -First 1

    if ($candidateTemplate)
    {
        $installedRuntime = [pscustomobject]@{
            Year = $year
            Console = $candidateConsole
            Template = $candidateTemplate
        }
        break
    }
}

if ($null -eq $installedRuntime)
{
    throw "No se encontró Civil 3D con accoreconsole.exe y acad.dwt."
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

if (Test-Path -LiteralPath $OutputPath)
{
    Remove-Item -LiteralPath $OutputPath -Force
}

$runId = [Guid]::NewGuid().ToString("N")
$scriptPath = Join-Path $env:TEMP ("quebrada-$runId.scr")
$loadPath = $lispPath.Replace("\", "\\")
$savePath = $OutputPath.Replace("\", "\\")
$scriptLines = @(
    '(setvar "FILEDIA" 0)',
    ('(load "' + $loadPath + '")'),
    '(c:QUEBRADADWG)',
    '(setq quebrada-selection (ssget "_X"))',
    '(setq quebrada-dimensions (ssget "_X" ''((0 . "DIMENSION"))))',
    '(princ (strcat "\nQUEBRADA_ENTITY_COUNT=" (itoa (if quebrada-selection (sslength quebrada-selection) 0))))',
    '(princ (strcat "\nQUEBRADA_DIMENSION_COUNT=" (itoa (if quebrada-dimensions (sslength quebrada-dimensions) 0))))',
    '(command "_.ZOOM" "_E")',
    ('(command "_.SAVEAS" "2018" "' + $savePath + '")'),
    '(princ "\nQUEBRADA_DWG_SAVED")'
)

try
{
    [System.IO.File]::WriteAllLines($scriptPath, $scriptLines, [System.Text.Encoding]::UTF8)
    $consoleOutput = & $installedRuntime.Console `
        /i $installedRuntime.Template `
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
        throw "AutoCAD no generó el DWG esperado: $OutputPath"
    }

    if ($consoleText.IndexOf("QUEBRADA_DWG_SAVED", [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "AutoCAD no confirmó el guardado del DWG."
    }

    if ($consoleText.IndexOf("QUEBRADADWG_CREATED", [System.StringComparison]::Ordinal) -lt 0)
    {
        throw "El comando de dibujo no terminó correctamente."
    }

    $countMatch = [regex]::Match($consoleText, 'QUEBRADA_ENTITY_COUNT=(\d+)')
    if (-not $countMatch.Success -or [int]$countMatch.Groups[1].Value -lt 50)
    {
        throw "El DWG no contiene la cantidad esperada de geometría y cotas."
    }

    $dimensionMatch = [regex]::Match($consoleText, 'QUEBRADA_DIMENSION_COUNT=(\d+)')
    if (-not $dimensionMatch.Success -or [int]$dimensionMatch.Groups[1].Value -ne 11)
    {
        throw "El DWG no contiene las 11 cotas esperadas."
    }
}
finally
{
    if (Test-Path -LiteralPath $scriptPath)
    {
        Remove-Item -LiteralPath $scriptPath -Force
    }

}

$drawing = Get-Item -LiteralPath $OutputPath
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $OutputPath).Hash
Write-Host ""
Write-Host "DWG DE QUEBRADA CREADO"
Write-Host "Civil 3D: $($installedRuntime.Year)"
Write-Host "Archivo: $($drawing.FullName)"
Write-Host "Tamaño: $($drawing.Length) bytes"
Write-Host "SHA-256: $hash"
