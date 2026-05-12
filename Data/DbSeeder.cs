using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IT15_Project.Models;

namespace IT15_Project.Data
{
    /// <summary>
    /// Database seeder for creating initial roles, users, and personnel
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Seeds roles, test company, and role-based test accounts
        /// Call this method in Program.cs after app.Build()
        /// </summary>
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Define roles (RBAC structure + SuperAdmin + Supervisor)
            string[] roleNames = { "SuperAdmin", "Owner", "Admin", "Supervisor", "Technician", "User" };

            // Create roles if they don't exist
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                    Console.WriteLine($"Role created: {roleName}");
                }
            }

            // Seed SuperAdmin account (platform-level, CompanyId = null)
            await SeedSuperAdminAsync(userManager);

            // Ensure test company exists
            await EnsureTestCompanyAsync(context);

            // Create test accounts for each role
            await CreateRoleTestAccountsAsync(userManager, context);
        }

        /// <summary>
        /// Seeds the initial SuperAdmin account for platform management
        /// SuperAdmin has CompanyId = null and manages the entire SaaS platform
        /// </summary>
        private static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
        {
            const string superAdminEmail = "superadmin@maintenx.com";
            const string superAdminPassword = "SuperAdmin123!";

            // Check if SuperAdmin already exists
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                // Create SuperAdmin user with CompanyId = null
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    CompanyId = null, // CRITICAL: SuperAdmin is NOT tenant-scoped
                    FullName = "Super Administrator",
                    LockoutEnabled = false // SuperAdmin cannot be locked out
                };

                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);

                if (result.Succeeded)
                {
                    // Assign SuperAdmin role
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                    
                    Console.WriteLine("════════════════════════════════════════════════════════");
                    Console.WriteLine("✓ SUPERADMIN ACCOUNT CREATED");
                    Console.WriteLine("════════════════════════════════════════════════════════");
                    Console.WriteLine($"  Email:    {superAdminEmail}");
                    Console.WriteLine($"  Password: {superAdminPassword}");
                    Console.WriteLine($"  Role:     SuperAdmin");
                    Console.WriteLine($"  CompanyId: NULL (Platform-level access)");
                    Console.WriteLine("════════════════════════════════════════════════════════");
                    Console.WriteLine("  IMPORTANT: Change this password after first login!");
                    Console.WriteLine("  Access:    /superadmin/dashboard");
                    Console.WriteLine("════════════════════════════════════════════════════════");
                }
                else
                {
                    Console.WriteLine("✗ Failed to create SuperAdmin account:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"SuperAdmin account already exists: {superAdminEmail}");
                
                // Verify SuperAdmin has correct configuration
                if (superAdmin.CompanyId.HasValue)
                {
                    Console.WriteLine("⚠ WARNING: SuperAdmin has CompanyId set. This should be NULL!");
                    Console.WriteLine("  Please fix this manually in the database.");
                }
            }
        }

        /// <summary>
        /// Ensures test company exists for seeding
        /// </summary>
        private static async Task EnsureTestCompanyAsync(ApplicationDbContext context)
        {
            var testCompany = await context.Companies.FirstOrDefaultAsync(c => c.CompanyId == 1);
            
            if (testCompany == null)
            {
                testCompany = new Company
                {
                    CompanyName = "Demo Company",
                    SubscriptionPlan = "Enterprise",
                    SubscriptionExpiry = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ContactEmail = "admin@democompany.com",
                    MaxUsers = 100,
                    MaxAssets = 1000
                };

                context.Companies.Add(testCompany);
                await context.SaveChangesAsync();
                Console.WriteLine($"Test company created: {testCompany.CompanyName} (ID: {testCompany.CompanyId})");
            }
            else
            {
                Console.WriteLine($"Test company already exists: {testCompany.CompanyName} (ID: {testCompany.CompanyId})");
            }
        }

        /// <summary>
        /// Creates test accounts for each role with proper CompanyId assignment
        /// </summary>
        private static async Task CreateRoleTestAccountsAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var testCompanyId = 1; // Demo Company

            var testAccounts = new[]
            {
                new { 
                    Email = "owner@test.com", 
                    Password = "Owner@123", 
                    Role = "Owner",
                    FullName = "Sarah Johnson",
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    SkillSet = "Business Management, Operations",
                    HourlyRate = 0m
                },
                new { 
                    Email = "admin@test.com", 
                    Password = "Admin@123", 
                    Role = "Admin",
                    FullName = "Michael Chen",
                    FirstName = "Michael",
                    LastName = "Chen",
                    SkillSet = "System Administration, IT Management",
                    HourlyRate = 0m
                },
                new { 
                    Email = "supervisor@test.com", 
                    Password = "Supervisor@123", 
                    Role = "Supervisor",
                    FullName = "Lisa Anderson",
                    FirstName = "Lisa",
                    LastName = "Anderson",
                    SkillSet = "Operations Management, Quality Control",
                    HourlyRate = 38.00m
                },
                new { 
                    Email = "technician@test.com", 
                    Password = "Tech@123", 
                    Role = "Technician",
                    FullName = "David Martinez",
                    FirstName = "David",
                    LastName = "Martinez",
                    SkillSet = "HVAC, Electrical, Plumbing, Mechanical",
                    HourlyRate = 45.00m
                },
                new { 
                    Email = "technician123@test.com", 
                    Password = "Tech@123", 
                    Role = "Technician",
                    FullName = "James Wilson",
                    FirstName = "James",
                    LastName = "Wilson",
                    SkillSet = "Electrical, Electronics, Automation",
                    HourlyRate = 42.00m
                },
                new { 
                    Email = "user@test.com", 
                    Password = "User@123", 
                    Role = "User",
                    FullName = "Emily Rodriguez",
                    FirstName = "Emily",
                    LastName = "Rodriguez",
                    SkillSet = "",
                    HourlyRate = 0m
                }
            };

            foreach (var accountData in testAccounts)
            {
                var user = await userManager.FindByEmailAsync(accountData.Email);
                
                if (user == null)
                {
                    // Create user account with CompanyId
                    user = new ApplicationUser
                    {
                        UserName = accountData.Email,
                        Email = accountData.Email,
                        EmailConfirmed = true,
                        CompanyId = testCompanyId,
                        FullName = accountData.FullName
                    };

                    var result = await userManager.CreateAsync(user, accountData.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, accountData.Role);
                        
                        // Create personnel record
                        var personnel = new Personnel
                        {
                            CompanyId = testCompanyId,
                            UserId = user.Id,
                            FirstName = accountData.FirstName,
                            LastName = accountData.LastName,
                            Role = accountData.Role,
                            SkillSet = accountData.SkillSet,
                            HourlyRate = accountData.HourlyRate > 0 ? accountData.HourlyRate : null,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        context.Personnel.Add(personnel);
                        await context.SaveChangesAsync();

                        Console.WriteLine($"✓ Test account created: {accountData.Email} | Role: {accountData.Role} | Password: {accountData.Password}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed to create {accountData.Email}:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"  - {error.Description}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Account already exists: {accountData.Email}");
                }
            }
        }

        /// <summary>
        /// Seeds sample users and personnel for development/testing
        /// Only call this in development environment
        /// </summary>
        public static async Task SeedSampleUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var sampleUsers = new[]
            {
                new { 
                    Email = "manager@maintenx.com", 
                    Password = "Manager@123", 
                    Role = "Manager",
                    FirstName = "Maria",
                    LastName = "Santos",
                    SkillSet = "Management, Planning, Budgeting",
                    HourlyRate = 50.00m
                },
                new { 
                    Email = "technician1@maintenx.com", 
                    Password = "Tech@123", 
                    Role = "Technician",
                    FirstName = "Juan",
                    LastName = "Dela Cruz",
                    SkillSet = "HVAC, Electrical, Plumbing",
                    HourlyRate = 35.00m
                },
                new { 
                    Email = "technician2@maintenx.com", 
                    Password = "Tech@123", 
                    Role = "Technician",
                    FirstName = "Carlo",
                    LastName = "Reyes",
                    SkillSet = "Mechanical, Welding, Fabrication",
                    HourlyRate = 32.00m
                },
                new { 
                    Email = "requester@maintenx.com", 
                    Password = "User@123", 
                    Role = "Requester",
                    FirstName = "Ana",
                    LastName = "Lim",
                    SkillSet = "",
                    HourlyRate = 0m
                }
            };

            foreach (var userData in sampleUsers)
            {
                var user = await userManager.FindByEmailAsync(userData.Email);
                
                if (user == null)
                {
                    // Create user account
                    user = new ApplicationUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, userData.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);
                        
                        // Create personnel record
                        var personnel = new Personnel
                        {
                            UserId = user.Id,
                            FirstName = userData.FirstName,
                            LastName = userData.LastName,
                            Role = userData.Role,
                            SkillSet = userData.SkillSet,
                            HourlyRate = userData.HourlyRate > 0 ? userData.HourlyRate : null,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        context.Personnel.Add(personnel);
                        await context.SaveChangesAsync();

                        Console.WriteLine($"Sample user and personnel created: {userData.Email} ({userData.Role})");
                    }
                }
            }
        }

        /// <summary>
        /// Seeds sample personnel WITHOUT user accounts (contractors, external workers)
        /// </summary>
        public static async Task SeedContractorPersonnelAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var contractors = new[]
            {
                new {
                    FirstName = "Roberto",
                    LastName = "Garcia",
                    Role = "Contractor",
                    SkillSet = "Electrical, Generator Maintenance",
                    HourlyRate = 45.00m
                },
                new {
                    FirstName = "Elena",
                    LastName = "Fernandez",
                    Role = "Contractor",
                    SkillSet = "HVAC Specialist",
                    HourlyRate = 48.00m
                },
                new {
                    FirstName = "Miguel",
                    LastName = "Torres",
                    Role = "External Technician",
                    SkillSet = "Elevator Maintenance, Safety Systems",
                    HourlyRate = 55.00m
                }
            };

            foreach (var contractor in contractors)
            {
                // Check if contractor already exists
                var exists = await context.Personnel
                    .AnyAsync(p => p.FirstName == contractor.FirstName && p.LastName == contractor.LastName);

                if (!exists)
                {
                    var personnel = new Personnel
                    {
                        UserId = null, // No user account
                        FirstName = contractor.FirstName,
                        LastName = contractor.LastName,
                        Role = contractor.Role,
                        SkillSet = contractor.SkillSet,
                        HourlyRate = contractor.HourlyRate,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    context.Personnel.Add(personnel);
                    Console.WriteLine($"Contractor personnel created: {contractor.FirstName} {contractor.LastName} (No user account)");
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a personnel ID by user email - useful for seeding related data
        /// </summary>
        public static async Task<int?> GetPersonnelIdByEmailAsync(ApplicationDbContext context, string email)
        {
            var personnel = await context.Personnel
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.User != null && p.User.Email == email);
            
            return personnel?.PersonnelId;
        }

        /// <summary>
        /// Seeds sample categories for assets
        /// </summary>
        public static async Task SeedCategoriesAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var testCompanyId = 1; // Demo Company

            var categories = new[]
            {
                "HVAC Systems",
                "Electrical Equipment",
                "Plumbing Systems",
                "Mechanical Equipment",
                "Safety Systems",
                "Building Infrastructure",
                "IT Equipment",
                "Vehicles"
            };

            foreach (var categoryName in categories)
            {
                var exists = await context.Categories
                    .AnyAsync(c => c.CategoryName == categoryName && c.CompanyId == testCompanyId);
                
                if (!exists)
                {
                    context.Categories.Add(new Category 
                    { 
                        CategoryName = categoryName,
                        CompanyId = testCompanyId
                    });
                    Console.WriteLine($"Category created: {categoryName}");
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds sample assets for testing work orders
        /// </summary>
        public static async Task SeedAssetsAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var testCompanyId = 1; // Demo Company

            // Get category IDs
            var hvacCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == "HVAC Systems" && c.CompanyId == testCompanyId);
            var electricalCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == "Electrical Equipment" && c.CompanyId == testCompanyId);
            var plumbingCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == "Plumbing Systems" && c.CompanyId == testCompanyId);
            var mechanicalCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == "Mechanical Equipment" && c.CompanyId == testCompanyId);
            var safetyCategory = await context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == "Safety Systems" && c.CompanyId == testCompanyId);

            if (hvacCategory == null || electricalCategory == null || plumbingCategory == null || 
                mechanicalCategory == null || safetyCategory == null)
            {
                Console.WriteLine("Categories not found. Please seed categories first.");
                return;
            }

            var assets = new[]
            {
                new {
                    AssetName = "Chiller Unit #1 - Building A",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Rooftop - Building A",
                    Status = "Active",
                    PurchaseDate = new DateTime(2020, 3, 15)
                },
                new {
                    AssetName = "Air Handling Unit - 3rd Floor",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Mechanical Room - 3rd Floor",
                    Status = "Active",
                    PurchaseDate = new DateTime(2021, 6, 10)
                },
                new {
                    AssetName = "Main Electrical Panel - Building A",
                    CategoryId = electricalCategory.CategoryId,
                    Location = "Electrical Room - Ground Floor",
                    Status = "Active",
                    PurchaseDate = new DateTime(2019, 1, 20)
                },
                new {
                    AssetName = "Emergency Generator #1",
                    CategoryId = electricalCategory.CategoryId,
                    Location = "Generator Room - Basement",
                    Status = "Active",
                    PurchaseDate = new DateTime(2018, 11, 5)
                },
                new {
                    AssetName = "Water Pump - Main Supply",
                    CategoryId = plumbingCategory.CategoryId,
                    Location = "Pump Room - Basement",
                    Status = "Active",
                    PurchaseDate = new DateTime(2020, 8, 12)
                },
                new {
                    AssetName = "Boiler System #1",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Boiler Room - Basement",
                    Status = "Active",
                    PurchaseDate = new DateTime(2019, 9, 25)
                },
                new {
                    AssetName = "Elevator #1 - Main Building",
                    CategoryId = mechanicalCategory.CategoryId,
                    Location = "Main Building - Lobby",
                    Status = "Active",
                    PurchaseDate = new DateTime(2017, 4, 18)
                },
                new {
                    AssetName = "Fire Suppression System - Building A",
                    CategoryId = safetyCategory.CategoryId,
                    Location = "Building A - All Floors",
                    Status = "Active",
                    PurchaseDate = new DateTime(2018, 2, 10)
                },
                new {
                    AssetName = "Cooling Tower #1",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Rooftop - Building B",
                    Status = "Active",
                    PurchaseDate = new DateTime(2020, 5, 22)
                },
                new {
                    AssetName = "Compressor Unit - Workshop",
                    CategoryId = mechanicalCategory.CategoryId,
                    Location = "Workshop - Ground Floor",
                    Status = "Active",
                    PurchaseDate = new DateTime(2021, 3, 8)
                }
            };

            foreach (var assetData in assets)
            {
                var exists = await context.Assets
                    .AnyAsync(a => a.AssetName == assetData.AssetName && a.CompanyId == testCompanyId);
                
                if (!exists)
                {
                    var asset = new Asset
                    {
                        CompanyId = testCompanyId,
                        AssetName = assetData.AssetName,
                        CategoryId = assetData.CategoryId,
                        Location = assetData.Location,
                        Status = assetData.Status,
                        PurchaseDate = assetData.PurchaseDate
                    };

                    context.Assets.Add(asset);
                    Console.WriteLine($"Asset created: {assetData.AssetName}");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
