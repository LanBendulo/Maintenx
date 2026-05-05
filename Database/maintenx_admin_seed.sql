-- =============================================================
--  MaintenX: Admin User Setup for ASP.NET Identity
--  Database: SQL Server (T-SQL)
--  
--  IMPORTANT: This file is for reference only.
--  ASP.NET Identity manages users and roles automatically.
--  
--  To create an admin user:
--  1. Register through the application UI
--  2. Use ASP.NET Identity UserManager in code
--  3. Or use the seeding approach in Program.cs/Startup.cs
-- =============================================================

USE DB_Maintenx;
GO

-- =============================================================
-- ASP.NET Identity Tables (created automatically by migrations):
-- - AspNetUsers
-- - AspNetRoles
-- - AspNetUserRoles
-- - AspNetUserClaims
-- - AspNetUserLogins
-- - AspNetUserTokens
-- - AspNetRoleClaims
-- =============================================================

-- =============================================================
-- Example: Create Admin Role and User using C# code
-- Add this to your Program.cs or a seeding service:
-- =============================================================

/*
using Microsoft.AspNetCore.Identity;

public static async Task SeedAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Create roles
    string[] roleNames = { "Admin", "Manager", "Technician", "Requester" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Create admin user
    var adminEmail = "admin@maintenx.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// Call in Program.cs after app.Build():
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     await SeedAdminUser(services);
// }
*/

-- =============================================================
-- To verify users and roles after seeding:
-- =============================================================

-- View all users
-- SELECT * FROM AspNetUsers;

-- View all roles
-- SELECT * FROM AspNetRoles;

-- View user-role assignments
-- SELECT 
--     u.Email,
--     r.Name as RoleName
-- FROM AspNetUsers u
-- INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
-- INNER JOIN AspNetRoles r ON ur.RoleId = r.Id;

-- =============================================================
-- End of reference file
-- =============================================================
