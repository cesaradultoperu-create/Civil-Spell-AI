param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateRange(1, 10000)]
    [int]$ExpectedTestCount = 151,

    [string]$ReportPath
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solutionPath = Join-Path $repoRoot "CivilSpellAI.slnx"
$testsScript = Join-Path $PSScriptRoot "Run-SpellCoreTests.ps1"
$pluginOutput = Join-Path $repoRoot "bin\x64\$Configuration"
$pluginPath = Join-Path $pluginOutput "CivilSpellAI.dll"
$pluginTargets = @(
    [pscustomobject]@{ Year = "2024"; Output = $pluginOutput; Path = $pluginPath; RequiresDeps = $false },
    [pscustomobject]@{ Year = "2025"; Output = (Join-Path $pluginOutput "A25"); Path = (Join-Path $pluginOutput "A25\CivilSpellAI.dll"); RequiresDeps = $true },
    [pscustomobject]@{ Year = "2026"; Output = (Join-Path $pluginOutput "A26"); Path = (Join-Path $pluginOutput "A26\CivilSpellAI.dll"); RequiresDeps = $true }
)
$glossaryPath = Join-Path $pluginOutput "Resources\technical-glossary.txt"
$hunspellPath = Join-Path $pluginOutput "WeCantSpell.Hunspell.dll"
$dictionaryPath = Join-Path $pluginOutput "Resources\Dictionaries\es_PE.dic"
$affixPath = Join-Path $pluginOutput "Resources\Dictionaries\es_PE.aff"
$englishDictionaryPath = Join-Path $pluginOutput "Resources\Dictionaries\en_US.dic"
$englishAffixPath = Join-Path $pluginOutput "Resources\Dictionaries\en_US.aff"
$dictionaryLicensePath = Join-Path $pluginOutput "Resources\Licenses\MPL-1.1.txt"
$englishDictionaryLicensePath = Join-Path $pluginOutput "Resources\Licenses\Hunspell-en_US-license.txt"
$thirdPartyNoticesPath = Join-Path $pluginOutput "THIRD_PARTY_NOTICES.md"

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

$requiredOfflineDictionaryFiles = @(
    $hunspellPath,
    $dictionaryPath,
    $affixPath,
    $englishDictionaryPath,
    $englishAffixPath,
    $dictionaryLicensePath,
    $englishDictionaryLicensePath,
    $thirdPartyNoticesPath
)

foreach ($requiredDictionaryFile in $requiredOfflineDictionaryFiles)
{
    if (-not (Test-Path -LiteralPath $requiredDictionaryFile -PathType Leaf))
    {
        throw "Falta un archivo del diccionario offline: $requiredDictionaryFile"
    }
}

