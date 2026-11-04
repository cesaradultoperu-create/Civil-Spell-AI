$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).ProviderPath
$source=(Resolve-Path -LiteralPath (Join-Path $repo 'artifacts\RelaveAuto-0.3.5-Respaldo')).ProviderPath
$targets=@(
 [pscustomobject]@{Parent='G:\01. PERSONALES\Cesar\Programas con el Visual Studio para el CIVIL3D';Name='RELAVEAUTO 0.3.5'},
 [pscustomobject]@{Parent='C:\Users\Cesar Ugaz\Pictures\poner fotos en cuadro';Name='Comando RELAVEAUTO 0.3.5'}
)
foreach($entry in $targets){
 $parent=(Resolve-Path -LiteralPath $entry.Parent).ProviderPath.TrimEnd('\')
 $destination=[IO.Path]::GetFullPath((Join-Path $parent $entry.Name))
 if((Split-Path $destination -Parent) -ne $parent){throw 'Destino fuera de la carpeta autorizada.'}
 if(Test-Path -LiteralPath $destination){throw "La carpeta ya existe; revisar antes de reemplazar: $destination"}
 New-Item -ItemType Directory -Path $destination | Out-Null
 foreach($item in Get-ChildItem -LiteralPath $source){Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse}
 $count=0
 foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File){
  $relative=$file.FullName.Substring($source.Length+1);$copy=Join-Path $destination $relative
  if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $copy).Hash){throw "Copia distinta: $relative"};$count++
 }
 $receipt=@('RELAVEAUTO 0.3.5',('Actualizado: '+(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')),('Archivos verificados por SHA256: '+$count),'Agregar puntos y confirmacion Esc/Cancelar/X comprobados.','Cuatro DLL incluidas; pruebas nativas 2024 correctas.','Instalacion actual del usuario no modificada: Civil 3D abierto.','Ver GUIA_INSTALACION.txt y VERIFICACION_JEFE.txt.')
 foreach($suffix in @('Respaldo','PARA_COMPARTIR')){
  $name="RELAVEAUTO_0.3.5_Civil3D_2023-2026_$suffix.zip";$zip=Join-Path $repo ('artifacts\'+$name);$copy=Join-Path $destination $name
  Copy-Item -LiteralPath $zip -Destination $copy
  $hash=(Get-FileHash -LiteralPath $zip).Hash
  if($hash -ne (Get-FileHash -LiteralPath $copy).Hash){throw "ZIP distinto: $name"}
  $receipt+=($name+' SHA256='+$hash)
 }
 $receipt|Set-Content -LiteralPath (Join-Path $destination 'COPIA_VERIFICADA.txt') -Encoding UTF8
 Write-Output ($destination+': '+$count+' archivos y 2 ZIP verificados.')
}
