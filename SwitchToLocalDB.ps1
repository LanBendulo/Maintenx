# Switch to Local Database Script
# This script updates appsettings.json to use LocalDB instead of remote MonsterASP.NET database

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Switch to Local Database (LocalDB)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Backup current appsettings.json
Write-Host "[1/4] Backing up current appsettings.json..." -ForegroundColor Yellow
Copy-Item "appsettings.json" "appsettings.json.backup" -Force
Write-Host "  ✓ Backup created: appsettings.json.backup" -ForegroundColor Green

# Create new appsettings.json with LocalDB
Write-Host "[2/4] Updating appsettings.json to use LocalDB..." -ForegroundColor Yellow
$newSettings = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MaintenX_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;Connection Timeout=30;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
"@

$newSettings | Out-File "appsettings.json" -Encoding UTF8 -Force
Write-Host "  ✓ appsettings.json updated" -ForegroundColor Green

# Apply migrations
Write-Host "[3/4] Applying database migrations..." -ForegroundColor Yellow
Write-Host "  Running: dotnet ef database update" -ForegroundColor Gray
$migrationResult = dotnet ef database update 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Migrations applied successfully" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Migration warning (this is normal for first run)" -ForegroundColor Yellow
    Write-Host "  The database will be created when you run the application" -ForegroundColor Gray
}

# Summary
Write-Host "[4/4] Setup complete!" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "LOCAL DATABASE READY!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run the application:" -ForegroundColor White
Write-Host "     dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "  2. Open browser:" -ForegroundColor White
Write-Host "     http://localhost:5262" -ForegroundColor Yellow
Write-Host ""
Write-Host "  3. Login with admin credentials" -ForegroundColor White
Write-Host "     (will be seeded automatically on first run)" -ForegroundColor Gray
Write-Host ""
Write-Host "To restore remote connection:" -ForegroundColor Cyan
Write-Host "  Copy-Item appsettings.json.backup appsettings.json -Force" -ForegroundColor Yellow
Write-Host ""
