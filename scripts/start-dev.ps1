param(
    [switch]$NoDocker,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src"
$dockerDir = Join-Path $root "docker"
$runDir = Join-Path $root ".run"
$logsDir = Join-Path $runDir "logs"
$stopScript = Join-Path $PSScriptRoot "stop-dev.ps1"

New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

$services = @(
    @{
        Name = "Users"
        Project = ".\Users\Users.csproj"
        Url = "http://localhost:5202"
        Swagger = "http://localhost:5202/swagger"
        PidFile = Join-Path $runDir "users.pid"
        OutLog = Join-Path $logsDir "users.out.log"
        ErrLog = Join-Path $logsDir "users.err.log"
    },
    @{
        Name = "Events"
        Project = ".\Events\Events.csproj"
        Url = "http://localhost:5203"
        Swagger = "http://localhost:5203/swagger"
        PidFile = Join-Path $runDir "events.pid"
        OutLog = Join-Path $logsDir "events.out.log"
        ErrLog = Join-Path $logsDir "events.err.log"
    },
    @{
        Name = "Bookings"
        Project = ".\Bookings\Bookings.csproj"
        Url = "http://localhost:5075"
        Swagger = "http://localhost:5075/swagger"
        PidFile = Join-Path $runDir "bookings.pid"
        OutLog = Join-Path $logsDir "bookings.out.log"
        ErrLog = Join-Path $logsDir "bookings.err.log"
    }
)

function Test-CommandExists {
    param([string]$Name)

    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Wait-HttpEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$TimeoutSeconds = 40
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Host "  $Name is ready: $Url"
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    Write-Warning "$Name did not answer in $TimeoutSeconds seconds. Check logs in $logsDir."
}

if (-not (Test-CommandExists "dotnet")) {
    throw "dotnet CLI was not found in PATH."
}

if (-not $NoDocker -and -not (Test-CommandExists "docker")) {
    throw "Docker CLI was not found in PATH."
}

if (Test-Path $stopScript) {
    & $stopScript -Quiet
}

if (-not $NoDocker) {
    Write-Host "Starting Docker infrastructure..."
    Push-Location $dockerDir
    try {
        & docker compose up -d
    }
    finally {
        Pop-Location
    }
}

if (-not $NoBuild) {
    Write-Host "Building services..."
    foreach ($service in $services) {
        & dotnet build (Join-Path $src $service.Project) | Out-Host
    }
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"

Write-Host "Starting API services..."
foreach ($service in $services) {
    Remove-Item -LiteralPath $service.OutLog, $service.ErrLog -Force -ErrorAction SilentlyContinue

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--no-build", "--project", $service.Project, "--urls", $service.Url) `
        -WorkingDirectory $src `
        -WindowStyle Hidden `
        -RedirectStandardOutput $service.OutLog `
        -RedirectStandardError $service.ErrLog `
        -PassThru

    Set-Content -LiteralPath $service.PidFile -Value $process.Id
    Write-Host "  $($service.Name) started. PID: $($process.Id)"
}

Write-Host "Waiting for Swagger endpoints..."
foreach ($service in $services) {
    Wait-HttpEndpoint -Name $service.Name -Url $service.Swagger
}

Write-Host ""
Write-Host "Swagger URLs:"
foreach ($service in $services) {
    Write-Host "  $($service.Name): $($service.Swagger)"
}

Write-Host ""
Write-Host "Logs: $logsDir"
Write-Host "Stop APIs: .\scripts\stop-dev.ps1"
Write-Host "Stop APIs and Docker: .\scripts\stop-dev.ps1 -WithDocker"
