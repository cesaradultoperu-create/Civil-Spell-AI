param([ValidateSet('Snapshot','Inspect','InspectAfter','Screenshot')][string]$Action='Inspect')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$app=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.24.3')
$source='Q:\Proyectos 2025\25-017-011 - Gest. de Relaves -Cerro de Pasco\03_Designs\Distancias figura.dwg'
$work=Join-Path $root 'Distancias_figura_TRABAJO.dwg'
if($Action -eq 'Snapshot') {
  $doc=$app.Documents.Item('Distancias figura.dwg')
  if($doc.FullName -ne $source){throw 'Unexpected source.'}
  if($doc.GetVariable('CMDACTIVE') -ne 0 -or -not $app.GetAcadState().IsQuiescent){throw 'Civil 3D is busy.'}
  if(Test-Path -LiteralPath $work){throw 'Work drawing already exists.'}
  $before=Get-Item -LiteralPath $source | Select-Object FullName,Length,LastWriteTimeUtc
  $before | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'original-hash.json') -Encoding UTF8
  $doc.Activate()
  $path=$work.Replace('\','/')
  $cmd='(progn (setq dfOldFiledia (getvar "FILEDIA")) (setvar "FILEDIA" 0) (command "_.SAVE" "'+$path+'") (setvar "FILEDIA" dfOldFiledia) (setq dfOldFiledia nil) (princ))'
  $doc.SendCommand($cmd+[Environment]::NewLine)
  if(-not(Test-Path -LiteralPath $work)){throw 'Snapshot not written.'}
  $after=Get-Item -LiteralPath $source
  if($after.Length -ne $before.Length -or $after.LastWriteTimeUtc -ne $before.LastWriteTimeUtc){throw 'Source file changed.'}
  Write-Output ('SOURCE_STILL_OPEN='+$doc.FullName)
  Copy-Item -LiteralPath $work -Destination (Join-Path $root 'Estado_abierto_ANTES.dwg')
  $copy=$app.Documents.Open($work,$false)
  $copy.Activate()
  Write-Output ('WORK_OPEN='+$copy.FullName)
  exit
}
$doc=$app.Documents.Item('Distancias_figura_TRABAJO.dwg')
if($doc.FullName -ne $work){throw 'Unexpected work drawing.'}
if($Action -eq 'Screenshot') {
  Add-Type -AssemblyName System.Drawing
  Add-Type -AssemblyName System.Windows.Forms
  $bounds=[Windows.Forms.SystemInformation]::VirtualScreen
  $bitmap=New-Object Drawing.Bitmap $bounds.Width,$bounds.Height
  $graphics=[Drawing.Graphics]::FromImage($bitmap)
  $graphics.CopyFromScreen($bounds.Location,[Drawing.Point]::Empty,$bounds.Size)
  $bitmap.Save((Join-Path $root 'pantalla.png'),[Drawing.Imaging.ImageFormat]::Png)
  $graphics.Dispose();$bitmap.Dispose()
  exit
}
$entities=@()
foreach($e in $doc.ModelSpace) {
  $d=[ordered]@{handle=$e.Handle;type=$e.ObjectName;layer=$e.Layer;color=$e.Color;linetype=$e.Linetype;ltscale=$e.LinetypeScale}
  if($e.ObjectName -eq 'AcDbPolyline') {
    $d.width=$e.ConstantWidth;$d.length=$e.Length;$d.closed=$e.Closed;$d.coordinates=@($e.Coordinates);$d.elevation=$e.Elevation
  }
  if($e.ObjectName -match 'Text|Leader'){try{$d.text=$e.TextString}catch{}}
  if($e.ObjectName -eq 'AcDbBlockReference'){$d.block=$e.Name}
  $entities+=[pscustomobject]$d
}
$suffix=if($Action -eq 'InspectAfter'){'after'}else{'before'}
$entities | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $root ('model-'+$suffix+'.json')) -Encoding UTF8
$layouts=@()
foreach($l in $doc.Layouts) {
  if($l.ModelType){continue}
  $items=@()
  foreach($e in $l.Block) {
    $d=[ordered]@{handle=$e.Handle;type=$e.ObjectName;layer=$e.Layer}
    if($e.ObjectName -eq 'AcDbViewport') {$d.scale=$e.CustomScale;$d.center=@($e.Center);$d.width=$e.Width;$d.height=$e.Height;$d.target=@($e.Target);$d.direction=@($e.Direction);$d.locked=$e.DisplayLocked;$d.on=$e.ViewportOn}
    if($e.ObjectName -match 'Text|Leader'){try{$d.text=$e.TextString;$d.position=@($e.InsertionPoint)}catch{}}
    $items+=[pscustomobject]$d
  }
  $layouts+=@{name=$l.Name;items=$items;plotOrigin=@($l.PlotOrigin);paperUnits=$l.PaperUnits;plotType=$l.PlotType;rotation=$l.PlotRotation}
}
$layouts | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $root ('layouts-'+$suffix+'.json')) -Encoding UTF8
Write-Output ('INSPECTED='+$entities.Count+' model objects; '+$layouts.Count+' paper layouts')
