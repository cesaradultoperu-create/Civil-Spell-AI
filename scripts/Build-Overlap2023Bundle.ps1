param()
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
& dotnet build (Join-Path $workspace 'CivilSpellAI.Overlap2023.csproj') --no-restore -c Release -p:Platform=x64 -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion de la variante 2023.' }
$source = Join-Path $workspace 'bin\Overlap2023\Release\CivilSpellAI.TramosDWG.dll'
if ([Reflection.AssemblyName]::GetAssemblyName($source).Version.ToString() -ne '1.3.3.2') {
    throw 'Version inesperada de la DLL.'
}
$parent = Join-Path $workspace 'artifacts\entrega-tramos-dwg'
$root = Join-Path $parent 'CivilSpellAI-TramosDWG-1.3.3.2'
$bundle = Join-Path $root 'CivilSpellAITramosDWG.bundle'
$runtime = Join-Path $bundle 'Contents\Windows\2023'
[IO.Directory]::CreateDirectory($runtime) | Out-Null
Copy-Item -LiteralPath $source -Destination $runtime -Force
Copy-Item -LiteralPath (Join-Path $workspace 'packaging\Overlap2023\PackageContents.xml') -Destination $bundle -Force
Copy-Item -LiteralPath (Join-Path $workspace 'packaging\Overlap2023\INSTALAR_2023.cmd') -Destination (Join-Path $root 'INSTALAR.cmd') -Force
Copy-Item -LiteralPath (Join-Path $workspace 'packaging\Overlap2023\LEEME_2023.txt') -Destination (Join-Path $root 'LEEME.txt') -Force
# Reuse the managed installation/backup workflow, with independent bundle and backup names.
$installer = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Manage-CivilSpellAI.ps1'))
$installer = $installer.Replace('$bundleName = "CivilSpellAI.bundle"', '$bundleName = "CivilSpellAITramosDWG.bundle"')
$installer = $installer.Replace('CivilSpellAI\deployment-backups\$Scope', 'CivilSpellAI-TramosDWG\deployment-backups\$Scope')
$installerPath = Join-Path $root 'Manage-TramosDWG.ps1'
[IO.File]::WriteAllText($installerPath, $installer, [Text.UTF8Encoding]::new($true))
$tokens = $null; $errors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($installerPath, [ref]$tokens, [ref]$errors)
if ($errors.Count -gt 0) { throw 'El instalador generado tiene errores de sintaxis.' }
if (-not $installer.Contains('$bundleName = "CivilSpellAITramosDWG.bundle"') -or
    -not $installer.Contains('CivilSpellAI-TramosDWG\deployment-backups\$Scope')) {
    throw 'No se separaron el destino y respaldo de la variante 2023.'
}
[xml]$manifest = [IO.File]::ReadAllText((Join-Path $bundle 'PackageContents.xml'))
$requirements = $manifest.ApplicationPackage.Components.RuntimeRequirements
if ($requirements.SeriesMin -ne 'R24.2' -or $requirements.SeriesMax -ne 'R24.3') {
    throw 'El manifiesto no limita la variante a Civil 3D 2023-2024.'
}
if ($manifest.ApplicationPackage.Components.ComponentEntry.Commands.Command.Global -contains 'REVISARPOLILINEAS') {
    throw 'El paquete independiente debe usar comandos propios para no cargar la version anterior.'
}
$archive = Join-Path $parent 'CivilSpellAI-TramosDWG-1.3.3.2.zip'
Compress-Archive -LiteralPath $root -DestinationPath $archive -Force
$hash = (Get-FileHash -LiteralPath $archive).Hash
[IO.File]::WriteAllText(($archive + '.sha256'), "$hash *$([IO.Path]::GetFileName($archive))`r`n")
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $files = @($zip.Entries | Where-Object { $_.Name })
    if ($files.Count -ne 5 -or @($files | Where-Object { $_.Name -match '^(acdbmgd|accoremgd|acmgd|AeccDbMgd)\.dll$' }).Count) {
        throw 'Contenido inesperado en el ZIP.'
    }
    $entry = $files | Where-Object { $_.Name -eq 'CivilSpellAI.TramosDWG.dll' }
    if (-not $entry) { throw 'Falta la DLL en el ZIP.' }
    $stream = $entry.Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $payloadHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($payloadHash -ne (Get-FileHash -LiteralPath $source).Hash) { throw 'DLL del ZIP diferente de la compilada.' }
} finally { $zip.Dispose() }
Write-Host "ZIP Tramos DWG verificado: $archive"
Write-Host "SHA256: $hash"
