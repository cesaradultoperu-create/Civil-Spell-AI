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
        "artifacts\testing\CivilSpellAI-Pilot-Fixture-1.2.0.dwg"
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
$scriptPath = Join-Path $validationDirectory ("extended-text-$testId.scr")
$testFixturePath = Join-Path $validationDirectory ("extended-text-$testId.dwg")
$isolatedUserDataPath = Join-Path $validationDirectory ("extended-text-profile-$testId")
$pluginCommandPath = $pluginPath.Replace("\", "/")
$scriptLines = @(
    '(setvar "CMDECHO" 0)',
    '(setq civilspell-secureload-original (getvar "SECURELOAD"))',
    '(setvar "SECURELOAD" 0)',
    '_.NETLOAD',
    ('"' + $pluginCommandPath + '"'),
    'AISPELLTESTDICTIONARY',
    'AISPELLTESTEXTENDEDCREATE',
    'AISPELLTESTEXTENDEDAPPLY',
    '_.U',
    'AISPELLTESTEXTENDEDVERIFY',
    '(setvar "SECURELOAD" civilspell-secureload-original)',
    '_.QUIT',
    '_Y'
)

try
{
    Copy-Item -LiteralPath $FixturePath -Destination $testFixturePath
    [IO.Directory]::CreateDirectory($isolatedUserDataPath) | Out-Null
    [IO.File]::WriteAllLines($scriptPath, $scriptLines, [Text.Encoding]::ASCII)
    $consoleOutput = & $consolePath `
        /i $testFixturePath `
        /s $scriptPath `
        /isolate "CivilSpellAI-$testId" $isolatedUserDataPath `
        /l en-US 2>&1
    $consoleExitCode = $LASTEXITCODE
    $consoleText = (($consoleOutput -join [Environment]::NewLine) -replace "`0", "")
    Write-Host $consoleText

    if ($consoleExitCode -ne 0)
    {
        throw "accoreconsole termino con codigo $consoleExitCode."
    }

    if ($consoleText.IndexOf(
        "PASS DICT-01: OFFLINE ES_PE DICTIONARY LOADED AND RESTORED THE ACCENT.",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "El diccionario es_PE no cargo o no corrigio dentro de AutoCAD."
    }

    if ($consoleText.IndexOf(
        "READY EXTENDED-01: FIXTURE CREATED",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "No se crearon los destinos extendidos de prueba."
    }

    if ($consoleText.IndexOf(
        "READY EXTENDED-01: 5 TARGETS APPLIED",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "No se aplicaron atomica y conjuntamente los cinco destinos."
    }

    if ($consoleText.IndexOf(
        "PASS EXTENDED-01: MLEADER, TABLE CELL, BLOCK TEXT AND ATTRIBUTE RESTORED BY ONE U.",
        [StringComparison]::Ordinal) -lt 0)
    {
        throw "Una sola U no restauro los cuatro destinos extendidos."
    }

    if ($consoleText.IndexOf("FAIL EXTENDED-01", [StringComparison]::Ordinal) -ge 0)
    {
        throw "La prueba extendida informo un fallo."
    }

    if ($consoleText.IndexOf("FAIL DICT-01", [StringComparison]::Ordinal) -ge 0)
    {
        throw "La prueba del diccionario offline informo un fallo."
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

    if (Test-Path -LiteralPath $isolatedUserDataPath)
    {
        $resolvedIsolationPath = (Resolve-Path -LiteralPath $isolatedUserDataPath).Path
        $resolvedValidationPath = (Resolve-Path -LiteralPath $validationDirectory).Path
        if (-not $resolvedIsolationPath.StartsWith(
            $resolvedValidationPath + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase))
        {
            throw "La carpeta aislada quedo fuera del directorio de validacion."
        }

        Remove-Item -LiteralPath $resolvedIsolationPath -Recurse -Force
    }
}

Write-Host "INTEGRACION DE TEXTOS EXTENDIDOS CORRECTA"
