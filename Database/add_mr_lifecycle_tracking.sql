-- =============================================
-- Add Maintenance Request Lifecycle Tracking Fields
-- Adds proper conversion and closure tracking to Maintenance_Request table
-- =============================================

USE [db50508];
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'converted_work_order_id')
BEGIN
    ALTER TABLE [dbo].[Maintenance_Request]
    ADD [converted_work_order_id] INT NULL;
    
    PRINT '✓ Added converted_work_order_id column';
END
ELSE
BEGIN
    PRINT '⊘ converted_work_order_id column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'converted_at')
BEGIN
    ALTER TABLE [dbo].[Maintenance_Request]
    ADD [converted_at] DATETIME NULL;
    
    PRINT '✓ Added converted_at column';
END
ELSE
BEGIN
    PRINT '⊘ converted_at column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'converted_by_user_id')
BEGIN
    ALTER TABLE [dbo].[Maintenance_Request]
    ADD [converted_by_user_id] NVARCHAR(450) NULL;
    
    PRINT '✓ Added converted_by_user_id column';
END
ELSE
BEGIN
    PRINT '⊘ converted_by_user_id column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'closed_at')
BEGIN
    ALTER TABLE [dbo].[Maintenance_Request]
    ADD [closed_at] DATETIME NULL;
    
    PRINT '✓ Added closed_at column';
END
ELSE
BEGIN
    PRINT '⊘ closed_at column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Maintenance_Request]') AND name = 'closed_by_user_id')
BEGIN
    ALTER TABLE [dbo].[Maintenance_Request]
    ADD [closed_by_user_id] NVARCHAR(450) NULL;
    
    PRINT '✓ Added closed_by_user_id column';
END
ELSE
BEGIN
    PRINT '⊘ closed_by_user_id column already exists';
END
GO

-- Backfill converted_work_order_id for existing converted requests
UPDATE mr
SET mr.converted_work_order_id = wo.work_order_id,
    mr.converted_at = wo.date_created
FROM [dbo].[Maintenance_Request] mr
INNER JOIN [dbo].[Work_Order] wo ON wo.maintenance_request_id = mr.request_id
WHERE mr.status = 'Converted'
  AND mr.converted_work_order_id IS NULL;
GO

PRINT '';
PRINT '================================================';
PRINT 'Maintenance Request Lifecycle Tracking Added';
PRINT '================================================';
PRINT '';
PRINT 'New Fields:';
PRINT '  - converted_work_order_id: ID of generated work order';
PRINT '  - converted_at: Timestamp of conversion';
PRINT '  - converted_by_user_id: User who performed conversion';
PRINT '  - closed_at: Timestamp of manual closure';
PRINT '  - closed_by_user_id: User who closed the request';
PRINT '';
PRINT 'Lifecycle Statuses:';
PRINT '  - Pending: Newly submitted, awaiting review';
PRINT '  - Approved: Approved for action, eligible for conversion';
PRINT '  - Rejected: Denied (terminal)';
PRINT '  - Converted: Successfully converted to Work Order (terminal)';
PRINT '  - Closed: Manually closed without conversion (terminal)';
PRINT '';
GO
