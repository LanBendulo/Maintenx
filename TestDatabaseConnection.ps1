# Database Connection Test Script for MonsterASP.NET
# This script tests the SQL Server connection before running the application

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Connection Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$server = "db50508.databaseasp.net,1433"
$database = "db50508"
$username = "db50508"
$password = "3k+L?Gm8n9Z_"

Write-Host "Testing connection to:" -ForegroundColor Yellow
Write-Host "  Server: $server" -ForegroundColor White
Write-Host "  Database: $database" -ForegroundColor White
Write-Host ""

# Test 1: Network connectivity
Write-Host "[1/4] Testing network connectivity..." -ForegroundColor Yellow
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect("db50508.databaseasp.net", 1433)
    $tcpClient.Close()
    Write-Host "  ✓ Network connection successful" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Network connection failed" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "POSSIBLE CAUSES:" -ForegroundColor Yellow
    Write-Host "  - Firewall blocking port 1433" -ForegroundColor White
    Write-Host "  - MonsterASP.NET server is down" -ForegroundColor White
    Write-Host "  - Your IP is not whitelisted" -ForegroundColor White
    exit 1
}

# Test 2: DNS resolution
Write-Host "[2/4] Testing DNS resolution..." -ForegroundColor Yellow
try {
    $dns = [System.Net.Dns]::GetHostAddresses("db50508.databaseasp.net")
    Write-Host "  ✓ DNS resolved to: $($dns[0].IPAddressToString)" -ForegroundColor Green
} catch {
    Write-Host "  ✗ DNS resolution failed" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    exit 1
}

# Test 3: SQL Server connection
Write-Host "[3/4] Testing SQL Server connection..." -ForegroundColor Yellow
$connectionString = "Server=tcp:$server;Database=$database;User Id=$username;Password=$password;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "  ✓ SQL Server connection successful" -ForegroundColor Green
    
    # Test 4: Query execution
    Write-Host "[4/4] Testing query execution..." -ForegroundColor Yellow
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT @@VERSION"
    $version = $command.ExecuteScalar()
    Write-Host "  ✓ Query executed successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "SQL Server Version:" -ForegroundColor Cyan
    Write-Host "  $version" -ForegroundColor White
    
    $connection.Close()
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "ALL TESTS PASSED!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your database connection is working correctly." -ForegroundColor White
    Write-Host "You can now run: dotnet run" -ForegroundColor Yellow
    
} catch {
    Write-Host "  ✗ SQL Server connection failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Yellow
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "TROUBLESHOOTING STEPS:" -ForegroundColor Yellow
    Write-Host "  1. Log into MonsterASP.NET control panel" -ForegroundColor White
    Write-Host "  2. Verify database status is 'Active'" -ForegroundColor White
    Write-Host "  3. Check IP whitelist/firewall settings" -ForegroundColor White
    Write-Host "  4. Verify username and password are correct" -ForegroundColor White
    Write-Host "  5. Contact MonsterASP.NET support if needed" -ForegroundColor White
    Write-Host ""
    exit 1
}
