param([string]$Command='FUGADIAG',[string]$Drawing='Fuente\Base.dwg',[string]$Tag='diag',[string]$Assembly='Fuga.dll')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$script=Join-Path $root ($Tag+'.scr')
$dll=(Join-Path $root $Assembly).Replace('\','/')
[IO.File]::WriteAllText($script,"(setvar `"FILEDIA`" 0)`r`n(setvar `"CMDECHO`" 0)`r`n(setvar `"SECURELOAD`" 0)`r`n_.NETLOAD`r`n`"$dll`"`r`n$Command`r`n_.QUIT`r`n_Y`r`n",[Text.Encoding]::ASCII)
$si=New-Object Diagnostics.ProcessStartInfo
$si.FileName='C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe'
$inputDrawing=if([IO.Path]::IsPathRooted($Drawing)){$Drawing}else{Join-Path $root $Drawing}
$si.Arguments='/i "'+$inputDrawing+'" /s "'+$script+'" /isolate Fuga-'+$Tag+' "'+(Join-Path $root ('profile-'+$Tag))+'" /l en-US'
$si.UseShellExecute=$false
$si.CreateNoWindow=$true
$si.RedirectStandardOutput=$true
$si.RedirectStandardError=$true
$p=New-Object Diagnostics.Process
$p.StartInfo=$si
[void]$p.Start()
try{$p.PriorityClass=[Diagnostics.ProcessPriorityClass]::BelowNormal}catch{}
$out=$p.StandardOutput.ReadToEndAsync()
$err=$p.StandardError.ReadToEndAsync()
$p.WaitForExit()
[IO.File]::WriteAllText((Join-Path $root ($Tag+'.log')),($out.Result+$err.Result).Replace([string][char]0,''))
Write-Output ('ExitCode='+$p.ExitCode)
Get-Content -LiteralPath (Join-Path $root ($Tag+'.log')) -Tail 35
