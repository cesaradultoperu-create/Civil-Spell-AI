param(
    [string]$Version = "1.3.7.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$manifestSource = Join-Path $repoRoot "packaging\PackageContents.xml"
$helpSource = Join-Path $repoRoot "packaging\Help.html"
$regressionRunbookSource = Join-Path $repoRoot "docs\testing\PILOT_1_2_EXTENDED_TEXT_RUNBOOK.md"
$releaseOutput = Join-Path $repoRoot "bin\x64\Release"
$runtimeTargets = @(
    [pscustomobject]@{ Year = "2024"; Series = "R24.3"; Source = $releaseOutput; RequiresDeps = $false },
    [pscustomobject]@{ Year = "2025"; Series = "R25.0"; Source = (Join-Path $releaseOutput "A25"); RequiresDeps = $true },
    [pscustomobject]@{ Year = "2026"; Series = "R25.1"; Source = (Join-Path $releaseOutput "A26"); RequiresDeps = $true }
)
$distributionParent = Join-Path $repoRoot "artifacts\distribution"
$distributionRoot = Join-Path $distributionParent "CivilSpellAI-$Version"
$bundleRoot = Join-Path $distributionRoot "CivilSpellAI.bundle"
$windowsContents = Join-Path $bundleRoot "Contents\Windows"
$resourceContents = Join-Path $bundleRoot "Contents\Resources"
$archivePath = Join-Path $distributionParent "CivilSpellAI-$Version.zip"
$checksumPath = "$archivePath.sha256"
$validationReport = Join-Path $repoRoot "artifacts\validation\pilot-release-$Version.md"

function Assert-ChildPath([string]$candidate, [string]$parent)
{
    $candidateFull = [System.IO.Path]::GetFullPath($candidate)
    $parentFull = [System.IO.Path]::GetFullPath($parent).TrimEnd('\') + '\'

    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase))
    {
        throw "La ruta generada queda fuera del directorio autorizado: $candidateFull"
    }
}

