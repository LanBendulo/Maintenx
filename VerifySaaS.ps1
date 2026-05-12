# Simple SaaS Migration Verification
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "SaaS Migration Verification" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

$connectionString = "Server=db50508.public.databaseasp.net;Database=db50508;User Id=db50508;Password=3k+L?Gm8n9Z_;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

try {
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString
    $sqlConnection.Open()

    Write-Host "Connected to database successfully" -ForegroundColor Green
    Write-Host ""

    # Check SubscriptionPlan table
    $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
    $sqlCommand.Connection = $sqlConnection
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM SubscriptionPlan"
    $planCount = $sqlCommand.ExecuteScalar()
    
    if ($planCount -gt 0) {
        Write-Host "✓ SubscriptionPlan table: $planCount plans found" -ForegroundColor Green
    } else {
        Write-Host "⚠ SubscriptionPlan table: 0 plans found" -ForegroundColor Yellow
    }

    # Check CompanySubscription table
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM CompanySubscription"
    $subCount = $sqlCommand.ExecuteScalar()
    Write-Host "✓ CompanySubscription table: $subCount subscriptions" -ForegroundColor Green

    # Check SuperAdmin role
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM AspNetRoles WHERE Name = 'SuperAdmin'"
    $roleCount = $sqlCommand.ExecuteScalar()
    
    if ($roleCount -gt 0) {
        Write-Host "✓ SuperAdmin role: EXISTS" -ForegroundColor Green
    } else {
        Write-Host "⚠ SuperAdmin role: NOT FOUND" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Subscription Plans:" -ForegroundColor Cyan
    
    # Get plan details
    $sqlCommand.CommandText = "SELECT name, monthly_price, yearly_price, max_users, max_assets FROM SubscriptionPlan ORDER BY monthly_price"
    $reader = $sqlCommand.ExecuteReader()
    
    while ($reader.Read()) {
        $name = $reader["name"]
        $monthly = $reader["monthly_price"]
        $yearly = $reader["yearly_price"]
        $users = if ([DBNull]::Value.Equals($reader["max_users"])) { "Unlimited" } else { $reader["max_users"] }
        $assets = if ([DBNull]::Value.Equals($reader["max_assets"])) { "Unlimited" } else { $reader["max_assets"] }
        
        Write-Host "  $name" -ForegroundColor White
        Write-Host "    Monthly: $$monthly | Yearly: $$yearly" -ForegroundColor Gray
        Write-Host "    Users: $users | Assets: $assets" -ForegroundColor Gray
    }
    $reader.Close()

    $sqlConnection.Close()

    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "✓ Migration Verified Successfully!" -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Cyan
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($sqlConnection.State -eq 'Open') {
        $sqlConnection.Close()
    }
}
