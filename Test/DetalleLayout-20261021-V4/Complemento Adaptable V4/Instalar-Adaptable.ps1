$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'DetalleAutoLado.bundle'
$target=Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\DetalleAutoLado.bundle'
$relative='Contents\Windows\DetalleAdaptable.2024.dll'
$dll=Join-Path $target $relative
if(-not (Test-Path -LiteralPath (Join-Path $source $relative))){throw 'Falta la carpeta del complemento junto al instalador.'}
if(Test-Path -LiteralPath $dll) {
    if((Get-FileHash -LiteralPath $dll).Hash -ne (Get-FileHash -LiteralPath (Join-Path $source $relative)).Hash){throw 'Existe otra version de DetalleAdaptable. Cierra Civil 3D antes de actualizarla.'}
} else {
    New-Item -ItemType Directory -Path (Split-Path -Parent $dll) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $source $relative) -Destination $dll
}
$manifest=Join-Path $target 'PackageContents.xml'
if(Test-Path -LiteralPath $manifest){Copy-Item -LiteralPath $manifest -Destination (Join-Path $target 'PackageContents.anterior.xml') -Force}
Copy-Item -LiteralPath (Join-Path $source 'PackageContents.xml') -Destination $manifest -Force
Write-Output "DETALLE_ADAPTABLE_INSTALADO: $target"
Write-Output 'Civil 3D / AutoCAD 2024 lo cargara al iniciar. Comandos: DETALLEESTIRAR, DETALLEAJUSTAR y DETALLEACTUALIZARTODOS.'
