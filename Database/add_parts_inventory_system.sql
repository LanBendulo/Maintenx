-- =====================================================
-- Spare Parts Inventory System Migration
-- Adds CompanyId to WorkOrderPart and creates indexes
-- =====================================================

USE [db50508];
GO

PRINT 'Starting Parts Inventory System Migration...';
GO

-- =====================================================
-- 1. Add CompanyId to WorkOrderPart (if not exists)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.WorkOrderPart') 
    AND name = 'company_id'
)
BEGIN
    PRINT 'Adding company_id to WorkOrderPart...';
    
    ALTER TABLE dbo.WorkOrderPart
    ADD company_id INT NULL;
    
    -- Populate company_id from related WorkOrder
    UPDATE wop
    SET wop.company_id = wo.company_id
    FROM dbo.WorkOrderPart wop
    INNER JOIN dbo.Work_Order wo ON wop.work_order_id = wo.work_order_id;
    
    -- Make it NOT NULL after population
    ALTER TABLE dbo.WorkOrderPart
    ALTER COLUMN company_id INT NOT NULL;
    
    PRINT '✓ company_id added to WorkOrderPart';
END
ELSE
BEGIN
    PRINT '✓ company_id already exists in WorkOrderPart';
END
GO

-- =====================================================
-- 2. Add Foreign Key: WorkOrderPart → Company
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_WorkOrderPart_Company'
)
BEGIN
    PRINT 'Adding FK_WorkOrderPart_Company...';
    
    ALTER TABLE dbo.WorkOrderPart
    ADD CONSTRAINT FK_WorkOrderPart_Company
    FOREIGN KEY (company_id) REFERENCES dbo.Company(company_id);
    
    PRINT '✓ FK_WorkOrderPart_Company created';
END
ELSE
BEGIN
    PRINT '✓ FK_WorkOrderPart_Company already exists';
END
GO

-- =====================================================
-- 3. Add Index: WorkOrderPart (CompanyId, WorkOrderId)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_WorkOrderPart_CompanyId_WorkOrderId'
)
BEGIN
    PRINT 'Creating IX_WorkOrderPart_CompanyId_WorkOrderId...';
    
    CREATE NONCLUSTERED INDEX IX_WorkOrderPart_CompanyId_WorkOrderId
    ON dbo.WorkOrderPart(company_id, work_order_id);
    
    PRINT '✓ IX_WorkOrderPart_CompanyId_WorkOrderId created';
END
ELSE
BEGIN
    PRINT '✓ IX_WorkOrderPart_CompanyId_WorkOrderId already exists';
END
GO

-- =====================================================
-- 4. Add Index: WorkOrderPart (CompanyId, PartId)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_WorkOrderPart_CompanyId_PartId'
)
BEGIN
    PRINT 'Creating IX_WorkOrderPart_CompanyId_PartId...';
    
    CREATE NONCLUSTERED INDEX IX_WorkOrderPart_CompanyId_PartId
    ON dbo.WorkOrderPart(company_id, part_id);
    
    PRINT '✓ IX_WorkOrderPart_CompanyId_PartId created';
END
ELSE
BEGIN
    PRINT '✓ IX_WorkOrderPart_CompanyId_PartId already exists';
END
GO

-- =====================================================
-- 5. Add Unique Index: Part (CompanyId, PartNumber)
-- =====================================================
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Part_CompanyId_PartNumber'
)
BEGIN
    PRINT 'Creating IX_Part_CompanyId_PartNumber...';
    
    CREATE UNIQUE NONCLUSTERED INDEX IX_Part_CompanyId_PartNumber
    ON dbo.Part(company_id, part_number)
    WHERE part_number IS NOT NULL;
    
    PRINT '✓ IX_Part_CompanyId_PartNumber created';
END
ELSE
BEGIN
    PRINT '✓ IX_Part_CompanyId_PartNumber already exists';
END
GO

-- =====================================================
-- 6. Ensure Part table has correct structure
-- =====================================================
PRINT 'Verifying Part table structure...';

-- Ensure quantity is NOT NULL with default 0
IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Part') 
    AND name = 'quantity'
    AND is_nullable = 1
)
BEGIN
    UPDATE dbo.Part SET quantity = 0 WHERE quantity IS NULL;
    
    ALTER TABLE dbo.Part
    ALTER COLUMN quantity INT NOT NULL;
    
    PRINT '✓ Part.quantity set to NOT NULL';
END

-- Ensure unit_cost has correct precision
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Part') 
    AND name = 'unit_cost'
    AND precision = 10
    AND scale = 2
)
BEGIN
    ALTER TABLE dbo.Part
    ALTER COLUMN unit_cost DECIMAL(10,2) NULL;
    
    PRINT '✓ Part.unit_cost precision updated';
END

PRINT '✓ Part table structure verified';
GO

-- =====================================================
-- 7. Seed Sample Parts (for testing)
-- =====================================================
PRINT 'Checking for sample parts...';

DECLARE @TestCompanyId INT;
SELECT TOP 1 @TestCompanyId = company_id FROM dbo.Company ORDER BY company_id;

IF @TestCompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Part WHERE company_id = @TestCompanyId)
BEGIN
    PRINT 'Seeding sample parts for testing...';
    
    INSERT INTO dbo.Part (company_id, part_name, part_number, description, quantity, unit_cost, reorder_level, is_active, created_at)
    VALUES
        (@TestCompanyId, 'Oil Filter', 'OF-001', 'Standard oil filter for equipment', 50, 12.50, 10, 1, GETDATE()),
        (@TestCompanyId, 'Air Filter', 'AF-001', 'High-efficiency air filter', 30, 18.75, 5, 1, GETDATE()),
        (@TestCompanyId, 'Hydraulic Fluid', 'HF-001', 'Premium hydraulic fluid (1L)', 100, 8.99, 20, 1, GETDATE()),
        (@TestCompanyId, 'Bearing Set', 'BR-001', 'Industrial bearing set', 15, 45.00, 5, 1, GETDATE()),
        (@TestCompanyId, 'Drive Belt', 'DB-001', 'Heavy-duty drive belt', 25, 22.50, 8, 1, GETDATE()),
        (@TestCompanyId, 'Spark Plug', 'SP-001', 'Standard spark plug', 60, 5.25, 15, 1, GETDATE()),
        (@TestCompanyId, 'Coolant', 'CL-001', 'Engine coolant (5L)', 40, 15.00, 10, 1, GETDATE()),
        (@TestCompanyId, 'Grease Cartridge', 'GC-001', 'Multi-purpose grease', 80, 6.50, 20, 1, GETDATE());
    
    PRINT '✓ Sample parts seeded';
END
ELSE
BEGIN
    PRINT '✓ Parts already exist or no test company found';
END
GO

-- =====================================================
-- VERIFICATION
-- =====================================================
PRINT '';
PRINT '=== VERIFICATION ===';

-- Check WorkOrderPart structure
SELECT 
    'WorkOrderPart Columns' AS [Check],
    COUNT(*) AS [Count]
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.WorkOrderPart');

-- Check Part structure
SELECT 
    'Part Columns' AS [Check],
    COUNT(*) AS [Count]
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.Part');

-- Check indexes
SELECT 
    'WorkOrderPart Indexes' AS [Check],
    COUNT(*) AS [Count]
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.WorkOrderPart')
AND name LIKE 'IX_%';

-- Check parts count
SELECT 
    'Total Parts' AS [Check],
    COUNT(*) AS [Count]
FROM dbo.Part;

PRINT '';
PRINT '✓✓✓ Parts Inventory System Migration Complete! ✓✓✓';
GO
