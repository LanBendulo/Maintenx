-- =============================================
-- Standardize Asset Operational Status System
-- =============================================
-- Purpose: Normalize Asset.Status values and prepare for
--          automated Work Order lifecycle integration
-- 
-- IMPORTANT: This does NOT create new columns.
--            It standardizes the EXISTING Asset.status field.
-- =============================================

USE [maintenx_db];
GO

PRINT '========================================';
PRINT 'STANDARDIZING ASSET STATUS VALUES';
PRINT '========================================';
PRINT '';

-- =============================================
-- STEP 1: Normalize existing status values
-- =============================================
PRINT 'Step 1: Normalizing existing Asset.status values...';

-- Map "Operational" → "Active"
UPDATE dbo.Asset
SET [status] = 'Active'
WHERE [status] = 'Operational' OR [status] = 'operational';

PRINT '  ✓ Mapped "Operational" → "Active"';

-- Map "Inactive" → "Out of Service"
UPDATE dbo.Asset
SET [status] = 'Out of Service'
WHERE [status] = 'Inactive' OR [status] = 'inactive';

PRINT '  ✓ Mapped "Inactive" → "Out of Service"';

-- Set NULL values to "Active" (default)
UPDATE dbo.Asset
SET [status] = 'Active'
WHERE [status] IS NULL;

PRINT '  ✓ Set NULL values to "Active"';

-- =============================================
-- STEP 2: Verify standardization
-- =============================================
PRINT '';
PRINT 'Step 2: Verifying standardized values...';

DECLARE @ActiveCount INT = (SELECT COUNT(*) FROM dbo.Asset WHERE [status] = 'Active');
DECLARE @UnderMaintenanceCount INT = (SELECT COUNT(*) FROM dbo.Asset WHERE [status] = 'Under Maintenance');
DECLARE @OutOfServiceCount INT = (SELECT COUNT(*) FROM dbo.Asset WHERE [status] = 'Out of Service');
DECLARE @RetiredCount INT = (SELECT COUNT(*) FROM dbo.Asset WHERE [status] = 'Retired');
DECLARE @OtherCount INT = (SELECT COUNT(*) FROM dbo.Asset WHERE [status] NOT IN ('Active', 'Under Maintenance', 'Out of Service', 'Retired'));

PRINT '  • Active: ' + CAST(@ActiveCount AS NVARCHAR(10));
PRINT '  • Under Maintenance: ' + CAST(@UnderMaintenanceCount AS NVARCHAR(10));
PRINT '  • Out of Service: ' + CAST(@OutOfServiceCount AS NVARCHAR(10));
PRINT '  • Retired: ' + CAST(@RetiredCount AS NVARCHAR(10));
PRINT '  • Other (non-standard): ' + CAST(@OtherCount AS NVARCHAR(10));

IF @OtherCount > 0
BEGIN
    PRINT '';
    PRINT '⚠️  WARNING: Non-standard status values detected:';
    SELECT DISTINCT [status], COUNT(*) as [Count]
    FROM dbo.Asset
    WHERE [status] NOT IN ('Active', 'Under Maintenance', 'Out of Service', 'Retired')
    GROUP BY [status];
END

-- =============================================
-- STEP 3: Add constraint (optional - commented out)
-- =============================================
-- Uncomment to enforce status values at database level
/*
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Asset_Status')
BEGIN
    ALTER TABLE dbo.Asset
    ADD CONSTRAINT CK_Asset_Status
    CHECK ([status] IN ('Active', 'Under Maintenance', 'Out of Service', 'Retired'));
    
    PRINT '';
    PRINT '  ✓ Added CHECK constraint for Asset.status';
END
*/

-- =============================================
-- STEP 4: Create Asset Status History table
-- =============================================
PRINT '';
PRINT 'Step 3: Creating Asset Status History table...';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssetStatusHistory')
BEGIN
    CREATE TABLE dbo.AssetStatusHistory (
        history_id INT IDENTITY(1,1) PRIMARY KEY,
        asset_id INT NOT NULL,
        company_id INT NOT NULL,
        old_status NVARCHAR(30) NULL,
        new_status NVARCHAR(30) NOT NULL,
        changed_by_user_id NVARCHAR(450) NULL,
        work_order_id INT NULL,
        reason NVARCHAR(500) NULL,
        changed_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_AssetStatusHistory_Asset FOREIGN KEY (asset_id) 
            REFERENCES dbo.Asset(asset_id) ON DELETE CASCADE,
        CONSTRAINT FK_AssetStatusHistory_Company FOREIGN KEY (company_id) 
            REFERENCES dbo.Company(company_id),
        CONSTRAINT FK_AssetStatusHistory_WorkOrder FOREIGN KEY (work_order_id) 
            REFERENCES dbo.Work_Order(work_order_id) ON DELETE SET NULL,
        CONSTRAINT FK_AssetStatusHistory_User FOREIGN KEY (changed_by_user_id) 
            REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_AssetStatusHistory_AssetId ON dbo.AssetStatusHistory(asset_id);
    CREATE INDEX IX_AssetStatusHistory_CompanyId ON dbo.AssetStatusHistory(company_id);
    CREATE INDEX IX_AssetStatusHistory_ChangedAt ON dbo.AssetStatusHistory(changed_at DESC);
    
    PRINT '  ✓ Created AssetStatusHistory table';
    PRINT '  ✓ Created indexes';
END
ELSE
BEGIN
    PRINT '  ℹ️  AssetStatusHistory table already exists';
END

-- =============================================
-- COMPLETION
-- =============================================
PRINT '';
PRINT '========================================';
PRINT '✅ ASSET STATUS STANDARDIZATION COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Standardized Status Values:';
PRINT '  • Active - Asset is operational';
PRINT '  • Under Maintenance - Asset has active work order(s)';
PRINT '  • Out of Service - Asset is not operational';
PRINT '  • Retired - Asset is decommissioned';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Update backend Work Order creation logic';
PRINT '  2. Update Work Order status change handlers';
PRINT '  3. Update UI badges and filters';
PRINT '  4. Test automated status transitions';
PRINT '';

GO
