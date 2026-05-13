-- Add missing columns to AspNetUsers table
USE [DB_Maintenx]
GO

-- Check and add CompanyId column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'CompanyId')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [CompanyId] INT NULL
    PRINT 'Added CompanyId column'
END
ELSE
    PRINT 'CompanyId column already exists'
GO

-- Check and add FullName column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'FullName')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [FullName] NVARCHAR(200) NULL
    PRINT 'Added FullName column'
END
ELSE
    PRINT 'FullName column already exists'
GO

-- Check and add IsActive column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [IsActive] BIT NOT NULL DEFAULT 1
    PRINT 'Added IsActive column'
END
ELSE
    PRINT 'IsActive column already exists'
GO

-- Check and add CreatedAt column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETDATE()
    PRINT 'Added CreatedAt column'
END
ELSE
    PRINT 'CreatedAt column already exists'
GO

-- Check and add LastLoginAt column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'LastLoginAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LastLoginAt] DATETIME2(7) NULL
    PRINT 'Added LastLoginAt column'
END
ELSE
    PRINT 'LastLoginAt column already exists'
GO

-- Check and add UpdatedAt column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [UpdatedAt] DATETIME2(7) NULL
    PRINT 'Added UpdatedAt column'
END
ELSE
    PRINT 'UpdatedAt column already exists'
GO

-- Verify all columns
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
ORDER BY ORDINAL_POSITION
GO

PRINT 'AspNetUsers table updated successfully!'
GO
