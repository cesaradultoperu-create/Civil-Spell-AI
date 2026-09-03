param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,

    [string]$ChecksumPath
)

$ErrorActionPreference = "Stop"
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path

if ([string]::IsNullOrWhiteSpace($ChecksumPath))
{
    $ChecksumPath = "$archive.sha256"
}

$checksum = (Resolve-Path -LiteralPath $ChecksumPath).Path
$archiveName = [IO.Path]::GetFileName($archive)
$match = [Text.RegularExpressions.Regex]::Match(
    [IO.File]::ReadAllText($checksum).Trim(),
    '^(?<hash>[A-Fa-f0-9]{64})\s+\*?(?<file>[^\r\n]+)$')

if (-not $match.Success)
{
    throw "El archivo de checksum no tiene el formato SHA-256 esperado."
}

if (-not [string]::Equals(
    $match.Groups["file"].Value,
    $archiveName,
    [StringComparison]::Ordinal))
{
    throw "El checksum corresponde a otro archivo: $($match.Groups['file'].Value)"
}

$expectedHash = $match.Groups["hash"].Value.ToUpperInvariant()
$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash

if ($actualHash -ne $expectedHash)
{
    throw "La huella del ZIP no coincide. Esperada=$expectedHash; actual=$actualHash"
}

$archiveMatch = [Text.RegularExpressions.Regex]::Match(
    $archiveName,
    '^CivilSpellAI-(?<version>\d+\.\d+\.\d+\.\d+)\.zip$')

if (-not $archiveMatch.Success)
{
    throw "El ZIP no usa el nombre versionado esperado."
}

$version = $archiveMatch.Groups["version"].Value
$root = "CivilSpellAI-$version/"
$requiredEntries = @(
    "${root}CivilSpellAI.bundle/PackageContents.xml",
    "${root}CivilSpellAI.bundle/Contents/Windows/CivilSpellAI.dll",
    "${root}CivilSpellAI.bundle/Contents/Windows/Resources/technical-glossary.txt",
    "${root}CivilSpellAI.bundle/Contents/Resources/Help.html",
    "${root}Manage-CivilSpellAI.ps1",
    "${root}Test-CivilSpellAIEnvironment.ps1",
    "${root}New-PilotFixture.ps1",
    "${root}LEAME.md",
    "${root}REGRESION_PILOTO_1_1.md"
)
$forbiddenNames = @(
    "accoremgd.dll",
    "acdbmgd.dll",
    "acmgd.dll",
    "AeccDbMgd.dll"
)
$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
$tempDirectory = Join-Path $tempParent ("CivilSpellAI-release-verify-" + [Guid]::NewGuid().ToString("N"))

if (-not ([IO.Path]::GetFullPath($tempDirectory) + '\').StartsWith(
    $tempParent,
    [StringComparison]::OrdinalIgnoreCase))
{
    throw "La ruta temporal queda fuera del directorio autorizado."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)

