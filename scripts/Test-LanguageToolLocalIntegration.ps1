param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$managerPath = Join-Path $PSScriptRoot "Manage-LanguageToolLocal.ps1"
$projectPath = Join-Path $repoRoot "Tests\CivilSpellAI.Tests.csproj"
$executablePath = Join-Path `
    $repoRoot `
    "Tests\bin\$Configuration\CivilSpellAI.Tests.exe"

& powershell -NoProfile -ExecutionPolicy Bypass `
    -File $managerPath `
    -Action Status

if ($LASTEXITCODE -ne 0)
{
    throw "LanguageTool local debe estar iniciado antes de la prueba."
}

dotnet build $projectPath `
    --configuration $Configuration `
    --no-restore `
    --verbosity minimal

if ($LASTEXITCODE -ne 0)
{
    throw "No se pudo compilar el ejecutable de pruebas."
}

& $executablePath --language-tool-integration

if ($LASTEXITCODE -ne 0)
{
    throw "La integracion real con LanguageTool local fallo."
}
