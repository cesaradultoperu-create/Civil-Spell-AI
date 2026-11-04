param([Parameter(Mandatory=$true)][string]$DrawingPath,[Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference='Stop'
$app=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.24.3')
$doc=$app.ActiveDocument
if($null -eq $doc -or $doc.FullName -ne $DrawingPath){throw 'La V3 no es el dibujo activo.'}
if($doc.GetVariable('CMDACTIVE') -ne 0){throw 'Hay un comando activo; no se mueve el simbolo.'}
$marker=@($doc.PaperSpace | Where-Object {$_.ObjectName -eq 'AcDbMLeader'})
if($marker.Count -ne 1){throw 'Se esperaba un unico detalle en Layout1.'}
$marker=$marker[0]
[double[]]$original=$marker.GetLeaderLineVertices(0)
$originalName=$marker.ContentBlockName
$cx=$original[$original.Length-3];$cy=$original[$original.Length-2]
function Read-Texts {
    $values=@{}
    foreach($attribute in $doc.Blocks.Item($marker.ContentBlockName)) {
        if($attribute.ObjectName -eq 'AcDbAttributeDefinition') {$values[$attribute.TagString]=$marker.GetBlockAttributeValue($attribute.ObjectID)}
    }
    return $values
}
$texts=Read-Texts
if($texts.Count -ne 4){throw 'Faltan atributos en el detalle.'}
$report=@()
try {
    foreach($dx in @(35.0,-35.0)) {
        [double[]]$coordinates=$original.Clone()
        $coordinates[0]=$cx+$dx;$coordinates[1]=$cy-30
        $marker.SetLeaderLineVertices(0,$coordinates)
        $expected=if($dx -lt 0){'DETALLE_LAYOUT_REFERENCIA_V3_DERECHA'}else{'DETALLE_LAYOUT_REFERENCIA_V2'}
        $matched=$false
        for($i=0;$i -lt 25;$i++) {
            Start-Sleep -Milliseconds 200
            if($marker.ContentBlockName -eq $expected){$matched=$true;break}
        }
        if(-not $matched){throw "El cambio automatico no respondio: $expected"}
        $after=Read-Texts
        foreach($tag in $texts.Keys){if($after[$tag] -ne $texts[$tag]){throw "Cambio el texto $tag"}}
        $report+="LIVE_AUTO_OK arrow_dx=$dx block=$($marker.ContentBlockName) attributes_preserved=4"
    }
} finally {
    $marker.SetLeaderLineVertices(0,$original)
    for($i=0;$i -lt 25;$i++){if($marker.ContentBlockName -eq $originalName){break};Start-Sleep -Milliseconds 200}
}
$report+='LIVE_ORIGINAL_GEOMETRY_RESTORED'
$report | Set-Content -LiteralPath $ReportPath -Encoding utf8
$report | Write-Output
