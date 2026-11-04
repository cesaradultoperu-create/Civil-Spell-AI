$ErrorActionPreference='Stop'
. "$PSScriptRoot/CasillasField-Sotrami-20261022/session-api.ps1"
$fieldActivateApp=[FieldDemoWindow]::Active()
$fieldActivateDoc=$fieldActivateApp.ActiveDocument
if($fieldActivateDoc.Name -ne '1107.001.00.14-200-15-P-009.dwg'){throw 'El documento activo ha cambiado.'}
if([string]$fieldActivateDoc.GetVariable('CMDNAMES')){throw 'Hay un comando activo; no se interrumpe.'}
if([string]$fieldActivateDoc.GetVariable('CTAB') -ne 'Layout1'){throw 'La presentacion activa ha cambiado.'}
$fieldBlock=$fieldActivateDoc.HandleToObject('21F16')
if($fieldBlock.EffectiveName -ne 'MEMB_WWL_P_A1_'){throw 'No coincide el membrete.'}
$fieldActivateDoc.SendCommand('(command "CASILLASFIELD" (handent "21F16"))' + "`n")
Write-Output 'Solicitud de activacion enviada para el membrete 21F16.'
