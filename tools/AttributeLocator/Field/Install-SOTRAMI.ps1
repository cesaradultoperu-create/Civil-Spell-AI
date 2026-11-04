param([string]$BuildDirectory)
$ErrorActionPreference='Stop'
if(-not $BuildDirectory){$BuildDirectory=Join-Path $PSScriptRoot '../../../artifacts/CasillasField-v0.4-SOTRAMI'}
$BuildDirectory=(Resolve-Path -LiteralPath $BuildDirectory).Path
$bundle=Join-Path $env:APPDATA 'Autodesk/ApplicationPlugins/CasillasFieldDemo.bundle'
$local=Join-Path $env:LOCALAPPDATA 'CasillasField'
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$backup=Join-Path $local ('InstallationBackups/'+$stamp)
foreach($file in @('LocalizarCasillas.Field.2024.dll','AcSmComponents.Interop.dll')){
 if(-not(Test-Path -LiteralPath (Join-Path $BuildDirectory $file))){throw "Falta $file"}
}
$version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $BuildDirectory 'LocalizarCasillas.Field.2024.dll')).Version
if($version -ne [version]'0.4.0.0'){throw "Version inesperada: $version"}
New-Item -ItemType Directory -Path $backup -Force | Out-Null
if(Test-Path -LiteralPath $bundle){Copy-Item -LiteralPath $bundle -Destination $backup -Recurse}
$configuration=Join-Path $local 'projects.xml'
if(Test-Path -LiteralPath $configuration){Copy-Item -LiteralPath $configuration -Destination $backup}
New-Item -ItemType Directory -Path (Join-Path $bundle 'Contents') -Force | Out-Null
foreach($file in @('LocalizarCasillas.Field.2024.dll','AcSmComponents.Interop.dll')){
 Copy-Item -LiteralPath (Join-Path $BuildDirectory $file) -Destination (Join-Path $bundle 'Contents') -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PackageContents.xml') -Destination $bundle -Force
# Preserve any other explicitly registered projects and replace only SOTRAMI.
$settings=New-Object Xml.XmlReaderSettings
$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit
$settings.XmlResolver=$null
$document=New-Object Xml.XmlDocument
$document.XmlResolver=$null
if(Test-Path -LiteralPath $configuration){
 $reader=[Xml.XmlReader]::Create($configuration,$settings)
 try{$document.Load($reader)}finally{$reader.Dispose()}
}else{[void]$document.AppendChild($document.CreateElement('Projects'))}
if($document.DocumentElement.Name -ne 'Projects'){throw 'Configuracion local no reconocida; se conserva el respaldo.'}
foreach($node in @($document.SelectNodes('/Projects/Project[@name="SOTRAMI"]'))){[void]$node.ParentNode.RemoveChild($node)}
$source=New-Object Xml.XmlDocument
$source.Load((Join-Path $PSScriptRoot 'Projects-SOTRAMI.xml'))
[void]$document.DocumentElement.AppendChild($document.ImportNode($source.SelectSingleNode('/Projects/Project'),$true))
$temporary=Join-Path $local ('projects-'+$stamp+'.tmp')
$document.Save($temporary)
Move-Item -LiteralPath $temporary -Destination $configuration -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LEEME-SOTRAMI.txt') -Destination (Join-Path $local 'LEEME-SOTRAMI.txt') -Force
Write-Output "Instalado para este usuario: $bundle"
Write-Output "Proyecto habilitado: SOTRAMI. Configuracion: $configuration"
Write-Output "Respaldo de instalacion: $backup"
Write-Output 'No se han cambiado datos ni vinculos de los archivos del proyecto.'
