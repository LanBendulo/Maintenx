# Setup Local Database with Schema and Seed Data
# Use this when remote database is not accessible

$ErrorActionPreference = "Continue"

$LocalServer = "localhost\SQLEXPRESS"
$LocalDatabase = "DB_Maintenx"
$LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX Local Database Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Test local connection
Write-Host "Step 1: Testing local SQL Server..." -ForegroundColor Green

try {
    $masterConn = New-Object System.Data.SqlClient.SqlConnection("Server=$LocalServer;Database=master;Integrated Security=True;TrustServerCertificate=True;")
    $masterConn.Open()
    Write-Host "  OK SQL Server Express is running" -ForegroundColor Green
    $masterConn.Close()
}
catch {
    Write-Host "  ERROR Cannot connect to SQL Server: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Create database
Write-Host "Step 2: Creating database..." -ForegroundColor Green

try {
    $masterConn = New-Object System.Data.SqlClient.SqlConnection("Server=$LocalServer;Database=master;Integrated Security=True;TrustServerCertificate=True;")
    $masterConn.Open()
    
    $checkDbCmd = $masterConn.CreateCommand()
    $checkDbCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = '$LocalDatabase'"
    $dbExists = $checkDbCmd.ExecuteScalar()
    
    if ($dbExists -gt 0) {
        Write-Host "  Database already exists. Drop it? (yes/no)" -ForegroundColor Yellow
        $response = Read-Host
        if ($response -eq "yes") {
            $dropDbCmd = $masterConn.CreateCommand()
            $dropDbCmd.CommandText = "ALTER DATABASE [$LocalDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$LocalDatabase];"
            $dropDbCmd.ExecuteNonQuery() | Out-Null
            Write-Host "  OK Database dropped" -ForegroundColor Green
            $dbExists = 0
        }
    }
    
    if ($dbExists -eq 0) {
        $createDbCmd = $masterConn.CreateCommand()
        $createDbCmd.CommandText = "CREATE DATABASE [$LocalDatabase]"
        $createDbCmd.ExecuteNonQuery() | Out-Null
        Write-Host "  OK Database created: $LocalDatabase" -ForegroundColor Green
    }
    
    $masterConn.Close()
}
catch {
    Write-Host "  ERROR Failed to create database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Apply schema
Write-Host "Step 3: Creating schema..." -ForegroundColor Green

$schemaFile = "Database\maintenx_schema.sql"
if (Test-Path $schemaFile) {
    try {
        $schemaScript = Get-Content $schemaFile -Raw
        
        # Split by GO statements
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
                    # Ignore errors for objects that already exist
                    if ($_.Exception.Message -notlike "*already exists*") {
                        Write-Host "  WARNING: $_" -ForegroundColor Yellow
                    }
                }
            }
        }
        
        $localConn.Close()
        Write-Host "  OK Schema created ($batchCount batches executed)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR Failed to create schema: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "  ERROR Schema file not found: $schemaFile" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Apply seed data
Write-Host "Step 4: Seeding initial data..." -ForegroundColor Green

$seedFiles = @(
    "Database\maintenx_seed.sql"
)

foreach ($seedFile in $seedFiles) {
    if (Test-Path $seedFile) {
        try {
            Write-Host "  -> Applying $seedFile..." -ForegroundColor Cyan
            $seedScript = Get-Content $seedFile -Raw
            
            $batches = $seedScript -split '\r?\nGO\r?\n'
            
            $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
            $localConn.Open()
            
            foreach ($batch in $batches) {
                $batch = $batch.Trim()
                if ($batch.Length -gt 0) {
                    try {
                        $cmd = $localConn.CreateCommand()
                        $cmd.CommandText = $batch
                        $cmd.CommandTimeout = 300
                        $cmd.ExecuteNonQuery() | Out-Null
                    }
                    catch {
                        Write-Host "  WARNING: $_" -ForegroundColor Yellow
                    }
                }
            }
            
            $localConn.Close()
            Write-Host "  OK Seed data applied" -ForegroundColor Green
        }
        catch {
            Write-Host "  WARNING Failed to apply seed data: $_" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "  SKIP Seed file not found: $seedFile" -ForegroundColor DarkGray
    }
}

Write-Host ""

# Step 5: Update appsettings.json
Write-Host "Step 5: Updating appsettings.json..." -ForegroundColor Green

$appsettingsPath = "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.ConnectionStrings.DefaultConnection = $LocalConnectionString
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "  OK Connection string updated" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR Failed to update appsettings.json: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Local database: $LocalDatabase" -ForegroundColor Green
Write-Host "Server: $LocalServer" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run the application" -ForegroundColor White
Write-Host "2. Register a new account or use seeded credentials" -ForegroundColor White
Write-Host "3. Test the functionality" -ForegroundColor White
Write-Host ""
