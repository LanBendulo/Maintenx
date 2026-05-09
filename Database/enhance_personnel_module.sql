-- ═══════════════════════════════════════════════════════════════════════════
-- Enhance Personnel Module for MaintenX CMMS
-- Adds comprehensive workforce management fields
-- ═══════════════════════════════════════════════════════════════════════════

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════════════════';
PRINT 'Starting Personnel Module Enhancement';
PRINT '═══════════════════════════════════════════════════════════════════════════';
PRINT '';

-- Add EmployeeId field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'employee_id')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [employee_id] NVARCHAR(50) NULL;
    PRINT '✓ Added employee_id column';
END
ELSE
BEGIN
    PRINT '✓ employee_id column already exists';
END
GO

-- Add MiddleName field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'middle_name')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [middle_name] NVARCHAR(100) NULL;
    PRINT '✓ Added middle_name column';
END
ELSE
BEGIN
    PRINT '✓ middle_name column already exists';
END
GO

-- Add Email field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'email')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [email] NVARCHAR(256) NULL;
    PRINT '✓ Added email column';
END
ELSE
BEGIN
    PRINT '✓ email column already exists';
END
GO

-- Add PhoneNumber field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'phone_number')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [phone_number] NVARCHAR(50) NULL;
    PRINT '✓ Added phone_number column';
END
ELSE
BEGIN
    PRINT '✓ phone_number column already exists';
END
GO

-- Add Address field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'address')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [address] NVARCHAR(500) NULL;
    PRINT '✓ Added address column';
END
ELSE
BEGIN
    PRINT '✓ address column already exists';
END
GO

-- Add Position field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'position')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [position] NVARCHAR(100) NULL;
    PRINT '✓ Added position column';
END
ELSE
BEGIN
    PRINT '✓ position column already exists';
END
GO

-- Add Department field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'department')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [department] NVARCHAR(100) NULL;
    PRINT '✓ Added department column';
END
ELSE
BEGIN
    PRINT '✓ department column already exists';
END
GO

-- Add EmploymentType field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'employment_type')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [employment_type] NVARCHAR(50) NULL;
    PRINT '✓ Added employment_type column';
END
ELSE
BEGIN
    PRINT '✓ employment_type column already exists';
END
GO

-- Add HireDate field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'hire_date')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [hire_date] DATE NULL;
    PRINT '✓ Added hire_date column';
END
ELSE
BEGIN
    PRINT '✓ hire_date column already exists';
END
GO

-- Add TerminationDate field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'termination_date')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [termination_date] DATE NULL;
    PRINT '✓ Added termination_date column';
END
ELSE
BEGIN
    PRINT '✓ termination_date column already exists';
END
GO

-- Add Status field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'status')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [status] NVARCHAR(50) NULL DEFAULT 'Active';
    PRINT '✓ Added status column';
END
ELSE
BEGIN
    PRINT '✓ status column already exists';
END
GO

-- Add Notes field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'notes')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [notes] NVARCHAR(MAX) NULL;
    PRINT '✓ Added notes column';
END
ELSE
BEGIN
    PRINT '✓ notes column already exists';
END
GO

-- Add EmergencyContactName field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'emergency_contact_name')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [emergency_contact_name] NVARCHAR(200) NULL;
    PRINT '✓ Added emergency_contact_name column';
END
ELSE
BEGIN
    PRINT '✓ emergency_contact_name column already exists';
END
GO

-- Add EmergencyContactPhone field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'emergency_contact_phone')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [emergency_contact_phone] NVARCHAR(50) NULL;
    PRINT '✓ Added emergency_contact_phone column';
END
ELSE
BEGIN
    PRINT '✓ emergency_contact_phone column already exists';
END
GO

-- Add ProfilePhotoUrl field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'profile_photo_url')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [profile_photo_url] NVARCHAR(500) NULL;
    PRINT '✓ Added profile_photo_url column';
