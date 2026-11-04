param([Parameter(Mandatory=$true)][string]$GeometryPath)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$run=Join-Path $repo ('artifacts\validation\tin-boundary\nine-run-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$outputDir=Join-Path $run 'plugin'
dotnet build (Join-Path $repo 'CivilSpellAI.BordeTin3D.csproj') --configuration Release --no-restore /p:IncludeNativeTests=true /p:AssemblyName=CivilSpellAI.BordeTin3D.NineTest "/p:OutputPath=$outputDir"
if($LASTEXITCODE -ne 0) {throw 'No se compilo la prueba de nueve TIN.'}
$plugin=(Join-Path $outputDir 'CivilSpellAI.BordeTin3D.NineTest.dll').Replace('\','/')
$env:CIVILSPELLAI_NINE_TSV=(Resolve-Path -LiteralPath $GeometryPath).Path
$template=Join-Path $env:LOCALAPPDATA 'Autodesk\C3D 2024\enu\Template\AutoCAD Template\acad.dwt'
$drawing=Join-Path $run 'Resultados-nueve-TIN.dwg'
Copy-Item -LiteralPath $template -Destination $drawing
$console=Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
foreach($stage in @('prepare','verify')) {
    $commands=if($stage -eq 'prepare') {"AISPELLTESTTINNINESESSION`nAISPELLVERIFYTINNINE`n_.QSAVE"} else {'AISPELLVERIFYTINNINE'}
    $contents=@"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"$plugin"
$commands
_.QUIT
_Y

"@
    $script=Join-Path $run ($stage+'.scr');[IO.File]::WriteAllText($script,$contents,[Text.Encoding]::ASCII)
    $profile=Join-Path $run ($stage+'-profile');New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output=& $console /i $drawing /s $script /isolate ('TinNine-'+[Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code=$LASTEXITCODE;$log=($output -join [Environment]::NewLine) -replace "`0",''
    $logPath=Join-Path $run ($stage+'.log');[IO.File]::WriteAllText($logPath,$log)
    if($code -ne 0 -or $log -match 'TIN_NINE_SESSION_FAILED' -or $log -notmatch 'TIN_NINE_REOPEN_OK') {throw "Fallo $stage. Registro: $logPath"}
    if($stage -eq 'prepare' -and $log -notmatch 'TIN_NINE_SESSION_OK') {throw "No se genero el conjunto. Registro: $logPath"}
    Write-Output ($log -split '[\r\n]+' | Where-Object {$_ -match 'TIN_NINE_SESSION_OK|TIN_NINE_REOPEN_OK'})
}
Write-Output "PASS: Nueve TIN reales exportados, exterior y Pick Points, XYZ nativos y reapertura. $drawing"
