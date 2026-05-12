-- =============================================================
-- Add archive_reason column to Work_Order table
-- Part of enterprise-grade soft archival system
-- =============================================================

USE DB_Maintenx;
GO

PRINT '=== Adding archive_reason column to Work_Order ===';

-- Add archive_reason column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' AND COLUMN_NAME = 'archive_reason'
)
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD archive_reason NVARCHAR(500) NULL;
    PRINT 'Added archive_reason column';
END
ELSE
    PRINT 'archive_reason column already exists';

GO

PRINT '';
PRINT '=== Verification ===';
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Work_Order'
AND COLUMN_NAME = 'archive_reason';

GO

PRINT '';
PRINT 'Migration complete!';
