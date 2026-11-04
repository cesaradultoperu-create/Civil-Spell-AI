$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$source = Join-Path $taskRoot 'EntregaFinal'
$userRoot = 'C:\Users\Cesar Ugaz\Pictures\levantar observaciones'
$received = Join-Path $userRoot '1107.001.00.14-200-14-P-005.dwg'
$expectedSourceHash = 'D069CA4600F3D5FA1574AB8D4A89AF330DDC9B0714D383F2B8D910CA628E9E28'
$sourceHash = (Get-FileHash -LiteralPath $received -Algorithm SHA256).Hash
if ($sourceHash -ne $expectedSourceHash) { throw 'La copia recibida cambió durante el trabajo; no se reemplazará ningún original.' }
$fileNames = @(
  '1107.001.00.14-200-14-P-005-LEVANTAMIENTO.dwg',
  '1107.001.00.14-200-14-P-005-LEVANTAMIENTO.pdf',
  'OBSERVACIONES-MARCADAS.pdf',
  'OBSERVACIONES-CON-X.png',
  'CONTROL-OBSERVACIONES.json',
  'VERIFICACION-FINAL.txt',
  'VERIFICACION-ENTREGA.txt',
  'WWL-Full.ctb',
  'LEEME.txt'
)
foreach ($name in $fileNames) {
  $path = Join-Path $source $name
  if (!(Test-Path -LiteralPath $path -PathType Leaf) -or (Get-Item -LiteralPath $path).Length -eq 0) { throw "Falta el archivo de entrega: $name" }
}
$verification = Get-Content -LiteralPath (Join-Path $source 'VERIFICACION-ENTREGA.txt') -Raw -Encoding UTF8
if (!$verification.Contains('Geometría Civil conservada')) { throw 'No está la comprobación de geometría de esta entrega.' }
$destination = Join-Path $userRoot 'REVISION_P005_2026-10-27'
if (Test-Path -LiteralPath $destination) { $destination = Join-Path $userRoot ('REVISION_P005_2026-10-27_' + (Get-Date -Format 'HHmmss')) }
if (Test-Path -LiteralPath $destination) { throw 'La carpeta de entrega ya existe. No se sobrescribió.' }
New-Item -ItemType Directory -Path $destination | Out-Null
foreach ($name in $fileNames) { Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $destination $name) }
Copy-Item -LiteralPath (Join-Path $source 'Assets') -Destination $destination -Recurse
$manifest = foreach ($file in (Get-ChildItem -LiteralPath $destination -File)) {
  $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
  $originalHash = (Get-FileHash -LiteralPath (Join-Path $source $file.Name) -Algorithm SHA256).Hash
  if ($hash -ne $originalHash) { throw "Error en copia de entrega: $($file.Name)" }
  [pscustomobject]@{ Archivo = $file.Name; Bytes = $file.Length; SHA256 = $hash }
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'ARCHIVOS-ENTREGADOS.json') -Encoding UTF8
[pscustomobject]@{ArchivoRecibido=$received;SHA256=$sourceHash;SinCambios=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'ORIGINAL-SIN-CAMBIOS.json') -Encoding UTF8
Set-Content -LiteralPath (Join-Path $taskRoot 'carpeta-entregada.txt') -Value $destination -Encoding UTF8
[pscustomobject]@{Carpeta=$destination;ConX=34;PendientesOParciales=7;Archivos=$fileNames.Count;Assets=(Get-ChildItem -LiteralPath (Join-Path $destination 'Assets') -File).Count;OriginalSinCambios=$true} | ConvertTo-Json