function New-DeterministicZip(
    [string]$sourceDirectory,
    [string]$destinationPath)
{
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $sourceFull = [System.IO.Path]::GetFullPath($sourceDirectory)
    $baseDirectory = [System.IO.Directory]::GetParent($sourceFull).FullName
    $fixedTimestamp = [DateTimeOffset]::new(
        2000,
        1,
        1,
        0,
        0,
        0,
        [TimeSpan]::Zero)
    $destination = [System.IO.File]::Open(
        $destinationPath,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)

    try
    {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $destination,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false)

        try
        {
            $files = Get-ChildItem -LiteralPath $sourceFull -Recurse -File |
                Sort-Object FullName

            foreach ($file in $files)
            {
                $relativePath = $file.FullName.Substring($baseDirectory.Length)
                $relativePath = $relativePath.TrimStart('\')
                $entryName = $relativePath.Replace('\', '/')
                $entry = $archive.CreateEntry(
                    $entryName,
                    [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedTimestamp
                $entryStream = $entry.Open()
                $sourceStream = [System.IO.File]::OpenRead($file.FullName)

                try
                {
                    $sourceStream.CopyTo($entryStream)
                }
                finally
                {
                    $sourceStream.Dispose()
                    $entryStream.Dispose()
                }
            }
        }
        finally
        {
            $archive.Dispose()
        }
    }
    finally
    {
        $destination.Dispose()
    }
}

function Copy-PluginRuntime(
    [object]$runtime,
    [string]$destinationRoot)
{
    $pluginSource = Join-Path $runtime.Source "CivilSpellAI.dll"
    $hunspellSource = Join-Path $runtime.Source "WeCantSpell.Hunspell.dll"
    $thirdPartyNoticesSource = Join-Path $runtime.Source "THIRD_PARTY_NOTICES.md"
    $resourcesSource = Join-Path $runtime.Source "Resources"
    $geometryDependencies = @("NetTopologySuite.dll")
    if ($runtime.Year -eq "2024") {
        $geometryDependencies += @("System.Memory.dll", "System.Buffers.dll", "System.Numerics.Vectors.dll", "System.Runtime.CompilerServices.Unsafe.dll")
    }
    $requiredFiles = @(
        $pluginSource,
        $hunspellSource,
        $thirdPartyNoticesSource,
        (Join-Path $resourcesSource "technical-glossary.txt"),
        (Join-Path $resourcesSource "Dictionaries\es_PE.aff"),
        (Join-Path $resourcesSource "Dictionaries\es_PE.dic"),
        (Join-Path $resourcesSource "Dictionaries\en_US.aff"),
        (Join-Path $resourcesSource "Dictionaries\en_US.dic"),
        (Join-Path $resourcesSource "Dictionaries\LICENSE.md"),
        (Join-Path $resourcesSource "Dictionaries\README_hunspell_es.txt"),
        (Join-Path $resourcesSource "Dictionaries\README_hunspell_en_US.txt"),
        (Join-Path $resourcesSource "Dictionaries\SOURCE.md"),
        (Join-Path $resourcesSource "Licenses\MPL-1.1.txt"),
        (Join-Path $resourcesSource "Licenses\Hunspell-en_US-license.txt"),
        (Join-Path $resourcesSource "Licenses\WeCantSpell.Hunspell-2.0.1-license.txt")
    )

    if ($runtime.RequiresDeps)
    {
        $requiredFiles += (Join-Path $runtime.Source "CivilSpellAI.deps.json")
    }

    $requiredFiles += $geometryDependencies | ForEach-Object { Join-Path $runtime.Source $_ }

    foreach ($requiredFile in $requiredFiles)
    {
        if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf))
        {
            throw "Falta un archivo de Civil 3D $($runtime.Year): $requiredFile"
        }
    }

    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
    Copy-Item -LiteralPath $pluginSource -Destination $destinationRoot
    Copy-Item -LiteralPath $hunspellSource -Destination $destinationRoot
    foreach ($geometryDependency in $geometryDependencies) {
        Copy-Item -LiteralPath (Join-Path $runtime.Source $geometryDependency) -Destination $destinationRoot
    }
    Copy-Item -LiteralPath $thirdPartyNoticesSource -Destination $destinationRoot
    Copy-Item -LiteralPath $resourcesSource -Destination $destinationRoot -Recurse

    if ($runtime.RequiresDeps)
    {
        Copy-Item `
            -LiteralPath (Join-Path $runtime.Source "CivilSpellAI.deps.json") `
            -Destination $destinationRoot
    }
}

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$')
{
    throw "La versión debe usar cuatro componentes numéricos."
}

& (Join-Path $PSScriptRoot "Validate-Mvp.ps1") `
    -Configuration Release `
    -ReportPath $validationReport

[xml]$manifest = Get-Content -Raw -LiteralPath $manifestSource
$package = $manifest.ApplicationPackage
$components = @($package.Components)
$expectedCommands = @(
    "AISPELL",
    "AISPELLALL",
    "AISPELLSETTINGS",
    "CREARSECCION",
    "EDITARSECCION",
    "CREARFIGURA3D",
    "CREARCOMPONENTE3D",
    "EDITARFIGURA3D",
        "REVISARPOLILINEAS",
        "AISPELLDETECTARSUPERPUESTAS",
        "AISPELLQUITARMARCAS",
        "AISPELLVERMARCAS",
        "RESTAURARPOLILINEAS",
        "RESTAURARSOLAPES"
)

if ($package.AppVersion -ne $Version)
{
    throw "La versión del manifiesto debe coincidir con el parámetro: manifiesto=$($package.AppVersion), solicitada=$Version."
}

if ($components.Count -ne $runtimeTargets.Count)
{
    throw "El manifiesto debe contener una entrada para Civil 3D 2024, 2025 y 2026."
}

foreach ($runtime in $runtimeTargets)
{
    $matchingComponents = @($components | Where-Object {
        $_.RuntimeRequirements.SeriesMin -eq $runtime.Series -and
        $_.RuntimeRequirements.SeriesMax -eq $runtime.Series
    })

    if ($matchingComponents.Count -ne 1)
    {
        throw "El manifiesto no contiene una única entrada para Civil 3D $($runtime.Year) ($($runtime.Series))."
    }

    $componentGroup = $matchingComponents[0]
    $requirements = $componentGroup.RuntimeRequirements
    $component = $componentGroup.ComponentEntry
    $commands = @($component.Commands.Command | ForEach-Object { $_.Global })
    $expectedModule = "./Contents/Windows/$($runtime.Year)/CivilSpellAI.dll"

    if ($requirements.OS -ne "Win64" -or
        $requirements.Platform -ne "Civil3D" -or
        $component.ModuleName -ne $expectedModule -or
        $component.LoadOnAutoCADStartup -ne "True" -or
        $null -ne $component.LoadReasons -or
        (Compare-Object $expectedCommands $commands))
    {
        throw "La entrada de Civil 3D $($runtime.Year) no coincide con la plataforma, DLL o comandos esperados."
    }

    $pluginSource = Join-Path $runtime.Source "CivilSpellAI.dll"
    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginSource).Version.ToString()
    if ($assemblyVersion -ne $Version)
    {
        throw "La DLL de Civil 3D $($runtime.Year) usa $assemblyVersion y se solicitó $Version."
    }
}

