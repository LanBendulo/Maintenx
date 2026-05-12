-- ═══════════════════════════════════════════════════════════════════════════════
-- PM WORK ORDER GENERATION GOVERNANCE MIGRATION
-- Adds PreventiveScheduleId to Work_Order for PM traceability and duplicate prevention
-- Enables proper CMMS lifecycle management for preventive maintenance
-- ═══════════════════════════════════════════════════════════════════════════════

USE MaintenX;
GO

PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT 'PM GOVERNANCE MIGRATION - Adding PreventiveScheduleId to Work_Order';
PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT '';

-- ───────────────────────────────────────────────────────────────────────────────
-- STEP 1: Check if column already exists
-- ───────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' 
    AND COLUMN_NAME = 'preventive_schedule_id'
)
BEGIN
    PRINT '→ Adding preventive_schedule_id column to Work_Order table...';
    
    ALTER TABLE Work_Order
    ADD preventive_schedule_id INT NULL;
    
    PRINT '✓ Column added successfully';
    PRINT '';
END
ELSE
BEGIN
    PRINT '⚠ Column preventive_schedule_id already exists - skipping';
    PRINT '';
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- STEP 2: Add foreign key constraint to PreventiveSchedule
-- ───────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_WorkOrder_PreventiveSchedule'
)
BEGIN
    PRINT '→ Adding foreign key constraint FK_WorkOrder_PreventiveSchedule...';
    
    ALTER TABLE Work_Order
    ADD CONSTRAINT FK_WorkOrder_PreventiveSchedule
    FOREIGN KEY (preventive_schedule_id) 
    REFERENCES PreventiveSchedule(schedule_id)
    ON DELETE SET NULL;  -- If PM schedule deleted, preserve work order but clear link
    
    PRINT '✓ Foreign key constraint added successfully';
    PRINT '';
END
ELSE
BEGIN
    PRINT '⚠ Foreign key FK_WorkOrder_PreventiveSchedule already exists - skipping';
    PRINT '';
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- STEP 3: Create index for performance (PM schedule lookups)
-- ───────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_WorkOrder_PreventiveScheduleId' 
    AND object_id = OBJECT_ID('Work_Order')
)
BEGIN
    PRINT '→ Creating index IX_WorkOrder_PreventiveScheduleId for performance...';
    
    CREATE NONCLUSTERED INDEX IX_WorkOrder_PreventiveScheduleId
    ON Work_Order(preventive_schedule_id)
    INCLUDE (status, is_archived, company_id)
    WHERE preventive_schedule_id IS NOT NULL;
    
    PRINT '✓ Index created successfully';
    PRINT '';
END
ELSE
BEGIN
    PRINT '⚠ Index IX_WorkOrder_PreventiveScheduleId already exists - skipping';
    PRINT '';
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- STEP 4: Backfill existing PM work orders (best effort)
-- Link existing Preventive work orders to their PM schedules based on:
-- - Source = 'Preventive'
-- - Asset match
-- - Date proximity to LastGeneratedWorkOrderId
-- ───────────────────────────────────────────────────────────────────────────────
PRINT '→ Backfilling existing PM work orders with PreventiveScheduleId...';
PRINT '';

-- Update work orders that match PreventiveSchedule.LastGeneratedWorkOrderId
UPDATE wo
SET wo.preventive_schedule_id = ps.schedule_id
FROM Work_Order wo
INNER JOIN PreventiveSchedule ps ON ps.last_generated_work_order_id = wo.work_order_id
WHERE wo.source = 'Preventive'
  AND wo.preventive_schedule_id IS NULL
  AND wo.company_id = ps.company_id;

DECLARE @backfillCount INT = @@ROWCOUNT;
PRINT '✓ Backfilled ' + CAST(@backfillCount AS VARCHAR(10)) + ' work orders using LastGeneratedWorkOrderId';
PRINT '';

-- ───────────────────────────────────────────────────────────────────────────────
-- STEP 5: Verification
-- ───────────────────────────────────────────────────────────────────────────────
PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT 'VERIFICATION REPORT';
PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT '';

-- Count PM work orders
DECLARE @totalPMWorkOrders INT;
DECLARE @linkedPMWorkOrders INT;
DECLARE @unlinkedPMWorkOrders INT;

SELECT @totalPMWorkOrders = COUNT(*)
FROM Work_Order
WHERE source = 'Preventive';

SELECT @linkedPMWorkOrders = COUNT(*)
FROM Work_Order
WHERE source = 'Preventive' AND preventive_schedule_id IS NOT NULL;

SELECT @unlinkedPMWorkOrders = COUNT(*)
FROM Work_Order
WHERE source = 'Preventive' AND preventive_schedule_id IS NULL;

PRINT 'Total PM Work Orders: ' + CAST(@totalPMWorkOrders AS VARCHAR(10));
PRINT 'Linked to PM Schedule: ' + CAST(@linkedPMWorkOrders AS VARCHAR(10));
PRINT 'Unlinked (legacy): ' + CAST(@unlinkedPMWorkOrders AS VARCHAR(10));
PRINT '';

-- Show sample of linked work orders
IF @linkedPMWorkOrders > 0
BEGIN
    PRINT 'Sample of linked PM work orders:';
    PRINT '─────────────────────────────────────────────────────────────────────────────';
    
    SELECT TOP 5
        wo.work_order_id AS [WO ID],
        wo.preventive_schedule_id AS [PM Schedule ID],
        ps.title AS [PM Schedule],
        a.asset_name AS [Asset],
        wo.status AS [Status],
        wo.date_created AS [Created]
    FROM Work_Order wo
    INNER JOIN PreventiveSchedule ps ON ps.schedule_id = wo.preventive_schedule_id
    INNER JOIN Asset a ON a.asset_id = wo.asset_id
    WHERE wo.source = 'Preventive'
    ORDER BY wo.date_created DESC;
    
    PRINT '';
END

PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT 'PM GOVERNANCE MIGRATION COMPLETED SUCCESSFULLY';
PRINT '═══════════════════════════════════════════════════════════════════════════════';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. All new PM work orders will automatically link to their schedule';
PRINT '2. Governance rules will prevent duplicate PM work order generation';
PRINT '3. UI will show generation status and disable button when not allowed';
PRINT '4. Historical PM work orders remain functional (unlinked is OK)';
PRINT '';
GO
