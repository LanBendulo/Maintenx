# =============================================
# Apply Supervisor Role Migration
# =============================================
# Description: Adds Supervisor role to MaintenX
# Date: 2026-05-12
# =============================================

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX - Supervisor Role Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection details
$Server = "db50508.public.databaseasp.net"
$Database = "db50508"
$Username = "db50508"
$Password = "3k+L?Gm8n9Z_"

$SqlFile = "Database/add_supervisor_role.sql"

# Check if SQL file exists
if (-not (Test-Path $SqlFile)) {
    Write-Host "✗ ERROR: SQL file not found: $SqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Database Server: $Server" -ForegroundColor Yellow
Write-Host "Database Name: $Database" -ForegroundColor Yellow
Write-Host "SQL Script: $SqlFile" -ForegroundColor Yellow
Write-Host ""

# Confirm before proceeding
$confirmation = Read-Host "Do you want to proceed with the migration? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Migration cancelled by user." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Executing migration..." -ForegroundColor Green

try {
    # Execute SQL script using sqlcmd
    sqlcmd -S $Server -d $Database -U $Username -P $Password -i $SqlFile -b
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✓ Migration completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "NEXT STEPS:" -ForegroundColor Cyan
        Write-Host "1. Restart the application" -ForegroundColor White
        Write-Host "2. Assign Supervisor role to users via User Management" -ForegroundColor White
        Write-Host "3. Test Supervisor access at /supervisor/dashboard" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "✗ Migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
