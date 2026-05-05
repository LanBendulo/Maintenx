-- =====================================================
-- Maintenance Cost Tracking System Migration
-- Adds CompanyId to WorkOrderCost and cost fields to MaintenanceLog
-- =====================================================

USE [db50508];
GO

PRINT 'Starting Cost Tracking System Migration...';
GO

-- =====================================================
-- 1. Add CompanyId to WorkOrderCost (if not exists)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.WorkOrderCost') 
    AND name = 'company_id'
)
BEGIN
    PRINT 'Adding company_id to WorkOrderCost...';
    
    ALTER TABLE dbo.WorkOrderCost
    ADD company_id INT NULL;
    
    -- Populate company_id from related WorkOrder
    UPDATE woc
    SET woc.company_id = wo.company_id
    FROM dbo.WorkOrderCost woc
    INNER JOIN dbo.Work_Order wo ON woc.work_order_id = wo.work_order_id;
    
    -- Make it NOT NULL after population
    ALTER TABLE dbo.WorkOrderCost
    ALTER COLUMN company_id INT NOT NULL;
    
    PRINT '✓ company_id added to WorkOrderCost';
END
ELSE
BEGIN
    PRINT '✓ company_id already exists in WorkOrderCost';
END
GO

-- =====================================================
-- 2. Add Foreign Key: WorkOrderCost → Company
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_WorkOrderCost_Company'
)
BEGIN
    PRINT 'Adding FK_WorkOrderCost_Company...';
    
    ALTER TABLE dbo.WorkOrderCost
    ADD CONSTRAINT FK_WorkOrderCost_Company
    FOREIGN KEY (company_id) REFERENCES dbo.Company(company_id);
    
    PRINT '✓ FK_WorkOrderCost_Company created';
END
ELSE
BEGIN
    PRINT '✓ FK_WorkOrderCost_Company already exists';
END
GO

-- =====================================================
-- 3. Add Index: WorkOrderCost (CompanyId, WorkOrderId)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_WorkOrderCost_CompanyId_WorkOrderId'
)
BEGIN
    PRINT 'Creating IX_WorkOrderCost_CompanyId_WorkOrderId...';
    
    CREATE NONCLUSTERED INDEX IX_WorkOrderCost_CompanyId_WorkOrderId
    ON dbo.WorkOrderCost(company_id, work_order_id);
    
    PRINT '✓ IX_WorkOrderCost_CompanyId_WorkOrderId created';
END
ELSE
BEGIN
    PRINT '✓ IX_WorkOrderCost_CompanyId_WorkOrderId already exists';
END
GO

-- =====================================================
-- 4. Add Cost Fields to MaintenanceLog
-- =====================================================
PRINT 'Adding cost fields to MaintenanceLog...';

-- Labor Cost
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.MaintenanceLog') 
    AND name = 'labor_cost'
)
BEGIN
    ALTER TABLE dbo.MaintenanceLog
    ADD labor_cost DECIMAL(10,2) NULL;
    
    PRINT '✓ labor_cost added to MaintenanceLog';
END
ELSE
BEGIN
    PRINT '✓ labor_cost already exists in MaintenanceLog';
END
GO

-- Parts Cost
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.MaintenanceLog') 
    AND name = 'parts_cost'
)
BEGIN
    ALTER TABLE dbo.MaintenanceLog
    ADD parts_cost DECIMAL(10,2) NULL;
    
    PRINT '✓ parts_cost added to MaintenanceLog';
END
ELSE
BEGIN
    PRINT '✓ parts_cost already exists in MaintenanceLog';
END
GO

-- Other Cost
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.MaintenanceLog') 
    AND name = 'other_cost'
)
BEGIN
    ALTER TABLE dbo.MaintenanceLog
    ADD other_cost DECIMAL(10,2) NULL;
    
    PRINT '✓ other_cost added to MaintenanceLog';
END
ELSE
BEGIN
    PRINT '✓ other_cost already exists in MaintenanceLog';
END
GO

-- Total Cost
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.MaintenanceLog') 
    AND name = 'total_cost'
)
BEGIN
    ALTER TABLE dbo.MaintenanceLog
    ADD total_cost DECIMAL(10,2) NULL;
    
    PRINT '✓ total_cost added to MaintenanceLog';
END
ELSE
BEGIN
    PRINT '✓ total_cost already exists in MaintenanceLog';
END
GO

-- =====================================================
-- 5. Create WorkOrderCost for existing Work Orders
-- =====================================================
PRINT 'Creating WorkOrderCost records for existing Work Orders...';

INSERT INTO dbo.WorkOrderCost (company_id, work_order_id, labor_cost, parts_cost, other_cost, total_cost, created_at)
SELECT 
    wo.company_id,
    wo.work_order_id,
    0.00 AS labor_cost,
    ISNULL((
        SELECT SUM(wop.quantity_used * ISNULL(wop.unit_cost, 0))
        FROM dbo.WorkOrderPart wop
        WHERE wop.work_order_id = wo.work_order_id
    ), 0.00) AS parts_cost,
    0.00 AS other_cost,
    ISNULL((
        SELECT SUM(wop.quantity_used * ISNULL(wop.unit_cost, 0))
        FROM dbo.WorkOrderPart wop
        WHERE wop.work_order_id = wo.work_order_id
    ), 0.00) AS total_cost,
    GETDATE() AS created_at
FROM dbo.Work_Order wo
WHERE NOT EXISTS (
    SELECT 1 
    FROM dbo.WorkOrderCost woc 
    WHERE woc.work_order_id = wo.work_order_id
);

PRINT '✓ WorkOrderCost records created for existing Work Orders';
GO

-- =====================================================
-- 6. Update existing MaintenanceLog with cost data
-- =====================================================
PRINT 'Updating existing MaintenanceLog records with cost data...';

UPDATE ml
SET 
    ml.labor_cost = ISNULL(woc.labor_cost, 0),
    ml.parts_cost = ISNULL(woc.parts_cost, 0),
    ml.other_cost = ISNULL(woc.other_cost, 0),
    ml.total_cost = ISNULL(woc.total_cost, 0)
FROM dbo.MaintenanceLog ml
INNER JOIN dbo.WorkOrderCost woc ON ml.work_order_id = woc.work_order_id
WHERE ml.labor_cost IS NULL;

PRINT '✓ MaintenanceLog records updated with cost data';
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT '';
PRINT '=== VERIFICATION ===';

-- Check WorkOrderCost structure
SELECT 
    'WorkOrderCost Columns' AS [Check],
    COUNT(*) AS [Count]
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.WorkOrderCost');

-- Check MaintenanceLog cost columns
SELECT 
    'MaintenanceLog Cost Columns' AS [Check],
    COUNT(*) AS [Count]
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.MaintenanceLog')
AND name IN ('labor_cost', 'parts_cost', 'other_cost', 'total_cost');

-- Check WorkOrderCost records
SELECT 
    'WorkOrderCost Records' AS [Check],
    COUNT(*) AS [Count]
FROM dbo.WorkOrderCost;

-- Check indexes
SELECT 
    'WorkOrderCost Indexes' AS [Check],
    COUNT(*) AS [Count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.WorkOrderCost')
AND name LIKE 'IX_%';

PRINT '';
PRINT '✓✓✓ Cost Tracking System Migration Complete! ✓✓✓';
GO
