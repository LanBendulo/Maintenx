# PowerShell script to update Starter plan to free 14-day trial
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Update Starter Plan to Free 14-Day Trial" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

# Parse connection string
$server = ($connectionString -split ';' | Where-Object { $_ -like 'Server=*' }) -replace 'Server=', ''
$database = ($connectionString -split ';' | Where-Object { $_ -like 'Database=*' }) -replace 'Database=', ''

Write-Host "Database Server: $server" -ForegroundColor Yellow
Write-Host "Database Name: $database" -ForegroundColor Yellow
Write-Host ""

# Read SQL script
$sqlScript = Get-Content "Database/update_starter_plan_free_trial.sql" -Raw

Write-Host "Executing update script..." -ForegroundColor Green
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
    Write-Host "Update executed successfully!" -ForegroundColor Green
    Write-Host "Total batches executed: $batchCount" -ForegroundColor Green

    $sqlConnection.Close()
}
catch {
    Write-Host "Error executing update:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($sqlConnection.State -eq 'Open') {
        $sqlConnection.Close()
    }
    exit 1
}

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Update Complete!" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "The Starter plan is now FREE for 14 days!" -ForegroundColor Green
Write-Host ""
Write-Host "When assigning Starter subscriptions:" -ForegroundColor Yellow
Write-Host "  1. Check 'Trial Subscription'" -ForegroundColor Yellow
Write-Host "  2. Set End Date to 14 days from Start Date" -ForegroundColor Yellow
Write-Host "  3. Payment Status can be 'Paid' (since it's free)" -ForegroundColor Yellow
Write-Host ""
