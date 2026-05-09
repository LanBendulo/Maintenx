# PowerShell script to apply SaaS Architecture migration
# Reads connection string from appsettings.json and executes SQL script

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "SaaS Architecture Migration Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

# Parse connection string
$server = ($connectionString -split ';' | Where-Object { $_ -like 'Server=*' }) -replace 'Server=', ''
$database = ($connectionString -split ';' | Where-Object { $_ -like 'Database=*' }) -replace 'Database=', ''
$userId = ($connectionString -split ';' | Where-Object { $_ -like 'User Id=*' }) -replace 'User Id=', ''
$password = ($connectionString -split ';' | Where-Object { $_ -like 'Password=*' }) -replace 'Password=', ''

Write-Host "Database Server: $server" -ForegroundColor Yellow
Write-Host "Database Name: $database" -ForegroundColor Yellow
Write-Host ""

# Read SQL script
$sqlScript = Get-Content "Database/add_saas_architecture.sql" -Raw

Write-Host "Executing migration script..." -ForegroundColor Green
Write-Host ""

try {
    # Create SQL connection
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString
    $sqlConnection.Open()

    # Split script by GO statements
    $batches = $sqlScript -split '\r?\nGO\r?\n'
    
    $batchCount = 0
    foreach ($batch in $batches) {
        $batch = $batch.Trim()
        if ($batch.Length -gt 0) {
            $batchCount++
            Write-Host "Executing batch $batchCount..." -ForegroundColor Gray
            
            $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
            $sqlCommand.Connection = $sqlConnection
            $sqlCommand.CommandText = $batch
            $sqlCommand.CommandTimeout = 120
            
            $result = $sqlCommand.ExecuteNonQuery()
        }
    }

    Write-Host ""
    Write-Host "Migration executed successfully!" -ForegroundColor Green
    Write-Host "Total batches executed: $batchCount" -ForegroundColor Green

    $sqlConnection.Close()
}
catch {
    Write-Host "Error executing migration:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($sqlConnection.State -eq 'Open') {
        $sqlConnection.Close()
    }
    exit 1
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
