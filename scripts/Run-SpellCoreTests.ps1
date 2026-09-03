param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "..\Tests\CivilSpellAI.Tests.csproj"

dotnet build $projectPath --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0)
{
    exit $LASTEXITCODE
}

$testExecutable = Join-Path $PSScriptRoot "..\Tests\bin\$Configuration\CivilSpellAI.Tests.exe"
& $testExecutable
exit $LASTEXITCODE
