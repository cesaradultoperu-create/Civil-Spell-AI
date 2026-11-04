$ErrorActionPreference='Stop'
if(Get-Process acad -ErrorAction SilentlyContinue){throw 'Guarde sus dibujos y cierre Civil 3D antes de actualizar. No se cambio ningun archivo.'}
$taskSource=(Resolve-Path -LiteralPath 'C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro\Comando RELAVEAUTO 0.3.5\RelaveAuto.bundle').ProviderPath
$taskParent=(Resolve-Path -LiteralPath 'C:\Users\Cesar Ugaz\AppData\Roaming\Autodesk\ApplicationPlugins').ProviderPath.TrimEnd('\')
$taskDestination=(Resolve-Path -LiteralPath (Join-Path $taskParent 'RelaveAuto.bundle')).ProviderPath
if([IO.Path]::GetFullPath($taskDestination) -ne [IO.Path]::GetFullPath((Join-Path $taskParent 'RelaveAuto.bundle'))){throw 'Destino de instalacion fuera del alcance.'}
foreach($taskFolder in @($taskSource,$taskDestination)){
 if((Get-Item -LiteralPath $taskFolder).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'No se actualiza un enlace de carpeta.'}
 if(@(Get-ChildItem -LiteralPath $taskFolder -Recurse -Force | Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Hay enlaces dentro del bundle.'}
}
$taskSourceXml=[xml](Get-Content -LiteralPath (Join-Path $taskSource 'PackageContents.xml') -Raw)
$taskDestinationXml=[xml](Get-Content -LiteralPath (Join-Path $taskDestination 'PackageContents.xml') -Raw)
if($taskSourceXml.ApplicationPackage.AppVersion -ne '0.3.5'){throw 'La fuente no es 0.3.5.'}
if($taskDestinationXml.ApplicationPackage.AppVersion -notin @('0.3.4','0.3.5')){throw 'La instalacion cambio de version; revisar antes de continuar.'}
foreach($taskFile in (Get-ChildItem -LiteralPath $taskDestination -Recurse -File)){
 try{$taskStream=[IO.File]::Open($taskFile.FullName,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None);$taskStream.Dispose()}
 catch{throw "Civil 3D o su motor aun usa: $($taskFile.FullName). No se cambio ningun archivo."}
}
$taskBackup=Join-Path $PSScriptRoot 'Respaldo_instalacion_0.3.4'
if(Test-Path -LiteralPath $taskBackup){throw 'Ya existe el respaldo instalado; revisar antes de repetir la actualizacion.'}
Copy-Item -LiteralPath $taskDestination -Destination $taskBackup -Recurse
$taskBefore=@()
foreach($taskFile in (Get-ChildItem -LiteralPath $taskDestination -Recurse -File)){
 $taskRelative=$taskFile.FullName.Substring($taskDestination.Length+1);$taskHash=(Get-FileHash -LiteralPath $taskFile.FullName).Hash
 if($taskHash -ne (Get-FileHash -LiteralPath (Join-Path $taskBackup $taskRelative)).Hash){throw 'No se verifico el respaldo; se conserva la instalacion.'}
 $taskBefore+=[pscustomobject]@{Archivo=$taskRelative;SHA256=$taskHash}
}
$taskBefore | Export-Csv -LiteralPath (Join-Path $PSScriptRoot 'instalacion-anterior.csv') -NoTypeInformation -Encoding UTF8
$taskFiles=Get-ChildItem -LiteralPath $taskSource -Recurse -File | Sort-Object @{Expression={if($_.Name -eq 'PackageContents.xml'){1}else{0}}},FullName
foreach($taskFile in $taskFiles){
 $taskRelative=$taskFile.FullName.Substring($taskSource.Length+1);$taskCopy=Join-Path $taskDestination $taskRelative
 New-Item -ItemType Directory -Path (Split-Path $taskCopy -Parent) -Force | Out-Null
 Copy-Item -LiteralPath $taskFile.FullName -Destination $taskCopy -Force
}
foreach($taskFile in $taskFiles){
 $taskRelative=$taskFile.FullName.Substring($taskSource.Length+1)
 if((Get-FileHash -LiteralPath $taskFile.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $taskDestination $taskRelative)).Hash){throw "No coincide la instalacion nueva: $taskRelative"}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'instalacion-0.3.5-ok.txt'),"Instalacion 0.3.5 verificada: $($taskFiles.Count) archivos. Respaldo de la instalacion anterior conservado fuera de ApplicationPlugins. No se opero ni cerro Civil 3D.",[Text.UTF8Encoding]::new($false))
'RELAVEAUTO instalado actualizado y verificado: 0.3.5. Abra Civil 3D y escriba RELAVEAUTO.'
