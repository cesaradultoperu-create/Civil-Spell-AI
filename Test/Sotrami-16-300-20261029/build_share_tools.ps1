$ErrorActionPreference = 'Stop'
$output = (Join-Path $PSScriptRoot 'share-tools').Replace('\','/') + '/'
foreach ($family in @('2023-2024', '2025')) {
    $intermediate = "obj/share-tools-$family/"
    & dotnet build (Join-Path $PSScriptRoot 'ExcelPlanSync.Multi.csproj') -c Release "-p:CadFamily=$family" "-p:AssemblyName=VinculoPlanosSOTRAMI.v33.$family" "-p:OutputPath=$output" "-p:BaseIntermediateOutputPath=$intermediate" "-p:MSBuildProjectExtensionsPath=$intermediate" -p:NuGetAudit=false --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw "Fallo la compilacion para $family" }
}







