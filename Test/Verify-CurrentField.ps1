param([switch]$OpenAttribute,[switch]$OpenContext,[switch]$OpenField)
$ErrorActionPreference='Stop'
. "$PSScriptRoot/CasillasField-Sotrami-20261022/session-api.ps1"
. "$PSScriptRoot/CasillasField-Sotrami-20261022/ventanas.ps1" -ProcessId 15812 | Out-Null
$fieldWindows=@([FieldLive]::Windows(15812) | Where-Object {[FieldLive]::IsWindowVisible($_)})
foreach($window in $fieldWindows){
 $title=[FieldLive]::Title($window)
 if($title -match 'Field|celdas|Enhanced Attribute Editor|Autodesk Civil'){
  Write-Output "WINDOW $window $title"
  if($title -eq 'Field' -or $title -like '*celdas*' -or $title -eq 'Enhanced Attribute Editor'){
   foreach($child in [FieldLive]::Children($window)){
    $value=[FieldLive]::Text($child)
    if($value){Write-Output ("CONTROL "+[FieldLive]::GetDlgCtrlID($child)+" "+[FieldLive]::Class($child)+" "+$value)}
   }
  }
 }
 if($OpenContext -and $title -eq 'Enhanced Attribute Editor'){
  if(-not [FieldDemoWindow]::OpenContext($window,5)){throw 'No se encontro Value.'}
 }
 if($OpenField -and [FieldLive]::Class($window) -eq '#32768'){
  if(-not [FieldDemoWindow]::HasEditField($window)){throw 'El menu no ofrece Edit Field.'}
  [void][FieldDemoWindow]::PostMessage($window,0x102,[IntPtr]101,[IntPtr]::Zero)
  Write-Output 'EDIT_FIELD_REQUESTED'
 }
}
if($OpenAttribute){
 $app=[FieldDemoWindow]::Active();$doc=$app.ActiveDocument
 if($doc.Name -ne '1107.001.00.14-200-15-P-009.dwg'){throw 'El documento activo ha cambiado.'}
 $command=[string]$doc.GetVariable('CMDNAMES')
 Write-Output ('COMMAND '+$command+' LASTPROMPT '+$doc.GetVariable('LASTPROMPT'))
 if($command){throw 'Hay un comando activo; no se interrumpe.'}
 $attribute=$doc.HandleToObject('21F20')
 if($attribute.TagString -ne '04_R2_DESCRIP'){throw 'No coincide la casilla.'}
 $point=($attribute.InsertionPoint | ForEach-Object {([double]$_).ToString('R',[Globalization.CultureInfo]::InvariantCulture)}) -join ' '
 $doc.SendCommand('(command "_.EATTEDIT" (list (handent "21F16") (trans ''('+ $point + ') 0 1)))' + "`n")
}
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$bounds=[Windows.Forms.SystemInformation]::VirtualScreen
$bitmap=New-Object Drawing.Bitmap $bounds.Width,$bounds.Height
$graphics=[Drawing.Graphics]::FromImage($bitmap)
try{$graphics.CopyFromScreen($bounds.Location,[Drawing.Point]::Empty,$bounds.Size);$bitmap.Save((Join-Path $PSScriptRoot 'field-activation-screen.png'))}finally{$graphics.Dispose();$bitmap.Dispose()}
