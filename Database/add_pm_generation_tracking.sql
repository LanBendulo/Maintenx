-- =============================================
-- Add PM Generation Tracking Fields
-- Adds fields to PreventiveSchedule for automatic work order generation
-- =============================================

USE [db50508];
GO

-- Check if columns already exist before adding
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PreventiveSchedule]') AND name = 'priority')
BEGIN
    ALTER TABLE [dbo].[PreventiveSchedule]
    ADD [priority] NVARCHAR(20) NULL;
    
    PRINT '✓ Added priority column';
END
ELSE
BEGIN
    PRINT '⊘ priority column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PreventiveSchedule]') AND name = 'last_generated_date')
BEGIN
    ALTER TABLE [dbo].[PreventiveSchedule]
    ADD [last_generated_date] DATE NULL;
    
    PRINT '✓ Added last_generated_date column';
END
ELSE
BEGIN
    PRINT '⊘ last_generated_date column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PreventiveSchedule]') AND name = 'last_generated_work_order_id')
BEGIN
    ALTER TABLE [dbo].[PreventiveSchedule]
    ADD [last_generated_work_order_id] INT NULL;
    
    PRINT '✓ Added last_generated_work_order_id column';
END
ELSE
BEGIN
    PRINT '⊘ last_generated_work_order_id column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PreventiveSchedule]') AND name = 'last_generation_attempt')
BEGIN
    ALTER TABLE [dbo].[PreventiveSchedule]
    ADD [last_generation_attempt] DATETIME NULL;
    
    PRINT '✓ Added last_generation_attempt column';
END
ELSE
BEGIN
    PRINT '⊘ last_generation_attempt column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PreventiveSchedule]') AND name = 'last_generation_error')
BEGIN
    ALTER TABLE [dbo].[PreventiveSchedule]
    ADD [last_generation_error] NVARCHAR(500) NULL;
    
    PRINT '✓ Added last_generation_error column';
END
ELSE
BEGIN
    PRINT '⊘ last_generation_error column already exists';
END
GO

-- Set default priority for existing schedules
UPDATE [dbo].[PreventiveSchedule]
SET [priority] = 'Medium'
WHERE [priority] IS NULL;
GO

PRINT '';
PRINT '================================================';
PRINT 'PM Generation Tracking Fields Added Successfully';
PRINT '================================================';
PRINT '';
PRINT 'New Fields:';
PRINT '  - priority: Default priority for generated work orders';
PRINT '  - last_generated_date: Date of last successful generation';
PRINT '  - last_generated_work_order_id: ID of last generated work order';
PRINT '  - last_generation_attempt: Timestamp of last generation attempt';
PRINT '  - last_generation_error: Error message from last failed attempt';
PRINT '';
PRINT 'Automatic PM work order generation is now enabled!';
PRINT '';
GO
