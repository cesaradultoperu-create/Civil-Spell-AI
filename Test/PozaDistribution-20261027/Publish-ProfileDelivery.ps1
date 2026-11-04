$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\Cesar Ugaz\Pictures\trabajo para code'
$delivery = Join-Path $taskRoot 'ENTREGA_FINAL_4_PLANOS_2026-10-27'
$revisionRoot = $PSScriptRoot
$final = Join-Path $revisionRoot 'PerfilesEntrega'
$staging = Join-Path $revisionRoot 'PaquetePerfilesCorregidos'
$codes = @('007','008','009','010')

# Only replace the agent's unchanged prior delivery.
$prior = Get-Content -LiteralPath (Join-Path $delivery 'ARCHIVOS-ENTREGADOS.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($item in $prior) {
    $actual = Get-FileHash -LiteralPath (Join-Path $delivery $item.Archivo) -Algorithm SHA256
    if ($actual.Hash -ne $item.SHA256) { throw "La entrega anterior fue modificada: $($item.Archivo). No se reemplazo ningun archivo." }
}
foreach ($code in $codes) {
    $report = Join-Path $final "REAPERTURA-PERFILES-$code.txt"
    if (!(Test-Path -LiteralPath $report)) { throw "Falta la comprobacion $code" }
}
if (Test-Path -LiteralPath $staging) { throw 'El paquete de esta revision ya existe; revisar antes de publicar de nuevo.' }
New-Item -ItemType Directory -Path $staging | Out-Null
Copy-Item -LiteralPath (Join-Path $delivery 'Assets') -Destination $staging -Recurse
Copy-Item -LiteralPath (Join-Path $delivery 'WWL-Full.ctb') -Destination $staging

foreach ($code in $codes) {
    foreach ($ext in @('dwg','pdf')) {
        Copy-Item -LiteralPath (Join-Path $final "1107.001.00.14-200-14-P-$code-PERFILES.$ext") -Destination (Join-Path $staging "1107.001.00.14-200-14-P-$code-TERMINADO.$ext")
    }
    Copy-Item -LiteralPath (Join-Path $final "VISTA-$code.png") -Destination $staging
    Copy-Item -LiteralPath (Join-Path $final "REAPERTURA-PERFILES-$code.txt") -Destination $staging
    Copy-Item -LiteralPath (Join-Path $revisionRoot "PerfilesVerificados\PERFILES-$code.txt") -Destination $staging
    foreach ($kind in @('REAPERTURA','VERIFICACION')) {
        Copy-Item -LiteralPath (Join-Path $final "REAPERTURA-PERFILES-$code.txt") -Destination (Join-Path $staging "$kind-$code.txt")
    }
    @("REVISION DE PERFILES - P-$code", 'Planta y perfiles a 1:200 en A1. Grilla gris, distancia en metros, titulos cian.', 'Bandas nativas conservadas y ocultas. Cotas geometricas reales sin texto forzado.', 'Ver REAPERTURA-PERFILES para comprobacion de geometria nativa y TIN.') | Set-Content -LiteralPath (Join-Path $staging "PRESENTACION-$code.txt") -Encoding UTF8
}

$readme = Get-Content -LiteralPath (Join-Path $delivery 'LEEME.txt') -Raw -Encoding UTF8
$readme = $readme.Replace('secciones 1:250','secciones 1:200')
$readme = $readme.Replace('Se conservan las superficies, los alineamientos, los perfiles y las bandas nativas del jefe.', 'Se conservan las superficies, los alineamientos y los perfiles nativos del jefe. Las bandas nativas conservan sus datos y quedan ocultas para obtener la presentacion solicitada.')
$readme += @'

