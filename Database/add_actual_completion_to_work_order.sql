-- =============================================================
-- Add actual_completion column to Work_Order table
-- Required for tracking when work was actually completed
-- =============================================================

USE DB_Maintenx;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' AND COLUMN_NAME = 'actual_completion'
)
BEGIN
    PRINT 'Adding actual_completion column to Work_Order table...';
    
    ALTER TABLE dbo.Work_Order
    ADD actual_completion DATE NULL;
    
    PRINT 'Column added successfully!';
END
ELSE
BEGIN
    PRINT 'Column actual_completion already exists in Work_Order table.';
END
GO

-- Verify the change
PRINT '';
PRINT '=== Verification ===';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Work_Order'
AND COLUMN_NAME = 'actual_completion';
GO

PRINT '';
PRINT 'Migration complete!';
