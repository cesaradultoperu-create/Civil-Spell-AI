$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$destination = 'Q:\Proyectos 2026\1107.001.00.16 - Pad SOTRAMI\05_Drawing\Correccion_Xref_A_20261027'
if (Test-Path -LiteralPath $destination) { throw 'La carpeta de entrega ya existe; no se sobrescribió.' }
$items = @(
    @{ Source='Entrega\1107.001.00.14-200-14-P-005-XREF-A-LISTO.dwg'; Target='1107.001.00.14-200-14-P-005-XREF-A.dwg' },
    @{ Source='preview-ubicacion.png'; Target='Vista-layout-A.png' },
    @{ Source='verificacion.json'; Target='Verificacion.json' },
    @{ Source='LEEME.txt'; Target='LEEME.txt' }
)
$names = @('Xref_Ubicación Hidráulica_ubicacion','Xref_Ubicación Hidráulica','Xref_Estructuras_Proyectadas_','Xref_LD-pozas','Xref_LD-Poza de agua','Xref_Ubicación plano model')
foreach ($name in $names) { $items += @{ Source=('Entrega\Xref\' + $name + '.dwg'); Target=('Xref\' + $name + '.dwg') } }
foreach ($item in $items) {
    $path = Join-Path $taskRoot $item.Source
    if (!(Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -eq 0) { throw ('Falta un archivo válido: ' + $path) }
}
New-Item -ItemType Directory -Path (Join-Path $destination 'Xref') | Out-Null
foreach ($item in $items) {
    $source = Join-Path $taskRoot $item.Source
    $target = Join-Path $destination $item.Target
    Copy-Item -LiteralPath $source -Destination $target
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw ('Error de verificación: ' + $target) }
}
Write-Output ('DWG_CORREGIDO=' + (Join-Path $destination '1107.001.00.14-200-14-P-005-XREF-A.dwg'))
Write-Output ('ARCHIVOS_VERIFICADOS=' + $items.Count)
