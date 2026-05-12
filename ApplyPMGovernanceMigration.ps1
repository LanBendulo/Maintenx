# ═══════════════════════════════════════════════════════════════════════════════
# PM GOVERNANCE MIGRATION SCRIPT
# Applies PreventiveScheduleId column to Work_Order table
# Enables PM lifecycle governance and duplicate prevention
# ═══════════════════════════════════════════════════════════════════════════════

param(
    [string]$Server = "db50508.public.databaseasp.net",
    [string]$Database = "db50508",
    [string]$Username = "",
    [string]$Password = "",
    [string]$SqlFile = "Database/add_pm_governance.sql"
)

Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "PM GOVERNANCE MIGRATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Prompt for credentials if not provided
if ([string]::IsNullOrEmpty($Username)) {
    $Username = Read-Host "Enter SQL Server username"
}

if ([string]::IsNullOrEmpty($Password)) {
    $SecurePassword = Read-Host "Enter SQL Server password" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecurePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
}

# Check if SQL file exists
if (-not (Test-Path $SqlFile)) {
    Write-Host "✗ ERROR: SQL file not found: $SqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Server: $Server" -ForegroundColor Gray
Write-Host "  Database: $Database" -ForegroundColor Gray
Write-Host "  SQL File: $SqlFile" -ForegroundColor Gray
Write-Host ""

# Confirm execution
$confirm = Read-Host "Apply PM governance migration? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Migration cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "→ Executing migration..." -ForegroundColor Yellow
Write-Host ""

try {
    # Build connection string
    $connectionString = "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;TrustServerCertificate=True;Connection Timeout=30;"
    
    # Read SQL file
    $sqlContent = Get-Content -Path $SqlFile -Raw
    
    # Replace database name placeholder
    $sqlContent = $sqlContent -replace "USE MaintenX;", "USE [$Database];"
    
    # Execute SQL
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlContent
    $command.CommandTimeout = 120
    
    # Execute and capture messages
    $connection.add_InfoMessage({
        param($sender, $event)
        Write-Host $event.Message -ForegroundColor Gray
    })
    
    $command.ExecuteNonQuery() | Out-Null
    
    $connection.Close()
    
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "✓ PM GOVERNANCE MIGRATION COMPLETED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "CHANGES APPLIED:" -ForegroundColor Yellow
    Write-Host "  • Added preventive_schedule_id column to Work_Order" -ForegroundColor Gray
    Write-Host "  • Added foreign key to PreventiveSchedule" -ForegroundColor Gray
    Write-Host "  • Created performance index" -ForegroundColor Gray
    Write-Host "  • Backfilled existing PM work orders" -ForegroundColor Gray
    Write-Host ""
    Write-Host "NEXT STEPS:" -ForegroundColor Yellow
    Write-Host "  1. Rebuild and deploy application" -ForegroundColor Gray
    Write-Host "  2. Test PM work order generation" -ForegroundColor Gray
    Write-Host "  3. Verify duplicate prevention works" -ForegroundColor Gray
    Write-Host "  4. Check UI governance indicators" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "✗ ERROR: Migration failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