try
{
    $entryNames = [Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)

    foreach ($entry in $zip.Entries)
    {
        $entryName = $entry.FullName
        $segments = $entryName.Split('/')

        if ($entryName.Contains('\') -or
            $entryName.StartsWith('/') -or
            $entryName -match '^[A-Za-z]:' -or
            $segments -contains '..')
        {
            throw "El ZIP contiene una ruta no segura: $entryName"
        }

        if (-not $entryNames.Add($entryName))
        {
            throw "El ZIP contiene una entrada duplicada: $entryName"
        }

        if (-not $entryName.StartsWith($root, [StringComparison]::Ordinal))
        {
            throw "El ZIP contiene una entrada fuera de la raiz esperada: $entryName"
        }

        if ($entry.Name -and $requiredEntries -notcontains $entryName)
        {
            throw "El ZIP contiene un archivo no previsto: $entryName"
        }

        if ($entry.Name -and
            ($forbiddenNames -contains $entry.Name -or
             [IO.Path]::GetExtension($entry.Name) -eq ".pdb"))
        {
            throw "El ZIP contiene un archivo prohibido: $entryName"
        }
    }

    foreach ($requiredEntry in $requiredEntries)
    {
        if (-not $entryNames.Contains($requiredEntry))
        {
            throw "Falta una entrada obligatoria: $requiredEntry"
        }
    }

    $manifestEntry = $zip.GetEntry("${root}CivilSpellAI.bundle/PackageContents.xml")
    $reader = [IO.StreamReader]::new($manifestEntry.Open())
    try
    {
        [xml]$manifest = $reader.ReadToEnd()
    }
    finally
    {
        $reader.Dispose()
    }

    $package = $manifest.ApplicationPackage
    $component = $package.Components.ComponentEntry
    $requirements = $package.Components.RuntimeRequirements
    $commands = @($component.Commands.Command | ForEach-Object { $_.Global })
    $expectedCommands = @("AISPELL", "AISPELLALL", "AISPELLSETTINGS")

    if ($package.AppVersion -ne $version -or
        $requirements.OS -ne "Win64" -or
        $requirements.Platform -ne "Civil3D" -or
        $requirements.SeriesMin -ne "R24.3" -or
        $requirements.SeriesMax -ne "R24.3" -or
        $component.LoadReasons -ne "LoadOnCommandInvocation" -or
        (Compare-Object $expectedCommands $commands))
    {
        throw "El manifiesto no coincide con version, plataforma o comandos esperados."
    }

    $runbookEntry = $zip.GetEntry("${root}REGRESION_PILOTO_1_1.md")
    $reader = [IO.StreamReader]::new($runbookEntry.Open())
    try
    {
        $runbook = $reader.ReadToEnd()
    }
    finally
    {
        $reader.Dispose()
    }

    $fixtureCommand =
        'powershell -NoProfile -ExecutionPolicy Bypass -File .\New-PilotFixture.ps1 -Force'
    $reinstallInstruction =
        "8. Reinstalar $version para continuar la matriz con el candidato autorizado."

    if (-not $runbook.Contains($fixtureCommand))
    {
        throw "El runbook no usa la ruta empaquetada de New-PilotFixture.ps1."
    }

    if (-not $runbook.Contains($reinstallInstruction))
    {
        throw "El runbook no reinstala el candidato autorizado $version."
    }

    [IO.Directory]::CreateDirectory($tempDirectory) | Out-Null
    $temporaryDll = Join-Path $tempDirectory "CivilSpellAI.dll"
    $dllEntry = $zip.GetEntry("${root}CivilSpellAI.bundle/Contents/Windows/CivilSpellAI.dll")
    $sourceStream = $dllEntry.Open()
    $destinationStream = [IO.File]::Open(
        $temporaryDll,
        [IO.FileMode]::CreateNew,
        [IO.FileAccess]::Write,
        [IO.FileShare]::None)

    try
    {
        $sourceStream.CopyTo($destinationStream)
    }
    finally
    {
        $destinationStream.Dispose()
        $sourceStream.Dispose()
    }

    $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($temporaryDll).Version.ToString()
    if ($assemblyVersion -ne $version)
    {
        throw "La DLL usa la version $assemblyVersion y el ZIP declara $version."
    }
}
finally
{
    $zip.Dispose()

    if (Test-Path -LiteralPath $tempDirectory)
    {
        $resolvedTemporary = [IO.Path]::GetFullPath($tempDirectory)
        if (-not ($resolvedTemporary + '\').StartsWith(
            $tempParent,
            [StringComparison]::OrdinalIgnoreCase))
        {
            throw "No se eliminara una ruta temporal fuera del directorio autorizado."
        }

        Remove-Item -LiteralPath $resolvedTemporary -Recurse -Force
    }
}

Write-Host "ARTEFACTO RELEASE CORRECTO"
Write-Host "Version: $version"
Write-Host "ZIP: $archive"
Write-Host "SHA-256: $actualHash"
