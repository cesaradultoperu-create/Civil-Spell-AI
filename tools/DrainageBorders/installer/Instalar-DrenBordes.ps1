param(
    [string]$ApplicationPluginsRoot = (Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins')
)
$ErrorActionPreference = 'Stop'
$bundleName = 'DrenBordes.bundle'
$source = Join-Path $PSScriptRoot $bundleName
$entries = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'BUNDLE-SHA256.json') -Raw -Encoding UTF8 | ConvertFrom-Json

function Test-Bundle([string]$Directory) {
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) { return $false }
    $root = [IO.Path]::GetFullPath($Directory).TrimEnd('\')
    foreach ($entry in $entries) {
        $file = [IO.Path]::GetFullPath((Join-Path $root $entry.Path))
        if (-not $file.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta fuera del paquete.' }
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { return $false }
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $entry.SHA256) { return $false }
    }
    return @(Get-ChildItem -LiteralPath $root -File -Recurse).Count -eq $entries.Count
}

if (-not (Test-Bundle $source)) { throw 'El paquete esta incompleto o ha cambiado. Recupere la copia original del instalador.' }
$installRoot = [IO.Path]::GetFullPath($ApplicationPluginsRoot).TrimEnd('\')
$target = [IO.Path]::GetFullPath((Join-Path $installRoot $bundleName))
if (-not $target.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Destino de instalacion invalido.' }
if ($target.Equals([IO.Path]::GetFullPath($source), [StringComparison]::OrdinalIgnoreCase)) { throw 'El instalador debe ejecutarse desde su carpeta de entrega.' }
if (Test-Bundle $target) {
    Write-Output "DrenBordes 0.1.1 ya esta instalado y sus archivos coinciden: $target"
    exit 0
}
[void][IO.Directory]::CreateDirectory($installRoot)
$stage = [IO.Path]::GetFullPath((Join-Path $installRoot ('.DrenBordes-install-' + [Guid]::NewGuid().ToString('N'))))
if (-not $stage.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta temporal invalida.' }
Copy-Item -LiteralPath $source -Destination $stage -Recurse -ErrorAction Stop
if (-not (Test-Bundle $stage)) { throw "No se pudo verificar la copia temporal: $stage" }
$backup = $null
if (Test-Path -LiteralPath $target) {
    $existing = Get-Item -LiteralPath $target -Force
    if ($existing.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'El destino es un enlace. Use una carpeta local normal.' }
    $backup = [IO.Path]::GetFullPath((Join-Path $installRoot ('DrenBordes.backup-' + [Guid]::NewGuid().ToString('N'))))
    if (-not $backup.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta de respaldo invalida.' }
    Move-Item -LiteralPath $target -Destination $backup -ErrorAction Stop
}
try {
    Move-Item -LiteralPath $stage -Destination $target -ErrorAction Stop
} catch {
    if ($backup -and -not (Test-Path -LiteralPath $target)) { Move-Item -LiteralPath $backup -Destination $target -ErrorAction Stop }
    throw
}
if (-not (Test-Bundle $target)) { throw "Fallo la verificacion final: $target" }
Write-Output "INSTALACION CORRECTA: $target"
if ($backup) { Write-Output "Version anterior conservada en: $backup" }
Write-Output 'Abra Civil 3D 2024. Consulte la guia para autorizar la ubicacion local si se solicita.'
Write-Output 'Comando: DRENBORDES. Offset predeterminado: 0.50 a cada lado. Anchos de polilinea: 0.'
