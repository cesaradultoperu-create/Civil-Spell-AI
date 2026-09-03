param(
    [ValidateSet("Debug")]
    [string]$Configuration = "Debug",

    [string]$FixturePath
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$consolePath = Join-Path $env:ProgramFiles "Autodesk\AutoCAD 2024\accoreconsole.exe"
$pluginPath = Join-Path $repoRoot "bin\x64\$Configuration\CivilSpellAI.dll"

if ([string]::IsNullOrWhiteSpace($FixturePath))
{
    $FixturePath = Join-Path $repoRoot `
        "artifacts\testing\CivilSpellAI-Pilot-Fixture-1.1.5.dwg"
}
elseif (-not [IO.Path]::IsPathRooted($FixturePath))
{
    $FixturePath = Join-Path $repoRoot $FixturePath
}

$FixturePath = (Resolve-Path -LiteralPath $FixturePath).Path

if (-not (Test-Path -LiteralPath $consolePath -PathType Leaf))
{
        throw "No se encontro accoreconsole.exe de AutoCAD 2024."
}

if (-not (Test-Path -LiteralPath $pluginPath -PathType Leaf))
{
        throw "No se encontro la DLL Debug requerida: $pluginPath"
}

$validationDirectory = Join-Path $repoRoot "artifacts\validation"
[IO.Directory]::CreateDirectory($validationDirectory) | Out-Null
$testId = [Guid]::NewGuid().ToString("N")
$scriptPath = Join-Path $validationDirectory ("batch-undo-$testId.scr")
$testFixturePath = Join-Path $validationDirectory ("batch-undo-$testId.dwg")
$pluginCommandPath = $pluginPath.Replace("\", "/")
$scriptLines = @(
    '(setvar "CMDECHO" 0)',
    '(setq civilspell-secureload-original (getvar "SECURELOAD"))',
    '(princ (strcat "\nCIVILSPELLAI_SECURELOAD_ORIGINAL=" (itoa civilspell-secureload-original)))',
    '(setvar "SECURELOAD" 0)',
    '_.NETLOAD',
    ('"' + $pluginCommandPath + '"'),
    'AISPELLTESTBATCHUNDO',
    '_.U',
    'AISPELLTESTBATCHUNDOVERIFY',
    '(setvar "SECURELOAD" civilspell-secureload-original)',
    '_.QUIT',
    '_Y'
)

try
{
    Copy-Item -LiteralPath $FixturePath -Destination $testFixturePath
    [IO.File]::WriteAllLines($scriptPath, $scriptLines, [Text.Encoding]::ASCII)
    $consoleOutput = & $consolePath `
        /i $testFixturePath `
        /s $scriptPath `
        /l en-US 2>&1
    $consoleExitCode = $LASTEXITCODE
    $consoleText = (($consoleOutput -join [Environment]::NewLine) -replace "`0", "")
    Write-Host $consoleText

    if ($consoleExitCode -ne 0)
    {
        throw "accoreconsole termino con codigo $consoleExitCode."
    }

    if ($consoleText.IndexOf(
        "READY UNDO-04",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "La escritura de prueba no llego al estado previo a U."
    }

    if ($consoleText.IndexOf(
        "PASS UNDO-04: ONE U RESTORED THE ENTIRE BATCH.",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "Una sola operacion U no restauro el lote completo."
    }

    if ($consoleText.IndexOf("FAIL UNDO-04", [StringComparison]::Ordinal) -ge 0)
    {
        throw "La prueba de integracion informo un fallo de UNDO."
    }
}
finally
{
    if (Test-Path -LiteralPath $scriptPath)
    {
        Remove-Item -LiteralPath $scriptPath -Force
    }

    if (Test-Path -LiteralPath $testFixturePath)
    {
        Remove-Item -LiteralPath $testFixturePath -Force
    }
}

Write-Host "INTEGRACION UNDO AISPELLALL CORRECTA"
