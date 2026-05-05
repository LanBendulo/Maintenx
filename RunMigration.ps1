# Simple Multi-Tenant Migration Runner
$server = "db50508.public.databaseasp.net"
$database = "db50508"
$username = "db50508"
$password = "3k+L?Gm8n9Z_"
$connectionString = "Server=$server;Database=$database;User Id=$username;Password=$password;Encrypt=True;TrustServerCertificate=True;Connection Timeout=60;"

function Execute-Sql {
    param([string]$sql)
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.CommandTimeout = 120
        $result = $command.ExecuteNonQuery()
        $connection.Close()
        return $true
    } catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "Starting Multi-Tenant Migration..." -ForegroundColor Cyan

# Step 1: Create Company table
Write-Host "[1/8] Creating Company table..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Company')
CREATE TABLE Company (
    company_id INT IDENTITY(1,1) PRIMARY KEY,
    company_name NVARCHAR(200) NOT NULL,
    subscription_plan NVARCHAR(50) NULL,
    subscription_expiry DATETIME2 NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    contact_email NVARCHAR(255) NULL,
    contact_phone NVARCHAR(50) NULL,
    address NVARCHAR(500) NULL,
    billing_email NVARCHAR(255) NULL,
    max_users INT NULL,
    max_assets INT NULL
);
"@

# Step 2: Insert default company
Write-Host "[2/8] Creating default company..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM Company WHERE company_id = 1)
BEGIN
    SET IDENTITY_INSERT Company ON;
    INSERT INTO Company (company_id, company_name, subscription_plan, is_active, created_at)
    VALUES (1, 'Default Company', 'Enterprise', 1, GETDATE());
    SET IDENTITY_INSERT Company OFF;
END
"@

# Step 3: Add columns to AspNetUsers
Write-Host "[3/8] Updating AspNetUsers..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CompanyId')
    ALTER TABLE AspNetUsers ADD CompanyId INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'FullName')
    ALTER TABLE AspNetUsers ADD FullName NVARCHAR(200) NULL;
"@

# Step 4: Add CompanyId to existing tables
Write-Host "[4/8] Adding CompanyId to existing tables..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Category') AND name = 'company_id')
    ALTER TABLE Category ADD company_id INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'company_id')
    ALTER TABLE Asset ADD company_id INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Personnel') AND name = 'company_id')
    ALTER TABLE Personnel ADD company_id INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Maintenance_Request') AND name = 'company_id')
    ALTER TABLE Maintenance_Request ADD company_id INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Work_Order') AND name = 'company_id')
    ALTER TABLE Work_Order ADD company_id INT NOT NULL DEFAULT 1;
"@

# Step 5: Create Part table
Write-Host "[5/8] Creating Part table..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Part')
CREATE TABLE Part (
    part_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NOT NULL,
    part_name NVARCHAR(200) NOT NULL,
    part_number NVARCHAR(100) NULL,
    description NVARCHAR(MAX) NULL,
    quantity INT NOT NULL DEFAULT 0,
    unit_cost DECIMAL(10,2) NULL,
    reorder_level INT NULL,
    location NVARCHAR(200) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_Part_Company FOREIGN KEY (company_id) REFERENCES Company(company_id)
);
"@

# Step 6: Create PreventiveSchedule table
Write-Host "[6/8] Creating PreventiveSchedule table..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PreventiveSchedule')
CREATE TABLE PreventiveSchedule (
    schedule_id INT IDENTITY(1,1) PRIMARY KEY,
    company_id INT NOT NULL,
    asset_id INT NOT NULL,
    schedule_name NVARCHAR(200) NULL,
    description NVARCHAR(MAX) NULL,
    frequency_days INT NOT NULL,
    next_due_date DATETIME2 NOT NULL,
    last_completed_date DATETIME2 NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_by INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_PreventiveSchedule_Company FOREIGN KEY (company_id) REFERENCES Company(company_id),
    CONSTRAINT FK_PreventiveSchedule_Asset FOREIGN KEY (asset_id) REFERENCES Asset(asset_id),
    CONSTRAINT FK_PreventiveSchedule_Personnel FOREIGN KEY (created_by) REFERENCES Personnel(personnel_id)
);
"@

# Step 7: Create WorkOrderPart table
Write-Host "[7/8] Creating WorkOrderPart table..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOrderPart')
CREATE TABLE WorkOrderPart (
    id INT IDENTITY(1,1) PRIMARY KEY,
    work_order_id INT NOT NULL,
    part_id INT NOT NULL,
    quantity_used INT NOT NULL,
    unit_cost DECIMAL(10,2) NULL,
    total_cost DECIMAL(10,2) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_WorkOrderPart_WorkOrder FOREIGN KEY (work_order_id) REFERENCES Work_Order(work_order_id),
    CONSTRAINT FK_WorkOrderPart_Part FOREIGN KEY (part_id) REFERENCES Part(part_id)
);
"@

# Step 8: Create WorkOrderCost table
Write-Host "[8/8] Creating WorkOrderCost table..." -ForegroundColor Yellow
Execute-Sql @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOrderCost')
CREATE TABLE WorkOrderCost (
    cost_id INT IDENTITY(1,1) PRIMARY KEY,
    work_order_id INT NOT NULL,
    labor_cost DECIMAL(10,2) NULL,
    parts_cost DECIMAL(10,2) NULL,
    other_cost DECIMAL(10,2) NULL,
    total_cost DECIMAL(10,2) NULL,
    notes NVARCHAR(MAX) NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_WorkOrderCost_WorkOrder FOREIGN KEY (work_order_id) REFERENCES Work_Order(work_order_id)
);
"@

Write-Host ""
Write-Host "Migration completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next: Run the application with dotnet run" -ForegroundColor Cyan
