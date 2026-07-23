param(
    [switch]$WithDocker,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$runDir = Join-Path $root ".run"
$dockerDir = Join-Path $root "docker"
$projectMarkers = @(
    "\Users\Users.csproj",
    "\Events\Events.csproj",
    "\Bookings\Bookings.csproj",
    "\Users\bin\Debug\net8.0\Users.exe",
    "\Events\bin\Debug\net8.0\Events.exe",
    "\Bookings\bin\Debug\net8.0\Bookings.exe"
)

function Stop-ProcessTree {
    param([int]$ProcessId)

    $children = Get-CimInstance Win32_Process |
        Where-Object { $_.ParentProcessId -eq $ProcessId }

    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    }
}

if (Test-Path $runDir) {
    Get-ChildItem -Path $runDir -Filter "*.pid" -ErrorAction SilentlyContinue | ForEach-Object {
        $rawPid = Get-Content -LiteralPath $_.FullName -ErrorAction SilentlyContinue | Select-Object -First 1
        $pidValue = 0

        if ([int]::TryParse($rawPid, [ref]$pidValue)) {
            if (-not $Quiet) {
                Write-Host "Stopping process tree for PID $pidValue..."
            }
            Stop-ProcessTree -ProcessId $pidValue
        }

        Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
    }
}

Get-CimInstance Win32_Process | Where-Object {
    $commandLine = $_.CommandLine
    if ([string]::IsNullOrWhiteSpace($commandLine)) {
        return $false
    }

    foreach ($marker in $projectMarkers) {
        if ($commandLine -like "*$marker*") {
            return $true
        }
    }

    return $false
} | ForEach-Object {
    if (-not $Quiet) {
        Write-Host "Stopping existing EventManager API process $($_.ProcessId)..."
    }
    Stop-ProcessTree -ProcessId $_.ProcessId
}

if ($WithDocker) {
    if (-not $Quiet) {
        Write-Host "Stopping Docker infrastructure..."
    }

    Push-Location $dockerDir
    try {
        & docker compose down
    }
    finally {
        Pop-Location
    }
}

if (-not $Quiet) {
    Write-Host "Done."
}
