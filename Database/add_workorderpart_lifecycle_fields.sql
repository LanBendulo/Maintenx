-- ============================================================
-- ADD WORKORDERPART LIFECYCLE FIELDS MIGRATION
-- ============================================================
-- Purpose: Add lifecycle governance fields to WorkOrderPart table
-- Implements staged parts usage workflow with consumption tracking
-- 
-- Architecture:
--   - Separates parts staging from inventory consumption
--   - Tracks who added parts and who approved consumption
--   - Records consumption timestamp for audit trail
--   - Supports lifecycle statuses: Pending, Approved, Consumed, Rejected
--
-- Migration Date: 2026-05-12
-- ============================================================

USE [db50508_maintenx];
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrderPart]') AND name = 'usage_status')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD [usage_status] VARCHAR(50) NOT NULL DEFAULT 'Pending';
    
    PRINT '✓ Added usage_status column';
END
ELSE
BEGIN
    PRINT '⊘ usage_status column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrderPart]') AND name = 'added_by_personnel_id')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD [added_by_personnel_id] INT NULL;
    
    PRINT '✓ Added added_by_personnel_id column';
END
ELSE
BEGIN
    PRINT '⊘ added_by_personnel_id column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrderPart]') AND name = 'approved_by_user_id')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD [approved_by_user_id] NVARCHAR(450) NULL;
    
    PRINT '✓ Added approved_by_user_id column';
END
ELSE
BEGIN
    PRINT '⊘ approved_by_user_id column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrderPart]') AND name = 'consumed_at')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD [consumed_at] DATETIME2 NULL;
    
    PRINT '✓ Added consumed_at column';
END
ELSE
BEGIN
    PRINT '⊘ consumed_at column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[WorkOrderPart]') AND name = 'updated_at')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD [updated_at] DATETIME2 NULL;
    
    PRINT '✓ Added updated_at column';
END
ELSE
BEGIN
    PRINT '⊘ updated_at column already exists';
END
GO

-- Add foreign key constraint for added_by_personnel_id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkOrderPart_Personnel_AddedBy')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD CONSTRAINT [FK_WorkOrderPart_Personnel_AddedBy]
    FOREIGN KEY ([added_by_personnel_id])
    REFERENCES [dbo].[Personnel]([personnel_id])
    ON DELETE NO ACTION;
    
    PRINT '✓ Added FK_WorkOrderPart_Personnel_AddedBy foreign key';
END
ELSE
BEGIN
    PRINT '⊘ FK_WorkOrderPart_Personnel_AddedBy foreign key already exists';
END
GO

-- Add foreign key constraint for approved_by_user_id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_WorkOrderPart_AspNetUsers_ApprovedBy')
BEGIN
    ALTER TABLE [dbo].[WorkOrderPart]
    ADD CONSTRAINT [FK_WorkOrderPart_AspNetUsers_ApprovedBy]
    FOREIGN KEY ([approved_by_user_id])
    REFERENCES [dbo].[AspNetUsers]([Id])
    ON DELETE NO ACTION;
    
    PRINT '✓ Added FK_WorkOrderPart_AspNetUsers_ApprovedBy foreign key';
END
ELSE
BEGIN
    PRINT '⊘ FK_WorkOrderPart_AspNetUsers_ApprovedBy foreign key already exists';
END
GO

-- Add index for usage_status queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOrderPart_UsageStatus')
BEGIN
    CREATE INDEX [IX_WorkOrderPart_UsageStatus]
    ON [dbo].[WorkOrderPart]([usage_status])
    INCLUDE ([work_order_id], [part_id], [quantity_used]);
    
    PRINT '✓ Added IX_WorkOrderPart_UsageStatus index';
END
ELSE
BEGIN
    PRINT '⊘ IX_WorkOrderPart_UsageStatus index already exists';
END
GO

-- Add index for added_by_personnel_id queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkOrderPart_AddedByPersonnel')
BEGIN
    CREATE INDEX [IX_WorkOrderPart_AddedByPersonnel]
    ON [dbo].[WorkOrderPart]([added_by_personnel_id])
    INCLUDE ([work_order_id], [usage_status]);
    
    PRINT '✓ Added IX_WorkOrderPart_AddedByPersonnel index';
END
ELSE
BEGIN
    PRINT '⊘ IX_WorkOrderPart_AddedByPersonnel index already exists';
END
GO

-- Migrate existing WorkOrderPart records to 'Consumed' status
-- (Assumes existing parts were already consumed)
UPDATE [dbo].[WorkOrderPart]
SET [usage_status] = 'Consumed',
    [consumed_at] = [created_at],
    [updated_at] = GETUTCDATE()
WHERE [usage_status] = 'Pending'
  AND [created_at] < DATEADD(DAY, -1, GETUTCDATE()); -- Only migrate old records

PRINT '✓ Migrated existing WorkOrderPart records to Consumed status';
GO

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

-- Verify columns exist
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WorkOrderPart'
  AND COLUMN_NAME IN ('usage_status', 'added_by_personnel_id', 'approved_by_user_id', 'consumed_at', 'updated_at')
ORDER BY ORDINAL_POSITION;

-- Verify foreign keys
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc 
    ON fk.object_id = fkc.constraint_object_id
WHERE fk.name IN ('FK_WorkOrderPart_Personnel_AddedBy', 'FK_WorkOrderPart_AspNetUsers_ApprovedBy');

-- Verify indexes
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
    ic.is_included_column AS IsIncluded
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic 
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('WorkOrderPart')
  AND i.name IN ('IX_WorkOrderPart_UsageStatus', 'IX_WorkOrderPart_AddedByPersonnel')
ORDER BY i.name, ic.key_ordinal;

-- Show sample data
SELECT TOP 5
    id,
    work_order_id,
    part_id,
    quantity_used,
    usage_status,
    added_by_personnel_id,
    consumed_at,
    created_at
FROM [dbo].[WorkOrderPart]
ORDER BY created_at DESC;

PRINT '';
PRINT '============================================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY';
PRINT '============================================================';
PRINT 'WorkOrderPart table now supports staged parts workflow';
PRINT 'Lifecycle statuses: Pending, Approved, Consumed, Rejected';
PRINT '============================================================';
GO
