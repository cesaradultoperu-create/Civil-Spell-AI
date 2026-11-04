param(
    [Parameter(Mandatory = $true)][string]$StagingPath,
    [Parameter(Mandatory = $true)][string]$DestinationPath
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $StagingPath).Path.TrimEnd('\')
$destinationRoot = (Resolve-Path -LiteralPath $DestinationPath).Path.TrimEnd('\')
if ($sourceRoot -eq $destinationRoot) { throw 'Origen y destino deben ser distintos.' }
$manifest = Get-Content -LiteralPath (Join-Path $sourceRoot 'SHA256.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$entries = @($manifest.Files) + @([PSCustomObject]@{
    Path = 'SHA256.json'
    SHA256 = (Get-FileHash -LiteralPath (Join-Path $sourceRoot 'SHA256.json') -Algorithm SHA256).Hash
})
# Preflight every file before writing; never overwrite different existing contents.
foreach ($entry in $entries) {
    $source = [IO.Path]::GetFullPath((Join-Path $sourceRoot $entry.Path))
    $target = [IO.Path]::GetFullPath((Join-Path $destinationRoot $entry.Path))
    if (-not $source.StartsWith($sourceRoot + '\', [StringComparison]::OrdinalIgnoreCase) -or
        -not $target.StartsWith($destinationRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Ruta fuera del archivo: $($entry.Path)"
    }
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "Origen alterado: $($entry.Path)"
    }
    if (Test-Path -LiteralPath $target) {
        if (-not (Test-Path -LiteralPath $target -PathType Leaf) -or
            (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.SHA256) {
            throw "El destino ya contiene un archivo distinto: $($entry.Path)"
        }
    }
}
$count = 0
foreach ($entry in $entries) {
    $source = Join-Path $sourceRoot $entry.Path
    $target = Join-Path $destinationRoot $entry.Path
    $parent = [IO.Path]::GetDirectoryName($target)
    [void][IO.Directory]::CreateDirectory($parent)
    if (-not (Test-Path -LiteralPath $target)) { Copy-Item -LiteralPath $source -Destination $target }
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "Copia no coincide: $($entry.Path)"
    }
    $count++
    if ($count % 50 -eq 0) { Write-Output "Copiados y verificados: $count / $($entries.Count)" }
}
& (Join-Path $destinationRoot 'VERIFICAR_ARCHIVO.ps1')
Write-Output "ARCHIVO GUARDADO: $count archivos; $destinationRoot"