END
ELSE
BEGIN
    PRINT '✓ profile_photo_url column already exists';
END
GO

-- Add UpdatedAt field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Personnel]') AND name = 'updated_at')
BEGIN
    ALTER TABLE [dbo].[Personnel]
    ADD [updated_at] DATETIME2 NULL;
    PRINT '✓ Added updated_at column';
END
ELSE
BEGIN
    PRINT '✓ updated_at column already exists';
END
GO

-- Create index on CompanyId for tenant filtering (if not exists)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Personnel_CompanyId' AND object_id = OBJECT_ID(N'[dbo].[Personnel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Personnel_CompanyId]
    ON [dbo].[Personnel] ([company_id])
    INCLUDE ([first_name], [last_name], [is_active], [status]);
    PRINT '✓ Created index on company_id';
END
ELSE
BEGIN
    PRINT '✓ Index on company_id already exists';
END
GO

-- Create index on EmployeeId for lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Personnel_EmployeeId' AND object_id = OBJECT_ID(N'[dbo].[Personnel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Personnel_EmployeeId]
    ON [dbo].[Personnel] ([employee_id])
    WHERE [employee_id] IS NOT NULL;
    PRINT '✓ Created index on employee_id';
END
ELSE
BEGIN
    PRINT '✓ Index on employee_id already exists';
END
GO

-- Create index on Department for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Personnel_Department' AND object_id = OBJECT_ID(N'[dbo].[Personnel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Personnel_Department]
    ON [dbo].[Personnel] ([department])
    WHERE [department] IS NOT NULL;
    PRINT '✓ Created index on department';
END
ELSE
BEGIN
    PRINT '✓ Index on department already exists';
END
GO

-- Create index on Status for filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Personnel_Status' AND object_id = OBJECT_ID(N'[dbo].[Personnel]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Personnel_Status]
    ON [dbo].[Personnel] ([status])
    WHERE [status] IS NOT NULL;
    PRINT '✓ Created index on status';
END
ELSE
BEGIN
    PRINT '✓ Index on status already exists';
END
GO

-- Update existing records to have default status if null
UPDATE [dbo].[Personnel]
SET [status] = 'Active'
WHERE [status] IS NULL AND [is_active] = 1;

UPDATE [dbo].[Personnel]
SET [status] = 'Inactive'
WHERE [status] IS NULL AND [is_active] = 0;
GO

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════════════════';
PRINT '✓ Personnel Module Enhancement Completed Successfully!';
PRINT '═══════════════════════════════════════════════════════════════════════════';
PRINT '';
PRINT 'New Fields Added:';
PRINT '  • employee_id - Unique employee identifier';
PRINT '  • middle_name - Middle name';
PRINT '  • email - Email address';
PRINT '  • phone_number - Contact phone';
PRINT '  • address - Physical address';
PRINT '  • position - Job position/title';
PRINT '  • department - Department name';
PRINT '  • employment_type - FullTime/PartTime/Contractor/etc';
PRINT '  • hire_date - Date of hire';
PRINT '  • termination_date - Date of termination (if applicable)';
PRINT '  • status - Active/Inactive/OnLeave/Retired/Terminated';
PRINT '  • notes - Additional notes';
PRINT '  • emergency_contact_name - Emergency contact';
PRINT '  • emergency_contact_phone - Emergency phone';
PRINT '  • profile_photo_url - Profile photo URL';
PRINT '  • updated_at - Last update timestamp';
PRINT '';
PRINT 'Indexes Created:';
PRINT '  • IX_Personnel_CompanyId - Tenant filtering';
PRINT '  • IX_Personnel_EmployeeId - Employee ID lookups';
PRINT '  • IX_Personnel_Department - Department filtering';
PRINT '  • IX_Personnel_Status - Status filtering';
PRINT '';
PRINT '═══════════════════════════════════════════════════════════════════════════';
