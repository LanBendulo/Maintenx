using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        // Enable retry on failure for transient errors (important for remote connections)
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        
        // Set command timeout for remote connections
        sqlServerOptions.CommandTimeout(60);
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ============================================================
// MULTI-TENANT SERVICES
// ============================================================
// Register HttpContextAccessor for tenant context
builder.Services.AddHttpContextAccessor();

// Register TenantService for company isolation
builder.Services.AddScoped<ITenantService, TenantService>();

// Register CostService for Work Order cost tracking
builder.Services.AddScoped<ICostService, CostService>();

// Register AssetStatusService for automated status lifecycle
builder.Services.AddScoped<AssetStatusService>();

// Register PreventiveMaintenanceGenerationService for automatic PM work order generation
builder.Services.AddScoped<PreventiveMaintenanceGenerationService>();
// ============================================================

// Identity with Roles support using ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ─── Test database connection and seed data ───────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Testing database connection...");
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Test connection with retry
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("✓ Database connection successful!");
            
            // Apply pending migrations
            logger.LogInformation("Checking for pending migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("✓ Migrations applied successfully!");
            
            // Seed roles, admin user, and admin personnel
            logger.LogInformation("Seeding roles and admin user...");
            await DbSeeder.SeedRolesAndAdminAsync(services);
            logger.LogInformation("✓ Roles and admin seeded successfully!");
            
            // Optional: Seed sample data (development only)
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Seeding development data...");
                await DbSeeder.SeedCategoriesAsync(services);
                await DbSeeder.SeedAssetsAsync(services);
                await DbSeeder.SeedSampleUsersAsync(services);
                await DbSeeder.SeedContractorPersonnelAsync(services);
                logger.LogInformation("✓ Development data seeded successfully!");
            }

            // ─── Execute PM Work Order Generation ───────────────────
            logger.LogInformation("Running PM work order generation...");
            var pmService = services.GetRequiredService<PreventiveMaintenanceGenerationService>();
            var pmResult = await pmService.GenerateDueWorkOrdersAsync();
            
            if (pmResult.Skipped)
            {
                logger.LogInformation("PM generation skipped: {Reason}", pmResult.Reason);
            }
            else
            {
                logger.LogInformation(
                    "✓ PM generation completed: {Success} generated, {Skipped} skipped, {Failed} failed",
                    pmResult.SuccessCount,
                    pmResult.SkippedCount,
                    pmResult.FailureCount
                );

                if (pmResult.HasErrors)
                {
                    foreach (var error in pmResult.Errors)
                    {
                        logger.LogWarning("PM generation error: {Error}", error);
                    }
                }
            }
        }
        else
        {
            logger.LogError("✗ Cannot connect to database. Please check:");
            logger.LogError("  1. Database server is accessible: db50508.databaseasp.net");
            logger.LogError("  2. Firewall allows outbound connections on port 1433");
            logger.LogError("  3. MonsterASP.NET database is active and running");
            logger.LogError("  4. Credentials are correct");
        }
    }
    catch (Microsoft.Data.SqlClient.SqlException sqlEx)
    {
        logger.LogError(sqlEx, "SQL Server connection error:");
        logger.LogError($"  Error Number: {sqlEx.Number}");
        logger.LogError($"  Error Message: {sqlEx.Message}");
        logger.LogError("");
        logger.LogError("TROUBLESHOOTING STEPS:");
        logger.LogError("  1. Verify MonsterASP.NET control panel shows database as 'Active'");
        logger.LogError("  2. Check if your IP address is whitelisted in MonsterASP.NET firewall");
        logger.LogError("  3. Try connecting with SQL Server Management Studio first");
        logger.LogError("  4. Contact MonsterASP.NET support if issue persists");
        logger.LogError("");
        logger.LogWarning("Application will continue but database features will not work.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unexpected error occurred during database initialization:");
        logger.LogWarning("Application will continue but database features may not work.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
