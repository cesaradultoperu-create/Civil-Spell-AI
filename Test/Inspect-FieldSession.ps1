$ErrorActionPreference='Stop'
. "$PSScriptRoot/CasillasField-Sotrami-20261022/session-api.ps1"
$fieldSessionApp=[FieldDemoWindow]::Active()
$fieldSessionDoc=$fieldSessionApp.ActiveDocument
$report=[ordered]@{Drawing=[string]$fieldSessionDoc.FullName;Command=[string]$fieldSessionDoc.GetVariable('CMDNAMES');Layout=[string]$fieldSessionDoc.GetVariable('CTAB');ReadOnly=[bool]$fieldSessionDoc.ReadOnly;Selection=@();Blocks=@()}
foreach($entity in $fieldSessionDoc.PickfirstSelectionSet){$report.Selection+=@{Handle=[string]$entity.Handle;Type=[string]$entity.ObjectName}}
foreach($entity in $fieldSessionDoc.PaperSpace){
 if($entity.ObjectName -eq 'AcDbBlockReference' -and $entity.HasAttributes){
  $report.Blocks+=@{Handle=[string]$entity.Handle;Name=[string]$entity.EffectiveName;Attributes=@($entity.GetAttributes() | ForEach-Object {@{Handle=[string]$_.Handle;Tag=[string]$_.TagString;Value=[string]$_.TextString}})}
 }
}
$report | ConvertTo-Json -Depth 6
