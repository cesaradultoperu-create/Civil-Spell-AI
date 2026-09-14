param(
    [ValidateSet("Start", "Status", "Stop")]
    [string]$Action = "Status",

    [ValidateRange(1024, 65535)]
    [int]$Port = 8081
)

$ErrorActionPreference = "Stop"
$localData = [Environment]::GetFolderPath(
    [Environment+SpecialFolder]::LocalApplicationData)
$languageToolDirectory = Join-Path $localData "CivilSpellAI\LanguageTool\6.6"
$serverJar = Join-Path $languageToolDirectory "languagetool-server.jar"
$serverProperties = Join-Path $languageToolDirectory "server.properties"
$pidPath = Join-Path $languageToolDirectory "server.pid"
$standardOutputPath = Join-Path $languageToolDirectory "server.stdout.log"
$standardErrorPath = Join-Path $languageToolDirectory "server.stderr.log"
$endpoint = "http://127.0.0.1:$Port/v2/check"

function Get-JavaExecutable
{
    $java = Get-Command java.exe -ErrorAction SilentlyContinue

    if ($null -ne $java)
    {
        return $java.Source
    }

    $adoptiumRoot = "C:\Program Files\Eclipse Adoptium"

    if (-not (Test-Path -LiteralPath $adoptiumRoot -PathType Container))
    {
        return $null
    }

    return Get-ChildItem -LiteralPath $adoptiumRoot `
        -Filter java.exe `
        -File `
        -Recurse | Select-Object -First 1 -ExpandProperty FullName
}

function Get-Listener
{
    return Get-NetTCPConnection `
        -LocalPort $Port `
        -State Listen `
        -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Test-LanguageToolEndpoint
{
    try
    {
        $response = Invoke-RestMethod `
            -Uri $endpoint `
            -Method Post `
            -Body @{ language = "es"; text = "La estructura esta instalado." } `
            -TimeoutSec 5
        return $null -ne $response.matches
    }
    catch
    {
        return $false
    }
}

function Write-Status
{
    $listener = Get-Listener

    if ($null -eq $listener)
    {
        Write-Host "LanguageTool local no esta iniciado en el puerto $Port."
        return $false
    }

    if (-not (Test-LanguageToolEndpoint))
    {
        Write-Host "El puerto $Port esta ocupado, pero no responde como LanguageTool."
        return $false
    }

    Write-Host "LanguageTool local esta disponible en $endpoint."
    Write-Host "PID: $($listener.OwningProcess)"
    return $true
}

switch ($Action)
{
    "Status"
    {
        if (-not (Write-Status))
        {
            exit 1
        }
    }

    "Start"
    {
        if (Write-Status)
        {
            exit 0
        }

        if (Get-Listener)
        {
            throw "El puerto $Port ya esta ocupado por otro proceso."
        }

        if (-not (Test-Path -LiteralPath $serverJar -PathType Leaf))
        {
            throw "No se encontro LanguageTool local en $languageToolDirectory."
        }

        if (-not (Test-Path -LiteralPath $serverProperties -PathType Leaf))
        {
            throw "No se encontro server.properties en $languageToolDirectory."
        }

        $javaExecutable = Get-JavaExecutable

        if ([string]::IsNullOrWhiteSpace($javaExecutable))
        {
            throw "No se encontro Java 17."
        }

        $arguments = @(
            "-cp",
            ('"' + $serverJar + '"'),
            "org.languagetool.server.HTTPServer",
            "--config",
            ('"' + $serverProperties + '"'),
            "--port",
            $Port.ToString()
        )
        $process = Start-Process `
            -FilePath $javaExecutable `
            -ArgumentList $arguments `
            -WorkingDirectory $languageToolDirectory `
            -WindowStyle Hidden `
            -RedirectStandardOutput $standardOutputPath `
            -RedirectStandardError $standardErrorPath `
            -PassThru
        [IO.File]::WriteAllText(
            $pidPath,
            $process.Id.ToString([Globalization.CultureInfo]::InvariantCulture))

        for ($attempt = 0; $attempt -lt 60; $attempt++)
        {
            Start-Sleep -Milliseconds 500

            if (Test-LanguageToolEndpoint)
            {
                Write-Host "LanguageTool local se inicio correctamente."
                Write-Host "Endpoint: $endpoint"
                Write-Host "PID: $($process.Id)"
                exit 0
            }

            if ($process.HasExited)
            {
                break
            }
        }

        if (-not $process.HasExited)
        {
            Stop-Process -Id $process.Id
        }

        throw "LanguageTool no inicio correctamente. Revise $standardErrorPath."
    }

    "Stop"
    {
        $listener = Get-Listener

        if ($null -eq $listener)
        {
            Write-Host "LanguageTool local ya esta detenido."
            exit 0
        }

        if (-not (Test-Path -LiteralPath $pidPath -PathType Leaf))
        {
            throw "No existe un PID administrado; no se detendra el proceso del puerto $Port."
        }

        $recordedPid = 0

        if (-not [int]::TryParse(
            [IO.File]::ReadAllText($pidPath).Trim(),
            [ref]$recordedPid) -or
            $recordedPid -ne $listener.OwningProcess)
        {
            throw "El listener no coincide con el proceso iniciado por este script."
        }

        Stop-Process -Id $recordedPid
        Remove-Item -LiteralPath $pidPath
        Write-Host "LanguageTool local fue detenido."
    }
}
