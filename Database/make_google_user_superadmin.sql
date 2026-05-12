-- ═══════════════════════════════════════════════════════════════════════════════
-- Make Google Account SuperAdmin
-- Email: n.bendulo.546481@umindanao.edu.ph
-- This script assigns SuperAdmin role to the Google OAuth user
-- ═══════════════════════════════════════════════════════════════════════════════

USE db50508;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Step 1: Find the user by email
DECLARE @UserId NVARCHAR(450);
DECLARE @Email NVARCHAR(256) = 'n.bendulo.546481@umindanao.edu.ph';

SELECT @UserId = Id 
FROM AspNetUsers 
WHERE NormalizedEmail = UPPER(@Email);

IF @UserId IS NULL
BEGIN
    PRINT 'ERROR: User with email ' + @Email + ' not found.';
    PRINT 'The user must log in with Google OAuth at least once before running this script.';
END
ELSE
BEGIN
    PRINT 'Found user: ' + @Email;
    PRINT 'User ID: ' + @UserId;

    -- Step 2: Get SuperAdmin role ID
    DECLARE @SuperAdminRoleId NVARCHAR(450);
    
    SELECT @SuperAdminRoleId = Id 
    FROM AspNetRoles 
    WHERE NormalizedName = 'SUPERADMIN';

    IF @SuperAdminRoleId IS NULL
    BEGIN
        PRINT 'ERROR: SuperAdmin role not found. Creating it now...';
        
        -- Create SuperAdmin role if it doesn't exist
        SET @SuperAdminRoleId = NEWID();
        
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@SuperAdminRoleId, 'SuperAdmin', 'SUPERADMIN', NEWID());
        
        PRINT 'SuperAdmin role created with ID: ' + @SuperAdminRoleId;
    END
    ELSE
    BEGIN
        PRINT 'SuperAdmin role ID: ' + @SuperAdminRoleId;
    END

    -- Step 3: Check if user already has SuperAdmin role
    IF EXISTS (
        SELECT 1 
        FROM AspNetUserRoles 
        WHERE UserId = @UserId AND RoleId = @SuperAdminRoleId
    )
    BEGIN
        PRINT 'User already has SuperAdmin role.';
    END
    ELSE
    BEGIN
        -- Step 4: Remove any existing roles
        DELETE FROM AspNetUserRoles WHERE UserId = @UserId;
        PRINT 'Removed existing roles for user.';

        -- Step 5: Assign SuperAdmin role
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@UserId, @SuperAdminRoleId);
        
        PRINT 'SUCCESS: SuperAdmin role assigned to ' + @Email;
    END

    -- Step 6: Verify the assignment
    SELECT 
        u.Email,
        u.UserName,
        r.Name AS RoleName,
        u.EmailConfirmed,
        u.LockoutEnabled
    FROM AspNetUsers u
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE u.Id = @UserId;

    PRINT '';
    PRINT '═══════════════════════════════════════════════════════════';
    PRINT 'COMPLETED: User ' + @Email + ' is now a SuperAdmin';
    PRINT '═══════════════════════════════════════════════════════════';
END
GO
