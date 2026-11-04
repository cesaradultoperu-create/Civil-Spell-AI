param([string]$PluginPath,[switch]$Load,[string]$DrawingPath,[switch]$Adaptive,[switch]$Mask,[switch]$Centered)
$ErrorActionPreference='Stop'
$app=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.24.3')
$doc=$app.ActiveDocument
if($null -eq $doc -and $DrawingPath) {
    if(-not (Test-Path -LiteralPath $DrawingPath)){throw 'No se encontro el DWG de prueba.'}
    $doc=$app.Documents.Open($DrawingPath,$false)
    $doc.Activate()
}
if($null -eq $doc){throw 'Civil 3D no tiene un documento activo para cargar el complemento.'}
Write-Output "ACTIVE_DRAWING: $($doc.Name)"
Write-Output "COMMAND_ACTIVE: $($doc.GetVariable('CMDACTIVE'))"
if($Load) {
    if($doc.GetVariable('CMDACTIVE') -ne 0) {throw 'Civil 3D esta ejecutando un comando. El complemento se cargara en el siguiente inicio.'}
    if(-not (Test-Path -LiteralPath $PluginPath)) {throw 'No se encontro la DLL.'}
    $normalized=$PluginPath.Replace('\','/')
    $trusted=[string]$doc.GetVariable('TRUSTEDPATHS')
    $folder=(Split-Path -Parent $PluginPath).Replace('\','/')
    if(-not (($trusted -split ';') | Where-Object {$_.Replace('\','/').TrimEnd('/') -eq $folder})) {
        $doc.SetVariable('TRUSTEDPATHS',($trusted.TrimEnd(';')+';'+$folder).TrimStart(';'))
    }
    $lisp='(progn (setq detalleFileDia (getvar "FILEDIA")) (setvar "FILEDIA" 0) (command "_.NETLOAD" "'+$normalized+'") (setvar "FILEDIA" detalleFileDia) (setq detalleFileDia nil) (princ))'
    $refresh=if($Centered){'DETALLEACTUALIZARTODOS6'}elseif($Mask){'DETALLEACTUALIZARTODOS5'}elseif($Adaptive){'DETALLEACTUALIZARTODOS'}else{'DETALLEAUTOLADO'}
    $status=if($Centered){'DETALLEADAPTABLEESTADO6'}elseif($Mask){'DETALLEADAPTABLEESTADO5'}elseif($Adaptive){'DETALLEADAPTABLEESTADO'}else{'DETALLEAUTOESTADO'}
    $command=$lisp+[Environment]::NewLine+$refresh+[Environment]::NewLine+$status+[Environment]::NewLine
    if($Centered){$command+='DETALLECOMPROBAR6'+[Environment]::NewLine}
    $doc.SendCommand($command)
    Write-Output 'AUTO_SIDE_LOAD_COMMAND_SENT'
}
