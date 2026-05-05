-- =============================================================
-- Add is_archived column to Maintenance_Request table
-- Enables archiving old/completed maintenance requests
-- =============================================================

USE DB_Maintenx;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Maintenance_Request' 
    AND COLUMN_NAME = 'is_archived'
)
BEGIN
    PRINT 'Adding is_archived column to Maintenance_Request table...';
    
    -- Add the column with default value FALSE
    ALTER TABLE dbo.Maintenance_Request
    ADD is_archived BIT NOT NULL DEFAULT 0;
    
    PRINT 'Column added successfully!';
    
    -- Add index for filtering archived/active requests
    IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MaintenanceRequest_is_archived' AND object_id = OBJECT_ID('dbo.Maintenance_Request'))
    BEGIN
        CREATE INDEX IX_MaintenanceRequest_is_archived ON dbo.Maintenance_Request (is_archived);
        PRINT 'Index created!';
    END
END
ELSE
BEGIN
    PRINT 'Column is_archived already exists in Maintenance_Request table.';
END
GO

-- Verify the change
PRINT '';
PRINT '=== Verification ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Maintenance_Request'
AND COLUMN_NAME = 'is_archived';
GO

-- Show count of archived vs active requests
PRINT '';
PRINT '=== Current Status ===';
SELECT 
    is_archived,
    COUNT(*) AS request_count
FROM dbo.Maintenance_Request
GROUP BY is_archived;
GO

PRINT '';
PRINT 'Migration complete!';
