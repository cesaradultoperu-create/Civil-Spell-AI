param([string]$Assembly='Fuga6.dll')
$ErrorActionPreference='Stop'
$refs=@('/nologo','/target:library',('/out:'+(Join-Path $PSScriptRoot $Assembly)),'/r:C:\Program Files\Autodesk\AutoCAD 2024\acdbmgd.dll','/r:C:\Program Files\Autodesk\AutoCAD 2024\accoremgd.dll','/r:C:\Program Files\Autodesk\AutoCAD 2024\acmgd.dll','/r:System.Web.Extensions.dll','/r:System.Drawing.dll')
$src=@('Fuga.cs','Build.cs','Delivery.cs','InspectV2.cs','PresentationV2.cs','PresentationLongV2.cs','PresentationDeliveryV2.cs')|ForEach-Object{Join-Path $PSScriptRoot $_}
& 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' $refs $src
if($LASTEXITCODE -ne 0){throw 'Error de compilacion.'}
