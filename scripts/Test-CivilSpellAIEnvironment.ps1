param(
    [ValidateSet("CurrentUser", "AllUsers")]
    [string]$Scope = "CurrentUser"
)

$ErrorActionPreference = "Stop"
$checks = New-Object System.Collections.Generic.List[object]
$warnings = New-Object System.Collections.Generic.List[object]

function Add-Check([string]$name, [bool]$passed, [string]$detail)
{
    $checks.Add([pscustomobject]@{
        Check = $name
        Result = if ($passed) { "PASS" } else { "FAIL" }
        Detail = $detail
    })
}

function Add-Warning([string]$name, [string]$detail)
{
    $warning = [pscustomobject]@{
        Check = $name
        Result = "WARN"
        Detail = $detail
    }
    $checks.Add($warning)
    $warnings.Add($warning)
}

$is64Bit = [Environment]::Is64BitOperatingSystem
Add-Check "Windows x64" $is64Bit $(if ($is64Bit) { "Sistema de 64 bits." } else { "Se requiere Windows de 64 bits." })

$civilTargets = @(
    [pscustomobject]@{ Year = "2024"; Folder = "AutoCAD 2024"; Series = "R24.3" },
    [pscustomobject]@{ Year = "2025"; Folder = "AutoCAD 2025"; Series = "R25.0" },
    [pscustomobject]@{ Year = "2026"; Folder = "AutoCAD 2026"; Series = "R25.1" }
)
$civilInstallations = @()

foreach ($target in $civilTargets)
{
    $civilRoot = Join-Path $env:ProgramFiles "Autodesk\$($target.Folder)"
    $acadPath = Join-Path $civilRoot "acad.exe"
    $civilApiPath = Join-Path $civilRoot "C3D\AeccDbMgd.dll"

    if ((Test-Path -LiteralPath $acadPath -PathType Leaf) -and
        (Test-Path -LiteralPath $civilApiPath -PathType Leaf))
    {
        $civilVersion = (Get-Item -LiteralPath $acadPath).VersionInfo.ProductVersion
        $civilInstallations += [pscustomobject]@{
            Year = $target.Year
            Series = $target.Series
            ProductVersion = $civilVersion
        }
    }
}

$detectedCivilVersions = if ($civilInstallations.Count -gt 0) {
    ($civilInstallations | ForEach-Object { "$($_.Year) ($($_.ProductVersion))" }) -join ", "
} else { "ninguna" }
Add-Check `
    "Civil 3D compatible" `
    ($civilInstallations.Count -gt 0) `
    "Versiones 2024/2025/2026 detectadas: $detectedCivilVersions"

$frameworkKey = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
$frameworkRelease = if (Test-Path $frameworkKey) {
    (Get-ItemProperty -Path $frameworkKey -Name Release).Release
} else { 0 }
$requiresFramework = @($civilInstallations | Where-Object { $_.Year -eq "2024" }).Count -gt 0
Add-Check `
    ".NET Framework 4.8 (Civil 3D 2024)" `
    (-not $requiresFramework -or $frameworkRelease -ge 528040) `
    $(if ($requiresFramework) { "Release detectado: $frameworkRelease" } else { "No aplica a Civil 3D 2025/2026." })

$pluginsRoot = if ($Scope -eq "AllUsers") {
    Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins"
} else {
    Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
}
$permissionProbe = Join-Path $pluginsRoot (".civilspell-write-test-" + [Guid]::NewGuid().ToString("N"))
$canWrite = $false
$permissionDetail = "Destino: $pluginsRoot"

try
{
    New-Item -ItemType Directory -Path $pluginsRoot -Force | Out-Null
    [System.IO.File]::WriteAllText($permissionProbe, "probe")
    $canWrite = Test-Path -LiteralPath $permissionProbe -PathType Leaf
}
catch [System.UnauthorizedAccessException]
{
    $permissionDetail = "Sin permiso para $pluginsRoot. Abra PowerShell como administrador para usar -Scope AllUsers."
}
catch [System.Security.SecurityException]
{
    $permissionDetail = "La política de seguridad impide escribir en $pluginsRoot."
}
finally
{
    if (Test-Path -LiteralPath $permissionProbe)
    {
        Remove-Item -LiteralPath $permissionProbe -Force
    }
}
Add-Check "Permiso de instalación" $canWrite $permissionDetail

$otherPluginsRoot = if ($Scope -eq "AllUsers") {
    Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
} else {
    Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins"
}
$otherBundle = Join-Path $otherPluginsRoot "CivilSpellAI.bundle"
if (Test-Path -LiteralPath $otherBundle -PathType Container)
{
    Add-Warning "Bundle en otro alcance" "Existe otra copia en $otherBundle. Retírela solo después de verificar la nueva instalación para evitar registros duplicados."
}

