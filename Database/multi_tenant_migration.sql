-- =============================================================
-- MULTI-TENANT SAAS MIGRATION
-- MaintenX Database - Company-Based Tenant Isolation
-- =============================================================
-- This script transforms the single-tenant system into a 
-- multi-tenant SaaS architecture with strict data isolation
-- per Company (tenant).
--
-- IMPORTANT: Run this script in a transaction and test thoroughly
-- before applying to production.
-- =============================================================

USE DB_Maintenx;
GO

BEGIN TRANSACTION;
GO

-- =============================================================
-- STEP 1: CREATE COMPANY (TENANT) TABLE
-- =============================================================
PRINT 'Creating Company table...';

IF OBJECT_ID('dbo.Company', 'U') IS NULL
CREATE TABLE dbo.Company (
    company_id              INT             NOT NULL IDENTITY(1,1),
    company_name            NVARCHAR(200)   NOT NULL,
    subscription_plan       NVARCHAR(50)    NULL DEFAULT 'Free',  -- Free, Basic, Pro, Enterprise
    subscription_expiry     DATETIME        NULL,
    is_active               BIT             NOT NULL DEFAULT 1,
    created_at              DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at              DATETIME        NULL,
    
    -- Contact information
    contact_email           NVARCHAR(255)   NULL,
    contact_phone           NVARCHAR(50)    NULL,
    address                 NVARCHAR(500)   NULL,
    
    -- Billing information
    billing_email           NVARCHAR(255)   NULL,
    max_users               INT             NULL DEFAULT 10,
    max_assets              INT             NULL DEFAULT 100,
    
    CONSTRAINT PK_Company PRIMARY KEY (company_id),
    CONSTRAINT UQ_Company_Name UNIQUE (company_name)
);
GO

-- Index for active companies
CREATE INDEX IX_Company_IsActive ON dbo.Company (is_active);
GO

PRINT 'Company table created successfully.';
GO

-- =============================================================
-- STEP 2: CREATE DEFAULT COMPANY FOR EXISTING DATA
-- =============================================================
PRINT 'Creating default company for existing data...';

IF NOT EXISTS (SELECT 1 FROM dbo.Company WHERE company_name = 'Default Company')
BEGIN
    INSERT INTO dbo.Company (
        company_name, 
        subscription_plan, 
        subscription_expiry, 
        is_active,
        max_users,
        max_assets
    )
    VALUES (
        'Default Company', 
        'Enterprise', 
        DATEADD(YEAR, 10, GETDATE()),  -- 10 years from now
        1,
        999,  -- Unlimited users
        9999  -- Unlimited assets
    );
    
    PRINT 'Default company created with ID: ' + CAST(SCOPE_IDENTITY() AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT 'Default company already exists.';
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
PRINT 'Default Company ID: ' + CAST(@DefaultCompanyId AS NVARCHAR(10));
GO

-- =============================================================
-- STEP 3: ADD COMPANY_ID TO ASPNETUSERS (IDENTITY)
-- =============================================================
PRINT 'Extending AspNetUsers with CompanyId and FullName...';

-- Add CompanyId column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD CompanyId INT NULL;
    
    PRINT 'CompanyId column added to AspNetUsers.';
END
ELSE
BEGIN
    PRINT 'CompanyId column already exists in AspNetUsers.';
END
GO

-- Add FullName column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'FullName')
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD FullName NVARCHAR(200) NULL;
    
    PRINT 'FullName column added to AspNetUsers.';
END
ELSE
BEGIN
    PRINT 'FullName column already exists in AspNetUsers.';
END
GO

-- Assign all existing users to default company
DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');

UPDATE dbo.AspNetUsers
SET CompanyId = @DefaultCompanyId
WHERE CompanyId IS NULL;

PRINT 'All existing users assigned to Default Company.';
GO

-- Make CompanyId required (non-nullable)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'CompanyId' AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ALTER COLUMN CompanyId INT NOT NULL;
    
    PRINT 'CompanyId made required in AspNetUsers.';
END
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AspNetUsers_Company')
BEGIN
    ALTER TABLE dbo.AspNetUsers
    ADD CONSTRAINT FK_AspNetUsers_Company
        FOREIGN KEY (CompanyId)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
    
    PRINT 'Foreign key constraint added to AspNetUsers.';
END
GO

-- Add index on CompanyId
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_AspNetUsers_CompanyId' AND object_id = OBJECT_ID('dbo.AspNetUsers'))
BEGIN
    CREATE INDEX IX_AspNetUsers_CompanyId ON dbo.AspNetUsers (CompanyId);
    PRINT 'Index created on AspNetUsers.CompanyId.';
END
GO

-- =============================================================
-- STEP 4: ADD COMPANY_ID TO PERSONNEL
-- =============================================================
PRINT 'Adding CompanyId to Personnel table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Personnel') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Personnel
    ADD company_id INT NULL;
    
    PRINT 'company_id column added to Personnel.';
