param(
    [string]$ArchivePath,
    [string]$ChecksumPath
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$verifier = Join-Path $PSScriptRoot "Test-ReleaseArtifact.ps1"

if ([string]::IsNullOrWhiteSpace($ArchivePath))
{
    $ArchivePath = Join-Path $repoRoot "artifacts\distribution\CivilSpellAI-1.1.4.0.zip"
}

$archive = (Resolve-Path -LiteralPath $ArchivePath).Path

if ([string]::IsNullOrWhiteSpace($ChecksumPath))
{
    $ChecksumPath = "$archive.sha256"
}

$checksum = (Resolve-Path -LiteralPath $ChecksumPath).Path
$validationParent = Join-Path $repoRoot "artifacts\validation"
[IO.Directory]::CreateDirectory($validationParent) | Out-Null
$testRoot = Join-Path $validationParent ("release-verifier-test-" + [Guid]::NewGuid().ToString("N"))
$validationPrefix = [IO.Path]::GetFullPath($validationParent).TrimEnd('\') + '\'
$testRootFull = [IO.Path]::GetFullPath($testRoot)

function New-RunbookMutation(
    [string]$sourceArchive,
    [string]$destinationDirectory,
    [string]$entryName,
    [string]$expectedText,
    [string]$replacementText)
{
    [IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null
    $destinationArchive = Join-Path $destinationDirectory ([IO.Path]::GetFileName($sourceArchive))
    Copy-Item -LiteralPath $sourceArchive -Destination $destinationArchive
    $mutatedZip = [IO.Compression.ZipFile]::Open(
        $destinationArchive,
        [IO.Compression.ZipArchiveMode]::Update)

    try
    {
        $runbookEntry = $mutatedZip.GetEntry($entryName)
        $reader = [IO.StreamReader]::new($runbookEntry.Open())
        try
        {
            $runbook = $reader.ReadToEnd()
        }
        finally
        {
            $reader.Dispose()
        }

        if (-not $runbook.Contains($expectedText))
        {
            throw "The expected runbook text was not found before mutation."
        }

        $mutatedRunbook = $runbook.Replace($expectedText, $replacementText)
        $runbookEntry.Delete()
        $replacementEntry = $mutatedZip.CreateEntry($entryName)
        $writer = [IO.StreamWriter]::new(
            $replacementEntry.Open(),
            [Text.UTF8Encoding]::new($false))
        try
        {
            $writer.Write($mutatedRunbook)
        }
        finally
        {
            $writer.Dispose()
        }
    }
    finally
    {
        $mutatedZip.Dispose()
    }

    $destinationChecksum = "$destinationArchive.sha256"
    $destinationHash =
        (Get-FileHash -LiteralPath $destinationArchive -Algorithm SHA256).Hash
    $checksumLine = $destinationHash + " *" +
        [IO.Path]::GetFileName($destinationArchive) + "`r`n"
    [IO.File]::WriteAllText(
        $destinationChecksum,
        $checksumLine,
        [Text.UTF8Encoding]::new($false))

    return [PSCustomObject]@{
        Archive = $destinationArchive
        Checksum = $destinationChecksum
    }
}

function Assert-ArchiveRejected(
    [string]$verifierPath,
    [string]$candidateArchive,
    [string]$candidateChecksum,
    [string]$failureMessage)
{
    $rejected = $false
    try
    {
        & $verifierPath `
            -ArchivePath $candidateArchive `
            -ChecksumPath $candidateChecksum
    }
    catch
    {
        $rejected = $true
    }

    if (-not $rejected)
    {
        throw $failureMessage
    }
}

if (-not ($testRootFull + '\').StartsWith(
    $validationPrefix,
    [StringComparison]::OrdinalIgnoreCase))
{
    throw "The test directory is outside the authorized validation folder."
}

[IO.Directory]::CreateDirectory($testRootFull) | Out-Null
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

try
{
    & $verifier -ArchivePath $archive -ChecksumPath $checksum
    Write-Host "PASS: valid release accepted."

    $archiveRoot = [IO.Path]::GetFileNameWithoutExtension($archive)
    $archiveVersion = $archiveRoot.Substring("CivilSpellAI-".Length)
    $runbookEntryName = "$archiveRoot/REGRESION_PILOTO_1_1.md"
    $fixtureMutation = New-RunbookMutation `
        -sourceArchive $archive `
        -destinationDirectory (Join-Path $testRootFull "invalid-fixture-path") `
        -entryName $runbookEntryName `
        -expectedText '-File .\New-PilotFixture.ps1 -Force' `
        -replacementText '-File scripts\New-PilotFixture.ps1 -Force'
    Assert-ArchiveRejected `
        -verifierPath $verifier `
        -candidateArchive $fixtureMutation.Archive `
        -candidateChecksum $fixtureMutation.Checksum `
        -failureMessage "The verifier accepted an invalid packaged fixture path."
    Write-Host "PASS: invalid packaged fixture path rejected."

    $reinstallMutation = New-RunbookMutation `
        -sourceArchive $archive `
        -destinationDirectory (Join-Path $testRootFull "invalid-reinstall-version") `
        -entryName $runbookEntryName `
        -expectedText "Reinstalar $archiveVersion para continuar" `
        -replacementText "Reinstalar 0.0.0.0 para continuar"
    Assert-ArchiveRejected `
        -verifierPath $verifier `
        -candidateArchive $reinstallMutation.Archive `
        -candidateChecksum $reinstallMutation.Checksum `
        -failureMessage "The verifier accepted an obsolete reinstall version."
    Write-Host "PASS: obsolete reinstall version rejected."

    $wrongNameArchive = Join-Path $testRootFull "CivilSpellAI-9.9.9.9.zip"
    $wrongNameChecksum = "$wrongNameArchive.sha256"
    Copy-Item -LiteralPath $archive -Destination $wrongNameArchive
    Copy-Item -LiteralPath $checksum -Destination $wrongNameChecksum

    $wrongNameRejected = $false
    try
    {
        & $verifier `
            -ArchivePath $wrongNameArchive `
            -ChecksumPath $wrongNameChecksum
    }
    catch
    {
        $wrongNameRejected = $true
    }

    if (-not $wrongNameRejected)
    {
        throw "The verifier accepted a checksum that names another archive."
    }

    Write-Host "PASS: inconsistent archive name rejected."

    $badHashArchive = Join-Path $testRootFull ([IO.Path]::GetFileName($archive))
    $badHashChecksum = "$badHashArchive.sha256"
    Copy-Item -LiteralPath $archive -Destination $badHashArchive
    $badHashLine = ("0" * 64) + " *" + [IO.Path]::GetFileName($badHashArchive) + "`r`n"
    [IO.File]::WriteAllText(
        $badHashChecksum,
        $badHashLine,
        [Text.UTF8Encoding]::new($false))

    $badHashRejected = $false
    try
    {
        & $verifier `
            -ArchivePath $badHashArchive `
            -ChecksumPath $badHashChecksum
    }
    catch
    {
        $badHashRejected = $true
    }

    if (-not $badHashRejected)
    {
        throw "The verifier accepted an invalid SHA-256 value."
    }

    Write-Host "PASS: invalid SHA-256 rejected."

    $extraFileArchive = Join-Path $testRootFull "CivilSpellAI-1.1.4.0.zip"
    $extraFileChecksum = "$extraFileArchive.sha256"
    Copy-Item -LiteralPath $archive -Destination $extraFileArchive -Force
    $extraFileZip = [IO.Compression.ZipFile]::Open(
        $extraFileArchive,
        [IO.Compression.ZipArchiveMode]::Update)

    try
    {
        $extraEntry = $extraFileZip.CreateEntry("unexpected.txt")
        $extraWriter = [IO.StreamWriter]::new($extraEntry.Open())
        try
        {
            $extraWriter.Write("This file must make the release invalid.")
        }
        finally
        {
            $extraWriter.Dispose()
        }
    }
    finally
    {
        $extraFileZip.Dispose()
    }

    $extraFileHash = (Get-FileHash -LiteralPath $extraFileArchive -Algorithm SHA256).Hash
    $extraFileLine = $extraFileHash + " *" + [IO.Path]::GetFileName($extraFileArchive) + "`r`n"
    [IO.File]::WriteAllText(
        $extraFileChecksum,
        $extraFileLine,
        [Text.UTF8Encoding]::new($false))

    $extraFileRejected = $false
    try
    {
        & $verifier `
            -ArchivePath $extraFileArchive `
            -ChecksumPath $extraFileChecksum
    }
    catch
    {
        $extraFileRejected = $true
    }

    if (-not $extraFileRejected)
    {
        throw "The verifier accepted an unexpected file inside the ZIP."
    }

    Write-Host "PASS: unexpected ZIP file rejected."
}
finally
{
    if (Test-Path -LiteralPath $testRootFull)
    {
        if (-not ($testRootFull + '\').StartsWith(
            $validationPrefix,
            [StringComparison]::OrdinalIgnoreCase))
        {
            throw "The test cleanup target is outside the authorized validation folder."
        }

        Remove-Item -LiteralPath $testRootFull -Recurse -Force
    }
}

Write-Host "RELEASE VERIFIER TESTS PASSED"
