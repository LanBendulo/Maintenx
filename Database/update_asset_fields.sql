-- ═══════════════════════════════════════════════════════════════
-- UPDATE ASSET TABLE - Add missing fields
-- ═══════════════════════════════════════════════════════════════

-- Add asset_code if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'asset_code')
BEGIN
    ALTER TABLE Asset ADD asset_code NVARCHAR(50) NULL;
    PRINT '✓ Added asset_code column';
END

-- Add description if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'description')
BEGIN
    ALTER TABLE Asset ADD description NVARCHAR(MAX) NULL;
    PRINT '✓ Added description column';
END

-- Add created_at if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'created_at')
BEGIN
    ALTER TABLE Asset ADD created_at DATETIME NOT NULL DEFAULT GETDATE();
    PRINT '✓ Added created_at column';
END

-- Add updated_at if not exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'updated_at')
BEGIN
    ALTER TABLE Asset ADD updated_at DATETIME NULL;
    PRINT '✓ Added updated_at column';
END

-- Create unique index on asset_code per company
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Asset_Code_Company')
BEGIN
    -- Check if asset_code column exists before creating index
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Asset') AND name = 'asset_code')
    BEGIN
        CREATE UNIQUE INDEX IX_Asset_Code_Company ON Asset(company_id, asset_code) 
        WHERE asset_code IS NOT NULL;
        PRINT '✓ Created unique index on asset_code per company';
    END
END

PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'Asset table update complete!';
PRINT '═══════════════════════════════════════════════════════════════';
