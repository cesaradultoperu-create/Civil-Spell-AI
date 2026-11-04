param([string]$DrawingPath,[string]$RunDirectory,[string]$PluginPath,[switch]$IncludeCD)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
if(-not $DrawingPath) {$DrawingPath=Join-Path $repo 'artifacts/validation/figure3d/reference-snapshot-84b0a83871964209bee0865ab7385b01.dwg'}
if(-not $RunDirectory) {$RunDirectory=Join-Path $repo ('artifacts/validation/figure3d/reference-run-'+[Guid]::NewGuid().ToString('N'))}
if(-not $PluginPath) {$PluginPath=Join-Path $repo 'artifacts/validation/figure3d/reference-candidate/CivilSpellAI.dll'}
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$RunDirectory=(Resolve-Path -LiteralPath $RunDirectory).Path
$source=(Resolve-Path -LiteralPath $DrawingPath).Path
$hash=(Get-FileHash -LiteralPath $source).Hash
$drawing=Join-Path $RunDirectory 'Referencia-TOP-FRONT-3D.dwg'
if(Test-Path -LiteralPath $drawing) {throw 'Use un directorio de prueba nuevo.'}
Copy-Item -LiteralPath $source -Destination $drawing
$plugin=(Resolve-Path -LiteralPath $PluginPath).Path.Replace('\','/')
$console=Join-Path $env:ProgramFiles 'Autodesk/AutoCAD 2024/accoreconsole.exe'
foreach($stage in @('prepare','verify')) {
    $command=if($stage -eq 'prepare'){'AISPELLTESTREFERENCE3D'}else{'AISPELLVERIFYREFERENCE3D'}
    if($IncludeCD -and $stage -eq 'prepare'){$command+="`nAISPELLTESTREFERENCE3DCD"}
    $finish=if($stage -eq 'prepare'){@'
AISPELLREFERENCE3DUNDOCHANGE
_.U
AISPELLREFERENCE3DUNDOVERIFY
(command "_.VPOINT" '(1 -1 1))
(if (setq new3d (ssget "_X" '((2 . "CIVILSPELLAI_REFERENCIAS_3D_*")))) (command "_.ZOOM" "_Object" new3d ""))
(command "_.ZOOM" "0.8x")
_.QSAVE
_.QUIT
'@}else{"_.QUIT`n_Y"}
    $contents=@"
(setvar "CMDECHO" 0)
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"$plugin"
$command
$finish
"@
    $script=Join-Path $RunDirectory ($stage+'.scr')
    [IO.File]::WriteAllText($script,$contents+[Environment]::NewLine,[Text.Encoding]::ASCII)
    $profile=Join-Path $RunDirectory ($stage+'-profile');New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output=& $console /i $drawing /s $script /isolate ('Reference3D-'+[Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code=$LASTEXITCODE
    $log=($output -join [Environment]::NewLine) -replace "`0",''
    $path=Join-Path $RunDirectory ($stage+'.log');[IO.File]::WriteAllText($path,$log)
    $markers=if($stage -eq 'prepare'){@('REFERENCE3D_NATIVE_TOP_FRONT_OK','REFERENCE3D_COMPONENT_ARCS_HOLES_OK','REFERENCE3D_PREVIEW_PERSISTENCE_OK','REFERENCE3D_SINGLE_UNDO_OK','REFERENCE3D_HIGHLIGHT_OK','REFERENCE3D_INCLINATION_OK')}else{@('REFERENCE3D_REOPEN_OK')}
    if($IncludeCD -and $stage -eq 'prepare'){$markers+=@('REFERENCE3D_CD_OK')}
    foreach($marker in $markers) {if($log -notmatch ('(?m)^\s*'+$marker+'\s*$')) {throw "Falta $marker. Registro: $path"}}
    if($code -ne 0 -or $log -match 'REFERENCE3D_(CD_)?FAILED') {throw "Falló $stage. Registro: $path"}
}
if((Get-FileHash -LiteralPath $source).Hash -ne $hash) {throw 'Cambió el DWG de referencia.'}
Write-Output "PASS: TOP/FRONT, saliente, materiales, solado, eje inclinado, curvas y vacíos, sólido hueco, vista previa nativa, persistencia. $drawing"
