param([Parameter(Mandatory=$true)][string]$Package)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..\..')).Path
$packageRoot=(Resolve-Path -LiteralPath $Package).Path
$validation=Join-Path $repo ('Test\auditoria-20261019\prueba-instalador\v35_'+[Guid]::NewGuid().ToString('N'))
$installer=Join-Path $packageRoot 'Instalar-SOTRAMI.ps1'
& $installer -ValidationRoot $validation | Out-Null
$bundle=Join-Path $validation 'SOTRAMIExcel.bundle'
$configFile=Join-Path $bundle 'Contents\SOTRAMI.config.json'
$settings=Get-Content -LiteralPath $configFile -Raw | ConvertFrom-Json
$settings.ProjectDirectory=Join-Path $validation 'Proyecto-configurado-por-usuario'
$settings | ConvertTo-Json | Set-Content -LiteralPath $configFile -Encoding UTF8
$expectedConfig=[IO.File]::ReadAllText($configFile)
New-Item -ItemType Directory -Force (Join-Path $bundle 'Contents\Registros'),(Join-Path $validation 'OtroPlugin.bundle') | Out-Null
Set-Content -LiteralPath (Join-Path $bundle 'Contents\Registros\sentinel.txt') -Value 'REGISTRO CONSERVADO'
Set-Content -LiteralPath (Join-Path $validation 'OtroPlugin.bundle\sentinel.txt') -Value 'OTRO PLUGIN CONSERVADO'
& $installer -ValidationRoot $validation | Out-Null
if([IO.File]::ReadAllText($configFile) -ne $expectedConfig){throw 'Configuracion no conservada'}
if((Get-Content -LiteralPath (Join-Path $bundle 'Contents\Registros\sentinel.txt')).Trim() -ne 'REGISTRO CONSERVADO'){throw 'Registro perdido'}
if((Get-Content -LiteralPath (Join-Path $validation 'OtroPlugin.bundle\sentinel.txt')).Trim() -ne 'OTRO PLUGIN CONSERVADO'){throw 'Otro plugin modificado'}
foreach($family in @('2023-2024','2025')){
    $name='VinculoPlanosSOTRAMI.v35.'+$family+'.dll'
    if((Get-FileHash -LiteralPath (Join-Path $packageRoot $name)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $bundle ('Contents\'+$name))).Hash){throw 'DLL distinto'}
}
$result=@{success=$true;newInstall=$true;reinstall=$true;configurationPreserved=$true;logsPreserved=$true;otherPluginUnchanged=$true;validationRoot=$validation}
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repo 'Test\Sotrami-Sheets-v35-20261030\installer-test.json') -Encoding UTF8
$result | ConvertTo-Json
