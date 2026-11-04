param([string]$Destino='')
$ErrorActionPreference='Stop'
$taskSource=Join-Path $PSScriptRoot 'RelaveAuto.bundle'
if(!(Test-Path -LiteralPath (Join-Path $taskSource 'PackageContents.xml'))){throw 'Falta RelaveAuto.bundle junto a este instalador.'}
if(![Environment]::Is64BitOperatingSystem){throw 'RELAVEAUTO requiere Windows y Civil 3D de 64 bits.'}
$taskXml=[xml](Get-Content -LiteralPath (Join-Path $taskSource 'PackageContents.xml') -Raw)
$taskVersion=[string]$taskXml.ApplicationPackage.AppVersion
foreach($taskYear in @(2023,2024,2025,2026)){
 $taskDll=Join-Path $taskSource "Contents\Windows\$taskYear\RelaveAuto.$taskYear.dll"
 if(!(Test-Path -LiteralPath $taskDll)){throw "El paquete esta incompleto: falta la DLL $taskYear."}
 if([Diagnostics.FileVersionInfo]::GetVersionInfo($taskDll).FileVersion -ne ($taskVersion+'.0')){throw "Version incorrecta de la DLL $taskYear."}
}
foreach($taskFile in @('RelaveVision.exe','RelaveVision.exe.config','OpenCvSharp.dll','OpenCvSharpExtern.dll','System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')){
 if(!(Test-Path -LiteralPath (Join-Path $taskSource "Contents\Engine\$taskFile"))){throw "El motor esta incompleto: falta $taskFile."}
}
$taskDestination=if($Destino){[IO.Path]::GetFullPath($Destino)}else{Join-Path $env:APPDATA 'Autodesk\ApplicationPlugins\RelaveAuto.bundle'}
if((Split-Path $taskDestination -Leaf) -ne 'RelaveAuto.bundle'){throw 'El destino debe terminar en RelaveAuto.bundle.'}
$taskFiles=@(Get-ChildItem -LiteralPath $taskSource -Recurse -File)
function Test-TaskCopy([string]$taskFolder){
 foreach($taskFile in $taskFiles){
  $taskRelative=$taskFile.FullName.Substring($taskSource.Length+1);$taskCopy=Join-Path $taskFolder $taskRelative
  if(!(Test-Path -LiteralPath $taskCopy)){return $false}
  if((Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $taskCopy -Algorithm SHA256).Hash){return $false}
 }
 return $true
}
if(Test-Path -LiteralPath $taskDestination){
 if(Test-TaskCopy $taskDestination){Write-Output "RELAVEAUTO $taskVersion ya esta instalado y su copia es correcta.";Write-Output "Ubicacion: $taskDestination";return}
 throw 'Ya existe una instalacion distinta o incompleta. Cierre Civil 3D y mueva la carpeta RelaveAuto.bundle anterior fuera de ApplicationPlugins como respaldo. Luego vuelva a instalar. No se sobrescribio ningun archivo.'
}
New-Item -ItemType Directory -Path (Split-Path $taskDestination -Parent) -Force | Out-Null
Copy-Item -LiteralPath $taskSource -Destination $taskDestination -Recurse
if(!(Test-TaskCopy $taskDestination)){throw 'No se pudo verificar la copia. Conserve el paquete y revise los permisos de destino.'}
Write-Output "RELAVEAUTO $taskVersion instalado y verificado."
Write-Output "Ubicacion: $taskDestination"
Write-Output 'En el proximo inicio de Civil 3D 2023, 2024, 2025 o 2026 escriba RELAVEAUTO.'
Write-Output 'Si Autodesk solicita confiar en la aplicacion, consulte GUIA_INSTALACION.txt.'
