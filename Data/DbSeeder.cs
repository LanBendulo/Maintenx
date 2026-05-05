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
        /// Seeds roles, admin user, and admin personnel record
        /// Call this method in Program.cs after app.Build()
        /// </summary>
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Define roles
            string[] roleNames = { "Admin", "Manager", "Technician", "Requester" };

            // Create roles if they don't exist
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    await roleManager.CreateAsync(role);
                }
            }

            // Create admin user and personnel
            await CreateAdminUserAsync(userManager, context);
        }

        /// <summary>
        /// Creates the default admin user and personnel record if they don't exist
        /// </summary>
        private static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            var adminEmail = "admin@maintenx.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // Create user account
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    
                    // Create personnel record
                    var adminPersonnel = new Personnel
                    {
                        UserId = adminUser.Id,
                        FirstName = "System",
                        LastName = "Administrator",
                        Role = "Admin",
                        SkillSet = "System Administration",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    context.Personnel.Add(adminPersonnel);
                    await context.SaveChangesAsync();

                    Console.WriteLine($"Admin user and personnel created: {adminEmail}");
                }
                else
                {
                    Console.WriteLine("Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"- {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists.");
                
                // Check if personnel record exists
                var personnelExists = await context.Personnel.AnyAsync(p => p.UserId == adminUser.Id);
                
                if (!personnelExists)
                {
                    Console.WriteLine("Creating missing personnel record for admin...");
                    var adminPersonnel = new Personnel
                    {
                        UserId = adminUser.Id,
                        FirstName = "System",
                        LastName = "Administrator",
                        Role = "Admin",
                        SkillSet = "System Administration",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    context.Personnel.Add(adminPersonnel);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Admin personnel record created.");
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
                var exists = await context.Categories.AnyAsync(c => c.CategoryName == categoryName);
                
                if (!exists)
                {
                    context.Categories.Add(new Category { CategoryName = categoryName });
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

            // Get category IDs
            var hvacCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "HVAC Systems");
            var electricalCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Electrical Equipment");
            var plumbingCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Plumbing Systems");
            var mechanicalCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Mechanical Equipment");
            var safetyCategory = await context.Categories.FirstOrDefaultAsync(c => c.CategoryName == "Safety Systems");

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
                    Status = "Operational",
                    PurchaseDate = new DateTime(2020, 3, 15)
                },
                new {
                    AssetName = "Air Handling Unit - 3rd Floor",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Mechanical Room - 3rd Floor",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2021, 6, 10)
                },
                new {
                    AssetName = "Main Electrical Panel - Building A",
                    CategoryId = electricalCategory.CategoryId,
                    Location = "Electrical Room - Ground Floor",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2019, 1, 20)
                },
                new {
                    AssetName = "Emergency Generator #1",
                    CategoryId = electricalCategory.CategoryId,
                    Location = "Generator Room - Basement",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2018, 11, 5)
                },
                new {
                    AssetName = "Water Pump - Main Supply",
                    CategoryId = plumbingCategory.CategoryId,
                    Location = "Pump Room - Basement",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2020, 8, 12)
                },
                new {
                    AssetName = "Boiler System #1",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Boiler Room - Basement",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2019, 9, 25)
                },
                new {
                    AssetName = "Elevator #1 - Main Building",
                    CategoryId = mechanicalCategory.CategoryId,
                    Location = "Main Building - Lobby",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2017, 4, 18)
                },
                new {
                    AssetName = "Fire Suppression System - Building A",
                    CategoryId = safetyCategory.CategoryId,
                    Location = "Building A - All Floors",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2018, 2, 10)
                },
                new {
                    AssetName = "Cooling Tower #1",
                    CategoryId = hvacCategory.CategoryId,
                    Location = "Rooftop - Building B",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2020, 5, 22)
                },
                new {
                    AssetName = "Compressor Unit - Workshop",
                    CategoryId = mechanicalCategory.CategoryId,
                    Location = "Workshop - Ground Floor",
                    Status = "Operational",
                    PurchaseDate = new DateTime(2021, 3, 8)
                }
            };

            foreach (var assetData in assets)
            {
                var exists = await context.Assets.AnyAsync(a => a.AssetName == assetData.AssetName);
                
                if (!exists)
                {
                    var asset = new Asset
                    {
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
