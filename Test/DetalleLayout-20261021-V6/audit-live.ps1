$ErrorActionPreference='Stop'
$app=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.24.3')
$doc=$app.ActiveDocument
if($doc.GetVariable('CMDACTIVE') -ne 0){throw 'Hay un comando activo; no se interrumpe.'}
$dll=(Join-Path $env:APPDATA 'Autodesk/ApplicationPlugins/DetalleAutoLado.bundle/Contents/Windows/DetalleTextAudit.2024.dll').Replace('\','/')
$lisp='(progn (setq detalleFileDia (getvar "FILEDIA")) (setvar "FILEDIA" 0) (command "_.NETLOAD" "'+$dll+'") (setvar "FILEDIA" detalleFileDia) (princ))'
$doc.SendCommand($lisp+"`nDETALLETEXTAUDIT`n")
Write-Output $doc.Name
