# Apply Asset Status Migration
# Creates AssetStatusHistory table and standardizes status values

$Server = "db50508.public.databaseasp.net"
$Database = "maintenx_db"
$Username = "maintenx_admin"
$SqlFile = "Database/standardize_asset_status.sql"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ASSET STATUS MIGRATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  1. Standardize Asset.Status values" -ForegroundColor White
Write-Host "  2. Create AssetStatusHistory table" -ForegroundColor White
Write-Host "  3. Create audit trail indexes" -ForegroundColor White
Write-Host ""

# Prompt for password securely
$Password = Read-Host "Enter database password" -AsSecurePtr
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
$PlainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host ""
Write-Host "Connecting to database..." -ForegroundColor Yellow

try {
    # Run SQL script
    $result = sqlcmd -S $Server -U $Username -P $PlainPassword -d $Database -i $SqlFile -b
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✓ MIGRATION SUCCESSFUL" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "AssetStatusHistory table created." -ForegroundColor Green
        Write-Host "Asset status values standardized." -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now create work orders without errors." -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "✗ MIGRATION FAILED" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error output:" -ForegroundColor Red
        Write-Host $result -ForegroundColor Red
    }
} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "✗ ERROR" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host $_.Exception.Message -ForegroundColor Red
} finally {
    # Clear password from memory
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
