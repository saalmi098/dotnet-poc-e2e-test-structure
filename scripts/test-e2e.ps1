[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5178",

    # Run the application with docker compose (PostgreSQL + API) instead of `dotnet run` with a temporary SQLite file.
    [switch]$Docker
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$serverProject = Join-Path $repositoryRoot "ShopDemo.Server\ShopDemo.Server.csproj"
$baselineProject = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Baseline\ShopDemo.Tests.E2E.Baseline.csproj"
$pomProject = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Pom.V1\ShopDemo.Tests.E2E.Pom.V1.csproj"
$pomV2Project = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Pom.V2\ShopDemo.Tests.E2E.Pom.V2.csproj"
$pomV3Project = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Pom.V3\ShopDemo.Tests.E2E.Pom.V3.csproj"
$pomV4Project = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Pom.V4\ShopDemo.Tests.E2E.Pom.V4.csproj"
$screenplayProject = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Screenplay\ShopDemo.Tests.E2E.Screenplay.csproj"
$playwrightInstaller = Join-Path $repositoryRoot "ShopDemo.Tests.E2E.Pom.V2\bin\Debug\net10.0\playwright.ps1"
$stdoutLog = Join-Path ([System.IO.Path]::GetTempPath()) "shopdemo-e2e-$PID.out.log"
$stderrLog = Join-Path ([System.IO.Path]::GetTempPath()) "shopdemo-e2e-$PID.err.log"
$databaseFile = Join-Path ([System.IO.Path]::GetTempPath()) "shopdemo-e2e-$PID.db"
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS
$previousConnectionString = $env:ConnectionStrings__ShopDb
$previousShopDemoBaseUrl = $env:SHOPDEMO_BASE_URL
$serverProcess = $null

try {
    Push-Location $repositoryRoot
    $env:ASPNETCORE_URLS = $BaseUrl
    $env:SHOPDEMO_BASE_URL = $BaseUrl

    & dotnet restore ShopDemo.sln --source https://api.nuget.org/v3/index.json
    if ($LASTEXITCODE -ne 0) {
        throw "Solution restore failed with exit code $LASTEXITCODE."
    }

    & dotnet build ShopDemo.sln --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Solution build failed with exit code $LASTEXITCODE."
    }

    & $playwrightInstaller install chromium
    if ($LASTEXITCODE -ne 0) {
        throw "Playwright Chromium installation failed with exit code $LASTEXITCODE."
    }

    if ($Docker) {
        # Publish the compose app on the port of $BaseUrl and wait until its health check passes.
        $env:SHOPDEMO_PORT = ([Uri]$BaseUrl).Port
        & docker compose up --build --detach --wait
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose up failed with exit code $LASTEXITCODE."
        }
    }
    else {
        $env:ConnectionStrings__ShopDb = "Data Source=$databaseFile"
        $serverProcess = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("run", "--project", "`"$serverProject`"", "--no-build", "--no-restore", "--no-launch-profile") `
            -WorkingDirectory $repositoryRoot `
            -PassThru `
            -RedirectStandardOutput $stdoutLog `
            -RedirectStandardError $stderrLog
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    $serverReady = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($null -ne $serverProcess -and $serverProcess.HasExited) {
            $serverOutput = if (Test-Path $stderrLog) { Get-Content $stderrLog -Raw } else { "" }
            throw "ShopDemo exited before becoming ready. $serverOutput"
        }

        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/api/health" -TimeoutSec 3 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                $serverReady = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $serverReady) {
        $serverOutput = if (Test-Path $stderrLog) { Get-Content $stderrLog -Raw } else { "" }
        throw "ShopDemo did not become ready at $BaseUrl within 60 seconds. $serverOutput"
    }

    & dotnet test (Join-Path $repositoryRoot "ShopDemo.Tests\ShopDemo.Tests.csproj") --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Unit tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $pomProject --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "POM E2E tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $pomV2Project --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "POM V2 E2E tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $pomV3Project --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "POM V3 E2E tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $pomV4Project --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "POM V4 E2E tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $baselineProject --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Baseline E2E tests failed with exit code $LASTEXITCODE."
    }

    & dotnet test $screenplayProject --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Screenplay E2E tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    if ($null -ne $serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
    }

    if ($Docker) {
        & docker compose down --volumes
    }

    $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
    $env:ConnectionStrings__ShopDb = $previousConnectionString
    $env:SHOPDEMO_BASE_URL = $previousShopDemoBaseUrl
    Pop-Location

    Remove-Item -LiteralPath $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$databaseFile*" -Force -ErrorAction SilentlyContinue
}
