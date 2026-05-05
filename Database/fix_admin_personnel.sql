-- Fix: Add Personnel record for admin user if missing
USE DB_Maintenx;
GO

-- Find admin user ID
DECLARE @AdminUserId NVARCHAR(450);
SELECT @AdminUserId = Id FROM dbo.AspNetUsers WHERE Email = 'admin123@gmail.com';

-- If not found, try the other admin email
IF @AdminUserId IS NULL
BEGIN
    SELECT @AdminUserId = Id FROM dbo.AspNetUsers WHERE Email = 'admin@maintenx.com';
END

-- Check if admin has personnel record
IF @AdminUserId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Personnel WHERE user_id = @AdminUserId)
    BEGIN
        PRINT 'Admin user found but no personnel record. Creating one...';
        
        INSERT INTO dbo.Personnel (user_id, first_name, last_name, role, skill_set, is_active, created_at)
        VALUES (@AdminUserId, 'Admin', 'User', 'Admin', 'System Administration', 1, GETDATE());
        
        PRINT 'Personnel record created for admin user!';
    END
    ELSE
    BEGIN
        PRINT 'Admin user already has a personnel record.';
    END
END
ELSE
BEGIN
    PRINT 'ERROR: Admin user not found! Please check the email address.';
END
GO

-- Verify the fix
SELECT 
    u.Email,
    p.personnel_id,
    p.first_name,
    p.last_name,
    p.role
FROM dbo.AspNetUsers u
INNER JOIN dbo.Personnel p ON u.Id = p.user_id
WHERE u.Email LIKE '%admin%';