$preferredYear = if ($civilInstallations.Count -gt 0) {
    $civilInstallations[0].Year
} else { "2024" }
$bundleDllRelativePath = "CivilSpellAI.bundle\Contents\Windows\$preferredYear\CivilSpellAI.dll"
$candidateDll = Join-Path $PSScriptRoot $bundleDllRelativePath
$installedDll = Join-Path $pluginsRoot $bundleDllRelativePath
$dllToInspect = if (Test-Path -LiteralPath $candidateDll -PathType Leaf) {
    $candidateDll
} elseif (Test-Path -LiteralPath $installedDll -PathType Leaf) {
    $installedDll
} else {
    $null
}

if ($null -eq $dllToInspect)
{
    Add-Warning "Confianza del bundle" "No se encontró la DLL candidata de Civil 3D $preferredYear para comprobar su firma."
}
else
{
    $runtimeDirectory = Split-Path -Parent $dllToInspect
    $requiredOfflineFiles = @(
        (Join-Path $runtimeDirectory "WeCantSpell.Hunspell.dll"),
        (Join-Path $runtimeDirectory "Resources\Dictionaries\es_PE.aff"),
        (Join-Path $runtimeDirectory "Resources\Dictionaries\es_PE.dic"),
        (Join-Path $runtimeDirectory "Resources\Dictionaries\en_US.aff"),
        (Join-Path $runtimeDirectory "Resources\Dictionaries\en_US.dic")
    )
    $missingOfflineFiles = @($requiredOfflineFiles | Where-Object {
        -not (Test-Path -LiteralPath $_ -PathType Leaf)
    })
    Add-Check `
        "Diccionarios offline español/inglés" `
        ($missingOfflineFiles.Count -eq 0) `
        $(if ($missingOfflineFiles.Count -eq 0) {
            "es_PE, en_US y el motor Hunspell están presentes."
        } else {
            "Faltan recursos: " + (($missingOfflineFiles | ForEach-Object {
                [IO.Path]::GetFileName($_)
            }) -join ", ")
        })

    $signature = Get-AuthenticodeSignature -LiteralPath $dllToInspect

    if ($signature.Status -eq [System.Management.Automation.SignatureStatus]::Valid)
    {
        Add-Check "Confianza del bundle" $true "Firma Authenticode válida."
    }
    elseif ($Scope -eq "AllUsers")
    {
        Add-Check "Confianza del bundle" $true "DLL sin firma destinada a Program Files, ubicación implícitamente confiable de Autodesk; requiere elevación para instalar."
    }
    elseif ($Scope -eq "CurrentUser")
    {
        Add-Warning "Confianza del bundle" "DLL sin firma en ApplicationPlugins del usuario. La primera carga puede requerir NETLOAD y Always Load, o una ruta incluida conscientemente en TRUSTEDPATHS."
    }
}

$settingsPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "CivilSpellAI\settings.v3.json"
if (Test-Path -LiteralPath $settingsPath -PathType Leaf)
{
    try
    {
        $settings = Get-Content -Raw -LiteralPath $settingsPath | ConvertFrom-Json
        Add-Check "Configuración" ($settings.SchemaVersion -eq 3) "Esquema detectado: $($settings.SchemaVersion)."
    }
    catch
    {
        Add-Check "Configuración" $false "El archivo settings.v3.json no es JSON válido."
    }
}
else
{
    Add-Check "Configuración" $true "Sin configuración previa; se crearán valores seguros por defecto."
}

$openAiPresent = -not [string]::IsNullOrWhiteSpace(
    [Environment]::GetEnvironmentVariable("OPENAI_API_KEY", "User")) -or
    -not [string]::IsNullOrWhiteSpace($env:OPENAI_API_KEY)
Add-Check "Credencial OpenAI (opcional)" $true $(if ($openAiPresent) { "Presente; el valor no se muestra." } else { "Ausente; las reglas locales siguen disponibles." })

$acadRunning = $null -ne (Get-Process -Name acad -ErrorAction SilentlyContinue)
Add-Check "Civil 3D cerrado" (-not $acadRunning) $(if ($acadRunning) { "Cierre Civil 3D antes de administrar el bundle." } else { "No se detectó acad.exe en ejecución." })

$checks | Format-Table -AutoSize -Wrap
$failures = @($checks | Where-Object { $_.Result -eq "FAIL" })

if ($failures.Count -gt 0)
{
    Write-Error "Preflight fallido: $($failures.Count) requisito(s) no cumplido(s)."
    exit 1
}

Write-Host ""
Write-Host "PREFLIGHT CORRECTO"
if ($warnings.Count -gt 0)
{
    Write-Warning "Preflight completado con $($warnings.Count) advertencia(s); revíselas antes de abrir Civil 3D."
}
