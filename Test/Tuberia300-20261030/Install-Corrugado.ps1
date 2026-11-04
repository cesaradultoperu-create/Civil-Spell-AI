$ErrorActionPreference='Stop'
trap { $_ | Out-String | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Error-instalacion.txt'); exit 1 }
$source=Join-Path $PSScriptRoot 'Entrega-Corrugado/Complemento-2024'
$target='C:\Program Files\Autodesk\ApplicationPlugins\CivilSpellAI.bundle\Contents\Windows\2024'
if(-not (Test-Path -LiteralPath (Join-Path $target 'CivilSpellAI.dll'))){throw 'No existe la instalación CivilSpellAI 2024 esperada.'}
$backup=Join-Path $PSScriptRoot ('Respaldo-instalacion-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $backup | Out-Null
# Test access without changing the installed assembly. Never close Civil 3D.
$probe=[IO.File]::Open((Join-Path $target 'CivilSpellAI.dll'),[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$probe.Dispose()
$receipt=@();$applied=@()
try {
    foreach($file in Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object Extension -ne '.pdb'){
        $relative=$file.FullName.Substring($source.Length).TrimStart('\','/')
        $destination=Join-Path $target $relative
        $existed=Test-Path -LiteralPath $destination
        $oldHash=$null
        if($existed){
            $saved=Join-Path $backup $relative
            New-Item -ItemType Directory -Path (Split-Path -Parent $saved) -Force | Out-Null
            Copy-Item -LiteralPath $destination -Destination $saved
            $oldHash=(Get-FileHash -LiteralPath $saved).Hash
        }
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
        $applied+=@{Path=$destination;Relative=$relative;Existed=$existed}
        $newHash=(Get-FileHash -LiteralPath $file.FullName).Hash
        if((Get-FileHash -LiteralPath $destination).Hash -ne $newHash){throw "No se verificó $relative"}
        $receipt+=[pscustomobject]@{File=$relative;PreviousSHA256=$oldHash;InstalledSHA256=$newHash}
    }
} catch {
    foreach($entry in $applied){
        if($entry.Existed){Copy-Item -LiteralPath (Join-Path $backup $entry.Relative) -Destination $entry.Path -Force}
        else {Remove-Item -LiteralPath $entry.Path -Force}
    }
    throw
}
$record=[pscustomobject]@{Target=$target;Backup=$backup;Files=$receipt;RequiresRestart=$true}
$record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Instalacion-corrugado-verificada.json') -Encoding UTF8
$record|Select-Object Target,Backup,@{n='VerifiedFiles';e={$_.Files.Count}},RequiresRestart|ConvertTo-Json
