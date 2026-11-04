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
    "${root}CivilSpellAI.bundle/Contents/Resources/Help.html",
    "${root}Manage-CivilSpellAI.ps1",
    "${root}Test-CivilSpellAIEnvironment.ps1",
    "${root}New-PilotFixture.ps1",
    "${root}LEAME.md",
    "${root}REGRESION_PILOTO_1_2.md",
    "${root}GUIA_CORREGIR_SOLAPES.md"
)
$runtimeTargets = @(
    [pscustomobject]@{ Year = "2024"; Series = "R24.3"; RequiresDeps = $false },
    [pscustomobject]@{ Year = "2025"; Series = "R25.0"; RequiresDeps = $true },
    [pscustomobject]@{ Year = "2026"; Series = "R25.1"; RequiresDeps = $true }
)
$runtimeFiles = @(
    "CivilSpellAI.dll",
    "WeCantSpell.Hunspell.dll",
    "NetTopologySuite.dll",
    "THIRD_PARTY_NOTICES.md",
    "Resources/technical-glossary.txt",
    "Resources/Dictionaries/es_PE.aff",
    "Resources/Dictionaries/es_PE.dic",
    "Resources/Dictionaries/en_US.aff",
    "Resources/Dictionaries/en_US.dic",
    "Resources/Dictionaries/LICENSE.md",
    "Resources/Dictionaries/README_hunspell_es.txt",
    "Resources/Dictionaries/README_hunspell_en_US.txt",
    "Resources/Dictionaries/SOURCE.md",
    "Resources/Licenses/MPL-1.1.txt",
    "Resources/Licenses/Hunspell-en_US-license.txt",
    "Resources/Licenses/WeCantSpell.Hunspell-2.0.1-license.txt",
    "Resources/Licenses/TinBoundary/NetTopologySuite-License.md",
    "Resources/Licenses/TinBoundary/DotNetRuntime-License.txt",
    "Resources/Licenses/TinBoundary/THIRD_PARTY_NOTICES.md"
)

