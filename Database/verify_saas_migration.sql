-- ============================================================
-- Verify SaaS Architecture Migration
-- ============================================================

PRINT '==================================================';
PRINT 'SaaS Migration Verification';
PRINT '==================================================';
PRINT '';

-- Check SubscriptionPlan table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionPlan')
BEGIN
    DECLARE @PlanCount INT;
    SELECT @PlanCount = COUNT(*) FROM SubscriptionPlan;
    PRINT '✓ SubscriptionPlan table exists';
    PRINT '  Plans found: ' + CAST(@PlanCount AS NVARCHAR(10));
    
    IF @PlanCount > 0
    BEGIN
        PRINT '';
        PRINT 'Subscription Plans:';
        SELECT 
            name AS [Plan Name],
            monthly_price AS [Monthly Price],
            yearly_price AS [Yearly Price],
            ISNULL(CAST(max_users AS NVARCHAR(10)), 'Unlimited') AS [Max Users],
            ISNULL(CAST(max_assets AS NVARCHAR(10)), 'Unlimited') AS [Max Assets],
            CASE WHEN is_active = 1 THEN 'Active' ELSE 'Inactive' END AS [Status]
        FROM SubscriptionPlan
        ORDER BY monthly_price;
    END
END
ELSE
BEGIN
    PRINT '✗ SubscriptionPlan table NOT FOUND';
END

PRINT '';

-- Check CompanySubscription table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanySubscription')
BEGIN
    DECLARE @SubCount INT;
    SELECT @SubCount = COUNT(*) FROM CompanySubscription;
    PRINT '✓ CompanySubscription table exists';
    PRINT '  Subscriptions found: ' + CAST(@SubCount AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '✗ CompanySubscription table NOT FOUND';
END

PRINT '';

-- Check SuperAdmin role
IF EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'SuperAdmin')
BEGIN
    PRINT '✓ SuperAdmin role exists';
    
    -- Check for SuperAdmin users
    DECLARE @SuperAdminCount INT;
    SELECT @SuperAdminCount = COUNT(*)
    FROM AspNetUsers u
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE r.Name = 'SuperAdmin';
    
    PRINT '  SuperAdmin users: ' + CAST(@SuperAdminCount AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '✗ SuperAdmin role NOT FOUND';
END

PRINT '';

-- Check if AspNetUsers.CompanyId is nullable
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('AspNetUsers') 
    AND name = 'CompanyId' 
    AND is_nullable = 1
)
BEGIN
    PRINT '✓ AspNetUsers.CompanyId is nullable (SuperAdmin support enabled)';
END
ELSE
BEGIN
    PRINT '✗ AspNetUsers.CompanyId is NOT nullable';
END

PRINT '';
PRINT '==================================================';
PRINT 'Verification Complete!';
PRINT '==================================================';
