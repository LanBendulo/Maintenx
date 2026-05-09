-- =============================================
-- Add User Management Fields to AspNetUsers
-- Fixed for remote SQL Server
-- =============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Add IsActive field (for soft deactivation)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [IsActive] BIT NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT 1;
    PRINT '✓ Added IsActive column';
END
ELSE
BEGIN
    PRINT '✓ IsActive column already exists';
END
GO

-- Add LastLoginAt field (track last login)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'LastLoginAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [LastLoginAt] DATETIME2 NULL;
    PRINT '✓ Added LastLoginAt column';
END
ELSE
BEGIN
    PRINT '✓ LastLoginAt column already exists';
END
GO

-- Add CreatedAt field (account creation timestamp)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [CreatedAt] DATETIME2 NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT GETDATE();
    PRINT '✓ Added CreatedAt column';
END
ELSE
BEGIN
    PRINT '✓ CreatedAt column already exists';
END
GO

-- Add UpdatedAt field (last account update)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [UpdatedAt] DATETIME2 NULL;
    PRINT '✓ Added UpdatedAt column';
END
ELSE
BEGIN
    PRINT '✓ UpdatedAt column already exists';
END
GO

-- Create index on CompanyId for tenant filtering optimization
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_CompanyId' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AspNetUsers_CompanyId]
    ON [dbo].[AspNetUsers] ([CompanyId])
    INCLUDE ([IsActive], [Email], [FullName]);
    PRINT '✓ Created index on CompanyId';
END
ELSE
BEGIN
    PRINT '✓ Index on CompanyId already exists';
END
GO

-- Create index on IsActive for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_IsActive' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AspNetUsers_IsActive]
    ON [dbo].[AspNetUsers] ([IsActive])
    INCLUDE ([CompanyId], [Email]);
    PRINT '✓ Created index on IsActive';
END
ELSE
BEGIN
    PRINT '✓ Index on IsActive already exists';
END
GO

-- Create index on LastLoginAt for activity tracking
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AspNetUsers_LastLoginAt' AND object_id = OBJECT_ID(N'[dbo].[AspNetUsers]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AspNetUsers_LastLoginAt]
    ON [dbo].[AspNetUsers] ([LastLoginAt] DESC)
    WHERE [LastLoginAt] IS NOT NULL;
    PRINT '✓ Created index on LastLoginAt';
END
ELSE
BEGIN
    PRINT '✓ Index on LastLoginAt already exists';
END
GO

PRINT '';
PRINT '════════════════════════════════════════════════════════';
PRINT '✓ User Management fields migration completed successfully!';
PRINT '════════════════════════════════════════════════════════';
PRINT '';
PRINT 'NOTE: Personnel → User relationship remains one-directional';
PRINT '      Personnel.UserId → AspNetUsers.Id (existing FK)';
PRINT '      No circular FK created.';
PRINT '════════════════════════════════════════════════════════';
GO
