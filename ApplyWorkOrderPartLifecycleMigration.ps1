# ============================================================
# APPLY WORKORDERPART LIFECYCLE FIELDS MIGRATION
# ============================================================
# Purpose: Apply lifecycle governance fields to WorkOrderPart table
# Implements staged parts usage workflow with consumption tracking
# ============================================================

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "WORKORDERPART LIFECYCLE FIELDS MIGRATION" -ForegroundColor Cyan
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
Write-Host "  1. Add usage_status column (Pending/Approved/Consumed/Rejected)" -ForegroundColor White
Write-Host "  2. Add added_by_personnel_id column (technician who staged)" -ForegroundColor White
Write-Host "  3. Add approved_by_user_id column (supervisor who approved)" -ForegroundColor White
Write-Host "  4. Add consumed_at column (consumption timestamp)" -ForegroundColor White
Write-Host "  5. Add updated_at column (tracking changes)" -ForegroundColor White
Write-Host "  6. Add foreign key constraints and indexes" -ForegroundColor White
Write-Host "  7. Migrate existing records to 'Consumed' status" -ForegroundColor White
Write-Host ""

$confirmation = Read-Host "Do you want to proceed? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "Migration cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Applying migration..." -ForegroundColor Yellow

# SQL Migration file
$sqlFile = "Database\add_workorderpart_lifecycle_fields.sql"

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
        Write-Host "WorkOrderPart table now supports staged parts workflow:" -ForegroundColor White
        Write-Host "  ✓ Lifecycle statuses: Pending, Approved, Consumed, Rejected" -ForegroundColor Green
        Write-Host "  ✓ Technician tracking (added_by_personnel_id)" -ForegroundColor Green
        Write-Host "  ✓ Approval tracking (approved_by_user_id)" -ForegroundColor Green
        Write-Host "  ✓ Consumption timestamp (consumed_at)" -ForegroundColor Green
        Write-Host "  ✓ Change tracking (updated_at)" -ForegroundColor Green
        Write-Host "  ✓ Foreign keys and indexes created" -ForegroundColor Green
        Write-Host "  ✓ Existing records migrated to 'Consumed' status" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Technicians can now stage parts for work orders" -ForegroundColor White
        Write-Host "  2. Parts remain editable while WO is active" -ForegroundColor White
        Write-Host "  3. Inventory is consumed when WO is completed" -ForegroundColor White
        Write-Host "  4. Full audit trail is maintained" -ForegroundColor White
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
