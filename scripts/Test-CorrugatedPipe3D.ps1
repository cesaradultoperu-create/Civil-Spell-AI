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
$drawing=Join-Path $run 'Tuberia300-3D.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$console=Join-Path $env:ProgramFiles 'Autodesk/AutoCAD 2024/accoreconsole.exe'
foreach($stage in @('prepare','reopen')){
    $command=if($stage -eq 'prepare'){'AISPELLTESTPIPE300'}else{'AISPELLVERIFYPIPE300'}
    $finish=if($stage -eq 'prepare'){@'
(command "_.VPOINT" '(1 -1 1))
(if (setq model (ssget "_X" '((2 . "CIVILSPELLAI_REFERENCIAS_3D_*")))) (command "_.ZOOM" "_Object" model ""))
(command "_.VSCURRENT" "_Conceptual")
_.QSAVE
'@}else{''}
    $contents=@"
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
_.NETLOAD
"$plugin"
$command
$finish
_.QUIT
_Y
"@
    $script=Join-Path $run ($stage+'.scr')
    [IO.File]::WriteAllText($script,$contents+[Environment]::NewLine,[Text.Encoding]::ASCII)
    $profile=Join-Path $run ($stage+'-profile')
    New-Item -ItemType Directory -Path $profile | Out-Null
    $output=& $console /i $drawing /s $script /isolate ('Pipe300-'+[Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $exitCode=$LASTEXITCODE
    $log=($output -join [Environment]::NewLine) -replace "`0",''
    [IO.File]::WriteAllText((Join-Path $run ($stage+'.log')),$log)
    $marker=if($stage -eq 'prepare'){'PIPE300_NATIVE_PASS'}else{'PIPE300_REOPEN_PASS'}
    if($exitCode -ne 0 -or $log -notmatch ('(?m)^\s*'+$marker+'\s*$') -or $log -match 'PIPE300_(NATIVE|REOPEN)_FAIL'){
        throw "Falló $stage. Consulte $run."
    }
    if($stage -eq 'prepare' -and $log -notmatch 'PIPE300_REAL_CORRUGATION_PASS'){throw 'No pasó la prueba de relieve físico.'}
}
if((Get-FileHash -LiteralPath $source).Hash -ne $hash){throw 'Cambió el DWG original.'}
Write-Output "PASS: bloques anidados/reflejados, diámetro 300 mm, FRONT gobierna X/Z, corrugado sólido con crestas/valles, ramal T, bandas de tapas, tubos huecos, unión interior libre, edición y reapertura. $drawing"
