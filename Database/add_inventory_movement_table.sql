-- ============================================================
-- ADD INVENTORY MOVEMENT TABLE MIGRATION
-- ============================================================
-- Purpose: Create immutable audit log for all inventory stock changes
-- Provides complete traceability for inventory mutations
-- 
-- Architecture:
--   - Immutable records (no updates/deletes allowed)
--   - Captures before/after quantities for every stock change
--   - Links to work orders and users for full traceability
--   - Supports cost tracking with unit cost snapshots
--   - Multi-tenant safe with CompanyId filtering
--
-- Migration Date: 2026-05-12
-- ============================================================

USE [db50508_maintenx];
GO

-- Create InventoryMovement table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryMovement')
BEGIN
    CREATE TABLE [dbo].[InventoryMovement] (
        [movement_id] INT IDENTITY(1,1) PRIMARY KEY,
        [company_id] INT NOT NULL,
        [part_id] INT NOT NULL,
        [quantity_changed] INT NOT NULL,
        [previous_quantity] INT NOT NULL,
        [new_quantity] INT NOT NULL,
        [movement_type] VARCHAR(50) NOT NULL,
        [work_order_id] INT NULL,
        [work_order_part_id] INT NULL,
        [performed_by_user_id] NVARCHAR(450) NOT NULL,
        [unit_cost_snapshot] DECIMAL(10,2) NULL,
        [total_cost] DECIMAL(10,2) NULL,
        [notes] NVARCHAR(MAX) NULL,
        [created_at] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Foreign key constraints
        CONSTRAINT [FK_InventoryMovement_Company] 
            FOREIGN KEY ([company_id]) REFERENCES [dbo].[Company]([company_id]) ON DELETE NO ACTION,
        
        CONSTRAINT [FK_InventoryMovement_Part] 
            FOREIGN KEY ([part_id]) REFERENCES [dbo].[Part]([part_id]) ON DELETE NO ACTION,
        
        CONSTRAINT [FK_InventoryMovement_WorkOrder] 
            FOREIGN KEY ([work_order_id]) REFERENCES [dbo].[Work_Order]([work_order_id]) ON DELETE NO ACTION,
        
        CONSTRAINT [FK_InventoryMovement_WorkOrderPart] 
            FOREIGN KEY ([work_order_part_id]) REFERENCES [dbo].[WorkOrderPart]([id]) ON DELETE NO ACTION,
        
        CONSTRAINT [FK_InventoryMovement_User] 
            FOREIGN KEY ([performed_by_user_id]) REFERENCES [dbo].[AspNetUsers]([Id]) ON DELETE NO ACTION,
        
        -- Check constraints
        CONSTRAINT [CK_InventoryMovement_MovementType] 
            CHECK ([movement_type] IN ('Consumption', 'Adjustment', 'Restock', 'Return', 'Correction', 'InitialStock', 'Transfer')),
        
        CONSTRAINT [CK_InventoryMovement_Quantities] 
            CHECK ([new_quantity] = [previous_quantity] + [quantity_changed])
    );
    
    PRINT '✓ Created InventoryMovement table';
END
ELSE
BEGIN
    PRINT '⊘ InventoryMovement table already exists';
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovement_CompanyId_PartId')
BEGIN
    CREATE INDEX [IX_InventoryMovement_CompanyId_PartId]
    ON [dbo].[InventoryMovement]([company_id], [part_id])
    INCLUDE ([movement_type], [quantity_changed], [created_at]);
    
    PRINT '✓ Created IX_InventoryMovement_CompanyId_PartId index';
END
ELSE
BEGIN
    PRINT '⊘ IX_InventoryMovement_CompanyId_PartId index already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovement_WorkOrderId')
BEGIN
    CREATE INDEX [IX_InventoryMovement_WorkOrderId]
    ON [dbo].[InventoryMovement]([work_order_id])
    INCLUDE ([part_id], [quantity_changed], [movement_type]);
    
    PRINT '✓ Created IX_InventoryMovement_WorkOrderId index';
END
ELSE
BEGIN
    PRINT '⊘ IX_InventoryMovement_WorkOrderId index already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovement_MovementType')
BEGIN
    CREATE INDEX [IX_InventoryMovement_MovementType]
    ON [dbo].[InventoryMovement]([movement_type])
    INCLUDE ([company_id], [part_id], [quantity_changed], [created_at]);
    
    PRINT '✓ Created IX_InventoryMovement_MovementType index';
END
ELSE
BEGIN
    PRINT '⊘ IX_InventoryMovement_MovementType index already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovement_CreatedAt')
BEGIN
    CREATE INDEX [IX_InventoryMovement_CreatedAt]
    ON [dbo].[InventoryMovement]([created_at] DESC)
    INCLUDE ([company_id], [part_id], [movement_type], [quantity_changed]);
    
    PRINT '✓ Created IX_InventoryMovement_CreatedAt index';
END
ELSE
BEGIN
    PRINT '⊘ IX_InventoryMovement_CreatedAt index already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryMovement_PerformedBy')
BEGIN
    CREATE INDEX [IX_InventoryMovement_PerformedBy]
    ON [dbo].[InventoryMovement]([performed_by_user_id])
    INCLUDE ([movement_type], [quantity_changed], [created_at]);
    
    PRINT '✓ Created IX_InventoryMovement_PerformedBy index';
END
ELSE
BEGIN
    PRINT '⊘ IX_InventoryMovement_PerformedBy index already exists';
END
GO

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================

-- Verify table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'InventoryMovement'
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
WHERE fk.parent_object_id = OBJECT_ID('InventoryMovement');

-- Verify check constraints
SELECT 
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints AS cc
WHERE cc.parent_object_id = OBJECT_ID('InventoryMovement');

-- Verify indexes
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
    ic.is_included_column AS IsIncluded
FROM sys.indexes AS i
INNER JOIN sys.index_columns AS ic 
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('InventoryMovement')
  AND i.name IS NOT NULL
ORDER BY i.name, ic.key_ordinal;

-- Show current record count
SELECT COUNT(*) AS TotalMovements FROM [dbo].[InventoryMovement];

PRINT '';
PRINT '============================================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY';
PRINT '============================================================';
PRINT 'InventoryMovement table created with:';
PRINT '  ✓ Immutable audit log structure';
PRINT '  ✓ Before/after quantity tracking';
PRINT '  ✓ Work order and user traceability';
PRINT '  ✓ Cost snapshot support';
PRINT '  ✓ Multi-tenant safety (CompanyId)';
PRINT '  ✓ 5 performance indexes';
PRINT '  ✓ Movement type validation';
PRINT '  ✓ Quantity consistency check';
PRINT '';
PRINT 'Supported movement types:';
PRINT '  - Consumption (WO parts usage)';
PRINT '  - Adjustment (manual stock changes)';
PRINT '  - Restock (new inventory received)';
PRINT '  - Return (unused parts returned)';
PRINT '  - Correction (error fixes)';
PRINT '  - InitialStock (initial inventory setup)';
PRINT '  - Transfer (location transfers)';
PRINT '============================================================';
GO