REVISION DE PERFILES SEGUN LA IMAGEN - 27/10/2026
Los ocho perfiles se presentan a 1:200 en A1, con cuadricula gris de trazo y punto, eje DISTANCIA (m), rotulos en metros y simbolos de seccion cian. Los rangos de elevacion corresponden al terreno de cada poza. Se conservan las curvas nativas de terreno y diseno y las anotaciones de revestimiento.
Las cotas azules se obtienen de las intersecciones de los ejes con las coronaciones reales SOTRAMI; no se copian los valores 0.50, 0.75 o 1.20 de la imagen. Son dimensiones geometricas editables, sin sustitucion manual del texto. Anchos medidos entre intersecciones de coronacion: P-007 19.00 y 19.00 m; P-008 40.50 y 22.1025 m; P-009 10.50 y 10.50 m; P-010 30.00 y 11.00 m. No representan necesariamente las dimensiones principales completas en planta.
Se comprobo la reapertura de los cuatro DWG con sus TIN de terreno y diseno validos. Los informes REAPERTURA-PERFILES documentan la comprobacion. Los objetos nativos de superficie, alineamiento y perfil se conservan. La grilla y los rotulos adicionales son entidades CAD editables; si se cambia el diseno, deben actualizarse conjuntamente.
La revision anterior con bandas queda respaldada en Revision_anterior_con_bandas dentro de la carpeta de entrega. El ZIP nuevo contiene unicamente la revision actual y sus recursos. Los dibujos de origen y la sesion abierta del usuario no se modificaron.
'@
$readme | Set-Content -LiteralPath (Join-Path $staging 'LEEME.txt') -Encoding UTF8

$originals = Get-Content -LiteralPath (Join-Path $revisionRoot 'fuentes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checks = foreach ($item in $originals) {
    $sourcePath = Join-Path $taskRoot $item.Name
    $currentHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
    [pscustomobject]@{Archivo=$item.Name; SHA256=$currentHash; CoincideConCopiaRecibida=($currentHash -eq $item.SHA256)}
}
$checks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $staging 'ORIGINALES-INTACTOS.json') -Encoding UTF8

$manifest = foreach ($file in (Get-ChildItem -LiteralPath $staging -File | Where-Object { $_.Extension -in @('.dwg','.pdf') } | Sort-Object Name)) {
    [pscustomobject]@{Archivo=$file.Name; Bytes=$file.Length; SHA256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $staging 'ARCHIVOS-ENTREGADOS.json') -Encoding UTF8

$backup = Join-Path $delivery 'Revision_anterior_con_bandas'
if (Test-Path -LiteralPath $backup) { throw 'Ya existe el respaldo de la revision anterior.' }
New-Item -ItemType Directory -Path $backup | Out-Null
Get-ChildItem -LiteralPath $delivery -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $backup }
Get-ChildItem -LiteralPath $staging -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $delivery -Force }
foreach ($item in $manifest) {
    if ((Get-FileHash -LiteralPath (Join-Path $delivery $item.Archivo) -Algorithm SHA256).Hash -ne $item.SHA256) { throw "Error de copia $($item.Archivo)" }
}

$zipPath = Join-Path $taskRoot 'ENTREGA_PERFILES_CORREGIDOS_2026-10-27.zip'
if (Test-Path -LiteralPath $zipPath) { throw 'Ya existe el ZIP de esta revision.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $zipPath, [IO.Compression.CompressionLevel]::Fastest, $true)
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $dwgCount = @($zip.Entries | Where-Object { $_.FullName.EndsWith('.dwg') }).Count
    $pdfCount = @($zip.Entries | Where-Object { $_.FullName.EndsWith('.pdf') }).Count
    if ($dwgCount -ne 4 -or $pdfCount -ne 4) { throw "ZIP incompleto: $dwgCount DWG, $pdfCount PDF" }
} finally { $zip.Dispose() }
[pscustomobject]@{Carpeta=$delivery; ZIP=$zipPath; DWG=4; PDF=4; OriginalesCoinciden=(@($checks | Where-Object { !$_.CoincideConCopiaRecibida }).Count -eq 0); BytesZIP=(Get-Item -LiteralPath $zipPath).Length} | ConvertTo-Json
