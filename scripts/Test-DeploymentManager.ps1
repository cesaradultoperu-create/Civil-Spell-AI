$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$validationParent = Join-Path $repoRoot "artifacts\validation"
[IO.Directory]::CreateDirectory($validationParent) | Out-Null
$testRoot = Join-Path $validationParent ("deployment-manager-test-" + [Guid]::NewGuid().ToString("N"))
$validationPrefix = [IO.Path]::GetFullPath($validationParent).TrimEnd('\') + '\'
$testRootFull = [IO.Path]::GetFullPath($testRoot)

if (-not ($testRootFull + '\').StartsWith(
    $validationPrefix,
    [StringComparison]::OrdinalIgnoreCase))
{
    throw "The test directory is outside the authorized validation folder."
}

$packageRoot = Join-Path $testRootFull "package"
$sourceBundle = Join-Path $packageRoot "CivilSpellAI.bundle"
$sourceWindows = Join-Path $sourceBundle "Contents\Windows"
$programFilesRoot = Join-Path $testRootFull "Program Files"
$appDataRoot = Join-Path $testRootFull "AppData\Roaming"
$localDataRoot = Join-Path $testRootFull "AppData\Local"
$targetBundle = Join-Path $programFilesRoot "Autodesk\ApplicationPlugins\CivilSpellAI.bundle"
$targetDll = Join-Path $targetBundle "Contents\Windows\CivilSpellAI.dll"
$backupRoot = Join-Path $localDataRoot "CivilSpellAI\deployment-backups\AllUsers"
$manager = Join-Path $packageRoot "Manage-CivilSpellAI.ps1"
$runner = Join-Path $testRootFull "Invoke-Manager.ps1"
$originalProgramFiles = $env:ProgramFiles
$originalAppData = $env:APPDATA
$originalLocalData = $env:LOCALAPPDATA
$originalFailureSwitch = $env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY

function Write-TestFile([string]$path, [string]$content)
{
    $directory = [IO.Path]::GetDirectoryName($path)
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false))
}

function Invoke-TestManager(
    [string]$action,
    [bool]$expectSuccess)
{
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass `
        -File $runner `
        -ManagerPath $manager `
        -ManagerAction $action
    $exitCode = $LASTEXITCODE

    if ($expectSuccess -and $exitCode -ne 0)
    {
        throw "$action failed with exit code $exitCode."
    }

    if (-not $expectSuccess -and $exitCode -eq 0)
    {
        throw "$action was expected to fail."
    }
}

function Assert-FileContent([string]$path, [string]$expected)
{
    if (-not (Test-Path -LiteralPath $path -PathType Leaf))
    {
        throw "Expected file not found: $path"
    }

    $actual = [IO.File]::ReadAllText($path)
    if ($actual -ne $expected)
    {
        throw "Unexpected content in: $path"
    }
}

[IO.Directory]::CreateDirectory($sourceWindows) | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "Manage-CivilSpellAI.ps1") `
    -Destination $manager
Write-TestFile (Join-Path $sourceBundle "PackageContents.xml") "manifest-v1"
Write-TestFile (Join-Path $sourceWindows "CivilSpellAI.dll") "plugin-v1"

$runnerContent = @'
param(
    [Parameter(Mandatory = $true)][string]$ManagerPath,
    [Parameter(Mandatory = $true)][string]$ManagerAction
)

function Get-Process
{
    param([string]$Name, [object]$ErrorAction)
    return $null
}

function Copy-Item
{
    param(
        [Parameter(Mandatory = $true)][string]$LiteralPath,
        [Parameter(Mandatory = $true)][string]$Destination,
        [switch]$Recurse
    )

    if ($env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY -eq "1" -and
        $LiteralPath -like "*\deployment-backups\*\backup-*")
    {
        [IO.Directory]::CreateDirectory($Destination) | Out-Null
        [IO.File]::WriteAllText(
            (Join-Path $Destination "partial.txt"),
            "partial")
        throw "Injected rollback copy failure."
    }

    Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters
}

& $ManagerPath -Action $ManagerAction -Scope AllUsers
'@
Write-TestFile $runner $runnerContent

try
{
    $env:ProgramFiles = $programFilesRoot
    $env:APPDATA = $appDataRoot
    $env:LOCALAPPDATA = $localDataRoot
    $env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY = "0"

    Invoke-TestManager "Install" $true
    Assert-FileContent $targetDll "plugin-v1"
    Write-Host "PASS: isolated install verified."

    Write-TestFile (Join-Path $sourceBundle "PackageContents.xml") "manifest-v2"
    Write-TestFile (Join-Path $sourceWindows "CivilSpellAI.dll") "plugin-v2"
    Invoke-TestManager "Update" $true
    Assert-FileContent $targetDll "plugin-v2"

    $backup = Get-ChildItem -LiteralPath $backupRoot -Directory -Filter "backup-*" |
        Select-Object -First 1
    if (-not $backup)
    {
        throw "Update did not create a rollback backup."
    }

    Assert-FileContent `
        (Join-Path $backup.FullName "Contents\Windows\CivilSpellAI.dll") `
        "plugin-v1"
    Write-Host "PASS: isolated update and backup verified."

    $env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY = "1"
    Invoke-TestManager "Rollback" $false
    Assert-FileContent $targetDll "plugin-v2"
    Write-Host "PASS: failed rollback restored the current bundle."

    $env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY = "0"
    Invoke-TestManager "Rollback" $true
    Assert-FileContent $targetDll "plugin-v1"
    Write-Host "PASS: isolated rollback verified."

    Invoke-TestManager "Uninstall" $true
    if (Test-Path -LiteralPath $targetBundle)
    {
        throw "Uninstall left the isolated target bundle in place."
    }

    Write-Host "PASS: isolated uninstall verified."
}
finally
{
    $env:ProgramFiles = $originalProgramFiles
    $env:APPDATA = $originalAppData
    $env:LOCALAPPDATA = $originalLocalData
    $env:CIVILSPELLAI_TEST_FAIL_ROLLBACK_COPY = $originalFailureSwitch

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

Write-Host "DEPLOYMENT MANAGER TESTS PASSED"
