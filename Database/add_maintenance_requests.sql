-- =============================================
-- Add Maintenance Request Feature
-- =============================================

-- Create Maintenance_Request table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Maintenance_Request] (
        [request_id] INT IDENTITY(1,1) PRIMARY KEY,
        [request_number] NVARCHAR(50) NOT NULL UNIQUE,
        [title] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(MAX) NOT NULL,
        [asset_id] INT NOT NULL,
        [priority] NVARCHAR(20) NOT NULL DEFAULT 'Medium',
        [status] NVARCHAR(30) NOT NULL DEFAULT 'Pending',
        [requested_by] INT NULL,
        [category] NVARCHAR(50) NULL,
        [location] NVARCHAR(200) NULL,
        [attachment_url] NVARCHAR(500) NULL,
        [created_at] DATETIME NOT NULL DEFAULT GETDATE(),
        [updated_at] DATETIME NULL,
        
        CONSTRAINT FK_MaintenanceRequest_Asset FOREIGN KEY ([asset_id]) 
            REFERENCES [dbo].[Asset]([asset_id]),
        CONSTRAINT FK_MaintenanceRequest_RequestedBy FOREIGN KEY ([requested_by]) 
            REFERENCES [dbo].[Personnel]([personnel_id]),
        CONSTRAINT CK_MaintenanceRequest_Priority CHECK ([priority] IN ('Low', 'Medium', 'High')),
        CONSTRAINT CK_MaintenanceRequest_Status CHECK ([status] IN ('Pending', 'Approved', 'Rejected', 'Converted'))
    );

    CREATE INDEX IX_MaintenanceRequest_Status ON [dbo].[Maintenance_Request]([status]);
    CREATE INDEX IX_MaintenanceRequest_Asset ON [dbo].[Maintenance_Request]([asset_id]);
    CREATE INDEX IX_MaintenanceRequest_RequestedBy ON [dbo].[Maintenance_Request]([requested_by]);
    
    PRINT 'Maintenance_Request table created successfully.';
END
ELSE
BEGIN
    PRINT 'Maintenance_Request table already exists.';
END
GO

-- Add maintenance_request_id column to Work_Order table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Work_Order]') AND name = 'maintenance_request_id')
BEGIN
    ALTER TABLE [dbo].[Work_Order]
    ADD [maintenance_request_id] INT NULL;
    
    ALTER TABLE [dbo].[Work_Order]
    ADD CONSTRAINT FK_WorkOrder_MaintenanceRequest FOREIGN KEY ([maintenance_request_id])
        REFERENCES [dbo].[Maintenance_Request]([request_id]);
    
    CREATE INDEX IX_WorkOrder_MaintenanceRequest ON [dbo].[Work_Order]([maintenance_request_id]);
    
    PRINT 'maintenance_request_id column added to Work_Order table.';
END
ELSE
BEGIN
    PRINT 'maintenance_request_id column already exists in Work_Order table.';
END
GO

-- Add new columns to existing Maintenance_Request table if they don't exist
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND type in (N'U'))
BEGIN
    -- Add category column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'category')
    BEGIN
        ALTER TABLE [dbo].[Maintenance_Request]
        ADD [category] NVARCHAR(50) NULL;
        PRINT 'category column added to Maintenance_Request table.';
    END

    -- Add location column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'location')
    BEGIN
        ALTER TABLE [dbo].[Maintenance_Request]
        ADD [location] NVARCHAR(200) NULL;
        PRINT 'location column added to Maintenance_Request table.';
    END

    -- Add attachment_url column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'attachment_url')
    BEGIN
        ALTER TABLE [dbo].[Maintenance_Request]
        ADD [attachment_url] NVARCHAR(500) NULL;
        PRINT 'attachment_url column added to Maintenance_Request table.';
    END

    -- Modify title column length if needed
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'title' AND max_length > 200)
    BEGIN
        ALTER TABLE [dbo].[Maintenance_Request]
        ALTER COLUMN [title] NVARCHAR(100) NOT NULL;
        PRINT 'title column length updated to 100 characters.';
    END
END
GO

PRINT 'Maintenance Request feature migration completed successfully!';
