# MaintenX Database Migration Script
# Migrates from remote to local SQL Server Express

$ErrorActionPreference = "Continue"

# Configuration
$RemoteServer = "db50508.public.databaseasp.net"
$RemoteDatabase = "db50508"
$RemoteUser = "db50508"
$RemotePassword = "3k+L?Gm8n9Z_"

$LocalServer = "localhost\SQLEXPRESS"
$LocalDatabase = "DB_Maintenx"

$RemoteConnectionString = "Server=$RemoteServer;Database=$RemoteDatabase;User Id=$RemoteUser;Password=$RemotePassword;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
$LocalConnectionString = "Server=$LocalServer;Database=$LocalDatabase;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MaintenX Database Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Test connections
Write-Host "Step 1: Testing connections..." -ForegroundColor Green

try {
    $remoteConn = New-Object System.Data.SqlClient.SqlConnection($RemoteConnectionString)
    $remoteConn.Open()
    Write-Host "  OK Remote database connected" -ForegroundColor Green
    $remoteConn.Close()
}
catch {
    Write-Host "  ERROR Cannot connect to remote database: $_" -ForegroundColor Red
    exit 1
}

try {
    $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
    $localConn.Open()
    Write-Host "  OK Local database connected" -ForegroundColor Green
    $localConn.Close()
}
catch {
    Write-Host "  ERROR Cannot connect to local database: $_" -ForegroundColor Red
    Write-Host "  Make sure SQL Server Express is running" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 2: Create database if needed
Write-Host "Step 2: Preparing local database..." -ForegroundColor Green

$masterConnectionString = "Server=$LocalServer;Database=master;Integrated Security=True;TrustServerCertificate=True;"

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
        Write-Host "  OK Database created: $LocalDatabase" -ForegroundColor Green
    }
    else {
        Write-Host "  OK Database exists: $LocalDatabase" -ForegroundColor Yellow
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
        $localConn = New-Object System.Data.SqlClient.SqlConnection($LocalConnectionString)
        $localConn.Open()
        
        $schemaCmd = $localConn.CreateCommand()
        $schemaCmd.CommandText = $schemaScript
        $schemaCmd.CommandTimeout = 300
        $schemaCmd.ExecuteNonQuery() | Out-Null
        
        $localConn.Close()
        Write-Host "  OK Schema created" -ForegroundColor Green
    }
    catch {
        Write-Host "  WARNING Schema creation had issues (may already exist): $_" -ForegroundColor Yellow
    }
}
else {
    Write-Host "  WARNING Schema file not found: $schemaFile" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Migrate data
Write-Host "Step 4: Migrating data..." -ForegroundColor Green

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
        $checkTableCmd = $remoteConn.CreateCommand()
        $checkTableCmd.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '$table'"
        $tableExists = $checkTableCmd.ExecuteScalar()
        
        if ($tableExists -eq 0) {
            Write-Host "  SKIP $table (not found)" -ForegroundColor DarkGray
            continue
        }
        
        $countCmd = $remoteConn.CreateCommand()
        $countCmd.CommandText = "SELECT COUNT(*) FROM [$table]"
        $rowCount = $countCmd.ExecuteScalar()
        
        if ($rowCount -eq 0) {
            Write-Host "  SKIP $table (empty)" -ForegroundColor DarkGray
            continue
        }
        
        Write-Host "  -> Migrating $table ($rowCount rows)..." -ForegroundColor Cyan
        
        $disableIdentityCmd = $localConn.CreateCommand()
        $disableIdentityCmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('$table') AND is_identity = 1) SET IDENTITY_INSERT [$table] ON"
        $disableIdentityCmd.ExecuteNonQuery() | Out-Null
        
        $selectCmd = $remoteConn.CreateCommand()
        $selectCmd.CommandText = "SELECT * FROM [$table]"
        $selectCmd.CommandTimeout = 300
        
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($selectCmd)
        $dataTable = New-Object System.Data.DataTable
        $adapter.Fill($dataTable) | Out-Null
        
        $bulkCopy = New-Object System.Data.SqlClient.SqlBulkCopy($localConn)
        $bulkCopy.DestinationTableName = $table
        $bulkCopy.BulkCopyTimeout = 300
        $bulkCopy.WriteToServer($dataTable)
        
        $enableIdentityCmd = $localConn.CreateCommand()
        $enableIdentityCmd.CommandText = "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('$table') AND is_identity = 1) SET IDENTITY_INSERT [$table] OFF"
        $enableIdentityCmd.ExecuteNonQuery() | Out-Null
        
        $totalRecords += $rowCount
        Write-Host "  OK Migrated $rowCount rows" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR Failed to migrate $table : $_" -ForegroundColor Red
    }
}

$remoteConn.Close()
$localConn.Close()

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
Write-Host "Migration Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total records migrated: $totalRecords" -ForegroundColor Green
Write-Host "Local database: $LocalDatabase" -ForegroundColor Green
Write-Host ""
