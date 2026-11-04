param([Parameter(Mandatory=$true)][string]$DrawingPath, [switch]$AllTin)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$run = Join-Path $repo ('artifacts\validation\tin-boundary\diagnose-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$outputDir = Join-Path $run 'plugin'
dotnet build (Join-Path $repo 'CivilSpellAI.BordeTin3D.csproj') --configuration Release --no-restore /p:IncludeNativeTests=true /p:AssemblyName=CivilSpellAI.BordeTin3D.Diagnostic "/p:OutputPath=$outputDir"
if ($LASTEXITCODE -ne 0) { throw 'No se compilo el diagnostico.' }
$plugin = (Join-Path $outputDir 'CivilSpellAI.BordeTin3D.Diagnostic.dll').Replace('\','/')
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
$hash = (Get-FileHash -LiteralPath $source).Hash
$diagnosticDrawing = Join-Path $run 'Diagnostico-TIN.dwg'
Copy-Item -LiteralPath $source -Destination $diagnosticDrawing
$script = Join-Path $run 'diagnose.scr'
$contents = @"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"$plugin"
AISPELLDIAGNOSETINBOUNDARY
_.QUIT
_Y

"@
[IO.File]::WriteAllText($script,$contents,[Text.Encoding]::ASCII)
$profile = Join-Path $run 'profile'
New-Item -ItemType Directory -Path $profile -Force | Out-Null
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
if ($AllTin) { $env:CIVILSPELLAI_DIAG_ALL_TINS = '1' }
$output = & $console /i $diagnosticDrawing /s $script /isolate ('TinDiagnose-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
$code = $LASTEXITCODE
$log = ($output -join [Environment]::NewLine) -replace "`0", ''
$logPath = Join-Path $run 'diagnose.log'
[IO.File]::WriteAllText($logPath,$log)
if ((Get-FileHash -LiteralPath $source).Hash -ne $hash) { throw 'Cambio la copia de entrada.' }
Write-Output "Registro: $logPath"
Write-Output "Geometria: $(Join-Path $outputDir 'live-tin-boundary.tsv')"
Write-Output ($log -split '[\r\n]+' | Where-Object { $_ -match 'TIN_DIAG_' })
if ($code -ne 0) { throw "Core Console termino con codigo $code" }
