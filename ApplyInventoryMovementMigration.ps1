# ============================================================
# APPLY INVENTORY MOVEMENT TABLE MIGRATION
# ============================================================
# Purpose: Create immutable audit log for inventory stock changes
# Implements transactional inventory traceability
# ============================================================

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "INVENTORY MOVEMENT TABLE MIGRATION" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Load configuration
$configPath = "appsettings.Production.json"
if (-not (Test-Path $configPath)) {
    Write-Host "ERROR: Configuration file not found: $configPath" -ForegroundColor Red
    Write-Host "Please ensure appsettings.Production.json exists in the project root." -ForegroundColor Yellow
    exit 1
}

Write-Host "Loading database configuration from $configPath..." -ForegroundColor Yellow
$config = Get-Content $configPath | ConvertFrom-Json
$connectionString = $config.ConnectionStrings.DefaultConnection

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host "ERROR: Connection string not found in configuration." -ForegroundColor Red
    exit 1
}

Write-Host "✓ Configuration loaded successfully" -ForegroundColor Green
Write-Host ""

# Extract connection details
if ($connectionString -match "Server=([^;]+);.*Database=([^;]+);.*User ID=([^;]+);.*Password=([^;]+)") {
    $server = $matches[1]
    $database = $matches[2]
    $username = $matches[3]
    $password = $matches[4]
    
    Write-Host "Database Server: $server" -ForegroundColor Cyan
    Write-Host "Database Name: $database" -ForegroundColor Cyan
    Write-Host "Username: $username" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "ERROR: Could not parse connection string." -ForegroundColor Red
    exit 1
}

# Confirm migration
Write-Host "This migration will:" -ForegroundColor Yellow
Write-Host "  1. Create InventoryMovement table for immutable audit logging" -ForegroundColor White
Write-Host "  2. Add foreign keys to Company, Part, WorkOrder, WorkOrderPart, User" -ForegroundColor White
Write-Host "  3. Add check constraints for movement type and quantity validation" -ForegroundColor White
Write-Host "  4. Create 5 performance indexes for queries" -ForegroundColor White
Write-Host "  5. Enable complete inventory traceability" -ForegroundColor White
Write-Host ""
Write-Host "Features:" -ForegroundColor Yellow
Write-Host "  ✓ Before/after quantity tracking" -ForegroundColor Green
Write-Host "  ✓ User and work order traceability" -ForegroundColor Green
Write-Host "  ✓ Cost snapshot support" -ForegroundColor Green
Write-Host "  ✓ Multi-tenant safe" -ForegroundColor Green
Write-Host "  ✓ Transactional safety" -ForegroundColor Green
Write-Host ""

$confirmation = Read-Host "Do you want to proceed? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Migration cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Applying migration..." -ForegroundColor Yellow

# SQL Migration file
$sqlFile = "Database\add_inventory_movement_table.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "ERROR: Migration file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

# Execute migration using sqlcmd
try {
    Write-Host "Executing SQL migration..." -ForegroundColor Yellow
    
    $sqlcmdArgs = @(
        "-S", $server,
        "-d", $database,
        "-U", $username,
        "-P", $password,
        "-i", $sqlFile,
        "-b"  # Stop on error
    )
    
    $output = & sqlcmd @sqlcmdArgs 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "MIGRATION COMPLETED SUCCESSFULLY" -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "InventoryMovement table created successfully:" -ForegroundColor White
        Write-Host "  ✓ Immutable audit log for all stock changes" -ForegroundColor Green
        Write-Host "  ✓ Before/after quantity tracking" -ForegroundColor Green
        Write-Host "  ✓ Work order and user traceability" -ForegroundColor Green
        Write-Host "  ✓ Cost snapshot support" -ForegroundColor Green
        Write-Host "  ✓ Multi-tenant safety (CompanyId)" -ForegroundColor Green
        Write-Host "  ✓ 5 performance indexes" -ForegroundColor Green
        Write-Host "  ✓ Movement type validation" -ForegroundColor Green
        Write-Host "  ✓ Quantity consistency check" -ForegroundColor Green
        Write-Host ""
        Write-Host "Supported movement types:" -ForegroundColor Yellow
        Write-Host "  - Consumption (WO parts usage)" -ForegroundColor White
        Write-Host "  - Adjustment (manual stock changes)" -ForegroundColor White
        Write-Host "  - Restock (new inventory received)" -ForegroundColor White
        Write-Host "  - Return (unused parts returned)" -ForegroundColor White
        Write-Host "  - Correction (error fixes)" -ForegroundColor White
        Write-Host "  - InitialStock (initial inventory setup)" -ForegroundColor White
        Write-Host "  - Transfer (location transfers)" -ForegroundColor White
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. All inventory consumption now creates movement records" -ForegroundColor White
        Write-Host "  2. Complete audit trail for all stock changes" -ForegroundColor White
        Write-Host "  3. Negative stock prevention enforced" -ForegroundColor White
        Write-Host "  4. Transactional safety guaranteed" -ForegroundColor White
        Write-Host "  5. Ready for cost tracking and analytics" -ForegroundColor White
        Write-Host ""
        
        # Show migration output
        Write-Host "Migration output:" -ForegroundColor Cyan
        Write-Host $output -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "ERROR: Migration failed!" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error output:" -ForegroundColor Yellow
        Write-Host $output -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: An exception occurred during migration:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
