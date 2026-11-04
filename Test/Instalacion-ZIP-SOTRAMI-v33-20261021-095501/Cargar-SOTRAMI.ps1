$ErrorActionPreference = 'Stop'
function Invoke-Cad([scriptblock]$Action) {
    for ($attempt=0; $attempt -lt 40; $attempt++) {
        try { return (& $Action) }
        catch {
            if ($_.Exception.ToString() -notmatch '80010001|8001010A') { throw }
            Start-Sleep -Milliseconds 250
        }
    }
    throw 'CIVIL 3D SIGUE OCUPADO. TERMINE EL COMANDO ACTIVO Y VUELVA A CARGAR.'
}
$config = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SOTRAMI.config.json') -Raw | ConvertFrom-Json
$drawing = [IO.Path]::GetFullPath((Join-Path $config.ProjectDirectory $config.DrawingName))
try { $app = [Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application') }
catch { throw 'Abra Civil 3D y el plano del proyecto antes de ejecutar este cargador.' }
if ((Invoke-Cad { $app.ActiveDocument.GetVariable('CMDACTIVE') }) -ne 0) { throw 'TERMINE EL COMANDO ACTIVO EN CIVIL 3D Y VUELVA A CARGAR.' }
$document = $null
foreach ($item in $app.Documents) {
    if ([String]::Equals($item.FullName, $drawing, [StringComparison]::OrdinalIgnoreCase)) { $document = $item; break }
}
if ($null -eq $document) { throw "Abra primero este plano desde Q: $drawing" }
Invoke-Cad { $document.Activate() }
if ($document.ActiveLayout.Name -ne 'A') { $document.ActiveLayout = $document.Layouts.Item('A') }
$version = ([string](Invoke-Cad { $document.GetVariable('ACADVER') })).Substring(0,4)
switch ($version) {
    '24.2' { $family = '2023-2024' }
    '24.3' { $family = '2023-2024' }
    '25.0' { $family = '2025' }
    default { throw 'Este paquete esta preparado para Civil 3D 2023, 2024 y 2025.' }
}
$dll = Join-Path $PSScriptRoot "VinculoPlanosSOTRAMI.v33.$family.dll"
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw "Falta el DLL: $dll. Descomprima toda la carpeta del ZIP." }
$set = Join-Path (Join-Path $config.ProjectDirectory 'Xref') $config.SheetSetName
if (-not (Test-Path -LiteralPath $set -PathType Leaf)) { throw "No se encuentra el conjunto del proyecto: $set" }
$teamSet = $set
if (-not (Test-Path -LiteralPath $teamSet -PathType Leaf)) { throw "No se encuentra el SOTRAMI original del equipo: $teamSet" }
foreach ($path in @($dll, $set)) { if ($path.Contains('"')) { throw 'Ruta con comillas no admitida.' } }
$dllArg = $dll.Replace('\','/')
$setArg = $set.Replace('\','/')
$started = [DateTime]::UtcNow
Invoke-Cad { $document.SendCommand("(progn (command `"_.NETLOAD`" `"$dllArg`") (command `"SOTRAMICONJUNTO`") (command `"SOTRAMIINFO`") (princ))`n") }
$log = Join-Path $PSScriptRoot 'Registros\carga.json'
for ($attempt=0; $attempt -lt 120; $attempt++) {
    if ((Test-Path -LiteralPath $log) -and (Get-Item -LiteralPath $log).LastWriteTimeUtc -ge $started.AddSeconds(-1)) {
        $info = Get-Content -LiteralPath $log -Raw | ConvertFrom-Json
        if ($info.success -and $info.assembly -eq "VinculoPlanosSOTRAMI.v33.$family") { break }
    }
    Start-Sleep -Milliseconds 250
}
if ($null -eq $info -or -not $info.success -or $info.assembly -ne "VinculoPlanosSOTRAMI.v33.$family" -or (Get-Item -LiteralPath $log).LastWriteTimeUtc -lt $started.AddSeconds(-1)) { throw 'NO SE CONFIRMO LA CARGA ACTUAL. REVISE LOS AVISOS DE CIVIL 3D.' }
Write-Host "VINCULO CARGADO PARA CIVIL 3D $family."
Write-Host "PLANO: $drawing"
Write-Host "CONJUNTO SOTRAMI DEL EQUIPO: $teamSet"
Write-Host 'GUARDE EL EXCEL, VUELVA A CIVIL 3D Y EJECUTE SOTRAMIEXCEL. DESPUES GUARDE LOS DWG MODIFICADOS CON CTRL+S.'




