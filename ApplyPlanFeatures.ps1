# ============================================================
# Apply Plan Features Migration
# Adds feature lists to subscription plans for landing page
# ============================================================

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "MaintenX - Apply Plan Features Migration" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appsettingsPath = "appsettings.json"
if (-not (Test-Path $appsettingsPath)) {
    Write-Host "ERROR: appsettings.json not found!" -ForegroundColor Red
    exit 1
}

$appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.DefaultConnection

if ([string]::IsNullOrEmpty($connectionString)) {
    Write-Host "ERROR: Connection string not found in appsettings.json!" -ForegroundColor Red
    exit 1
}

Write-Host "Connection String: $($connectionString.Substring(0, 30))..." -ForegroundColor Gray
Write-Host ""

# SQL script path
$sqlScriptPath = "Database/add_plan_features.sql"

if (-not (Test-Path $sqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at $sqlScriptPath!" -ForegroundColor Red
    exit 1
}

Write-Host "Executing: $sqlScriptPath" -ForegroundColor Yellow
Write-Host ""

try {
    # Execute the SQL script using sqlcmd
    $output = sqlcmd -S $connectionString.Split(';')[0].Split('=')[1] `
                     -d $connectionString.Split(';')[1].Split('=')[1] `
                     -U $connectionString.Split(';')[2].Split('=')[1] `
                     -P $connectionString.Split(';')[3].Split('=')[1] `
                     -i $sqlScriptPath `
                     -C # Trust server certificate
    
    # Display output
    $output | ForEach-Object {
        if ($_ -match "✓") {
            Write-Host $_ -ForegroundColor Green
        }
        elseif ($_ -match "ERROR|⚠") {
            Write-Host $_ -ForegroundColor Red
        }
        elseif ($_ -match "==") {
            Write-Host $_ -ForegroundColor Cyan
        }
        else {
            Write-Host $_
        }
    }
    
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "Plan Features Migration Applied Successfully!" -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Restart the application if it's running" -ForegroundColor White
    Write-Host "2. Visit the landing page to see dynamic pricing" -ForegroundColor White
    Write-Host "3. Edit plans in SuperAdmin to update landing page" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host "ERROR: Migration Failed!" -ForegroundColor Red
    Write-Host "============================================================" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
