# Apply Remote Database Schema from script.ipynb to Local Database

$ErrorActionPreference = "Continue"

$LocalServer = "localhost\SQLEXPRESS"
$LocalDatabase = "DB_Maintenx"
$LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Apply Remote Schema to Local Database" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Read the notebook file
Write-Host "Step 1: Reading script.ipynb..." -ForegroundColor Green

if (-not (Test-Path "script.ipynb")) {
    Write-Host "  ERROR script.ipynb not found" -ForegroundColor Red
    exit 1
}

$notebookContent = Get-Content "script.ipynb" -Raw | ConvertFrom-Json
Write-Host "  OK Notebook loaded" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2: Extracting SQL statements..." -ForegroundColor Green

$sqlStatements = @()
$cellCount = 0

foreach ($cell in $notebookContent.cells) {
    if ($cell.cell_type -eq "code" -and $cell.source) {
        $source = $cell.source -join ""
        if ($source.Trim().Length -gt 0 -and $source -notlike "*USE [master]*" -and $source -notlike "*CREATE DATABASE*" -and $source -notlike "*ALTER DATABASE*") {
            $sqlStatements += $source
            $cellCount++
        }
    }
}

Write-Host "  OK Extracted $cellCount SQL code cells" -ForegroundColor Green

Write-Host ""
Write-Host "Step 3: Connecting to local database..." -ForegroundColor Green

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
    $conn.Open()
    Write-Host "  OK Connected to $LocalDatabase" -ForegroundColor Green
}
catch {
    Write-Host "  ERROR Cannot connect: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 4: Executing SQL statements..." -ForegroundColor Green

$successCount = 0
$skipCount = 0
$errorCount = 0

foreach ($sql in $sqlStatements) {
    # Split by GO statements
    $batches = $sql -split '\r?\nGO\r?\n'
    
    foreach ($batch in $batches) {
        $batch = $batch.Trim()
        if ($batch.Length -eq 0) {
            continue
        }
        
        try {
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = $batch
            $cmd.CommandTimeout = 300
            $cmd.ExecuteNonQuery() | Out-Null
            $successCount++
        }
        catch {
            $errorMsg = $_.Exception.Message
            
            # Skip expected errors (objects already exist)
            if ($errorMsg -like "*already exists*" -or 
                $errorMsg -like "*already an object*" -or
                $errorMsg -like "*Cannot drop*does not exist*") {
                $skipCount++
            }
            else {
                Write-Host "  WARNING: $errorMsg" -ForegroundColor Yellow
                $errorCount++
            }
        }
    }
}

$conn.Close()

Write-Host "  OK Executed: $successCount | Skipped: $skipCount | Errors: $errorCount" -ForegroundColor Green

Write-Host ""
Write-Host "Step 5: Verifying schema..." -ForegroundColor Green

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $tableCount = $cmd.ExecuteScalar()
    
    Write-Host "  OK Database has $tableCount tables" -ForegroundColor Green
    
    $conn.Close()
}
catch {
    Write-Host "  ERROR Verification failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Schema Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your local database now has the remote schema structure." -ForegroundColor White
Write-Host "Run your application to test the migration." -ForegroundColor White
Write-Host ""
