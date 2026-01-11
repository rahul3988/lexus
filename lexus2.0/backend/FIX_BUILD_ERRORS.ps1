# PowerShell script to fix build errors
Write-Host "🔧 Fixing Build Errors..." -ForegroundColor Green

# Navigate to backend directory
$backendPath = Join-Path $PSScriptRoot "."
Set-Location $backendPath

Write-Host "`n1. Cleaning solution..." -ForegroundColor Yellow
dotnet clean Lexus2.0.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Clean had some issues, continuing..." -ForegroundColor Yellow
}

Write-Host "`n2. Deleting bin and obj folders..." -ForegroundColor Yellow
$folders = @(
    "Lexus2.0.Core\bin",
    "Lexus2.0.Core\obj",
    "Lexus2.0.Automation\bin",
    "Lexus2.0.Automation\obj",
    "Lexus2.0.API\bin",
    "Lexus2.0.API\obj"
)
foreach ($folder in $folders) {
    if (Test-Path $folder) {
        Write-Host "   Deleting: $folder" -ForegroundColor Gray
        Remove-Item -Path $folder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "`n3. Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore Lexus2.0.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to restore packages" -ForegroundColor Red
    exit 1
}

Write-Host "`n4. Verifying Playwright package..." -ForegroundColor Yellow
$playwrightInstalled = dotnet list Lexus2.0.Automation\Lexus2.0.Automation.csproj package | Select-String "Microsoft.Playwright"
if (-not $playwrightInstalled) {
    Write-Host "   Installing Microsoft.Playwright..." -ForegroundColor Cyan
    Set-Location Lexus2.0.Automation
    dotnet add package Microsoft.Playwright --version 1.40.0
    Set-Location ..
    dotnet restore Lexus2.0.sln
}

Write-Host "`n5. Building projects in order..." -ForegroundColor Yellow

Write-Host "`n   Building Lexus2.0.Core..." -ForegroundColor Cyan
dotnet build Lexus2.0.Core\Lexus2.0.Core.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build Lexus2.0.Core" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
    exit 1
}
Write-Host "   ✓ Lexus2.0.Core built successfully" -ForegroundColor Green

Write-Host "`n   Building Lexus2.0.Automation..." -ForegroundColor Cyan
dotnet build Lexus2.0.Automation\Lexus2.0.Automation.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build Lexus2.0.Automation" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
    exit 1
}
Write-Host "   ✓ Lexus2.0.Automation built successfully" -ForegroundColor Green

Write-Host "`n   Building Lexus2.0.API..." -ForegroundColor Cyan
dotnet build Lexus2.0.API\Lexus2.0.API.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build Lexus2.0.API" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
    exit 1
}
Write-Host "   ✓ Lexus2.0.API built successfully" -ForegroundColor Green

Write-Host "`n✅ Build completed successfully!" -ForegroundColor Green
Write-Host "`nYou can now open the solution in Visual Studio 2022" -ForegroundColor Cyan

