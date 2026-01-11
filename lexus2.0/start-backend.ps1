# PowerShell script to start the backend API
Write-Host "Starting Lexus 2.0 Backend API..." -ForegroundColor Green

# Check if .NET is installed
$dotnetVersion = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: .NET SDK not found. Please install .NET 8.0 SDK." -ForegroundColor Red
    exit 1
}

Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Cyan

# Navigate to API directory
$apiPath = Join-Path $PSScriptRoot "backend\Lexus2.0.API"
if (-not (Test-Path $apiPath)) {
    Write-Host "Error: API directory not found at $apiPath" -ForegroundColor Red
    exit 1
}

Set-Location $apiPath

# Restore and build
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host "Building project..." -ForegroundColor Yellow
dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    exit 1
}

# Run the API
Write-Host "Starting API server on http://localhost:5000..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

dotnet run