END
GO

-- Assign existing personnel to default company
DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');

UPDATE dbo.Personnel
SET company_id = @DefaultCompanyId
WHERE company_id IS NULL;

PRINT 'All existing personnel assigned to Default Company.';
GO

-- Make company_id required
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Personnel') AND name = 'company_id' AND is_nullable = 1)
BEGIN
    ALTER TABLE dbo.Personnel
    ALTER COLUMN company_id INT NOT NULL;
    
    PRINT 'company_id made required in Personnel.';
END
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Personnel_Company')
BEGIN
    ALTER TABLE dbo.Personnel
    ADD CONSTRAINT FK_Personnel_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
    
    PRINT 'Foreign key constraint added to Personnel.';
END
GO

-- Add index
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Personnel_company_id' AND object_id = OBJECT_ID('dbo.Personnel'))
BEGIN
    CREATE INDEX IX_Personnel_company_id ON dbo.Personnel (company_id);
    PRINT 'Index created on Personnel.company_id.';
END
GO

-- =============================================================
-- STEP 5: ADD COMPANY_ID TO CATEGORY
-- =============================================================
PRINT 'Adding CompanyId to Category table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Category
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Category SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Category ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Category_Company')
BEGIN
    ALTER TABLE dbo.Category
    ADD CONSTRAINT FK_Category_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_Category_company_id ON dbo.Category (company_id);
GO

PRINT 'CompanyId added to Category.';
GO

-- =============================================================
-- STEP 6: ADD COMPANY_ID TO ASSET
-- =============================================================
PRINT 'Adding CompanyId to Asset table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Asset') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Asset
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Asset SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Asset ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Asset_Company')
BEGIN
    ALTER TABLE dbo.Asset
    ADD CONSTRAINT FK_Asset_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_Asset_company_id ON dbo.Asset (company_id);
GO

PRINT 'CompanyId added to Asset.';
GO

-- =============================================================
-- STEP 7: ADD COMPANY_ID TO MAINTENANCE_REQUEST
-- =============================================================
PRINT 'Adding CompanyId to Maintenance_Request table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Maintenance_Request') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Maintenance_Request
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Maintenance_Request SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Maintenance_Request ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaintenanceRequest_Company')
BEGIN
    ALTER TABLE dbo.Maintenance_Request
    ADD CONSTRAINT FK_MaintenanceRequest_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_MaintenanceRequest_company_id ON dbo.Maintenance_Request (company_id);
GO

PRINT 'CompanyId added to Maintenance_Request.';
GO

-- =============================================================
-- STEP 8: ADD COMPANY_ID AND SOURCE TO WORK_ORDER
-- =============================================================
PRINT 'Adding CompanyId and Source to Work_Order table...';

-- Add company_id
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Work_Order') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD company_id INT NULL;
END
GO

-- Add source field
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Work_Order') AND name = 'source')
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD source NVARCHAR(50) NULL DEFAULT 'Manual';  -- Request, Preventive, Manual
    
    PRINT 'source column added to Work_Order.';
END
GO

-- Update source based on maintenance_request_id
UPDATE dbo.Work_Order
SET source = CASE 
    WHEN maintenance_request_id IS NOT NULL THEN 'Request'
    ELSE 'Manual'
END
WHERE source IS NULL;
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Work_Order SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Work_Order ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkOrder_Company')
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD CONSTRAINT FK_WorkOrder_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_WorkOrder_company_id ON dbo.Work_Order (company_id);
CREATE INDEX IX_WorkOrder_source ON dbo.Work_Order (source);
GO

PRINT 'CompanyId and Source added to Work_Order.';
GO

-- =============================================================
-- STEP 9: ADD COMPANY_ID TO MAINTENANCE_SCHEDULE
-- =============================================================
PRINT 'Adding CompanyId to Maintenance_Schedule table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Maintenance_Schedule') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Maintenance_Schedule
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Maintenance_Schedule SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Maintenance_Schedule ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaintenanceSchedule_Company')
BEGIN
    ALTER TABLE dbo.Maintenance_Schedule
    ADD CONSTRAINT FK_MaintenanceSchedule_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_MaintenanceSchedule_company_id ON dbo.Maintenance_Schedule (company_id);
GO

PRINT 'CompanyId added to Maintenance_Schedule.';
GO

-- =============================================================
-- STEP 10: ADD COMPANY_ID TO SPARE_PART
-- =============================================================
PRINT 'Adding CompanyId to Spare_Part table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Spare_Part') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Spare_Part
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Spare_Part SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Spare_Part ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SparePart_Company')
BEGIN
    ALTER TABLE dbo.Spare_Part
    ADD CONSTRAINT FK_SparePart_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_SparePart_company_id ON dbo.Spare_Part (company_id);
