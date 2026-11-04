$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'DetalleAutoLado.bundle'
$target=Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\DetalleAutoLado.bundle'
$relativeDll='Contents\Windows\DetalleAutoLado.2024.dll'
if(-not (Test-Path -LiteralPath (Join-Path $source $relativeDll))) {throw 'Falta la carpeta DetalleAutoLado.bundle junto al instalador.'}
if(Test-Path -LiteralPath $target) {
    $installed=Join-Path $target $relativeDll
    if(-not (Test-Path -LiteralPath $installed) -or (Get-FileHash -LiteralPath $installed).Hash -ne (Get-FileHash -LiteralPath (Join-Path $source $relativeDll)).Hash) {
        throw "Ya existe otra version en $target. No se ha sobrescrito."
    }
} else {
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target -Recurse
}
Write-Output "COMPLEMENTO_INSTALADO: $target"
Write-Output 'AutoCAD/Civil 3D 2024 lo cargara al iniciar. Comando de actualizacion: DETALLEAUTOLADO.'
