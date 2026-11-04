param([string]$BuildDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(-not $BuildDirectory){$BuildDirectory=Join-Path $PSScriptRoot '../../../artifacts/CasillasField-v0.4-SOTRAMI'}
if(-not $OutputDirectory){$OutputDirectory=Join-Path $PSScriptRoot '../../../artifacts'}
$BuildDirectory=(Resolve-Path -LiteralPath $BuildDirectory).Path
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$version=[Reflection.AssemblyName]::GetAssemblyName((Join-Path $BuildDirectory 'LocalizarCasillas.Field.2024.dll')).Version
if($version -ne [version]'0.4.0.0'){throw "Version inesperada: $version"}
$files=@('Install-SOTRAMI.ps1','Instalar.cmd','PackageContents.xml','Projects-SOTRAMI.xml','LEEME-SOTRAMI.txt','LEEME-INSTALACION.txt')
foreach($file in $files){if(-not(Test-Path -LiteralPath (Join-Path $PSScriptRoot $file))){throw "Falta $file"}}
$payload=@('LocalizarCasillas.Field.2024.dll','AcSmComponents.Interop.dll')
foreach($file in $payload){if(-not(Test-Path -LiteralPath (Join-Path $BuildDirectory $file))){throw "Falta $file"}}
$name='CasillasField-SOTRAMI-2024-v0.4.0-'+(Get-Date -Format 'yyyyMMdd-HHmmssfff')
$stage=Join-Path $OutputDirectory $name
New-Item -ItemType Directory -Path (Join-Path $stage 'payload') | Out-Null
foreach($file in $files){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $stage}
foreach($file in $payload){Copy-Item -LiteralPath (Join-Path $BuildDirectory $file) -Destination (Join-Path $stage 'payload')}
$archive=$stage+'.zip'
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Output $archive
