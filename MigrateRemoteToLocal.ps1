# =============================================
# Migrate Remote Database to Local SQL Server
# =============================================

param(
    [string]$LocalServer = "localhost\SQLEXPRESS",
    [string]$LocalDatabase = "DB_Maintenx",
    [string]$LocalUser = "",  # Leave empty for Windows Authentication
    [string]$LocalPassword = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX Database Migration Tool" -ForegroundColor Cyan
Write-Host "Remote to Local Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Remote database connection details
$RemoteServer = "db50508.public.databaseasp.net"
$RemoteDatabase = "db50508"
$RemoteUser = "db50508"
$RemotePassword = "3k+L?Gm8n9Z_"

# Build connection strings
if ([string]::IsNullOrEmpty($LocalUser)) {
    $LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    Write-Host "Using Windows Authentication for local database" -ForegroundColor Yellow
} else {
    $LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;User Id=$LocalUser;Password=$LocalPassword;TrustServerCertificate=True;MultipleActiveResultSets=True;"
    Write-Host "Using SQL Authentication for local database" -ForegroundColor Yellow
}

$RemoteConnectionString = "Server=$RemoteServer;Database=$RemoteDatabase;User Id=$RemoteUser;Password=$RemotePassword;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host ""
Write-Host "Step 1: Testing database connections..." -ForegroundColor Green

# Test remote connection
try {
    $remoteConn = New-Object System.Data.SqlClient.SqlConnection($RemoteConnectionString)
    $remoteConn.Open()
    Write-Host "✓ Remote database connection successful" -ForegroundColor Green
    $remoteConn.Close()
} catch {
    Write-Host "✗ Failed to connect to remote database: $_" -ForegroundColor Red
    exit 1
}

# Test local connection
try {
    $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
    $localConn.Open()
    Write-Host "✓ Local database connection successful" -ForegroundColor Green
    $localConn.Close()
} catch {
    Write-Host "✗ Failed to connect to local database: $_" -ForegroundColor Red
    Write-Host "Make sure SQL Server is running and you have proper permissions" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Step 2: Creating local database if not exists..." -ForegroundColor Green

# Create database if it doesn't exist
$masterConnectionString = $LocalConnectionString -replace "Database=$LocalDatabase", "Database=master"
try {
    $masterConn = New-Object System.Data.SqlClient.SqlConnection($masterConnectionString)
    $masterConn.Open()
    
    $checkDbCmd = $masterConn.CreateCommand()
    $checkDbCmd.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = '$LocalDatabase'"
    $dbExists = $checkDbCmd.ExecuteScalar()
    
    if ($dbExists -eq 0) {
        $createDbCmd = $masterConn.CreateCommand()
        $createDbCmd.CommandText = "CREATE DATABASE [$LocalDatabase]"
        $createDbCmd.ExecuteNonQuery() | Out-Null
        Write-Host "✓ Local database created: $LocalDatabase" -ForegroundColor Green
    } else {
        Write-Host "✓ Local database already exists: $LocalDatabase" -ForegroundColor Yellow
        $response = Read-Host "Do you want to drop and recreate it? (yes/no)"
        if ($response -eq "yes") {
            $dropDbCmd = $masterConn.CreateCommand()
            $dropDbCmd.CommandText = "ALTER DATABASE [$LocalDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$LocalDatabase]; CREATE DATABASE [$LocalDatabase]"
            $dropDbCmd.ExecuteNonQuery() | Out-Null
            Write-Host "✓ Database recreated" -ForegroundColor Green
        }
    }
    
    $masterConn.Close()
} catch {
    Write-Host "✗ Failed to create database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Creating schema in local database..." -ForegroundColor Green

# Apply schema
$schemaFile = "Database\maintenx_schema.sql"
if (Test-Path $schemaFile) {
    try {
        $schemaScript = Get-Content $schemaFile -Raw
        $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
        $localConn.Open()
        
        $schemaCmd = $localConn.CreateCommand()
        $schemaCmd.CommandText = $schemaScript
        $schemaCmd.CommandTimeout = 300
        $schemaCmd.ExecuteNonQuery() | Out-Null
        
        $localConn.Close()
        Write-Host "✓ Schema created successfully" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to create schema: $_" -ForegroundColor Red
        Write-Host "Continuing with migration..." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠ Schema file not found: $schemaFile" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 4: Exporting data from remote database..." -ForegroundColor Green

# Tables to export in order (respecting foreign key dependencies)
$tables = @(
    "SubscriptionPlans",
    "Companies",
    "AspNetRoles",
    "AspNetUsers",
    "AspNetUserRoles",
    "AspNetUserClaims",
    "AspNetUserLogins",
    "AspNetUserTokens",
    "AspNetRoleClaims",
    "Assets",
    "Personnel",
    "MaintenanceRequests",
    "WorkOrders",
    "Parts",
    "WorkOrderParts",
    "InventoryMovements",
    "MaintenanceLog",
    "WorkOrderCosts",
    "PreventiveSchedules",
    "PMGenerationLog",
    "PMGovernanceLog"
)

$remoteConn = New-Object System.Data.SqlClient.SqlConnection($RemoteConnectionString)
$remoteConn.Open()

$localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
$localConn.Open()

$totalRecords = 0

foreach ($table in $tables) {
    try {
        # Check if table exists in remote
        $checkTableCmd = $remoteConn.CreateCommand()
        $checkTableCmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '$table'"
        $tableExists = $checkTableCmd.ExecuteScalar()
        
        if ($tableExists -eq 0) {
            Write-Host "  ⊘ Table $table does not exist in remote database, skipping..." -ForegroundColor DarkGray
            continue
        }
        
        # Get row count
        $countCmd = $remoteConn.CreateCommand()
        $countCmd.CommandText = "SELECT COUNT(*) FROM [$table]"
        $rowCount = $countCmd.ExecuteScalar()
        
        if ($rowCount -eq 0) {
            Write-Host "  ⊘ Table $table is empty, skipping..." -ForegroundColor DarkGray
            continue
        }
        
        Write-Host "  → Exporting $table ($rowCount rows)..." -ForegroundColor Cyan
        
        # Disable identity insert if needed
        $disableIdentityCmd = $localConn.CreateCommand()
        $disableIdentityCmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('$table') AND is_identity = 1) SET IDENTITY_INSERT [$table] ON"
        $disableIdentityCmd.ExecuteNonQuery() | Out-Null
        
        # Export data
        $selectCmd = $remoteConn.CreateCommand()
        $selectCmd.CommandText = "SELECT * FROM [$table]"
        $selectCmd.CommandTimeout = 300
        $reader = $selectCmd.ExecuteReader()
        
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter
        $adapter.SelectCommand = $selectCmd
        $dataTable = New-Object System.Data.DataTable
        $adapter.Fill($dataTable) | Out-Null
        
        $reader.Close()
        
        # Bulk insert into local
        $bulkCopy = New-Object System.Data.SqlClient.SqlBulkCopy($localConn)
        $bulkCopy.DestinationTableName = $table
        $bulkCopy.BulkCopyTimeout = 300
        $bulkCopy.WriteToServer($dataTable)
        
        # Re-enable identity insert
        $enableIdentityCmd = $localConn.CreateCommand()
        $enableIdentityCmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('$table') AND is_identity = 1) SET IDENTITY_INSERT [$table] OFF"
        $enableIdentityCmd.ExecuteNonQuery() | Out-Null
        
        $totalRecords += $rowCount
        Write-Host "  ✓ Exported $rowCount rows from $table" -ForegroundColor Green
        
    } catch {
        Write-Host "  ✗ Failed to export $table : $_" -ForegroundColor Red
    }
}

$remoteConn.Close()
$localConn.Close()

Write-Host ""
Write-Host "Step 5: Updating appsettings.json..." -ForegroundColor Green

# Update appsettings.json
$appsettingsPath = "appsettings.json"
if (Test-Path $appsettingsPath) {
    try {
        $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $appsettings.ConnectionStrings.DefaultConnection = $LocalConnectionString
        $appsettings | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
        Write-Host "✓ appsettings.json updated with local connection string" -ForegroundColor Green
    } catch {
        Write-Host "✗ Failed to update appsettings.json: $_" -ForegroundColor Red
    }
} else {
    Write-Host "⚠ appsettings.json not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total records migrated: $totalRecords" -ForegroundColor Green
Write-Host "Local database: $LocalDatabase" -ForegroundColor Green
Write-Host "Connection string updated in appsettings.json" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Verify the data in your local database" -ForegroundColor White
Write-Host "2. Run the application and test functionality" -ForegroundColor White
Write-Host "3. Keep a backup of your remote database" -ForegroundColor White
Write-Host ""
