param([Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference='Stop'
$Package=(Resolve-Path -LiteralPath $Package).Path
$bundle=Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Autodesk\ApplicationPlugins\SOTRAMIExcel.bundle'
$contents=Join-Path $bundle 'Contents'
if(-not(Test-Path -LiteralPath (Join-Path $contents 'SOTRAMI.config.json'))){throw 'Existing SOTRAMI installation required.'}
$backup=Join-Path (Split-Path $bundle) ('SOTRAMI_Respaldos\Antes_v34_'+[DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach($file in @('PackageContents.xml','INSTALACION.json','Contents\layout-catalog.json','Contents\SOTRAMI.config.json')){
    $from=Join-Path $bundle $file
    if(Test-Path -LiteralPath $from){$to=Join-Path $backup $file;New-Item -ItemType Directory -Path (Split-Path $to) -Force | Out-Null;Copy-Item -LiteralPath $from -Destination $to}
}
foreach($family in @('2023-2024','2025')){
    $name='VinculoPlanosSOTRAMI.v34.'+$family+'.dll'
    $from=Join-Path $Package $name; $to=Join-Path $contents $name
    Copy-Item -LiteralPath $from -Destination $to
    if((Get-FileHash -LiteralPath $from).Hash -ne (Get-FileHash -LiteralPath $to).Hash){throw 'Installed DLL hash mismatch.'}
}
Copy-Item -LiteralPath (Join-Path $Package 'PackageContents.xml') -Destination (Join-Path $bundle 'PackageContents.xml') -Force
@{success=$true;version=34;installedBundle=$bundle;backup=$backup;utc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bundle 'INSTALACION.json') -Encoding UTF8
Write-Output ('INSTALLED='+$bundle)
Write-Output ('BACKUP='+$backup)
