$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$editor=[Windows.Automation.AutomationElement]::FromHandle([IntPtr]662116)
if($editor.Current.Name -ne 'Enhanced Attribute Editor' -or $editor.Current.ProcessId -ne 39836){throw 'No coincide el editor de la instancia de prueba.'}
$rows=$editor.FindAll([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.Condition]::TrueCondition)
Write-Output ('ROWS '+$rows.Count)
$target=$null
foreach($row in $rows){if($row.Current.Name -eq 'R2_DS'){$target=$row;break}}
if(-not $target){$rows | Select-Object -First 25 | ForEach-Object { Write-Output ($_.Current.ControlType.ProgrammaticName+' '+$_.Current.Name) }; throw 'No se encontro R2_DS en la lista nativa.'}
$pattern=$target.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)
$pattern.Select()
Write-Output ('SELECTED '+$target.Current.Name)