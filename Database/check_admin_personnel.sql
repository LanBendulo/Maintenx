-- Check if admin user has a personnel record
USE DB_Maintenx;
GO

-- Check all users
SELECT 
    u.Id AS UserId,
    u.Email,
    u.UserName,
    p.personnel_id,
    p.first_name,
    p.last_name,
    p.role
FROM dbo.AspNetUsers u
LEFT JOIN dbo.Personnel p ON u.Id = p.user_id
WHERE u.Email LIKE '%admin%'
ORDER BY u.Email;

-- Check all personnel records
SELECT 
    personnel_id,
    user_id,
    first_name,
    last_name,
    role,
    is_active
FROM dbo.Personnel
ORDER BY personnel_id;