foreach ($target in $pluginTargets)
{
    $requiredRuntimeFiles = @(
        $target.Path,
        (Join-Path $target.Output "WeCantSpell.Hunspell.dll"),
        (Join-Path $target.Output "THIRD_PARTY_NOTICES.md"),
        (Join-Path $target.Output "Resources\technical-glossary.txt"),
        (Join-Path $target.Output "Resources\Dictionaries\es_PE.aff"),
        (Join-Path $target.Output "Resources\Dictionaries\es_PE.dic"),
        (Join-Path $target.Output "Resources\Dictionaries\en_US.aff"),
        (Join-Path $target.Output "Resources\Dictionaries\en_US.dic")
    )

    if ($target.RequiresDeps)
    {
        $requiredRuntimeFiles += (Join-Path $target.Output "CivilSpellAI.deps.json")
    }

    foreach ($requiredRuntimeFile in $requiredRuntimeFiles)
    {
        if (-not (Test-Path -LiteralPath $requiredRuntimeFile -PathType Leaf))
        {
            throw "Falta un archivo de la salida de Civil 3D $($target.Year): $requiredRuntimeFile"
        }
    }
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

$preflightPath = Join-Path $repoRoot "scripts\Test-CivilSpellAIEnvironment.ps1"
$preflightText = [IO.File]::ReadAllText($preflightPath)
$preflightDictionarySnippets = @(
    "Resources\Dictionaries\es_PE.aff",
    "Resources\Dictionaries\es_PE.dic",
    "Resources\Dictionaries\en_US.aff",
    "Resources\Dictionaries\en_US.dic"
)

foreach ($snippet in $preflightDictionarySnippets)
{
    if ($preflightText.IndexOf($snippet, [StringComparison]::Ordinal) -lt 0)
    {
        throw "La comprobacion previa no exige el recurso offline '$snippet'."
    }
}

$undoGuidancePath = Join-Path $repoRoot "Commands\AiSpellAllCommand.cs"
$undoGuidanceSnippets = @(
    "use U inmediatamente",
    "antes de hacer zoom",
    "desplazarse o ejecutar otro comando"
)

if (-not (Test-Path -LiteralPath $undoGuidancePath -PathType Leaf))
{
    throw "No se encontro el comando AISPELLALL: $undoGuidancePath"
}

$undoGuidanceText = [IO.File]::ReadAllText($undoGuidancePath)

foreach ($snippet in $undoGuidanceSnippets)
{
    if ($undoGuidanceText.IndexOf($snippet, [StringComparison]::Ordinal) -lt 0)
    {
        throw "La guia de UNDO inmediato '$snippet' falta en AISPELLALL."
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
    foreach ($target in $pluginTargets)
    {
        $candidate = Join-Path $target.Output $assemblyName

        if (Test-Path -LiteralPath $candidate -PathType Leaf)
        {
            $copiedAutodeskAssemblies += "$($target.Year):$assemblyName"
        }
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
        "AISPELLTESTBATCHUNDOVERIFY",
        "AISPELLTESTDICTIONARY",
        "AISPELLTESTSECTION",
        "AISPELLTESTEXTENDEDCREATE",
        "AISPELLTESTEXTENDEDAPPLY",
        "AISPELLTESTEXTENDEDVERIFY"
    )
    $includedDiagnostics = @()
    foreach ($target in $pluginTargets)
    {
        $assemblyText = [Text.Encoding]::ASCII.GetString(
            [IO.File]::ReadAllBytes($target.Path))
        $includedDiagnostics += @($forbiddenReleaseCommands | Where-Object {
            $assemblyText.IndexOf($_, [StringComparison]::Ordinal) -ge 0
        } | ForEach-Object { "$($target.Year):$_" })
    }

    if ($includedDiagnostics.Count -gt 0)
    {
        throw "Release contiene comandos exclusivos de diagnóstico: $($includedDiagnostics -join ', ')"
    }
}

$pluginMetadata = @($pluginTargets | ForEach-Object {
    [pscustomobject]@{
        Year = $_.Year
        Path = $_.Path
        Version = [Reflection.AssemblyName]::GetAssemblyName($_.Path).Version.ToString()
        Hash = (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash
    }
})
$versions = @($pluginMetadata | Select-Object -ExpandProperty Version -Unique)
if ($versions.Count -ne 1)
{
    throw "Las DLL multiversión no comparten la misma versión: $($versions -join ', ')"
}
$assemblyVersion = $versions[0]
$commit = (& git -C $repoRoot rev-parse --short HEAD 2>$null)

if ($LASTEXITCODE -ne 0)
{
    $commit = "no disponible"
}

$localChangeCount = @(& git -C $repoRoot status --porcelain 2>$null).Count
$validatedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss zzz"
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
    "- Version: $assemblyVersion",
    "- DLL 2024: $($pluginMetadata[0].Path) | SHA-256 $($pluginMetadata[0].Hash)",
    "- DLL 2025: $($pluginMetadata[1].Path) | SHA-256 $($pluginMetadata[1].Hash)",
    "- DLL 2026: $($pluginMetadata[2].Path) | SHA-256 $($pluginMetadata[2].Hash)",
    "",
    "## Resultado",
    "",
    "- Compilacion de la solucion para Civil 3D 2024, 2025 y 2026: correcta.",
    "- Pruebas: $ExpectedTestCount correctas, 0 fallidas.",
    "- Glosario incluido en la salida: correcto.",
    "- Diccionario es_PE, motor administrado y licencias: correctos.",
    "- Garantias de accesibilidad XAML: correctas.",
    "- Guia de UNDO inmediato de AISPELLALL: correcta.",
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
Write-Host "DLL 2024/2025/2026 validadas."
Write-Host "SHA-256: $(($pluginMetadata | ForEach-Object { "$($_.Year)=$($_.Hash)" }) -join '; ')"
Write-Host "Informe: $ReportPath"
