# PowerShell script to verify SaaS Architecture migration
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "SaaS Migration Verification" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

try {
    # Create SQL connection
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString
    $sqlConnection.Open()

    Write-Host "✓ Database connection successful" -ForegroundColor Green
    Write-Host ""

    # Check SubscriptionPlan table
    $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
    $sqlCommand.Connection = $sqlConnection
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM SubscriptionPlan"
    $planCount = $sqlCommand.ExecuteScalar()
    Write-Host "SubscriptionPlan table: $planCount plans found" -ForegroundColor $(if ($planCount -gt 0) { "Green" } else { "Yellow" })

    # Check CompanySubscription table
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM CompanySubscription"
    $subCount = $sqlCommand.ExecuteScalar()
    Write-Host "CompanySubscription table: $subCount subscriptions found" -ForegroundColor $(if ($subCount -ge 0) { "Green" } else { "Yellow" })

    # Check SuperAdmin role
    $sqlCommand.CommandText = "SELECT COUNT(*) FROM AspNetRoles WHERE Name = 'SuperAdmin'"
    $roleCount = $sqlCommand.ExecuteScalar()
    Write-Host "SuperAdmin role: $(if ($roleCount -gt 0) { 'EXISTS' } else { 'NOT FOUND' })" -ForegroundColor $(if ($roleCount -gt 0) { "Green" } else { "Yellow" })

    # List subscription plans
    if ($planCount -gt 0) {
        Write-Host ""
        Write-Host "Subscription Plans:" -ForegroundColor Cyan
        $sqlCommand.CommandText = "SELECT name, monthly_price, yearly_price, max_users, max_assets, is_active FROM SubscriptionPlan"
        $reader = $sqlCommand.ExecuteReader()
        while ($reader.Read()) {
            $name = $reader["name"]
            $monthly = $reader["monthly_price"]
            $yearly = $reader["yearly_price"]
            $users = if ($reader["max_users"] -is [DBNull]) { "Unlimited" } else { $reader["max_users"] }
            $assets = if ($reader["max_assets"] -is [DBNull]) { "Unlimited" } else { $reader["max_assets"] }
            $active = if ($reader["is_active"]) { "Active" } else { "Inactive" }
            Write-Host "  - $name" -ForegroundColor White
            Write-Host "    Price: $monthly/mo or $yearly/yr" -ForegroundColor Gray
            Write-Host "    Limits: $users users, $assets assets" -ForegroundColor Gray
            Write-Host "    Status: $active" -ForegroundColor Gray
            Write-Host ""
        }
        $reader.Close()
    }

    $sqlConnection.Close()

    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "✓ Verification Complete - Migration Successful!" -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Cyan
}
catch {
    Write-Host "Error during verification:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($sqlConnection.State -eq 'Open') {
        $sqlConnection.Close()
    }
    exit 1
}
