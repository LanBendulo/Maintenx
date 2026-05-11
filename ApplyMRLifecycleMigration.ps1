# Apply Maintenance Request Lifecycle Tracking Migration
# Run this script to add the new columns to the Maintenance_Request table

$connectionString = "Server=db50508.public.databaseasp.net;Database=db50508;User Id=db50508;Password=3k+L?Gm8n9Z_;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$sqlFile = "Database/add_mr_lifecycle_tracking.sql"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Maintenance Request Lifecycle Migration" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $sqlFile)) {
    Write-Host "ERROR: SQL file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reading SQL script..." -ForegroundColor Yellow
$sql = Get-Content $sqlFile -Raw

Write-Host "Connecting to database..." -ForegroundColor Yellow
Write-Host "Server: db50508.databaseasp.net" -ForegroundColor Gray
Write-Host "Database: db50508_maintenx" -ForegroundColor Gray
Write-Host ""

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    Write-Host "✓ Connected successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executing migration..." -ForegroundColor Yellow
    Write-Host "Database: db50508" -ForegroundColor Gray
    Write-Host ""
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    
    $result = $command.ExecuteNonQuery()
    
    Write-Host ""
    Write-Host "✓ Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "New columns added:" -ForegroundColor Cyan
    Write-Host "  - converted_work_order_id" -ForegroundColor Gray
    Write-Host "  - converted_at" -ForegroundColor Gray
    Write-Host "  - converted_by_user_id" -ForegroundColor Gray
    Write-Host "  - closed_at" -ForegroundColor Gray
    Write-Host "  - closed_by_user_id" -ForegroundColor Gray
    Write-Host ""
    
    $connection.Close()
}
catch {
    Write-Host ""
    Write-Host "ERROR: Migration failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "You can now run the application!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
