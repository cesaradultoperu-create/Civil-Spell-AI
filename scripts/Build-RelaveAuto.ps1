param([string]$OutputFolder='artifacts\RelaveAuto-0.3.5')
$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
dotnet build (Join-Path $taskRepo 'tools\RelaveAuto\RelaveVision.csproj') -c Release --nologo
if($LASTEXITCODE -ne 0){throw 'No se compilo el motor de vision'}
foreach($taskYear in @(2023,2024,2025,2026)){
 dotnet build (Join-Path $taskRepo 'tools\RelaveAuto\RelaveAuto.Multi.csproj') -c Release "/p:CivilYear=$taskYear" --nologo
 if($LASTEXITCODE -ne 0){throw "No se compilo el comando para $taskYear"}
}
$taskOutput=Join-Path $taskRepo $OutputFolder
$taskBundle=Join-Path $taskOutput 'RelaveAuto.bundle'
$taskContents=Join-Path $taskBundle 'Contents'
$taskEngine=Join-Path $taskContents 'Engine'
New-Item -ItemType Directory -Path $taskEngine -Force | Out-Null
$taskBin=Join-Path $taskRepo 'tools\RelaveAuto\bin\Release\net48'
foreach($taskYear in @(2023,2024,2025,2026)){
 $taskVersion=Join-Path $taskContents "Windows\$taskYear"
 New-Item -ItemType Directory -Path $taskVersion -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $taskRepo "tools\RelaveAuto\bin\Multi\$taskYear\RelaveAuto.$taskYear.dll") -Destination $taskVersion -Force
}
foreach($taskName in @('RelaveVision.exe','RelaveVision.exe.config','OpenCvSharp.dll','OpenCvSharpExtern.dll','System.Buffers.dll','System.Memory.dll','System.Numerics.Vectors.dll','System.Runtime.CompilerServices.Unsafe.dll')){
 Copy-Item -LiteralPath (Join-Path $taskBin $taskName) -Destination $taskEngine -Force
}
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\PackageContents.xml') -Destination $taskBundle -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\LEEME.txt') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\Instalar.ps1') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\INSTALAR.cmd') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\GUIA_INSTALACION.txt') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\EMPEZAR_AQUI.txt') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\VERIFICACION_JEFE.txt') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\THIRD_PARTY_NOTICES.txt') -Destination $taskOutput -Force
Copy-Item -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\RESULTADOS_PRUEBA.txt') -Destination $taskOutput -Force
$taskLicenses=Join-Path $taskOutput 'Licenses'
New-Item -ItemType Directory -Path $taskLicenses -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskRepo 'tools\RelaveAuto\Licenses') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $taskLicenses -Force }
Get-ChildItem -LiteralPath (Join-Path $taskContents 'Windows') -Recurse -Filter *.dll | Select-Object Name,Length
Write-Output $taskOutput