$parsedProductCode = [Guid]::Empty
$parsedUpgradeCode = [Guid]::Empty
if (-not [Guid]::TryParse($package.ProductCode, [ref]$parsedProductCode) -or
    -not [Guid]::TryParse($package.UpgradeCode, [ref]$parsedUpgradeCode))
{
    throw "ProductCode y UpgradeCode deben ser GUID válidos."
}

Assert-ChildPath $distributionRoot $distributionParent
Assert-ChildPath $archivePath $distributionParent
Assert-ChildPath $checksumPath $distributionParent

if (Test-Path -LiteralPath $distributionRoot)
{
    Remove-Item -LiteralPath $distributionRoot -Recurse -Force
}

if (Test-Path -LiteralPath $archivePath)
{
    Remove-Item -LiteralPath $archivePath -Force
}

if (Test-Path -LiteralPath $checksumPath)
{
    Remove-Item -LiteralPath $checksumPath -Force
}

New-Item -ItemType Directory -Path $windowsContents -Force | Out-Null
New-Item -ItemType Directory -Path $resourceContents -Force | Out-Null
Copy-Item -LiteralPath $manifestSource -Destination (Join-Path $bundleRoot "PackageContents.xml")

foreach ($runtime in $runtimeTargets)
{
    Copy-PluginRuntime `
        -runtime $runtime `
        -destinationRoot (Join-Path $windowsContents $runtime.Year)
}

Copy-Item -LiteralPath $helpSource -Destination (Join-Path $resourceContents "Help.html")
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Manage-CivilSpellAI.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Test-CivilSpellAIEnvironment.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "New-PilotFixture.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\PILOT_INSTALLATION.md") -Destination (Join-Path $distributionRoot "LEAME.md")
Copy-Item -LiteralPath $regressionRunbookSource -Destination (Join-Path $distributionRoot "REGRESION_PILOTO_1_2.md")
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\GUIA_CORREGIR_SOLAPES.md") -Destination (Join-Path $distributionRoot "GUIA_CORREGIR_SOLAPES.md")

$forbidden = @("accoremgd.dll", "acdbmgd.dll", "acmgd.dll", "AeccDbMgd.dll")
$packagedForbidden = @(Get-ChildItem -LiteralPath $distributionRoot -Recurse -File |
    Where-Object { $forbidden -contains $_.Name })

if ($packagedForbidden.Count -gt 0)
{
    throw "El paquete contiene ensamblados de Autodesk."
}

$invalidScriptEncodings = @()

foreach ($scriptFile in Get-ChildItem -LiteralPath $distributionRoot -Recurse -File -Filter "*.ps1")
{
    $scriptBytes = [IO.File]::ReadAllBytes($scriptFile.FullName)
    $hasNonAscii = @($scriptBytes | Where-Object { $_ -gt 127 }).Count -gt 0
    $hasUtf8Bom = $scriptBytes.Length -ge 3 -and
        $scriptBytes[0] -eq 239 -and
        $scriptBytes[1] -eq 187 -and
        $scriptBytes[2] -eq 191

    if ($hasNonAscii -and -not $hasUtf8Bom)
    {
        $invalidScriptEncodings += $scriptFile.Name
    }
}

if ($invalidScriptEncodings.Count -gt 0)
{
    throw "Los scripts con texto no ASCII deben usar UTF-8 con BOM para Windows PowerShell 5.1: $($invalidScriptEncodings -join ', ')"
}

foreach ($componentGroup in $components)
{
    $moduleName = $componentGroup.ComponentEntry.ModuleName
    $modulePath = Join-Path $bundleRoot ($moduleName.Replace('/', '\').TrimStart('.', '\'))
    if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf))
    {
        throw "ModuleName no resuelve a la DLL empaquetada: $modulePath"
    }
}

New-DeterministicZip $distributionRoot $archivePath
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$pluginHashes = @($runtimeTargets | ForEach-Object {
    $source = Join-Path $_.Source "CivilSpellAI.dll"
    "$($_.Year)=$((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash)"
})
$checksumLine = "$archiveHash *$([IO.Path]::GetFileName($archivePath))`r`n"
[IO.File]::WriteAllText($checksumPath, $checksumLine, [Text.UTF8Encoding]::new($false))

& (Join-Path $PSScriptRoot "Test-ReleaseArtifact.ps1") `
    -ArchivePath $archivePath `
    -ChecksumPath $checksumPath

Write-Host ""
Write-Host "PAQUETE PILOTO CORRECTO"
Write-Host "Versión: $Version"
Write-Host "Carpeta: $distributionRoot"
Write-Host "ZIP: $archivePath"
Write-Host "Checksum: $checksumPath"
Write-Host "SHA-256 DLL: $($pluginHashes -join '; ')"
Write-Host "SHA-256 ZIP: $archiveHash"