GO

PRINT 'CompanyId added to Spare_Part.';
GO

-- =============================================================
-- STEP 11: ADD COMPANY_ID TO MAINTENANCE_LOG
-- =============================================================
PRINT 'Adding CompanyId to Maintenance_Log table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Maintenance_Log') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Maintenance_Log
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Maintenance_Log SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Maintenance_Log ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaintenanceLog_Company')
BEGIN
    ALTER TABLE dbo.Maintenance_Log
    ADD CONSTRAINT FK_MaintenanceLog_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_MaintenanceLog_company_id ON dbo.Maintenance_Log (company_id);
GO

PRINT 'CompanyId added to Maintenance_Log.';
GO

-- =============================================================
-- STEP 12: ADD COMPANY_ID TO MAINTENANCE_COST
-- =============================================================
PRINT 'Adding CompanyId to Maintenance_Cost table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Maintenance_Cost') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Maintenance_Cost
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Maintenance_Cost SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Maintenance_Cost ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MaintenanceCost_Company')
BEGIN
    ALTER TABLE dbo.Maintenance_Cost
    ADD CONSTRAINT FK_MaintenanceCost_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_MaintenanceCost_company_id ON dbo.Maintenance_Cost (company_id);
GO

PRINT 'CompanyId added to Maintenance_Cost.';
GO

-- =============================================================
-- STEP 13: ADD COMPANY_ID TO INVENTORY_TRANSACTION
-- =============================================================
PRINT 'Adding CompanyId to Inventory_Transaction table...';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Inventory_Transaction') AND name = 'company_id')
BEGIN
    ALTER TABLE dbo.Inventory_Transaction
    ADD company_id INT NULL;
END
GO

DECLARE @DefaultCompanyId INT = (SELECT company_id FROM dbo.Company WHERE company_name = 'Default Company');
UPDATE dbo.Inventory_Transaction SET company_id = @DefaultCompanyId WHERE company_id IS NULL;
GO

ALTER TABLE dbo.Inventory_Transaction ALTER COLUMN company_id INT NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_InventoryTransaction_Company')
BEGIN
    ALTER TABLE dbo.Inventory_Transaction
    ADD CONSTRAINT FK_InventoryTransaction_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE;
END
GO

CREATE INDEX IX_InventoryTransaction_company_id ON dbo.Inventory_Transaction (company_id);
GO

PRINT 'CompanyId added to Inventory_Transaction.';
GO

-- =============================================================
-- STEP 14: CREATE PREVENTIVE MAINTENANCE SCHEDULE TABLE
-- =============================================================
PRINT 'Creating PreventiveSchedule table...';

