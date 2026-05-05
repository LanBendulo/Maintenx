-- =============================================================
-- Add maintenance_request_id column to Work_Order table
-- This enables linking work orders to maintenance requests
-- =============================================================

USE DB_Maintenx;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' 
    AND COLUMN_NAME = 'maintenance_request_id'
)
BEGIN
    PRINT 'Adding maintenance_request_id column to Work_Order table...';
    
    -- Add the column
    ALTER TABLE dbo.Work_Order
    ADD maintenance_request_id INT NULL;
    
    PRINT 'Column added successfully!';
    
    -- Add foreign key constraint if Maintenance_Request table exists
    IF OBJECT_ID('dbo.Maintenance_Request', 'U') IS NOT NULL
    BEGIN
        PRINT 'Adding foreign key constraint...';
        
        ALTER TABLE dbo.Work_Order
        ADD CONSTRAINT FK_WorkOrder_MaintenanceRequest
            FOREIGN KEY (maintenance_request_id)
            REFERENCES dbo.Maintenance_Request (request_id)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION;
        
        PRINT 'Foreign key constraint added!';
        
        -- Add index for performance
        IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WorkOrder_maintenance_request_id' AND object_id = OBJECT_ID('dbo.Work_Order'))
        BEGIN
            CREATE INDEX IX_WorkOrder_maintenance_request_id ON dbo.Work_Order (maintenance_request_id);
            PRINT 'Index created!';
        END
    END
    ELSE
    BEGIN
        PRINT 'WARNING: Maintenance_Request table not found. Foreign key constraint not created.';
    END
END
ELSE
BEGIN
    PRINT 'Column maintenance_request_id already exists in Work_Order table.';
END
GO

-- Verify the change
PRINT '';
PRINT '=== Verification ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Work_Order'
ORDER BY ORDINAL_POSITION;
GO

PRINT '';
PRINT 'Migration complete!';
