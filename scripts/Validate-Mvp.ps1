param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateRange(1, 10000)]
    [int]$ExpectedTestCount = 105,

    [string]$ReportPath
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solutionPath = Join-Path $repoRoot "CivilSpellAI.slnx"
$testsScript = Join-Path $PSScriptRoot "Run-SpellCoreTests.ps1"
$pluginOutput = Join-Path $repoRoot "bin\x64\$Configuration"
$pluginPath = Join-Path $pluginOutput "CivilSpellAI.dll"
$glossaryPath = Join-Path $pluginOutput "Resources\technical-glossary.txt"

if ([string]::IsNullOrWhiteSpace($ReportPath))
{
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $ReportPath = Join-Path $repoRoot "artifacts\validation\mvp-$($Configuration.ToLowerInvariant())-$timestamp.md"
}
elseif (-not [System.IO.Path]::IsPathRooted($ReportPath))
{
    $ReportPath = Join-Path $repoRoot $ReportPath
}

Write-Host "Validando CivilSpellAI MVP ($Configuration|x64)..."

$buildOutput = & dotnet build $solutionPath `
    --configuration $Configuration `
    --no-restore `
    -p:Platform=x64 2>&1
$buildExitCode = $LASTEXITCODE
$buildOutput | ForEach-Object { Write-Host $_ }

if ($buildExitCode -ne 0)
{
    throw "La compilacion del complemento termino con codigo $buildExitCode."
}

$testOutput = & powershell -NoProfile -ExecutionPolicy Bypass `
    -File $testsScript `
    -Configuration $Configuration 2>&1
$testExitCode = $LASTEXITCODE
$testOutput | ForEach-Object { Write-Host $_ }

if ($testExitCode -ne 0)
{
    throw "Las pruebas terminaron con codigo $testExitCode."
}

$expectedSummary = "Resultados: $ExpectedTestCount correctos, 0 fallidos."
$testText = $testOutput -join [Environment]::NewLine

if ($testText.IndexOf($expectedSummary, [StringComparison]::Ordinal) -lt 0)
{
    throw "No se encontro el resumen esperado: $expectedSummary"
}

if (-not (Test-Path -LiteralPath $pluginPath -PathType Leaf))
{
    throw "No se encontro el complemento esperado: $pluginPath"
}

if (-not (Test-Path -LiteralPath $glossaryPath -PathType Leaf))
{
    throw "No se encontro el glosario en la salida: $glossaryPath"
}

$accessibilityRequirements = @(
    @{
        Path = "UI\SpellSettingsWindow.xaml"
        Snippets = @(
            'FocusManager.FocusedElement=',
            'Property="IsKeyboardFocusWithin"',
            'AutomationProperties.Name="Estado de la prueba de conexión"'
        )
    },
    @{
        Path = "UI\SpellReviewWindow.xaml"
        Snippets = @(
            'FocusManager.FocusedElement=',
            'AutomationProperties.Name="Cambios calculados localmente"',
            'AutomationProperties.LiveSetting="Polite"'
        )
    },
    @{
        Path = "UI\BatchPreparationWindow.xaml"
        Snippets = @(
            'FocusManager.FocusedElement=',
            'AutomationProperties.Name="Estado de preparación de la revisión"'
        )
    },
    @{
        Path = "UI\BatchReviewWindow.xaml"
        Snippets = @(
            'FocusManager.FocusedElement=',
            'Property="IsKeyboardFocusWithin"',
            'AutomationProperties.HelpText="{Binding ValidationText}"'
        )
    }
)

foreach ($requirement in $accessibilityRequirements)
{
    $xamlPath = Join-Path $repoRoot $requirement.Path

    if (-not (Test-Path -LiteralPath $xamlPath -PathType Leaf))
    {
        throw "No se encontro la ventana WPF requerida: $xamlPath"
    }

    $xamlText = [IO.File]::ReadAllText($xamlPath)

    foreach ($snippet in $requirement.Snippets)
    {
        if ($xamlText.IndexOf($snippet, [StringComparison]::Ordinal) -lt 0)
        {
            throw "La garantia de accesibilidad '$snippet' falta en $($requirement.Path)."
        }
    }
}

$forbiddenAssemblies = @(
    "accoremgd.dll",
    "acdbmgd.dll",
    "acmgd.dll",
    "AeccDbMgd.dll"
)
$copiedAutodeskAssemblies = @()

foreach ($assemblyName in $forbiddenAssemblies)
{
    $candidate = Join-Path $pluginOutput $assemblyName

    if (Test-Path -LiteralPath $candidate -PathType Leaf)
    {
        $copiedAutodeskAssemblies += $assemblyName
    }
}

if ($copiedAutodeskAssemblies.Count -gt 0)
{
    throw "La salida contiene DLL de Autodesk que no deben distribuirse: $($copiedAutodeskAssemblies -join ', ')"
}

if ($Configuration -eq "Release")
{
    $forbiddenReleaseCommands = @(
        "AISPELLTESTCONFLICT",
        "AISPELLTESTBATCHCONFLICT",
        "AISPELLTESTDOCUMENTSWITCH",
        "AISPELLTESTBATCHUNDO",
        "AISPELLTESTBATCHUNDOVERIFY"
    )
    $assemblyText = [Text.Encoding]::ASCII.GetString(
        [IO.File]::ReadAllBytes($pluginPath))
    $includedDiagnostics = @($forbiddenReleaseCommands | Where-Object {
        $assemblyText.IndexOf($_, [StringComparison]::Ordinal) -ge 0
    })

    if ($includedDiagnostics.Count -gt 0)
    {
        throw "Release contiene comandos exclusivos de diagnóstico: $($includedDiagnostics -join ', ')"
    }
}

$hash = (Get-FileHash -LiteralPath $pluginPath -Algorithm SHA256).Hash
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginPath).Version.ToString()
$commit = (& git -C $repoRoot rev-parse --short HEAD 2>$null)

if ($LASTEXITCODE -ne 0)
{
    $commit = "no disponible"
}

$localChangeCount = @(& git -C $repoRoot status --porcelain 2>$null).Count
$validatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
$relativePluginPath = "bin\x64\$Configuration\CivilSpellAI.dll"
$reportDirectory = Split-Path -Parent $ReportPath

if (-not (Test-Path -LiteralPath $reportDirectory))
{
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
}

$reportLines = @(
    "# Validacion automatizada del MVP",
    "",
    "- Fecha: $validatedAt",
    "- Configuracion: $Configuration|x64",
    "- Commit base: $commit",
    "- Cambios locales detectados: $localChangeCount",
    "- Ensamblado: $relativePluginPath",
    "- Version: $assemblyVersion",
    "- SHA-256: $hash",
    "",
    "## Resultado",
    "",
    "- Compilacion de la solucion: correcta.",
    "- Pruebas: $ExpectedTestCount correctas, 0 fallidas.",
    "- Glosario incluido en la salida: correcto.",
    "- Garantias de accesibilidad XAML: correctas.",
    "- DLL de Autodesk copiadas a la salida: ninguna.",
    ("- Comandos de diagnóstico exclusivos de Debug en Release: " +
        $(if ($Configuration -eq "Release") { "ninguno." } else { "no aplica." })),
    "",
    "Esta huella identifica la DLL que debe cargarse con NETLOAD durante la sesion",
    "manual. El informe no contiene claves, texto de dibujos ni respuestas remotas."
)
$report = $reportLines -join [Environment]::NewLine

Set-Content -LiteralPath $ReportPath -Value $report -Encoding UTF8

Write-Host ""
Write-Host "VALIDACION AUTOMATIZADA CORRECTA"
Write-Host "DLL: $pluginPath"
Write-Host "SHA-256: $hash"
Write-Host "Informe: $ReportPath"