foreach ($runtime in $runtimeTargets)
{
    $runtimeRoot = "${root}CivilSpellAI.bundle/Contents/Windows/$($runtime.Year)/"
    $requiredEntries += @($runtimeFiles | ForEach-Object { "$runtimeRoot$_" })
    if ($runtime.Year -eq "2024")
    {
        $requiredEntries += @(
            "${runtimeRoot}System.Memory.dll",
            "${runtimeRoot}System.Buffers.dll",
            "${runtimeRoot}System.Numerics.Vectors.dll",
            "${runtimeRoot}System.Runtime.CompilerServices.Unsafe.dll"
        )
    }
    if ($runtime.RequiresDeps)
    {
        $requiredEntries += "${runtimeRoot}CivilSpellAI.deps.json"
    }
}
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

    if ($package.AppVersion -ne $version -or
        $components.Count -ne $runtimeTargets.Count)
    {
        throw "El manifiesto no coincide con version, plataforma o comandos esperados."
    }

    foreach ($runtime in $runtimeTargets)
    {
        $matches = @($components | Where-Object {
            $_.RuntimeRequirements.SeriesMin -eq $runtime.Series -and
            $_.RuntimeRequirements.SeriesMax -eq $runtime.Series
        })

        if ($matches.Count -ne 1)
        {
            throw "El manifiesto no contiene una entrada única para Civil 3D $($runtime.Year)."
        }

        $requirements = $matches[0].RuntimeRequirements
        $component = $matches[0].ComponentEntry
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
    }

    $runbookEntry = $zip.GetEntry("${root}REGRESION_PILOTO_1_2.md")
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
    foreach ($runtime in $runtimeTargets)
    {
        $temporaryDll = Join-Path $tempDirectory "CivilSpellAI-$($runtime.Year).dll"
        $dllEntry = $zip.GetEntry("${root}CivilSpellAI.bundle/Contents/Windows/$($runtime.Year)/CivilSpellAI.dll")
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
            throw "La DLL de Civil 3D $($runtime.Year) usa $assemblyVersion y el ZIP declara $version."
        }
    }

    $dictionaryEntry = $zip.GetEntry(
        "${root}CivilSpellAI.bundle/Contents/Windows/2024/Resources/Dictionaries/es_PE.dic")
    $dictionaryStream = $dictionaryEntry.Open()
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try
    {
        $dictionaryHash = -join ($sha256.ComputeHash($dictionaryStream) |
            ForEach-Object { $_.ToString("X2") })
    }
    finally
    {
        $sha256.Dispose()
        $dictionaryStream.Dispose()
    }

    if ($dictionaryHash -ne "1D810A0A319FEE329000538CF4DF76621BB71228172558B87490F8B9C19B9D75")
    {
        throw "El diccionario es_PE no coincide con la revision autorizada."
    }

    $spanishAffixEntry = $zip.GetEntry(
        "${root}CivilSpellAI.bundle/Contents/Windows/2024/Resources/Dictionaries/es_PE.aff")
    $spanishAffixStream = $spanishAffixEntry.Open()
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try
    {
        $spanishAffixHash = -join ($sha256.ComputeHash($spanishAffixStream) |
            ForEach-Object { $_.ToString("X2") })
    }
    finally
    {
        $sha256.Dispose()
        $spanishAffixStream.Dispose()
    }

    if ($spanishAffixHash -ne "D966CB748E4A688ED75EC84B50C0835AC438E5325EE4FCE703A093794F9CBA7E")
    {
        throw "Los afijos es_PE no coinciden con la revision autorizada."
    }

    $englishDictionaryEntry = $zip.GetEntry(
        "${root}CivilSpellAI.bundle/Contents/Windows/2024/Resources/Dictionaries/en_US.dic")
    $englishDictionaryStream = $englishDictionaryEntry.Open()
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try
    {
        $englishDictionaryHash = -join ($sha256.ComputeHash($englishDictionaryStream) |
            ForEach-Object { $_.ToString("X2") })
    }
    finally
    {
        $sha256.Dispose()
        $englishDictionaryStream.Dispose()
    }

    if ($englishDictionaryHash -ne "F0B1A234BD178BDD01875B2A392A9647F888B8FE879F79C52AAE62C2759B3647")
    {
        throw "El diccionario en_US no coincide con la revision autorizada."
    }

    $englishAffixEntry = $zip.GetEntry(
        "${root}CivilSpellAI.bundle/Contents/Windows/2024/Resources/Dictionaries/en_US.aff")
    $englishAffixStream = $englishAffixEntry.Open()
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try
    {
        $englishAffixHash = -join ($sha256.ComputeHash($englishAffixStream) |
            ForEach-Object { $_.ToString("X2") })
    }
    finally
    {
        $sha256.Dispose()
        $englishAffixStream.Dispose()
    }

    if ($englishAffixHash -ne "E746C882DD6F303C2C46E7452804B9201115A6942CFEB15F18F8EDF774D2E24E")
    {
        throw "Los afijos en_US no coinciden con la revision autorizada."
    }

    $noticeEntry = $zip.GetEntry(
        "${root}CivilSpellAI.bundle/Contents/Windows/2024/THIRD_PARTY_NOTICES.md")
    $reader = [IO.StreamReader]::new($noticeEntry.Open())
    try
    {
        $thirdPartyNotices = $reader.ReadToEnd()
    }
    finally
    {
        $reader.Dispose()
    }

    if (-not $thirdPartyNotices.Contains("Mozilla Public License 1.1") -or
        -not $thirdPartyNotices.Contains("Diccionario Hunspell en_US") -or
        -not $thirdPartyNotices.Contains("WeCantSpell.Hunspell 2.0.1"))
    {
        throw "Los avisos de terceros no identifican el motor y su licencia."
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
