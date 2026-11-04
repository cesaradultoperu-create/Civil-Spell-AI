param([Parameter(Mandatory=$true)][string]$DrawingPath, [double]$Tolerance = 0.001)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$run = Join-Path $repo ('artifacts\validation\tin-boundary\real-run-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$outputDir = Join-Path $run 'plugin'
dotnet build (Join-Path $repo 'CivilSpellAI.BordeTin3D.csproj') --configuration Release --no-restore /p:IncludeNativeTests=true /p:AssemblyName=CivilSpellAI.BordeTin3D.RealTest "/p:OutputPath=$outputDir"
if ($LASTEXITCODE -ne 0) { throw 'No se compilo la prueba.' }
$plugin = (Join-Path $outputDir 'CivilSpellAI.BordeTin3D.RealTest.dll').Replace('\','/')
$source = (Resolve-Path -LiteralPath $DrawingPath).Path
$hash = (Get-FileHash -LiteralPath $source).Hash
$drawing = Join-Path $run 'prueba estructura proyectada - BORDE3D.dwg'
Copy-Item -LiteralPath $source -Destination $drawing
$console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
$tol = $Tolerance.ToString('R',[Globalization.CultureInfo]::InvariantCulture)
foreach ($stage in @('prepare','verify')) {
    $commands = if ($stage -eq 'prepare') { @"
AISPELLDIAGNOSETINBOUNDARY
BORDETIN3D
Seleccion
_ALL

$tol
AISPELLVERIFYTINREALRESULT
_.QSAVE
"@ } else { 'AISPELLVERIFYTINREALRESULT' }
    $script = Join-Path $run ($stage + '.scr')
    $contents = @"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"$plugin"
$commands
_.QUIT
_Y

"@
    [IO.File]::WriteAllText($script,$contents,[Text.Encoding]::ASCII)
    $profile = Join-Path $run ($stage + '-profile')
    New-Item -ItemType Directory -Path $profile -Force | Out-Null
    $output = & $console /i $drawing /s $script /isolate ('TinReal-' + [Guid]::NewGuid().ToString('N')) $profile /l en-US 2>&1
    $code = $LASTEXITCODE
    $log = ($output -join [Environment]::NewLine) -replace "`0", ''
    $logPath = Join-Path $run ($stage + '.log')
    [IO.File]::WriteAllText($logPath,$log)
    if ($code -ne 0 -or $log -match 'TIN_REAL_RESULT_FAILED' -or $log -notmatch 'TIN_REAL_RESULT_OK') { throw "Fallo $stage. Registro: $logPath" }
    Write-Output ($log -split '[\r\n]+' | Where-Object { $_ -match 'TIN_REAL_RESULT_OK|CREACION CORRECTA|Cotas de referencia' })
}
if ((Get-FileHash -LiteralPath $source).Hash -ne $hash) { throw 'Cambio la copia de entrada.' }
Write-Output "PASS: Contorno del archivo real creado y reabierto. $drawing"
