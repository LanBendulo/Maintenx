# ═══════════════════════════════════════════════════════════════════════════════
# Make Google User SuperAdmin
# Assigns SuperAdmin role to n.bendulo.546481@umindanao.edu.ph
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Make Google User SuperAdmin" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Database connection details
$Server = "db50508.public.databaseasp.net"
$Database = "db50508"
$Username = "db50508"
$Password = "3k+L?Gm8n9Z_"

Write-Host "Target Email: n.bendulo.546481@umindanao.edu.ph" -ForegroundColor Yellow
Write-Host "Target Role: SuperAdmin" -ForegroundColor Yellow
Write-Host ""

# Confirm execution
$confirmation = Read-Host "IMPORTANT: The user must have logged in with Google OAuth at least once. Continue? (Y/N)"
if ($confirmation -ne 'Y' -and $confirmation -ne 'y') {
    Write-Host "Operation cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Connecting to database: $Database on $Server" -ForegroundColor Cyan

# SQL script path
$SqlScriptPath = "Database\make_google_user_superadmin.sql"

if (-not (Test-Path $SqlScriptPath)) {
    Write-Host "ERROR: SQL script not found at $SqlScriptPath" -ForegroundColor Red
    exit 1
}

try {
    # Execute SQL script using sqlcmd
    Write-Host "Executing SQL script..." -ForegroundColor Cyan
    
    $result = sqlcmd -S $Server -d $Database -U $Username -P $Password -i $SqlScriptPath -b
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host "SUCCESS: SuperAdmin role assigned!" -ForegroundColor Green
        Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Green
        Write-Host ""
        Write-Host "Output:" -ForegroundColor Cyan
        Write-Host $result
        Write-Host ""
        Write-Host "Next Steps:" -ForegroundColor Yellow
        Write-Host "1. User should log out and log back in" -ForegroundColor White
        Write-Host "2. User will now have SuperAdmin access" -ForegroundColor White
        Write-Host "3. User can access /superadmin/dashboard" -ForegroundColor White
    }
    else {
        Write-Host ""
        Write-Host "ERROR: SQL script execution failed" -ForegroundColor Red
        Write-Host $result
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "ERROR: Failed to execute SQL script" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
