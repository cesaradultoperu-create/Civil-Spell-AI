param([ValidateSet('Load','Prepare','Validate','Plot','Save')][string]$Action='Load')
$ErrorActionPreference='Stop'
$app=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application.24.3')
$d=$app.Documents.Item('Distancias_figura_TRABAJO.dwg')
$work=Join-Path $PSScriptRoot 'Distancias_figura_TRABAJO.dwg'
if($d.FullName -ne $work){throw 'Unexpected document.'}
if($d.GetVariable('CMDACTIVE') -ne 0 -or -not $app.GetAcadState().IsQuiescent){throw 'Civil 3D is busy.'}
$d.Activate()
if($Action -eq 'Load') {
  $folder=$PSScriptRoot.Replace('\','/')
  $dll=(Join-Path $PSScriptRoot 'Figuras.2024.v3.dll').Replace('\','/')
  $cmd='(progn (setq dfPreviousTrust (getvar "TRUSTEDPATHS")) (setvar "TRUSTEDPATHS" (strcat dfPreviousTrust ";'+$folder+'")) (command "_.NETLOAD" "'+$dll+'") (setvar "TRUSTEDPATHS" dfPreviousTrust) (setq dfPreviousTrust nil) (princ))'
  $d.SendCommand($cmd+[Environment]::NewLine+'DFINSPECT3'+[Environment]::NewLine)
} elseif($Action -eq 'Prepare') {
  $d.SendCommand('DFPREPARAR3'+[Environment]::NewLine+'DFVALIDAR3'+[Environment]::NewLine)
} elseif($Action -eq 'Validate') {
  $d.SendCommand('DFVALIDAR3'+[Environment]::NewLine)
} elseif($Action -eq 'Save') {
  $d.Save()
  Write-Output ('SAVED='+$d.FullName)
} elseif($Action -eq 'Plot') {
  $previous=$d.GetVariable('BACKGROUNDPLOT')
  try {
    $d.SetVariable('BACKGROUNDPLOT',0)
    foreach($n in 2..5) {
      $layout=@($d.Layouts | Where-Object {$_.Name -match ('^OPCION\s+'+$n+'$')})[0]
      if($null -eq $layout){throw ('Missing option '+$n)}
      $d.ActiveLayout=$layout
      $d.Regen(1)
      $path=Join-Path $PSScriptRoot ('OPCION_'+$n+'.pdf')
      if(-not $d.Plot.PlotToFile($path,'DWG To PDF.pc3')){throw ('Plot failed '+$n)}
      Write-Output ('PDF='+$path)
    }
    $d.ActiveLayout=$d.Layouts.Item('OPCION  2')
    $d.Regen(1)
  } finally {$d.SetVariable('BACKGROUNDPLOT',$previous)}
}
