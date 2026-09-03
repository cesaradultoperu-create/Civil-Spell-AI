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

$civilRoot = Join-Path $env:ProgramFiles "Autodesk\AutoCAD 2024"
$acadPath = Join-Path $civilRoot "acad.exe"
$civilApiPath = Join-Path $civilRoot "C3D\AeccDbMgd.dll"
$civilPresent = (Test-Path -LiteralPath $acadPath -PathType Leaf) -and
    (Test-Path -LiteralPath $civilApiPath -PathType Leaf)
$civilVersion = if (Test-Path -LiteralPath $acadPath) {
    (Get-Item -LiteralPath $acadPath).VersionInfo.ProductVersion
} else { "no detectada" }
Add-Check "Civil 3D 2024" ($civilPresent -and $civilVersion.StartsWith("R24.3")) "Versión detectada: $civilVersion"

$frameworkKey = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
$frameworkRelease = if (Test-Path $frameworkKey) {
    (Get-ItemProperty -Path $frameworkKey -Name Release).Release
} else { 0 }
Add-Check ".NET Framework 4.8" ($frameworkRelease -ge 528040) "Release detectado: $frameworkRelease"

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

$bundleDllRelativePath = "CivilSpellAI.bundle\Contents\Windows\CivilSpellAI.dll"
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
    Add-Warning "Confianza del bundle" "No se encontró una DLL candidata para comprobar su firma."
}
else
{
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
