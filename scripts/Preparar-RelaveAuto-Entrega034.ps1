$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$stage = Join-Path $repoRoot 'artifacts\RelaveAuto-0.3.4-Entrega-aprobada'
$share = Join-Path $repoRoot 'artifacts\RelaveAuto-0.3.4-Para-compartir'
$installed = Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\RelaveAuto.bundle'
$bundle = Join-Path $stage 'RelaveAuto.bundle'
$runtime = @(Get-ChildItem -LiteralPath $bundle -Recurse -File)
if ($runtime.Count -ne 13) { throw 'Se esperaban 13 componentes.' }
foreach ($file in $runtime) {
    $relative = $file.FullName.Substring($bundle.Length + 1)
    $other = Join-Path $installed $relative
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $other).Hash) { throw "Componente distinto del aprobado: $relative" }
}
if (Test-Path -LiteralPath $share) { throw 'La carpeta de preparacion ya existe; revisar antes de repetir.' }
New-Item -ItemType Directory -Path $share | Out-Null
foreach ($name in @('RelaveAuto.bundle','Licenses','Plantilla','EMPEZAR_AQUI.txt','VERIFICACION_JEFE.txt','GUIA_INSTALACION.txt','LEEME.txt','Instalar.ps1','INSTALAR.cmd','THIRD_PARTY_NOTICES.txt','APROBACION_USUARIO.txt','RESULTADOS_PRUEBA.txt')) {
    Copy-Item -LiteralPath (Join-Path $stage $name) -Destination $share -Recurse
}
function Write-Manifest([string]$folder) {
    Get-ChildItem -LiteralPath $folder -Recurse -File | Where-Object Name -ne 'MANIFIESTO_SHA256.csv' | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{ Archivo = $_.FullName.Substring($folder.Length+1); Bytes = $_.Length; SHA256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
    } | Export-Csv -LiteralPath (Join-Path $folder 'MANIFIESTO_SHA256.csv') -NoTypeInformation -Encoding UTF8
}
Write-Manifest $stage
Write-Manifest $share
$shareZip = Join-Path $repoRoot 'artifacts\RELAVEAUTO_0.3.4_Civil3D_2023-2026_PARA_COMPARTIR.zip'
$fullZip = Join-Path $repoRoot 'artifacts\RELAVEAUTO_0.3.4_Civil3D_2023-2026_Respaldo_aprobado.zip'
foreach ($zip in @($shareZip,$fullZip)) { if (Test-Path -LiteralPath $zip) { throw "Ya existe $zip" } }
Compress-Archive -Path (Join-Path $share '*') -DestinationPath $shareZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $fullZip -CompressionLevel Optimal
Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($zipPath in @($shareZip,$fullZip)) {
    $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        foreach ($file in $runtime) {
            $relative = 'RelaveAuto.bundle/' + $file.FullName.Substring($bundle.Length+1).Replace('\','/')
            $entry = $zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq $relative }
            if (!$entry) { throw "Falta en ZIP: $relative" }
            $stream = $entry.Open()
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') } finally { $stream.Dispose(); $sha.Dispose() }
            if ($hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) { throw "ZIP distinto: $relative" }
        }
        Write-Output ((Split-Path $zipPath -Leaf) + ': ' + $zip.Entries.Count + ' entradas; 13 componentes identicos a los aprobados.')
    } finally { $zip.Dispose() }
}
Write-Output 'Preparacion y verificacion completadas.'
