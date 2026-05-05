-- ============================================================
-- MULTI-TENANT MIGRATION FOR EXISTING DATABASE
-- This script adds multi-tenant support to an existing MaintenX database
-- ============================================================

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'MULTI-TENANT MIGRATION STARTED';
PRINT '========================================';

-- ============================================================
-- STEP 1: Create Company Table (Tenant)
-- ============================================================
PRINT '';
PRINT '[1/8] Creating Company table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Company')
BEGIN
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
    PRINT '  ✓ Company table created';
END
ELSE
BEGIN
    PRINT '  ⚠ Company table already exists, skipping';
END

-- ============================================================
-- STEP 2: Insert Default Company
-- ============================================================
PRINT '';
PRINT '[2/8] Creating default company...';

IF NOT EXISTS (SELECT * FROM Company WHERE company_id = 1)
BEGIN
    SET IDENTITY_INSERT Company ON;
    INSERT INTO Company (company_id, company_name, subscription_plan, is_active, created_at)
    VALUES (1, 'Default Company', 'Enterprise', 1, GETDATE());
    SET IDENTITY_INSERT Company OFF;
    PRINT '  ✓ Default company created (ID: 1)';
END
ELSE
BEGIN
    PRINT '  ⚠ Default company already exists';
END

-- ============================================================
-- STEP 3: Add CompanyId to AspNetUsers
-- ============================================================
PRINT '';
PRINT '[3/8] Adding CompanyId and FullName to AspNetUsers...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE AspNetUsers ADD CompanyId INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to AspNetUsers';
END
ELSE
BEGIN
    PRINT '  ⚠ CompanyId already exists in AspNetUsers';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'FullName')
BEGIN
    ALTER TABLE AspNetUsers ADD FullName NVARCHAR(200) NULL;
    PRINT '  ✓ FullName added to AspNetUsers';
END
ELSE
BEGIN
    PRINT '  ⚠ FullName already exists in AspNetUsers';
END

-- ============================================================
-- STEP 4: Add CompanyId to Existing Tables
-- ============================================================
PRINT '';
PRINT '[4/8] Adding CompanyId to existing tables...';

-- Category
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Category') AND name = 'company_id')
BEGIN
    ALTER TABLE Category ADD company_id INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to Category';
END

-- Asset
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'company_id')
BEGIN
    ALTER TABLE Asset ADD company_id INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to Asset';
END

-- Personnel
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Personnel') AND name = 'company_id')
BEGIN
    ALTER TABLE Personnel ADD company_id INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to Personnel';
END

-- Maintenance_Request
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Maintenance_Request') AND name = 'company_id')
BEGIN
    ALTER TABLE Maintenance_Request ADD company_id INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to Maintenance_Request';
END

-- Work_Order
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Work_Order') AND name = 'company_id')
BEGIN
    ALTER TABLE Work_Order ADD company_id INT NOT NULL DEFAULT 1;
    PRINT '  ✓ CompanyId added to Work_Order';
END

-- ============================================================
-- STEP 5: Create Part Table
-- ============================================================
PRINT '';
PRINT '[5/8] Creating Part table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Part')
BEGIN
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
    PRINT '  ✓ Part table created';
END
ELSE
BEGIN
    PRINT '  ⚠ Part table already exists';
END

-- ============================================================
-- STEP 6: Create PreventiveSchedule Table
-- ============================================================
PRINT '';
PRINT '[6/8] Creating PreventiveSchedule table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PreventiveSchedule')
BEGIN
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
    PRINT '  ✓ PreventiveSchedule table created';
END
ELSE
BEGIN
    PRINT '  ⚠ PreventiveSchedule table already exists';
END

-- ============================================================
-- STEP 7: Create WorkOrderPart Table
-- ============================================================
PRINT '';
PRINT '[7/8] Creating WorkOrderPart table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOrderPart')
BEGIN
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
    PRINT '  ✓ WorkOrderPart table created';
END
ELSE
BEGIN
    PRINT '  ⚠ WorkOrderPart table already exists';
END

-- ============================================================
-- STEP 8: Create WorkOrderCost Table
-- ============================================================
PRINT '';
PRINT '[8/8] Creating WorkOrderCost table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WorkOrderCost')
BEGIN
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
    PRINT '  ✓ WorkOrderCost table created';
END
ELSE
BEGIN
    PRINT '  ⚠ WorkOrderCost table already exists';
END

-- ============================================================
-- VERIFICATION
-- ============================================================
PRINT '';
PRINT '========================================';
PRINT 'VERIFICATION';
PRINT '========================================';

DECLARE @CompanyCount INT;
DECLARE @UserCount INT;
DECLARE @CategoryCount INT;
DECLARE @AssetCount INT;
DECLARE @PersonnelCount INT;
DECLARE @RequestCount INT;
DECLARE @WorkOrderCount INT;

SELECT @CompanyCount = COUNT(*) FROM Company;
SELECT @UserCount = COUNT(*) FROM AspNetUsers WHERE CompanyId = 1;
SELECT @CategoryCount = COUNT(*) FROM Category WHERE company_id = 1;
SELECT @AssetCount = COUNT(*) FROM Asset WHERE company_id = 1;
SELECT @PersonnelCount = COUNT(*) FROM Personnel WHERE company_id = 1;
SELECT @RequestCount = COUNT(*) FROM Maintenance_Request WHERE company_id = 1;
SELECT @WorkOrderCount = COUNT(*) FROM Work_Order WHERE company_id = 1;

PRINT '';
PRINT 'Data assigned to Default Company (ID: 1):';
PRINT '  Companies: ' + CAST(@CompanyCount AS NVARCHAR(10));
PRINT '  Users: ' + CAST(@UserCount AS NVARCHAR(10));
PRINT '  Categories: ' + CAST(@CategoryCount AS NVARCHAR(10));
PRINT '  Assets: ' + CAST(@AssetCount AS NVARCHAR(10));
PRINT '  Personnel: ' + CAST(@PersonnelCount AS NVARCHAR(10));
PRINT '  Maintenance Requests: ' + CAST(@RequestCount AS NVARCHAR(10));
PRINT '  Work Orders: ' + CAST(@WorkOrderCount AS NVARCHAR(10));

COMMIT TRANSACTION;

PRINT '';
PRINT '========================================';
PRINT '✓ MULTI-TENANT MIGRATION COMPLETED!';
PRINT '========================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Run the application: dotnet run';
PRINT '  2. Login and verify all features work';
PRINT '  3. All existing data is assigned to CompanyId = 1';
PRINT '  4. TenantService is hardcoded to return CompanyId = 1';
PRINT '  5. System will work exactly as before';
PRINT '';
