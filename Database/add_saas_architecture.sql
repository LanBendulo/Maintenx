-- ============================================================
-- SaaS Architecture Migration
-- Adds SuperAdmin role, SubscriptionPlan, and CompanySubscription tables
-- Makes ApplicationUser.CompanyId nullable for SuperAdmin support
-- ============================================================

-- Step 1: Create SubscriptionPlan table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionPlan')
BEGIN
    CREATE TABLE SubscriptionPlan (
        plan_id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(500),
        monthly_price DECIMAL(10,2) NOT NULL DEFAULT 0,
        yearly_price DECIMAL(10,2) NOT NULL DEFAULT 0,
        max_users INT,
        max_assets INT,
        max_work_orders_per_month INT,
        features_json NVARCHAR(MAX),
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME
    );
    PRINT 'SubscriptionPlan table created successfully';
END
ELSE
BEGIN
    PRINT 'SubscriptionPlan table already exists';
END
GO

-- Step 2: Create CompanySubscription table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanySubscription')
BEGIN
    CREATE TABLE CompanySubscription (
        subscription_id INT IDENTITY(1,1) PRIMARY KEY,
        company_id INT NOT NULL,
        plan_id INT NOT NULL,
        start_date DATETIME NOT NULL,
        end_date DATETIME NOT NULL,
        is_trial BIT NOT NULL DEFAULT 0,
        is_active BIT NOT NULL DEFAULT 1,
        payment_status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        external_payment_id NVARCHAR(200),
        last_payment_date DATETIME,
        created_at DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME,
        CONSTRAINT FK_CompanySubscription_Company FOREIGN KEY (company_id) REFERENCES Company(company_id),
        CONSTRAINT FK_CompanySubscription_Plan FOREIGN KEY (plan_id) REFERENCES SubscriptionPlan(plan_id)
    );
    PRINT 'CompanySubscription table created successfully';
END
ELSE
BEGIN
    PRINT 'CompanySubscription table already exists';
END
GO

-- Step 3: Make AspNetUsers.CompanyId nullable for SuperAdmin support
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('AspNetUsers') 
    AND name = 'CompanyId' 
    AND is_nullable = 0
)
BEGIN
    -- Drop foreign key constraint temporarily
    DECLARE @ConstraintName NVARCHAR(200);
    SELECT @ConstraintName = name 
    FROM sys.foreign_keys 
    WHERE parent_object_id = OBJECT_ID('AspNetUsers') 
    AND referenced_object_id = OBJECT_ID('Company');
    
    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE AspNetUsers DROP CONSTRAINT ' + @ConstraintName);
        PRINT 'Dropped FK constraint: ' + @ConstraintName;
    END

    -- Alter column to nullable
    ALTER TABLE AspNetUsers ALTER COLUMN CompanyId INT NULL;
    PRINT 'AspNetUsers.CompanyId is now nullable';

    -- Recreate foreign key constraint
    ALTER TABLE AspNetUsers 
    ADD CONSTRAINT FK_AspNetUsers_Company 
    FOREIGN KEY (CompanyId) REFERENCES Company(company_id);
    PRINT 'Recreated FK constraint: FK_AspNetUsers_Company';
END
ELSE
BEGIN
    PRINT 'AspNetUsers.CompanyId is already nullable';
END
GO

-- Step 4: Create SuperAdmin role
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'SuperAdmin')
BEGIN
    DECLARE @SuperAdminRoleId NVARCHAR(450) = NEWID();
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (@SuperAdminRoleId, 'SuperAdmin', 'SUPERADMIN', NEWID());
    PRINT 'SuperAdmin role created successfully';
END
ELSE
BEGIN
    PRINT 'SuperAdmin role already exists';
END
GO

-- Step 5: Seed default subscription plans
IF NOT EXISTS (SELECT * FROM SubscriptionPlan WHERE name = 'Starter')
BEGIN
    INSERT INTO SubscriptionPlan (name, description, monthly_price, yearly_price, max_users, max_assets, max_work_orders_per_month, is_active)
    VALUES 
    ('Starter', 'Perfect for small teams getting started with maintenance management', 999.00, 9990.00, 5, 50, 100, 1),
    ('Professional', 'Ideal for growing businesses with advanced needs', 2499.00, 24990.00, 20, 200, 500, 1),
    ('Enterprise', 'Complete solution for large organizations', 4999.00, 49990.00, NULL, NULL, NULL, 1);
    PRINT 'Default subscription plans seeded successfully';
END
ELSE
BEGIN
    PRINT 'Subscription plans already exist';
END
GO

-- Step 6: Optional - Create initial SuperAdmin user
-- To create a SuperAdmin user, uncomment and modify the section below
-- Then run this script manually or use Identity tools to create the user

/*
-- Example SuperAdmin user creation (COMMENTED OUT - MODIFY BEFORE USE)
DECLARE @SuperAdminEmail NVARCHAR(256) = 'superadmin@maintenx.com';
DECLARE @SuperAdminUserId NVARCHAR(450);

IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = @SuperAdminEmail)
BEGIN
    SET @SuperAdminUserId = NEWID();
    
    -- Create SuperAdmin user (CompanyId = NULL)
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        CompanyId, FullName
    )
    VALUES (
        @SuperAdminUserId,
        @SuperAdminEmail,
        UPPER(@SuperAdminEmail),
        @SuperAdminEmail,
        UPPER(@SuperAdminEmail),
        1,
        'AQAAAAIAAYagAAAAEJ8xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',
        NEWID(),
        NEWID(),
        0,
        0,
        1,
        0,
        NULL,
        'Super Administrator'
    );

    -- Assign SuperAdmin role
    DECLARE @SuperAdminRoleId NVARCHAR(450);
    SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';
    
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@SuperAdminUserId, @SuperAdminRoleId);
    
    PRINT 'SuperAdmin user created successfully';
END
*/

-- Step 7: Verification queries
PRINT '============================================================';
PRINT 'VERIFICATION RESULTS:';
PRINT '============================================================';

SELECT COUNT(*) AS SubscriptionPlanCount FROM SubscriptionPlan;
SELECT COUNT(*) AS CompanySubscriptionCount FROM CompanySubscription;
SELECT COUNT(*) AS SuperAdminRoleCount FROM AspNetRoles WHERE Name = 'SuperAdmin';
SELECT COUNT(*) AS SuperAdminUserCount FROM AspNetUsers WHERE CompanyId IS NULL;

PRINT '============================================================';
PRINT 'SaaS Architecture Migration Completed Successfully!';
PRINT '============================================================';
