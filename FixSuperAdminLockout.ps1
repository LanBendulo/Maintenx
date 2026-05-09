# PowerShell script to fix SuperAdmin lockout setting

Write-Host "Fixing SuperAdmin Lockout Setting..." -ForegroundColor Cyan

# Read connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

try {
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString
    $sqlConnection.Open()

    $query = "UPDATE AspNetUsers SET LockoutEnabled = 0 WHERE Email = 'superadmin@maintenx.com'"

    $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
    $sqlCommand.Connection = $sqlConnection
    $sqlCommand.CommandText = $query
    
    $rowsAffected = $sqlCommand.ExecuteNonQuery()
    
    if ($rowsAffected -gt 0) {
        Write-Host "SuperAdmin lockout disabled successfully!" -ForegroundColor Green
    } else {
        Write-Host "No rows updated" -ForegroundColor Yellow
    }
    
    $sqlConnection.Close()
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
