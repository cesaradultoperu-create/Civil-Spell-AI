param([string]$ValidationRoot)
$ErrorActionPreference='Stop'
$source=$PSScriptRoot
$testMode=-not [string]::IsNullOrWhiteSpace($ValidationRoot)
if($testMode){
 $validation=[IO.Path]::GetFullPath($ValidationRoot)
 if($validation.IndexOf('\auditoria-20261019\prueba-instalador\',[StringComparison]::OrdinalIgnoreCase) -lt 0){throw 'La validacion debe permanecer en la carpeta de ensayo del instalador.'}
 $plugins=$validation
}else{
 if(Get-Process acad -ErrorAction SilentlyContinue){throw 'GUARDE SU TRABAJO Y CIERRE CIVIL 3D / AUTOCAD ANTES DE INSTALAR. EL INSTALADOR NO CIERRA SUS DIBUJOS.'}
 $plugins=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Autodesk\ApplicationPlugins'
}
$plugins=[IO.Path]::GetFullPath($plugins).TrimEnd('\')
$destination=Join-Path $plugins 'SOTRAMIExcel.bundle'
if([IO.Path]::GetFullPath($destination) -ne ($plugins+'\SOTRAMIExcel.bundle')){throw 'Destino de instalacion no valido.'}
$manifest=Get-Content -LiteralPath (Join-Path $source 'ARCHIVOS_VERIFICADOS.json') -Raw | ConvertFrom-Json
foreach($property in $manifest.sha256.PSObject.Properties){
 $file=Join-Path $source $property.Name
 if(-not(Test-Path -LiteralPath $file -PathType Leaf)){throw ('FALTA ARCHIVO: '+$property.Name+'. DESCOMPRIMA TODO EL ZIP.')}
 if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $property.Value){throw ('ARCHIVO DISTINTO AL PAQUETE: '+$property.Name+'. DESCOMPRIMA DE NUEVO EL ZIP COMPLETO.')}
}
New-Item -ItemType Directory -Path $plugins -Force | Out-Null
$prepared=Join-Path $plugins ('SOTRAMIExcel.preparando.'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $prepared 'Contents\Plantillas') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'PackageContents.xml') -Destination $prepared
foreach($name in @('VinculoPlanosSOTRAMI.v35.2023-2024.dll','VinculoPlanosSOTRAMI.v35.2025.dll','SOTRAMI.config.json','layout-catalog.json')){
 Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $prepared 'Contents')
}
Copy-Item -LiteralPath (Join-Path $source 'Plantillas\SOTRAMI - PLANO VACIO.dwg') -Destination (Join-Path $prepared 'Contents\Plantillas')
$oldConfig=Join-Path $destination 'Contents\SOTRAMI.config.json'
$oldLogs=Join-Path $destination 'Contents\Registros'
if(Test-Path -LiteralPath $oldLogs){Copy-Item -LiteralPath $oldLogs -Destination (Join-Path $prepared 'Contents\Registros') -Recurse}
if(Test-Path -LiteralPath $oldConfig){
 $previous=Get-Content -LiteralPath $oldConfig -Raw | ConvertFrom-Json
 if(-not [String]::IsNullOrWhiteSpace($previous.ProjectDirectory)){Copy-Item -LiteralPath $oldConfig -Destination (Join-Path $prepared 'Contents\SOTRAMI.config.json') -Force}
}
$backup=$null
if(Test-Path -LiteralPath $destination){
 $backupParent=Join-Path $plugins 'SOTRAMI_Respaldos'
 New-Item -ItemType Directory -Path $backupParent -Force | Out-Null
 $backup=Join-Path $backupParent ('Version_anterior_'+(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+[Guid]::NewGuid().ToString('N'))
 # Both absolute move targets are checked before moving the previous bundle.
 if(-not [IO.Path]::GetFullPath($backup).StartsWith($plugins+'\SOTRAMI_Respaldos\',[StringComparison]::OrdinalIgnoreCase)){throw 'Respaldo fuera del directorio previsto.'}
 Move-Item -LiteralPath $destination -Destination $backup
}
try{Move-Item -LiteralPath $prepared -Destination $destination}
catch{if($backup -and -not(Test-Path -LiteralPath $destination)){Move-Item -LiteralPath $backup -Destination $destination};throw}
$contents=Join-Path $destination 'Contents'
@{success=$true;version=35;installedBundle=$destination;trustedFolder=$contents;backup=$backup;validationOnly=$testMode;utc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $destination 'INSTALACION.json') -Encoding UTF8
Write-Host ''
Write-Host 'SOTRAMI VERSION 35 INSTALADO PARA ESTE USUARIO DE WINDOWS.'
Write-Host 'COMPATIBLE CON CIVIL 3D 2023 / 2024 / 2025; ELIGE SU DLL AUTOMATICAMENTE.'
Write-Host ('CARPETA: '+$destination)
Write-Host ''
Write-Host 'PASO SIGUIENTE: EN CADA VERSION DE CIVIL 3D QUE UTILICE, ABRA OPTIONS.'
Write-Host 'FILES -> TRUSTED LOCATIONS -> ADD: AGREGUE SOLO ESTA CARPETA:'
Write-Host $contents
Write-Host 'LUEGO CIERRE Y VUELVA A ABRIR CIVIL 3D. AUTORICE EL DLL SOTRAMI SI SE SOLICITA.'
Write-Host 'EL INSTALADOR NO CAMBIA SECURELOAD, TRUSTEDPATHS NI EL CARGADOR DE OTRAS APLICACIONES.'
Write-Host 'ABRA UN DWG DEL PROYECTO Y ESCRIBA QUIENGUARDO PARA COMPROBAR.'
Write-Host 'PARA ACTUALIZAR DESDE EXCEL, ACTIVE LA PORTADA Y USE SOTRAMIEXCEL.'
