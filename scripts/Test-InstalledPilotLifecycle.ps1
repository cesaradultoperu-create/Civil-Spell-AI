param(
    [Parameter(Mandatory = $true)]
    [string]$ManagerPath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedRollbackHash,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedFinalHash
)

$ErrorActionPreference = "Stop"

$manager = (Resolve-Path -LiteralPath $ManagerPath).Path
$targetBundle = Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins\CivilSpellAI.bundle"
$installedDll = Join-Path $targetBundle "Contents\Windows\CivilSpellAI.dll"
$localData = Join-Path $env:LOCALAPPDATA "CivilSpellAI"

function Get-LocalDataSnapshot
{
    $snapshot = @{}

    Get-ChildItem -LiteralPath $localData -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notlike "*\deployment-backups\*" } |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($localData.Length)
            $snapshot[$relativePath] = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash
        }

    return $snapshot
}

function Invoke-Manager
{
    param([Parameter(Mandatory = $true)][string]$Action)

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $manager `
        -Action $Action -Scope AllUsers

    if ($LASTEXITCODE -ne 0)
    {
        throw "$Action failed with exit code $LASTEXITCODE."
    }
}

function Assert-InstalledHash
{
    param(
        [Parameter(Mandatory = $true)][string]$ExpectedHash,
        [Parameter(Mandatory = $true)][string]$Stage
    )

    if (-not (Test-Path -LiteralPath $installedDll -PathType Leaf))
    {
        throw "$Stage did not leave an installed DLL."
    }

    $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $installedDll).Hash
    if ($actualHash -ne $ExpectedHash)
    {
        throw "$Stage hash mismatch. Expected $ExpectedHash; found $actualHash."
    }
}

if (Get-Process -Name acad -ErrorAction SilentlyContinue)
{
    throw "Close Civil 3D/AutoCAD before validating the lifecycle."
}

$before = Get-LocalDataSnapshot

Invoke-Manager -Action "Rollback"
Assert-InstalledHash -ExpectedHash $ExpectedRollbackHash -Stage "Rollback"
Write-Output "ROLLBACK_MATCH=True"

Invoke-Manager -Action "Uninstall"
if (Test-Path -LiteralPath $targetBundle)
{
    throw "Uninstall left the target bundle in place."
}
Write-Output "UNINSTALL_REMOVED=True"

Invoke-Manager -Action "Install"
Assert-InstalledHash -ExpectedHash $ExpectedFinalHash -Stage "Reinstall"
Write-Output "REINSTALL_MATCH=True"

$after = Get-LocalDataSnapshot
if ($before.Count -ne $after.Count)
{
    throw "The local data file count changed from $($before.Count) to $($after.Count)."
}

foreach ($relativePath in $before.Keys)
{
    if (-not $after.ContainsKey($relativePath) -or
        $after[$relativePath] -ne $before[$relativePath])
    {
        throw "Local data changed: $relativePath"
    }
}

Write-Output "LOCAL_DATA_UNCHANGED=True FILES=$($after.Count)"
Write-Output "PILOT_LIFECYCLE_VALIDATION=PASS"
