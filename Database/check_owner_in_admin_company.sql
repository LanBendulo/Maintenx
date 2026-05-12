-- ============================================================
-- Check if there's an Owner role in the same company as admin123@gmail.com
-- ============================================================

PRINT '==================================================';
PRINT 'Checking for Owner role in admin123 company';
PRINT '==================================================';
PRINT '';

-- Step 1: Find admin123@gmail.com user and their company
PRINT 'Step 1: Finding admin123@gmail.com user details...';
PRINT '';

DECLARE @AdminUserId NVARCHAR(450);
DECLARE @AdminCompanyId INT;
DECLARE @AdminEmail NVARCHAR(256);

SELECT 
    @AdminUserId = Id,
    @AdminCompanyId = CompanyId,
    @AdminEmail = Email
FROM AspNetUsers
WHERE Email = 'admin123@gmail.com';

IF @AdminUserId IS NULL
BEGIN
    PRINT '❌ User admin123@gmail.com not found!';
    RETURN;
END

PRINT '✓ Found user:';
PRINT '  User ID: ' + @AdminUserId;
PRINT '  Email: ' + @AdminEmail;
PRINT '  Company ID: ' + CAST(ISNULL(@AdminCompanyId, 0) AS NVARCHAR(10));

IF @AdminCompanyId IS NULL
BEGIN
    PRINT '';
    PRINT '⚠ This user has no CompanyId (might be SuperAdmin)';
    PRINT '';
END
ELSE
BEGIN
    PRINT '';
    PRINT 'Company Details:';
    SELECT 
        company_id AS [Company ID],
        company_name AS [Company Name],
        is_active AS [Active]
    FROM Company
    WHERE company_id = @AdminCompanyId;
END

PRINT '';
PRINT '==================================================';
PRINT 'Step 2: Finding admin123 role...';
PRINT '==================================================';
PRINT '';

-- Find admin123's role
SELECT 
    u.Email,
    r.Name AS [Role],
    u.CompanyId AS [Company ID]
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin123@gmail.com';

PRINT '';
PRINT '==================================================';
PRINT 'Step 3: Checking for Owner role in same company...';
PRINT '==================================================';
PRINT '';

IF @AdminCompanyId IS NULL
BEGIN
    PRINT '⚠ Cannot check - admin123 has no company';
END
ELSE
BEGIN
    -- Find all users with Owner role in the same company
    SELECT 
        u.Email,
        u.FullName,
        r.Name AS [Role],
        u.CompanyId AS [Company ID],
        u.IsActive AS [Active],
        u.CreatedAt AS [Created At]
    FROM AspNetUsers u
    INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE u.CompanyId = @AdminCompanyId
      AND r.Name = 'Owner';

    IF @@ROWCOUNT = 0
    BEGIN
        PRINT '';
        PRINT '❌ NO OWNER ROLE FOUND in Company ID ' + CAST(@AdminCompanyId AS NVARCHAR(10));
        PRINT '';
        PRINT 'This company needs an Owner!';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT '✓ Owner role(s) found in the company';
    END
END

PRINT '';
PRINT '==================================================';
PRINT 'Step 4: All users in admin123 company...';
PRINT '==================================================';
PRINT '';

IF @AdminCompanyId IS NOT NULL
BEGIN
    SELECT 
        u.Email,
        u.FullName,
        r.Name AS [Role],
        u.IsActive AS [Active],
        u.CreatedAt AS [Created At]
    FROM AspNetUsers u
    LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
    LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
    WHERE u.CompanyId = @AdminCompanyId
    ORDER BY r.Name, u.Email;
END

PRINT '';
PRINT '==================================================';
PRINT 'Analysis Complete';
PRINT '==================================================';
