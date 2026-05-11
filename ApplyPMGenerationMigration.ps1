# Apply PM Generation Tracking Migration
# Run this script to add the new columns to the PreventiveSchedule table

$connectionString = "Server=db50508.public.databaseasp.net;Database=db50508;User Id=db50508;Password=3k+L?Gm8n9Z_;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$sqlFile = "Database/add_pm_generation_tracking.sql"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "PM Generation Tracking Migration" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $sqlFile)) {
    Write-Host "ERROR: SQL file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Reading SQL script..." -ForegroundColor Yellow
$sql = Get-Content $sqlFile -Raw

Write-Host "Connecting to database..." -ForegroundColor Yellow
Write-Host "Server: db50508.public.databaseasp.net" -ForegroundColor Gray
Write-Host "Database: db50508" -ForegroundColor Gray
Write-Host ""

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    
    Write-Host "Connected successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Executing migration..." -ForegroundColor Yellow
    
    # Split SQL by GO statements
    $batches = $sql -split '\r?\nGO\r?\n'
    
    foreach ($batch in $batches) {
        $batch = $batch.Trim()
        if ($batch.Length -gt 0) {
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 120
            $command.ExecuteNonQuery() | Out-Null
        }
    }
    
    Write-Host ""
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "New columns added:" -ForegroundColor Cyan
    Write-Host "  - priority" -ForegroundColor Gray
    Write-Host "  - last_generated_date" -ForegroundColor Gray
    Write-Host "  - last_generated_work_order_id" -ForegroundColor Gray
    Write-Host "  - last_generation_attempt" -ForegroundColor Gray
    Write-Host "  - last_generation_error" -ForegroundColor Gray
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
Write-Host "PM automatic generation is now enabled!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan
