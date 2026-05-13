# Automatic Local Database Setup (No prompts)

$ErrorActionPreference = "Continue"

$LocalServer = "localhost\SQLEXPRESS"
$LocalDatabase = "DB_Maintenx"
$LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX Local Database Setup (Auto)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Test connection
Write-Host "Step 1: Testing SQL Server..." -ForegroundColor Green

try {
    $masterConn = New-Object System.Data.SqlClient.SqlConnection("Server=$LocalServer;Database=master;Integrated Security=True;TrustServerCertificate=True;")
    $masterConn.Open()
    Write-Host "  OK Connected" -ForegroundColor Green
    $masterConn.Close()
}
catch {
    Write-Host "  ERROR Cannot connect: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Create/Recreate database
Write-Host "Step 2: Setting up database..." -ForegroundColor Green

try {
    $masterConn = New-Object System.Data.SqlClient.SqlConnection("Server=$LocalServer;Database=master;Integrated Security=True;TrustServerCertificate=True;")
    $masterConn.Open()
    
    $checkDbCmd = $masterConn.CreateCommand()
    $checkDbCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = '$LocalDatabase'"
    $dbExists = $checkDbCmd.ExecuteScalar()
    
    if ($dbExists -gt 0) {
        Write-Host "  -> Dropping existing database..." -ForegroundColor Yellow
        $dropDbCmd = $masterConn.CreateCommand()
        $dropDbCmd.CommandText = "ALTER DATABASE [$LocalDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$LocalDatabase];"
        $dropDbCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  OK Dropped" -ForegroundColor Green
    }
    
    Write-Host "  -> Creating database..." -ForegroundColor Cyan
    $createDbCmd = $masterConn.CreateCommand()
    $createDbCmd.CommandText = "CREATE DATABASE [$LocalDatabase]"
    $createDbCmd.ExecuteNonQuery() | Out-Null
    Write-Host "  OK Created: $LocalDatabase" -ForegroundColor Green
    
    $masterConn.Close()
}
catch {
    Write-Host "  ERROR $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Apply schema
Write-Host "Step 3: Creating schema..." -ForegroundColor Green

$schemaFile = "Database\maintenx_schema.sql"
if (Test-Path $schemaFile) {
    try {
        $schemaScript = Get-Content $schemaFile -Raw
        $batches = $schemaScript -split '\r?\nGO\r?\n'
        
        $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
        $localConn.Open()
        
        $batchCount = 0
        foreach ($batch in $batches) {
            $batch = $batch.Trim()
            if ($batch.Length -gt 0) {
                try {
                    $cmd = $localConn.CreateCommand()
                    $cmd.CommandText = $batch
                    $cmd.CommandTimeout = 300
                    $cmd.ExecuteNonQuery() | Out-Null
                    $batchCount++
                }
                catch {
                    Write-Host "  WARNING: $_" -ForegroundColor DarkYellow
                }
            }
        }
        
        $localConn.Close()
        Write-Host "  OK Schema created ($batchCount batches)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "  ERROR Schema file not found" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Seed data
Write-Host "Step 4: Seeding data..." -ForegroundColor Green

$seedFile = "Database\maintenx_seed.sql"
if (Test-Path $seedFile) {
    try {
        $seedScript = Get-Content $seedFile -Raw
        $batches = $seedScript -split '\r?\nGO\r?\n'
        
        $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
        $localConn.Open()
        
        $batchCount = 0
        foreach ($batch in $batches) {
            $batch = $batch.Trim()
            if ($batch.Length -gt 0) {
                try {
                    $cmd = $localConn.CreateCommand()
                    $cmd.CommandText = $batch
                    $cmd.CommandTimeout = 300
                    $cmd.ExecuteNonQuery() | Out-Null
                    $batchCount++
                }
                catch {
                    Write-Host "  WARNING: $_" -ForegroundColor DarkYellow
                }
            }
        }
        
        $localConn.Close()
        Write-Host "  OK Seed data applied ($batchCount batches)" -ForegroundColor Green
    }
    catch {
        Write-Host "  WARNING Seed data had issues: $_" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  SKIP No seed file found" -ForegroundColor DarkGray
}

Write-Host ""

# Step 5: Update appsettings.json
Write-Host "Step 5: Updating appsettings.json..." -ForegroundColor Green

$appsettingsPath = "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $oldConnection = $appsettings.ConnectionStrings.DefaultConnection
        $appsettings.ConnectionStrings.DefaultConnection = $LocalConnectionString
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "  OK Updated" -ForegroundColor Green
        Write-Host "  Old: $oldConnection" -ForegroundColor DarkGray
        Write-Host "  New: $LocalConnectionString" -ForegroundColor DarkGray
    }
    catch {
        Write-Host "  ERROR $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database: $LocalDatabase" -ForegroundColor White
Write-Host "Server: $LocalServer" -ForegroundColor White
Write-Host "Connection: Windows Authentication" -ForegroundColor White
Write-Host ""
Write-Host "You can now run your application!" -ForegroundColor Green
Write-Host ""
