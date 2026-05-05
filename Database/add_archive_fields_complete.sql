-- =============================================================
-- Add Archive (Soft Delete) Fields to Maintenance_Request and Work_Order
-- Implements proper audit trail for archived records
-- =============================================================

USE DB_Maintenx;
GO

PRINT '=== Adding Archive Fields to Maintenance_Request ===';

-- Add is_archived column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Maintenance_Request' AND COLUMN_NAME = 'is_archived'
)
BEGIN
    ALTER TABLE dbo.Maintenance_Request
    ADD is_archived BIT NOT NULL DEFAULT 0;
    PRINT 'Added is_archived column';
END
ELSE
    PRINT 'is_archived column already exists';

-- Add archived_at column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Maintenance_Request' AND COLUMN_NAME = 'archived_at'
)
BEGIN
    ALTER TABLE dbo.Maintenance_Request
    ADD archived_at DATETIME NULL;
    PRINT 'Added archived_at column';
END
ELSE
    PRINT 'archived_at column already exists';

-- Add archived_by_user_id column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Maintenance_Request' AND COLUMN_NAME = 'archived_by_user_id'
)
BEGIN
    ALTER TABLE dbo.Maintenance_Request
    ADD archived_by_user_id NVARCHAR(450) NULL;
    PRINT 'Added archived_by_user_id column';
    
    -- Add foreign key to AspNetUsers
    IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Maintenance_Request
        ADD CONSTRAINT FK_MaintenanceRequest_ArchivedByUser
            FOREIGN KEY (archived_by_user_id)
            REFERENCES dbo.AspNetUsers(Id)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION;
        PRINT 'Added foreign key constraint';
    END
END
ELSE
    PRINT 'archived_by_user_id column already exists';

-- Add index for filtering
IF NOT EXISTS (
    SELECT name FROM sys.indexes 
    WHERE name = N'IX_MaintenanceRequest_is_archived' 
    AND object_id = OBJECT_ID('dbo.Maintenance_Request')
)
BEGIN
    CREATE INDEX IX_MaintenanceRequest_is_archived 
    ON dbo.Maintenance_Request (is_archived);
    PRINT 'Created index on is_archived';
END

GO

PRINT '';
PRINT '=== Adding Archive Fields to Work_Order ===';

-- Add is_archived column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' AND COLUMN_NAME = 'is_archived'
)
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD is_archived BIT NOT NULL DEFAULT 0;
    PRINT 'Added is_archived column';
END
ELSE
    PRINT 'is_archived column already exists';

-- Add archived_at column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' AND COLUMN_NAME = 'archived_at'
)
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD archived_at DATETIME NULL;
    PRINT 'Added archived_at column';
END
ELSE
    PRINT 'archived_at column already exists';

-- Add archived_by_user_id column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Work_Order' AND COLUMN_NAME = 'archived_by_user_id'
)
BEGIN
    ALTER TABLE dbo.Work_Order
    ADD archived_by_user_id NVARCHAR(450) NULL;
    PRINT 'Added archived_by_user_id column';
    
    -- Add foreign key to AspNetUsers
    IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Work_Order
        ADD CONSTRAINT FK_WorkOrder_ArchivedByUser
            FOREIGN KEY (archived_by_user_id)
            REFERENCES dbo.AspNetUsers(Id)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION;
        PRINT 'Added foreign key constraint';
    END
END
ELSE
    PRINT 'archived_by_user_id column already exists';

-- Add index for filtering
IF NOT EXISTS (
    SELECT name FROM sys.indexes 
    WHERE name = N'IX_WorkOrder_is_archived' 
    AND object_id = OBJECT_ID('dbo.Work_Order')
)
BEGIN
    CREATE INDEX IX_WorkOrder_is_archived 
    ON dbo.Work_Order (is_archived);
    PRINT 'Created index on is_archived';
END

GO

PRINT '';
PRINT '=== Verification ===';
PRINT 'Maintenance_Request columns:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Maintenance_Request'
AND COLUMN_NAME IN ('is_archived', 'archived_at', 'archived_by_user_id');

PRINT '';
PRINT 'Work_Order columns:';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Work_Order'
AND COLUMN_NAME IN ('is_archived', 'archived_at', 'archived_by_user_id');

GO

PRINT '';
PRINT 'Migration complete!';
