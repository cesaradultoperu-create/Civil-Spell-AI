$ErrorActionPreference='Stop'
$configuration=Join-Path $env:LOCALAPPDATA 'CasillasField/projects.xml'
$document=New-Object Xml.XmlDocument
$document.XmlResolver=$null
$document.Load($configuration)
if($document.DocumentElement.Name -ne 'Projects'){throw 'No se reconoce la configuracion actual.'}
$source=New-Object Xml.XmlDocument
$source.XmlResolver=$null
$source.Load((Join-Path $PSScriptRoot '../tools/AttributeLocator/Field/Projects-SOTRAMI.xml'))
$project=$source.SelectSingleNode('/Projects/Project[@name="SOTRAMI"]')
if(-not $project){throw 'Falta el perfil SOTRAMI.'}
$backup=Join-Path (Split-Path $configuration) ('projects-before-field-edit-'+(Get-Date -Format 'yyyyMMdd-HHmmssfff')+'.xml')
Copy-Item -LiteralPath $configuration -Destination $backup
foreach($node in @($document.SelectNodes('/Projects/Project[@name="SOTRAMI"]'))){[void]$node.ParentNode.RemoveChild($node)}
[void]$document.DocumentElement.AppendChild($document.ImportNode($project,$true))
$document.Save($configuration)
Write-Output ('Perfil de SOTRAMI restaurado. Respaldo: '+$backup)
Write-Output 'No se han escrito valores en el DWG ni en el DST.'
