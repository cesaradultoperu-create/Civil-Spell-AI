param(
    [string]$SourceDirectory = 'C:\Users\Cesar Ugaz\Pictures\cambio de numero',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo ('artifacts\validation\dren-bordes\' + [Guid]::NewGuid().ToString('N')) }
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$sources = @('Drainage Stage 1.dxf','Drainage Stage 2.dxf','Drainage Stage 3.dxf') | ForEach-Object {
    $path = Join-Path $SourceDirectory $_
    if (Test-Path -LiteralPath $path) { Get-FileHash -LiteralPath $path -Algorithm SHA256 }
}
& dotnet build (Join-Path $PSScriptRoot 'DrenBordes.csproj') -c Release --no-restore /p:IncludeNativeTests=true /p:AssemblyName=DrenBordes.Tests /p:OutputPath=bin/NativeTests -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion.' }
$dll = (Join-Path $PSScriptRoot 'bin\NativeTests\DrenBordes.Tests.dll').Replace('\','/')
$script = Join-Path $OutputDirectory 'test.scr'
[IO.File]::WriteAllText($script, @"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"$dll"
DRENBORDESTEST
_.QUIT
_Y

"@, [Text.Encoding]::ASCII)
$previousSource = $env:DRENBORDES_SOURCE_DIR
$previousOutput = $env:DRENBORDES_OUTPUT_DIR
try {
    $env:DRENBORDES_SOURCE_DIR = $SourceDirectory
    $env:DRENBORDES_OUTPUT_DIR = $OutputDirectory
    $console = Join-Path $env:ProgramFiles 'Autodesk\AutoCAD 2024\accoreconsole.exe'
    $processInfo = New-Object Diagnostics.ProcessStartInfo
    $processInfo.FileName = $console
    $template = Join-Path $env:LOCALAPPDATA 'Autodesk\C3D 2024\enu\Template\AutoCAD Template\acad.dwt'
    $drawing = Join-Path $OutputDirectory 'test.dwg'
    Copy-Item -LiteralPath $template -Destination $drawing
    $processInfo.Arguments = '/i "' + $drawing + '" /s "' + $script + '" /isolate DrenBordes-' + [Guid]::NewGuid().ToString('N') + ' "' + (Join-Path $OutputDirectory 'profile') + '" /l en-US'
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $processInfo
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'AutoCAD excedio tres minutos.' }
    $log = ($stdout.Result + $stderr.Result) -replace "`0", ''
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'console.log'), $log)
    foreach ($marker in @('DREN_GEOMETRY_OK','DREN_STAGE_1_OK','DREN_STAGE_2_OK','DREN_STAGE_3_OK','DREN_ALL_OK')) {
        if ($log -notmatch [regex]::Escape($marker)) { throw "Falta $marker. Consulte $OutputDirectory\console.log" }
    }
    if ($process.ExitCode -ne 0 -or $log -match 'DREN_TEST_FAILED') { throw "Fallo de prueba. Consulte $OutputDirectory\console.log" }
    & dotnet build (Join-Path $PSScriptRoot 'DrenBordes.csproj') -c Release --no-restore -v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion de entrega.' }
    $production = (Join-Path $PSScriptRoot 'bin\Release\net48\DrenBordes.2024.dll').Replace('\','/')
    $interactive = Join-Path $OutputDirectory 'interactive.scr'
    [IO.File]::WriteAllText($interactive, @"
(setvar "FILEDIA" 0)
(setvar "CMDECHO" 0)
(setvar "SECURELOAD" 0)
_.NETLOAD
"$production"
_.NETLOAD
"$dll"
DRENTESTPREPARE
DRENBORDES



DRENTESTCHECK
_.U
DRENTESTUNDONE
DRENBORDES



_.U
_.REDO
DRENTESTCHECK
DRENBORDES



DRENTESTCHECK
_.QUIT
_Y

"@, [Text.Encoding]::ASCII)
    $interactiveDrawing = Join-Path $OutputDirectory 'Drainage Stage 2 - Bordes 0.50.dwg'
    $processInfo.Arguments = '/i "' + $interactiveDrawing + '" /s "' + $interactive + '" /isolate DrenBordes-' + [Guid]::NewGuid().ToString('N') + ' "' + (Join-Path $OutputDirectory 'profile') + '" /l en-US'
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $processInfo
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(180000)) { $process.Kill(); throw 'AutoCAD excedio tres minutos.' }
    $log = ($stdout.Result + $stderr.Result) -replace "`0", ''
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'interactive.log'), $log)
    if ($process.ExitCode -ne 0 -or $log -match 'DREN_INTERACTIVE_FAILED' -or
        ([regex]::Matches($log, '(?m)^\s*DREN_INTERACTIVE_OK\s*$')).Count -ne 3 -or
        $log -notmatch '(?m)^\s*DREN_INTERACTIVE_UNDO_OK\s*$') {
        throw "Fallo el comando real o Undo/Redo. Consulte $OutputDirectory\interactive.log"
    }
    foreach ($source in $sources) {
        if ((Get-FileHash -LiteralPath $source.Path -Algorithm SHA256).Hash -ne $source.Hash) { throw "Cambio el archivo original: $($source.Path)" }
    }
    $sources | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'source-sha256.json') -Encoding UTF8
    Write-Output "PASS: geometria, capa azul, ejes intactos, regeneracion, exportacion y reapertura de DXF/DWG. $OutputDirectory"
} finally {
    $env:DRENBORDES_SOURCE_DIR = $previousSource
    $env:DRENBORDES_OUTPUT_DIR = $previousOutput
}
