-- Add missing 'source' column to Work_Order table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Work_Order') AND name = 'source')
BEGIN
    ALTER TABLE Work_Order ADD source NVARCHAR(50) NULL DEFAULT 'Manual';
    PRINT 'Added source column to Work_Order';
END
ELSE
BEGIN
    PRINT 'source column already exists in Work_Order';
END
