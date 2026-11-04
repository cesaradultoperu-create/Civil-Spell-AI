param([Parameter(Mandatory=$true)][string]$DrawingPath)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $DrawingPath -PathType Leaf)) { throw 'No se encuentra el DWG indicado.' }
. "$PSScriptRoot/../CasillasField-Sotrami-20261022/session-api.ps1"
$app = [FieldDemoWindow]::Active()
if (-not $app.GetAcadState().IsQuiescent) { throw 'Civil 3D esta ocupado; no se envio ninguna orden de apertura.' }
$existing = @($app.Documents | Where-Object { $_.FullName -eq $DrawingPath })
if ($existing.Count -gt 0) {
    $doc = $existing[0]
    if (-not $doc.ReadOnly) { throw 'El dibujo ya esta abierto para escritura. No se cambio su estado.' }
    $doc.Activate()
} else {
    $doc = $app.Documents.Open($DrawingPath, $true)
}
[ordered]@{Drawing=$doc.FullName; ReadOnly=$doc.ReadOnly; Saved=$doc.Saved; ActiveDrawing=$app.ActiveDocument.Name} | ConvertTo-Json
