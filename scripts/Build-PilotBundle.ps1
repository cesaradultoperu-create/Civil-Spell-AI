param(
    [string]$Version = "1.1.4.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$manifestSource = Join-Path $repoRoot "packaging\PackageContents.xml"
$helpSource = Join-Path $repoRoot "packaging\Help.html"
$regressionRunbookSource = Join-Path $repoRoot "docs\testing\PILOT_1_1_REGRESSION_RUNBOOK.md"
$releaseOutput = Join-Path $repoRoot "bin\x64\Release"
$pluginSource = Join-Path $releaseOutput "CivilSpellAI.dll"
$glossarySource = Join-Path $releaseOutput "Resources\technical-glossary.txt"
$distributionParent = Join-Path $repoRoot "artifacts\distribution"
$distributionRoot = Join-Path $distributionParent "CivilSpellAI-$Version"
$bundleRoot = Join-Path $distributionRoot "CivilSpellAI.bundle"
$windowsContents = Join-Path $bundleRoot "Contents\Windows"
$glossaryContents = Join-Path $windowsContents "Resources"
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

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$')
{
    throw "La versión debe usar cuatro componentes numéricos."
}

& (Join-Path $PSScriptRoot "Validate-Mvp.ps1") `
    -Configuration Release `
    -ReportPath $validationReport

[xml]$manifest = Get-Content -Raw -LiteralPath $manifestSource
$package = $manifest.ApplicationPackage
$component = $package.Components.ComponentEntry
$requirements = $package.Components.RuntimeRequirements
$commands = @($component.Commands.Command | ForEach-Object { $_.Global })
$expectedCommands = @("AISPELL", "AISPELLALL", "AISPELLSETTINGS")
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginSource).Version.ToString()

if ($assemblyVersion -ne $Version -or $package.AppVersion -ne $Version)
{
    throw "La versión debe coincidir en AssemblyInfo, manifiesto y parámetro: ensamblado=$assemblyVersion, manifiesto=$($package.AppVersion), solicitada=$Version."
}

if ($requirements.OS -ne "Win64" -or
    $requirements.Platform -ne "Civil3D" -or
    $requirements.SeriesMin -ne "R24.3" -or
    $requirements.SeriesMax -ne "R24.3")
{
    throw "El manifiesto no está limitado a Civil 3D 2024 x64 (R24.3)."
}

if ($component.LoadReasons -ne "LoadOnCommandInvocation" -or
    (Compare-Object $expectedCommands $commands))
{
    throw "El manifiesto debe registrar únicamente los tres comandos públicos con carga diferida."
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
New-Item -ItemType Directory -Path $glossaryContents -Force | Out-Null
New-Item -ItemType Directory -Path $resourceContents -Force | Out-Null
Copy-Item -LiteralPath $manifestSource -Destination (Join-Path $bundleRoot "PackageContents.xml")
Copy-Item -LiteralPath $pluginSource -Destination (Join-Path $windowsContents "CivilSpellAI.dll")
Copy-Item -LiteralPath $glossarySource -Destination (Join-Path $glossaryContents "technical-glossary.txt") -Force
Copy-Item -LiteralPath $helpSource -Destination (Join-Path $resourceContents "Help.html")
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Manage-CivilSpellAI.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Test-CivilSpellAIEnvironment.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "New-PilotFixture.ps1") -Destination $distributionRoot
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\PILOT_INSTALLATION.md") -Destination (Join-Path $distributionRoot "LEAME.md")
Copy-Item -LiteralPath $regressionRunbookSource -Destination (Join-Path $distributionRoot "REGRESION_PILOTO_1_1.md")

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

$modulePath = Join-Path $bundleRoot ($component.ModuleName.Replace('/', '\').TrimStart('.', '\'))
if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf))
{
    throw "ModuleName no resuelve a la DLL empaquetada: $modulePath"
}

New-DeterministicZip $distributionRoot $archivePath
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$pluginHash = (Get-FileHash -LiteralPath $pluginSource -Algorithm SHA256).Hash
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
Write-Host "SHA-256 DLL: $pluginHash"
Write-Host "SHA-256 ZIP: $archiveHash"
