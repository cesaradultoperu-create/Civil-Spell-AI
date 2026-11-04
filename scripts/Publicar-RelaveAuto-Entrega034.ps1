$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$stage = Join-Path $repoRoot 'artifacts\RelaveAuto-0.3.4-Entrega-aprobada'
$parent = [IO.Path]::GetFullPath('G:\01. PERSONALES\Cesar\Programas con el Visual Studio para el CIVIL3D').TrimEnd('\')
$old = [IO.Path]::GetFullPath((Join-Path $parent 'RELAVEAUTO 0.3.4 - Prueba'))
$destination = [IO.Path]::GetFullPath((Join-Path $parent 'RELAVEAUTO 0.3.4'))
foreach ($path in @($old,$destination)) {
    if ((Split-Path $path -Parent) -ne $parent) { throw 'Destino fuera de la carpeta autorizada.' }
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Se encontro un enlace; no se modifica.' }
}
if ((Test-Path -LiteralPath $old) -and (Test-Path -LiteralPath $destination)) { throw 'Existen dos carpetas de la misma version; revisar.' }
if (Test-Path -LiteralPath $old) { Rename-Item -LiteralPath $old -NewName 'RELAVEAUTO 0.3.4' }
if (!(Test-Path -LiteralPath $destination)) { New-Item -ItemType Directory -Path $destination | Out-Null }
foreach ($item in Get-ChildItem -LiteralPath $stage -Force) { Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse -Force }
$zipMapping = @{
    'RELAVEAUTO_0.3.4_Civil3D_2023-2026_PARA_COMPARTIR.zip' = 'RELAVEAUTO_0.3.4_Civil3D_2023-2026_PARA_COMPARTIR.zip'
    'RELAVEAUTO_0.3.4_Civil3D_2023-2026_Respaldo_aprobado.zip' = 'RELAVEAUTO_0.3.4_Civil3D_2023-2026_Respaldo.zip'
}
foreach ($name in $zipMapping.Keys) {
    $source = Join-Path $repoRoot ('artifacts\' + $name)
    $target = Join-Path $destination $zipMapping[$name]
    Copy-Item -LiteralPath $source -Destination $target -Force
    if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw "ZIP copiado incorrectamente: $target" }
}
$checked = 0
foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
    $relative = $file.FullName.Substring($stage.Length+1)
    $target = Join-Path $destination $relative
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw "Copia distinta: $relative" }
    $checked++
}
$receipt = @(
    'RELAVEAUTO 0.3.4 - ENTREGA APROBADA',
    ('Actualizacion verificada: ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),
    ('Carpeta: ' + $destination),
    ('Archivos del respaldo comprobados por SHA256: ' + $checked),
    '13 componentes de ejecucion identicos a los aprobados por Cesar en Civil 3D 2024.',
    'Incluye DLL para 2023, 2024, 2025 y 2026; pruebas instaladas en 2023/2025/2026 pendientes.',
    'Incluye zoom/mano y alturas cada 0.10 m del ultimo perfil en un Excel adicional.',
    'La version 0.3.3 se conserva como respaldo anterior.'
)
foreach ($name in $zipMapping.Values) { $receipt += ($name + ' SHA256=' + (Get-FileHash -LiteralPath (Join-Path $destination $name)).Hash) }
$receipt | Set-Content -LiteralPath (Join-Path $destination 'COPIA_VERIFICADA.txt') -Encoding UTF8
Write-Output ($destination + ': ' + $checked + ' archivos y 2 ZIP verificados.')
Write-Output ('Existe carpeta con nombre anterior: ' + (Test-Path -LiteralPath $old))
