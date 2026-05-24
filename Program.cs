using IT15_Project.Configuration;
using IT15_Project.Data;
using IT15_Project.Models;
using IT15_Project.Services;
using IT15_Project.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

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

// Register PMGovernanceService for PM lifecycle governance and duplicate prevention
builder.Services.AddScoped<IPMGovernanceService, PMGovernanceService>();

// Register ArchiveService for soft archival of operational records
builder.Services.AddScoped<IT15_Project.Services.Archiving.IArchiveService, 
    IT15_Project.Services.Archiving.ArchiveService>();

// Register PartsService for staged parts usage workflow
builder.Services.AddScoped<IT15_Project.Services.Parts.IPartsService, 
    IT15_Project.Services.Parts.PartsService>();

// Register SubscriptionService for SaaS subscription management and enforcement
builder.Services.AddScoped<SubscriptionService>();

// Register WorkOrderPdfService for PDF export functionality
builder.Services.AddScoped<IWorkOrderPdfService, WorkOrderPdfService>();

// ============================================================
// EMAIL INFRASTRUCTURE
// ============================================================
// Register EmailSettings from configuration
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Register Email Template Service for reusable HTML templates
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();

// Register EmailService for SMTP functionality
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Email Confirmation Service for registration flow
builder.Services.AddScoped<IEmailConfirmationService, EmailConfirmationService>();
// ============================================================

// ============================================================
// SECURITY & ANTI-ABUSE SERVICES
// ============================================================
// Register Turnstile Settings from configuration
builder.Services.Configure<TurnstileSettings>(
    builder.Configuration.GetSection("Turnstile"));

// Register Turnstile Validation Service for CAPTCHA protection
builder.Services.AddScoped<IT15_Project.Services.Security.ITurnstileValidationService, 
    IT15_Project.Services.Security.TurnstileValidationService>();

// Register HttpClient for Turnstile API calls
builder.Services.AddHttpClient();

// Add Rate Limiting middleware for anti-abuse protection
builder.Services.AddRateLimiter(options =>
{
    // Login endpoint rate limiting
    options.AddFixedWindowLimiter("login", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
        limiterOptions.Window = TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("RateLimiting:Login:WindowSeconds", 60));
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // No queueing, reject immediately
    });

    // Forgot Password endpoint rate limiting
    options.AddFixedWindowLimiter("forgotPassword", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:ForgotPassword:PermitLimit", 3);
        limiterOptions.Window = TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("RateLimiting:ForgotPassword:WindowSeconds", 300));
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Registration endpoint rate limiting
    options.AddFixedWindowLimiter("registration", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:Registration:PermitLimit", 3);
        limiterOptions.Window = TimeSpan.FromSeconds(
            builder.Configuration.GetValue<int>("RateLimiting:Registration:WindowSeconds", 3600));
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Global rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", cancellationToken);
    };
});
// ============================================================

// Identity with Roles support using ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ============================================================
// EXTERNAL AUTHENTICATION PROVIDERS
// ============================================================
// Add Google OAuth authentication (optional - only if credentials are configured)
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(googleOptions =>
        {
            // Load Google OAuth credentials from configuration
            // Development: appsettings.Development.json or user secrets
            // Production: Environment variables or appsettings.Production.json
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
            
            // Callback path for OAuth redirect (default: /signin-google)
            // Must match Google Cloud Console authorized redirect URI
            googleOptions.CallbackPath = "/signin-google";
            
            // Request email and profile scopes
            googleOptions.Scope.Add("email");
            googleOptions.Scope.Add("profile");
            
            // Save tokens for future API calls (optional)
            googleOptions.SaveTokens = true;
        });
}
// ============================================================

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

// Enable rate limiting middleware
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
