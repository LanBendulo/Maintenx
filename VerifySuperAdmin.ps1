# PowerShell script to verify SuperAdmin account creation

Write-Host "SuperAdmin Account Verification" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appSettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appSettings.ConnectionStrings.DefaultConnection

try {
    # Create SQL connection
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString
    $sqlConnection.Open()

    # Query to check SuperAdmin user
    $query = "SELECT u.Id, u.UserName, u.Email, u.CompanyId, u.FullName, u.EmailConfirmed, u.LockoutEnabled, r.Name AS RoleName FROM AspNetUsers u LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id WHERE u.Email = 'superadmin@maintenx.com'"

    $sqlCommand = New-Object System.Data.SqlClient.SqlCommand
    $sqlCommand.Connection = $sqlConnection
    $sqlCommand.CommandText = $query
    
    $reader = $sqlCommand.ExecuteReader()
    
    if ($reader.Read()) {
        Write-Host "SuperAdmin account found!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Email: $($reader['Email'])"
        Write-Host "Username: $($reader['UserName'])"
        Write-Host "Full Name: $($reader['FullName'])"
        Write-Host "Role: $($reader['RoleName'])"
        
        if ($reader['CompanyId'] -eq [DBNull]::Value) {
            Write-Host "CompanyId: NULL (Platform-level)" -ForegroundColor Green
        } else {
            Write-Host "CompanyId: $($reader['CompanyId'])" -ForegroundColor Red
            Write-Host "WARNING: CompanyId should be NULL for SuperAdmin!" -ForegroundColor Red
        }
        
        Write-Host "Email Confirmed: $($reader['EmailConfirmed'])"
        Write-Host "Lockout Enabled: $($reader['LockoutEnabled'])"
    } else {
        Write-Host "SuperAdmin account NOT found!" -ForegroundColor Red
    }
    
    $reader.Close()
    $sqlConnection.Close()
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
