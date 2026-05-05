# Apply Multi-Tenant Migration Script
# This script executes the SQL migration against the MonsterASP.NET database

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Multi-Tenant Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$server = "db50508.public.databaseasp.net"
$database = "db50508"
$username = "db50508"
$password = "3k+L?Gm8n9Z_"
$sqlFile = "Database/apply_multi_tenant_to_existing_db.sql"

Write-Host "Target Database:" -ForegroundColor Yellow
Write-Host "  Server: $server" -ForegroundColor White
Write-Host "  Database: $database" -ForegroundColor White
Write-Host ""

# Check if SQL file exists
if (-not (Test-Path $sqlFile)) {
    Write-Host "✗ SQL file not found: $sqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "Executing migration script..." -ForegroundColor Yellow
Write-Host ""

try {
    # Create connection string
    $connectionString = "Server=$server;Database=$database;User Id=$username;Password=$password;Encrypt=True;TrustServerCertificate=True;Connection Timeout=60;"
    
    # Read SQL file
    $sqlScript = Get-Content $sqlFile -Raw
    
    # Execute SQL
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sqlScript
    $command.CommandTimeout = 120
    
    # Execute and capture messages
    $connection.FireInfoMessageEventOnUserErrors = $true
    $connection.add_InfoMessage({
        param($sender, $event)
        Write-Host $event.Message -ForegroundColor Gray
    })
    
    $command.ExecuteNonQuery() | Out-Null
    
    $connection.Close()
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "✓ MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run the application:" -ForegroundColor White
    Write-Host "     dotnet run" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  2. Open browser:" -ForegroundColor White
    Write-Host "     http://localhost:5262" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  3. Login and test all features" -ForegroundColor White
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "✗ MIGRATION FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
