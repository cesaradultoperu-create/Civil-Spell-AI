param(
    [string]$AutoCadPath = "C:\Program Files\Autodesk\AutoCAD 2024\acad.exe",
    [string]$AecBasePath = "C:\Program Files\Autodesk\AutoCAD 2024\AecBase.dbx",
    [string]$Profile = "<<C3D_Metric>>",
    [string]$Language = "en-US",
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $AutoCadPath -PathType Leaf))
{
    throw "No se encontro acad.exe: $AutoCadPath"
}

if (-not (Test-Path -LiteralPath $AecBasePath -PathType Leaf))
{
    throw "No se encontro AecBase.dbx: $AecBasePath"
}

$runningProcesses = @(Get-Process -Name "acad" -ErrorAction SilentlyContinue)

if ($ValidateOnly)
{
    Write-Host "Lanzador valido."
    Write-Host "Procesos de Civil 3D abiertos: $($runningProcesses.Count)"
    Write-Host "La ejecucion real no modifica la variable de usuario."
    exit 0
}

if ($runningProcesses.Count -gt 0)
{
    throw "Guarde y cierre todos los procesos de Civil 3D antes de iniciar la sesion aislada."
}

$arguments = @(
    "/ld",
    ('"' + $AecBasePath + '"'),
    "/p",
    ('"' + $Profile + '"'),
    "/product",
    "C3D",
    "/language",
    $Language
)
$previousProcessKey = [Environment]::GetEnvironmentVariable(
    "OPENAI_API_KEY",
    [EnvironmentVariableTarget]::Process)

try
{
    [Environment]::SetEnvironmentVariable(
        "OPENAI_API_KEY",
        "civilspellai-invalid-regression-key",
        [EnvironmentVariableTarget]::Process)
    Start-Process -FilePath $AutoCadPath -ArgumentList $arguments
}
finally
{
    [Environment]::SetEnvironmentVariable(
        "OPENAI_API_KEY",
        $previousProcessKey,
        [EnvironmentVariableTarget]::Process)
}

Write-Host "Civil 3D iniciado con una credencial ficticia solo para ese proceso."
Write-Host "La variable de entorno persistente del usuario no fue modificada."

