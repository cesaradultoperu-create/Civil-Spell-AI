param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Install", "Update", "Rollback", "Uninstall")]
    [string]$Action,

    [ValidateSet("CurrentUser", "AllUsers")]
    [string]$Scope = "CurrentUser"
)

$ErrorActionPreference = "Stop"
$bundleName = "CivilSpellAI.bundle"
$sourceBundle = Join-Path $PSScriptRoot $bundleName
$localData = $env:LOCALAPPDATA

if ([string]::IsNullOrWhiteSpace($localData) -or
    -not [System.IO.Path]::IsPathRooted($localData))
{
    throw "LOCALAPPDATA no contiene una ruta absoluta válida."
}

$pluginsRoot = if ($Scope -eq "AllUsers") {
    Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins"
} else {
    Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
}
$targetBundle = Join-Path $pluginsRoot $bundleName
$backupRoot = Join-Path $localData "CivilSpellAI\deployment-backups\$Scope"
$otherPluginsRoot = if ($Scope -eq "AllUsers") {
    Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
} else {
    Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins"
}
$otherBundle = Join-Path $otherPluginsRoot $bundleName

function Assert-BundleTarget([string]$path)
{
    if ([System.IO.Path]::GetFileName($path) -ne $bundleName)
    {
        throw "Destino de bundle no válido: $path"
    }
}

function Assert-ChildPath([string]$candidate, [string]$parent)
{
    $candidateFull = [System.IO.Path]::GetFullPath($candidate)
    $parentFull = [System.IO.Path]::GetFullPath($parent).TrimEnd('\') + '\'

    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase))
    {
        throw "La ruta queda fuera del directorio autorizado: $candidateFull"
    }
}

function Get-BundleFileHashes([string]$bundlePath)
{
    $bundleFull = [System.IO.Path]::GetFullPath($bundlePath)
    $bundlePrefix = $bundleFull.TrimEnd('\', '/') + '\'
    $hashes = @{}

    Get-ChildItem -LiteralPath $bundleFull -Recurse -File |
        ForEach-Object {
            $fileFull = [System.IO.Path]::GetFullPath($_.FullName)
            if (-not $fileFull.StartsWith(
                $bundlePrefix,
                [StringComparison]::OrdinalIgnoreCase))
            {
                throw "Un archivo queda fuera del bundle esperado: $fileFull"
            }

            $relativePath = $fileFull.Substring($bundlePrefix.Length)
            $hashes[$relativePath] = (Get-FileHash `
                -Algorithm SHA256 `
                -LiteralPath $fileFull).Hash
        }

    return $hashes
}

function Assert-BundleCopyMatches(
    [string]$sourcePath,
    [string]$destinationPath)
{
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Container) -or
        -not (Test-Path -LiteralPath $destinationPath -PathType Container))
    {
        throw "No se puede verificar una copia incompleta del bundle."
    }

    $sourceHashes = Get-BundleFileHashes $sourcePath
    $destinationHashes = Get-BundleFileHashes $destinationPath

    if ($sourceHashes.Count -ne $destinationHashes.Count)
    {
        throw "La copia instalada no contiene la misma cantidad de archivos que el origen."
    }

    foreach ($relativePath in $sourceHashes.Keys)
    {
        if (-not $destinationHashes.ContainsKey($relativePath) -or
            $destinationHashes[$relativePath] -ne $sourceHashes[$relativePath])
        {
            throw "La copia instalada no coincide con el origen: $relativePath"
        }
    }
}

function New-Backup([string]$prefix)
{
    if (-not (Test-Path -LiteralPath $targetBundle -PathType Container))
    {
        return $null
    }

    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $destination = Join-Path $backupRoot "$prefix-$stamp"
    Move-Item -LiteralPath $targetBundle -Destination $destination
    return $destination
}

Assert-BundleTarget $targetBundle
Assert-ChildPath $targetBundle $pluginsRoot
Assert-ChildPath $backupRoot $localData

if (Get-Process -Name acad -ErrorAction SilentlyContinue)
{
    throw "Cierre Civil 3D/AutoCAD antes de instalar, actualizar, revertir o desinstalar."
}

if ($Action -in @("Install", "Update"))
{
    if ($Scope -eq "CurrentUser" -and
        (Test-Path -LiteralPath $otherBundle -PathType Container))
    {
        throw "Ya existe una instalación administrada en $otherBundle. No cree una copia paralela por usuario."
    }

    if ($Scope -eq "AllUsers" -and
        (Test-Path -LiteralPath $otherBundle -PathType Container))
    {
        Write-Warning "Existe una copia por usuario en $otherBundle. Verifique primero esta instalación y después desinstale la copia CurrentUser."
    }

    if (-not (Test-Path -LiteralPath (Join-Path $sourceBundle "PackageContents.xml") -PathType Leaf))
    {
        throw "No se encontró el bundle junto a este script: $sourceBundle"
    }

    if ($Action -eq "Install" -and (Test-Path -LiteralPath $targetBundle))
    {
        throw "CivilSpellAI ya está instalado. Use -Action Update."
    }

    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
    $backup = New-Backup "backup"

    try
    {
        Copy-Item -LiteralPath $sourceBundle -Destination $targetBundle -Recurse
        Assert-BundleCopyMatches $sourceBundle $targetBundle
    }
    catch
    {
        if (Test-Path -LiteralPath $targetBundle)
        {
            Remove-Item -LiteralPath $targetBundle -Recurse -Force
        }

        if ($backup)
        {
            Move-Item -LiteralPath $backup -Destination $targetBundle
        }

        throw
    }

    Write-Host "CivilSpellAI instalado en: $targetBundle"
    if ($backup) { Write-Host "Versión anterior respaldada en: $backup" }
    Write-Host "Al abrir Civil 3D, pruebe AISPELLSETTINGS sin usar NETLOAD."
    Write-Host "Si el comando no se reconoce después de migrar o actualizar, ejecute APPAUTOLOADER y elija Reload una sola vez."
    exit 0
}

if ($Action -eq "Uninstall")
{
    $backup = New-Backup "backup"
    if ($backup)
    {
        Write-Host "CivilSpellAI desinstalado. Copia recuperable: $backup"
    }
    else
    {
        Write-Host "CivilSpellAI no estaba instalado en el alcance $Scope."
    }
    Write-Host "La configuración de usuario se conservó en: $(Join-Path $localData 'CivilSpellAI')"
    exit 0
}

$previous = Get-ChildItem -LiteralPath $backupRoot -Directory -Filter "backup-*" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $previous)
{
    throw "No existe una versión anterior para restaurar en $backupRoot"
}

$replaced = New-Backup "replaced"
New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
try
{
    Copy-Item -LiteralPath $previous.FullName -Destination $targetBundle -Recurse
    Assert-BundleCopyMatches $previous.FullName $targetBundle
}
catch
{
    if (Test-Path -LiteralPath $targetBundle)
    {
        Remove-Item -LiteralPath $targetBundle -Recurse -Force
    }

    if ($replaced)
    {
        Move-Item -LiteralPath $replaced -Destination $targetBundle
    }

    throw
}

Write-Host "Rollback completado desde: $($previous.FullName)"
if ($replaced) { Write-Host "Versión sustituida conservada en: $replaced" }
Write-Host "Al abrir Civil 3D, pruebe AISPELLSETTINGS sin usar NETLOAD."
Write-Host "Si el comando no se reconoce, ejecute APPAUTOLOADER y elija Reload una sola vez."
