-- =============================================================
-- Verify and Manually Seed Assets (if needed)
-- Run this in SQL Server Management Studio (SSMS)
-- =============================================================

USE DB_Maintenx;
GO

-- =============================================================
-- STEP 1: Check if data exists
-- =============================================================
PRINT '=== Checking existing data ===';
PRINT 'Categories: ' + CAST((SELECT COUNT(*) FROM dbo.Category) AS VARCHAR(10));
PRINT 'Assets: ' + CAST((SELECT COUNT(*) FROM dbo.Asset) AS VARCHAR(10));
PRINT 'Personnel: ' + CAST((SELECT COUNT(*) FROM dbo.Personnel) AS VARCHAR(10));
GO

-- =============================================================
-- STEP 2: View existing data
-- =============================================================
PRINT '';
PRINT '=== Existing Categories ===';
SELECT * FROM dbo.Category;

PRINT '';
PRINT '=== Existing Assets ===';
SELECT * FROM dbo.Asset;

PRINT '';
PRINT '=== Existing Personnel ===';
SELECT personnel_id, first_name, last_name, role, is_active FROM dbo.Personnel;
GO

-- =============================================================
-- STEP 3: Manual Seed (if tables are empty)
-- Only run this section if the counts above are 0
-- =============================================================

-- Seed Categories (if empty)
IF NOT EXISTS (SELECT 1 FROM dbo.Category)
BEGIN
    PRINT '';
    PRINT '=== Seeding Categories ===';
    
    INSERT INTO dbo.Category (category_name) VALUES
        ('HVAC Systems'),
        ('Electrical Equipment'),
        ('Plumbing Systems'),
        ('Mechanical Equipment'),
        ('Safety Systems'),
        ('Building Infrastructure'),
        ('IT Equipment'),
        ('Vehicles');
    
    PRINT 'Categories seeded successfully!';
END
ELSE
BEGIN
    PRINT 'Categories already exist. Skipping...';
END
GO

-- Seed Assets (if empty)
IF NOT EXISTS (SELECT 1 FROM dbo.Asset)
BEGIN
    PRINT '';
    PRINT '=== Seeding Assets ===';
    
    -- Get category IDs
    DECLARE @HvacCategoryId INT = (SELECT category_id FROM dbo.Category WHERE category_name = 'HVAC Systems');
    DECLARE @ElectricalCategoryId INT = (SELECT category_id FROM dbo.Category WHERE category_name = 'Electrical Equipment');
    DECLARE @PlumbingCategoryId INT = (SELECT category_id FROM dbo.Category WHERE category_name = 'Plumbing Systems');
    DECLARE @MechanicalCategoryId INT = (SELECT category_id FROM dbo.Category WHERE category_name = 'Mechanical Equipment');
    DECLARE @SafetyCategoryId INT = (SELECT category_id FROM dbo.Category WHERE category_name = 'Safety Systems');
    
    INSERT INTO dbo.Asset (asset_name, category_id, location, status, purchase_date) VALUES
        ('Chiller Unit #1 - Building A', @HvacCategoryId, 'Rooftop - Building A', 'Operational', '2020-03-15'),
        ('Air Handling Unit - 3rd Floor', @HvacCategoryId, 'Mechanical Room - 3rd Floor', 'Operational', '2021-06-10'),
        ('Main Electrical Panel - Building A', @ElectricalCategoryId, 'Electrical Room - Ground Floor', 'Operational', '2019-01-20'),
        ('Emergency Generator #1', @ElectricalCategoryId, 'Generator Room - Basement', 'Operational', '2018-11-05'),
        ('Water Pump - Main Supply', @PlumbingCategoryId, 'Pump Room - Basement', 'Operational', '2020-08-12'),
        ('Boiler System #1', @HvacCategoryId, 'Boiler Room - Basement', 'Operational', '2019-09-25'),
        ('Elevator #1 - Main Building', @MechanicalCategoryId, 'Main Building - Lobby', 'Operational', '2017-04-18'),
        ('Fire Suppression System - Building A', @SafetyCategoryId, 'Building A - All Floors', 'Operational', '2018-02-10'),
        ('Cooling Tower #1', @HvacCategoryId, 'Rooftop - Building B', 'Operational', '2020-05-22'),
        ('Compressor Unit - Workshop', @MechanicalCategoryId, 'Workshop - Ground Floor', 'Operational', '2021-03-08');
    
    PRINT 'Assets seeded successfully!';
END
ELSE
BEGIN
    PRINT 'Assets already exist. Skipping...';
END
GO

-- =============================================================
-- STEP 4: Verify the seed was successful
-- =============================================================
PRINT '';
PRINT '=== Final Verification ===';
PRINT 'Categories: ' + CAST((SELECT COUNT(*) FROM dbo.Category) AS VARCHAR(10));
PRINT 'Assets: ' + CAST((SELECT COUNT(*) FROM dbo.Asset) AS VARCHAR(10));
GO

-- =============================================================
-- STEP 5: Test the query used by the application
-- =============================================================
PRINT '';
PRINT '=== Testing Application Query ===';
SELECT 
    asset_id AS [value], 
    asset_name AS [text]
FROM dbo.Asset
WHERE status != 'Retired'
ORDER BY asset_name;
GO

PRINT '';
PRINT '=== Script Complete ===';
PRINT 'If you see assets listed above, the /admin/assets/list endpoint should work.';
PRINT 'If not, check:';
PRINT '1. Database connection string in appsettings.json';
PRINT '2. Application is running in Development mode';
PRINT '3. Entity Framework migrations are up to date';
GO
