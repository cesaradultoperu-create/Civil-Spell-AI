param(
    [Parameter(Mandatory=$true)][string]$DrawingPath,
    [Parameter(Mandatory=$true)][string]$PluginPath,
    [Parameter(Mandatory=$true)][string]$RunDirectory
)
$ErrorActionPreference='Stop'
$source=(Resolve-Path -LiteralPath $DrawingPath).Path
$hash=(Get-FileHash -LiteralPath $source).Hash
$plugin=(Resolve-Path -LiteralPath $PluginPath).Path.Replace('\','/')
if(Test-Path -LiteralPath $RunDirectory){throw 'Use un directorio de prueba nuevo.'}
New-Item -ItemType Directory -Path $RunDirectory | Out-Null
$run=(Resolve-Path -LiteralPath $RunDirectory).Path
$drawing=Join-Path $run 'Tres-parejas-TOP-FRONT.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$console=Join-Path $env:ProgramFiles 'Autodesk/AutoCAD 2024/accoreconsole.exe'
foreach($stage in @('prepare','reopen')){
    $command=if($stage -eq 'prepare'){'AISPELLPROBEREFERENCE3DREPORTED'}else{'AISPELLVERIFYREPORTEDREFERENCE3D'}
    # The source may already include delivered 3D examples at the test insertion
    # points. Remove only those generated blocks from this disposable copy.
    $reset=if($stage -eq 'prepare'){'(if (setq old3d (ssget "_X" ''((0 . "INSERT") (2 . "CIVILSPELLAI_REFERENCIAS_3D_*")))) (command "_.ERASE" old3d ""))'}else{'(princ)'}
    $finish=if($stage -eq 'prepare'){@'
(command "_.VPOINT" '(1 -1 1))
(if (setq new3d (ssget "_X" '((2 . "CIVILSPELLAI_REFERENCIAS_3D_*")))) (command "_.ZOOM" "_Object" new3d ""))
(command "_.ZOOM" "0.85x")
_.QSAVE
'@}else{''}
    $script=Join-Path $run ($stage+'.scr')
    $contents=@"
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"$plugin"
$reset
$command
$finish
_.QUIT
_Y
"@
    [IO.File]::WriteAllText($script,$contents+[Environment]::NewLine,[Text.Encoding]::ASCII)
    $output=& $console /i $drawing /s $script /isolate ('ReportedReference-'+[Guid]::NewGuid().ToString('N')) $run /l en-US 2>&1
    $code=$LASTEXITCODE
    $log=($output -join [Environment]::NewLine) -replace "`0",''
    [IO.File]::WriteAllText((Join-Path $run ($stage+'.log')),$log)
    $log -split '[\r\n]+' | Where-Object {$_ -match 'REPORTED_PAIR(_PASS|_FAIL| )|REPORTED_REOPEN_'}
    $markers=if($stage -eq 'prepare'){@('REPORTED_PAIR_PASS 0','REPORTED_PAIR_PASS 1','REPORTED_PAIR_PASS 2','REFERENCE3D_INCLINATION_OK')}else{@('REPORTED_REOPEN_PASS')}
    foreach($marker in $markers){if($log -notmatch ('(?m)^\s*'+$marker+'\s*$')){throw "Falta $marker en $run"}}
    if($code -ne 0 -or $log -match 'REPORTED_(PAIR|REOPEN)_FAIL'){throw "Falló $stage en $run"}
}
if((Get-FileHash -LiteralPath $source).Hash -ne $hash){throw 'Cambió la referencia original.'}
Write-Output "PASS: tres parejas, escala/desplazamiento independientes, sección circular, diámetro incompatible rechazado, sólidos huecos, resaltado/edición Y, persistencia/reapertura. $drawing"
