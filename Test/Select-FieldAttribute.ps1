$ErrorActionPreference='Stop'
. "$PSScriptRoot/CasillasField-Sotrami-20261022/ventanas.ps1" -ProcessId 15812 | Out-Null
$visible=@([FieldLive]::Windows(15812) | Where-Object {[FieldLive]::IsWindowVisible($_)})
foreach($window in $visible){if([FieldLive]::Class($window) -eq '#32768'){[void][FieldLive]::PostMessage($window,0x100,[IntPtr]27,[IntPtr]::Zero)}}
Start-Sleep -Milliseconds 250
$editor=$visible | Where-Object {[FieldLive]::Title($_) -eq 'Enhanced Attribute Editor'} | Select-Object -First 1
if(-not $editor){throw 'El editor no esta abierto.'}
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$root=[Windows.Automation.AutomationElement]::FromHandle($editor)
$condition=New-Object Windows.Automation.PropertyCondition ([Windows.Automation.AutomationElement]::ControlTypeProperty),([Windows.Automation.ControlType]::ListItem)
$items=$root.FindAll([Windows.Automation.TreeScope]::Descendants,$condition)
foreach($item in $items){
 if($item.Current.Name -eq 'R2_DS'){
  $pattern=$item.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)
  $pattern.Select()
  Write-Output ('SELECTED '+$item.Current.Name)
  exit 0
 }
}
$list=[FieldLive]::Children($editor) | Where-Object {[FieldLive]::Class($_) -eq 'SysListView32' -and [FieldLive]::GetDlgCtrlID($_) -eq 1001} | Select-Object -First 1
if(-not $list){throw 'No se encontro la lista de atributos.'}
[void][FieldLive]::SendNumber($list,0x100,[IntPtr]36,[IntPtr]::Zero)
[void][FieldLive]::SendNumber($list,0x101,[IntPtr]36,[IntPtr]::Zero)
for($i=0;$i -lt 36;$i++){
 [void][FieldLive]::SendNumber($list,0x100,[IntPtr]40,[IntPtr]::Zero)
 [void][FieldLive]::SendNumber($list,0x101,[IntPtr]40,[IntPtr]::Zero)
}
Start-Sleep -Milliseconds 400
$tag=[FieldLive]::Children($editor) | Where-Object {[FieldLive]::Class($_) -eq 'Static' -and [FieldLive]::GetDlgCtrlID($_) -eq 1026} | Select-Object -First 1
$selected=[FieldLive]::Text($tag)
Write-Output ('SELECTED '+$selected)
if($selected -ne 'R2_DS'){throw 'No coincide la casilla; no se abre Field.'}
