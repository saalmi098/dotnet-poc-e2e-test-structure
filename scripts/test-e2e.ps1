[CmdletBinding()]
param(
    [string]$BaseUrl = "http://127.0.0.1:5178"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$clientProject = Join-Path $repositoryRoot "ShopDemo.Client\ShopDemo.Client.csproj"
$pomProject = Join-Path $repositoryRoot "ShopDemo.E2E.Pom.Tests\ShopDemo.E2E.Pom.Tests.csproj"
$screenplayProject = Join-Path $repositoryRoot "ShopDemo.E2E.Screenplay.Tests\ShopDemo.E2E.Screenplay.Tests.csproj"
$playwrightInstaller = Join-Path $repositoryRoot "ShopDemo.E2E.Pom.Tests\bin\Debug\net10.0\playwright.ps1"
$stdoutLog = Join-Path ([System.IO.Path]::GetTempPath()) "shopdemo-e2e-$PID.out.log"
$stderrLog = Join-Path ([System.IO.Path]::GetTempPath()) "shopdemo-e2e-$PID.err.log"
$previousAspNetCoreUrls = $env:ASPNETCORE_URLS
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

    $serverProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", "`"$clientProject`"", "--no-build", "--no-restore", "--no-launch-profile") `
        -WorkingDirectory $repositoryRoot `
        -PassThru `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog

    $deadline = [DateTime]::UtcNow.AddSeconds(60)
    $serverReady = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($serverProcess.HasExited) {
            $serverOutput = if (Test-Path $stderrLog) { Get-Content $stderrLog -Raw } else { "" }
            throw "ShopDemo exited before becoming ready. $serverOutput"
        }

        try {
            $response = Invoke-WebRequest -Uri $BaseUrl -TimeoutSec 3
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

    & dotnet test $pomProject --no-build --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "POM E2E tests failed with exit code $LASTEXITCODE."
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

    $env:ASPNETCORE_URLS = $previousAspNetCoreUrls
    $env:SHOPDEMO_BASE_URL = $previousShopDemoBaseUrl
    Pop-Location

    Remove-Item -LiteralPath $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue
}
