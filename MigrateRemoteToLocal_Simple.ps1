# =============================================
# Simple Migration Script using BCP utility
# =============================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX Simple Migration Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$RemoteServer = "db50508.public.databaseasp.net"
$RemoteDatabase = "db50508"
$RemoteUser = "db50508"
$RemotePassword = "3k+L?Gm8n9Z_"

$LocalServer = "localhost\SQLEXPRESS"
$LocalDatabase = "DB_Maintenx"

$ExportFolder = ".\Database\export"

# Create export folder
if (-not (Test-Path $ExportFolder)) {
    New-Item -ItemType Directory -Path $ExportFolder | Out-Null
    Write-Host "✓ Created export folder: $ExportFolder" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 1: Creating local database..." -ForegroundColor Green

# Create local database
$createDbScript = @"
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '$LocalDatabase')
BEGIN
    CREATE DATABASE [$LocalDatabase]
END
GO
USE [$LocalDatabase]
GO
"@

$createDbScript | sqlcmd -S $LocalServer -E

Write-Host "✓ Local database ready" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Creating schema..." -ForegroundColor Green

# Apply schema
sqlcmd -S $LocalServer -d $LocalDatabase -E -i "Database\maintenx_schema.sql"

Write-Host "✓ Schema created" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Exporting and importing data..." -ForegroundColor Green

# List of tables
$tables = @(
    "SubscriptionPlans",
    "Companies",
    "AspNetRoles",
    "AspNetUsers",
    "AspNetUserRoles",
    "Assets",
    "Personnel",
    "MaintenanceRequests",
    "WorkOrders",
    "Parts",
    "WorkOrderParts",
    "InventoryMovements",
    "MaintenanceLog",
    "WorkOrderCosts",
    "PreventiveSchedules"
)

foreach ($table in $tables) {
    Write-Host "  → Processing $table..." -ForegroundColor Cyan
    
    $exportFile = "$ExportFolder\$table.dat"
    $formatFile = "$ExportFolder\$table.fmt"
    
    # Export from remote using BCP
    bcp "$RemoteDatabase.dbo.$table" out $exportFile -S $RemoteServer -U $RemoteUser -P $RemotePassword -n -q
    
    if ($LASTEXITCODE -eq 0) {
        # Import to local using BCP
        bcp "$LocalDatabase.dbo.$table" in $exportFile -S $LocalServer -T -n -q
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Migrated $table" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Failed to import $table" -ForegroundColor Red
        }
    } else {
        Write-Host "  ⊘ Skipped $table (no data or doesn't exist)" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "Step 4: Updating appsettings.json..." -ForegroundColor Green

# Update connection string
$appsettingsPath = "appsettings.json"
$newConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    $appsettings.ConnectionStrings.DefaultConnection = $newConnectionString
    $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
    Write-Host "✓ Connection string updated" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Local database: $LocalDatabase" -ForegroundColor Green
Write-Host ""
