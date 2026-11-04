param(
    [Parameter(Mandatory=$true)][string]$DrawingPath,
    [Parameter(Mandatory=$true)][string]$PluginPath,
    [Parameter(Mandatory=$true)][string]$RunDirectory,
    [switch]$ExpectFailure
)
$ErrorActionPreference='Stop'
$source=(Resolve-Path -LiteralPath $DrawingPath).Path
$sourceHash=(Get-FileHash -LiteralPath $source).Hash
$plugin=(Resolve-Path -LiteralPath $PluginPath).Path.Replace('\','/')
if(Test-Path -LiteralPath $RunDirectory){throw 'Use un directorio nuevo.'}
New-Item -ItemType Directory -Path $RunDirectory | Out-Null
$run=(Resolve-Path -LiteralPath $RunDirectory).Path
$drawing=Join-Path $run 'Detalle-inclinado.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$console=Join-Path $env:ProgramFiles 'Autodesk/AutoCAD 2024/accoreconsole.exe'
$stages=if($ExpectFailure){@('prepare')}else{@('prepare','reopen')}
foreach($stage in $stages){
    $command=if($stage -eq 'prepare'){'AISPELLPROBEINCLINEDDETAIL'}else{'AISPELLVERIFYINCLINEDDETAIL'}
    $finish=if($stage -eq 'prepare'){'_.QSAVE'}else{'(princ)'}
    $script=Join-Path $run ($stage+'.scr')
    $contents=@"
(setvar "FILEDIA" 0)
(setvar "SECURELOAD" 0)
(setvar "TILEMODE" 1)
(command "_.UCS" "_World")
_.NETLOAD
"$plugin"
$command
$finish
_.QUIT
_Y
"@
    [IO.File]::WriteAllText($script,$contents+[Environment]::NewLine,[Text.Encoding]::ASCII)
    $profile=Join-Path $run ($stage+'-profile');New-Item -ItemType Directory -Path $profile | Out-Null
    $output=& $console /i $drawing /s $script /isolate ('InclinedDetail-'+[Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code=$LASTEXITCODE
    $log=($output -join [Environment]::NewLine) -replace "`0",''
    [IO.File]::WriteAllText((Join-Path $run ($stage+'.log')),$log)
    $log -split '[\r\n]+' | Where-Object {$_ -match 'INCLINED_(VIEW|DETAIL_|BLOCK_)'}
    $marker=if($ExpectFailure){'INCLINED_DETAIL_RECOGNITION_FAILED'}elseif($stage -eq 'prepare'){'INCLINED_DETAIL_RECOGNITION_OK'}else{'INCLINED_DETAIL_REOPEN_OK'}
    if($code -ne 0 -or $log -notmatch ('(?m)^\s*'+$marker+'(?:\s|$)')){throw "No se encontro $marker en $run"}
    if(-not $ExpectFailure -and $stage -eq 'prepare' -and $log -notmatch '(?m)^\s*INCLINED_BLOCK_ROTATION_AND_AXIS_OK\s*$'){
        throw "Fallo la prueba de BlockReference girado/eje desplazado en $run"
    }
}
if((Get-FileHash -LiteralPath $source).Hash -ne $sourceHash){throw 'Cambio la copia de referencia.'}
Write-Output "PASS: $marker; referencia conservada. $drawing"