IF OBJECT_ID('dbo.PreventiveSchedule', 'U') IS NULL
CREATE TABLE dbo.PreventiveSchedule (
    schedule_id             INT             NOT NULL IDENTITY(1,1),
    company_id              INT             NOT NULL,
    asset_id                INT             NOT NULL,
    schedule_name           NVARCHAR(200)   NULL,
    description             NVARCHAR(MAX)   NULL,
    frequency_days          INT             NOT NULL,  -- Frequency in days
    next_due_date           DATE            NOT NULL,
    last_completed_date     DATE            NULL,
    is_active               BIT             NOT NULL DEFAULT 1,
    created_by              INT             NULL,  -- FK to Personnel
    created_at              DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at              DATETIME        NULL,
    
    CONSTRAINT PK_PreventiveSchedule PRIMARY KEY (schedule_id),
    
    CONSTRAINT FK_PreventiveSchedule_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    
    CONSTRAINT FK_PreventiveSchedule_Asset
        FOREIGN KEY (asset_id)
        REFERENCES dbo.Asset (asset_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,
    
    CONSTRAINT FK_PreventiveSchedule_CreatedBy
        FOREIGN KEY (created_by)
        REFERENCES dbo.Personnel (personnel_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

CREATE INDEX IX_PreventiveSchedule_company_id ON dbo.PreventiveSchedule (company_id);
CREATE INDEX IX_PreventiveSchedule_asset_id ON dbo.PreventiveSchedule (asset_id);
CREATE INDEX IX_PreventiveSchedule_next_due_date ON dbo.PreventiveSchedule (next_due_date);
CREATE INDEX IX_PreventiveSchedule_is_active ON dbo.PreventiveSchedule (is_active);
GO

PRINT 'PreventiveSchedule table created successfully.';
GO

-- =============================================================
-- STEP 15: CREATE PART TABLE (RENAMED FROM SPARE_PART)
-- =============================================================
PRINT 'Creating Part table...';

IF OBJECT_ID('dbo.Part', 'U') IS NULL
CREATE TABLE dbo.Part (
    part_id         INT             NOT NULL IDENTITY(1,1),
    company_id      INT             NOT NULL,
    part_name       NVARCHAR(200)   NOT NULL,
    part_number     NVARCHAR(100)   NULL,
    description     NVARCHAR(MAX)   NULL,
    quantity        INT             NOT NULL DEFAULT 0,
    unit_cost       DECIMAL(10,2)   NULL,
    reorder_level   INT             NULL,
    location        NVARCHAR(200)   NULL,
    is_active       BIT             NOT NULL DEFAULT 1,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME        NULL,
    
    CONSTRAINT PK_Part PRIMARY KEY (part_id),
    
    CONSTRAINT FK_Part_Company
        FOREIGN KEY (company_id)
        REFERENCES dbo.Company (company_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

CREATE INDEX IX_Part_company_id ON dbo.Part (company_id);
CREATE INDEX IX_Part_part_number ON dbo.Part (part_number);
GO

PRINT 'Part table created successfully.';
GO

-- =============================================================
-- STEP 16: CREATE WORKORDER_PART TABLE
-- =============================================================
PRINT 'Creating WorkOrderPart table...';

IF OBJECT_ID('dbo.WorkOrderPart', 'U') IS NULL
CREATE TABLE dbo.WorkOrderPart (
    id              INT             NOT NULL IDENTITY(1,1),
    work_order_id   INT             NOT NULL,
    part_id         INT             NOT NULL,
    quantity_used   INT             NOT NULL,
    unit_cost       DECIMAL(10,2)   NULL,
    total_cost      DECIMAL(10,2)   NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT PK_WorkOrderPart PRIMARY KEY (id),
    
    CONSTRAINT FK_WorkOrderPart_WorkOrder
        FOREIGN KEY (work_order_id)
        REFERENCES dbo.Work_Order (work_order_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    
    CONSTRAINT FK_WorkOrderPart_Part
        FOREIGN KEY (part_id)
        REFERENCES dbo.Part (part_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

CREATE INDEX IX_WorkOrderPart_work_order_id ON dbo.WorkOrderPart (work_order_id);
CREATE INDEX IX_WorkOrderPart_part_id ON dbo.WorkOrderPart (part_id);
GO

PRINT 'WorkOrderPart table created successfully.';
GO

-- =============================================================
-- STEP 17: CREATE WORKORDER_COST TABLE
-- =============================================================
PRINT 'Creating WorkOrderCost table...';

IF OBJECT_ID('dbo.WorkOrderCost', 'U') IS NULL
CREATE TABLE dbo.WorkOrderCost (
    cost_id         INT             NOT NULL IDENTITY(1,1),
    work_order_id   INT             NOT NULL,
    labor_cost      DECIMAL(10,2)   NULL DEFAULT 0,
    parts_cost      DECIMAL(10,2)   NULL DEFAULT 0,
    other_cost      DECIMAL(10,2)   NULL DEFAULT 0,
    total_cost      DECIMAL(10,2)   NULL DEFAULT 0,
    notes           NVARCHAR(MAX)   NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME        NULL,
    
    CONSTRAINT PK_WorkOrderCost PRIMARY KEY (cost_id),
    
    CONSTRAINT FK_WorkOrderCost_WorkOrder
        FOREIGN KEY (work_order_id)
        REFERENCES dbo.Work_Order (work_order_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

CREATE INDEX IX_WorkOrderCost_work_order_id ON dbo.WorkOrderCost (work_order_id);
GO

PRINT 'WorkOrderCost table created successfully.';
GO

-- =============================================================
-- MIGRATION COMPLETE
-- =============================================================

PRINT '';
PRINT '=============================================================';
PRINT 'MULTI-TENANT MIGRATION COMPLETED SUCCESSFULLY';
PRINT '=============================================================';
PRINT '';
PRINT 'Summary:';
PRINT '- Company table created';
PRINT '- Default Company created and assigned to all existing data';
PRINT '- CompanyId added to all business tables';
PRINT '- AspNetUsers extended with CompanyId and FullName';
PRINT '- PreventiveSchedule table created';
PRINT '- Part table created';
PRINT '- WorkOrderPart table created';
PRINT '- WorkOrderCost table created';
PRINT '- All foreign key constraints and indexes created';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Review the changes and test thoroughly';
PRINT '2. Update application code to filter by CompanyId';
PRINT '3. Update controllers to enforce tenant isolation';
PRINT '4. Create company registration and management UI';
PRINT '5. If everything looks good, COMMIT the transaction';
PRINT '6. If there are issues, ROLLBACK the transaction';
PRINT '';
PRINT 'To commit: COMMIT TRANSACTION;';
PRINT 'To rollback: ROLLBACK TRANSACTION;';
PRINT '=============================================================';

-- COMMIT TRANSACTION;  -- Uncomment after testing
-- ROLLBACK TRANSACTION;  -- Use this if you need to undo changes
