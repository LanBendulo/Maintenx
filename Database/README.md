# MaintenX Database Setup Guide

## Overview

This database schema is designed to work with **ASP.NET Core Identity** for user authentication and authorization. The custom `Users` and `Role` tables have been removed in favor of Identity's built-in tables.

## Database Tables

### ASP.NET Identity Tables (Managed Automatically)
- `AspNetUsers` - User accounts
- `AspNetRoles` - User roles (Admin, Manager, Technician, Requester)
- `AspNetUserRoles` - User-role assignments
- `AspNetUserClaims` - User claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - Authentication tokens
- `AspNetRoleClaims` - Role-based claims

### MaintenX Business Tables
- `Category` - Asset categories (HVAC, Electrical, etc.)
- `Asset` - Equipment and assets
- `Work_Order` - Maintenance work orders
- `Maintenance_Log` - Maintenance history
- `Maintenance_Schedule` - Preventive maintenance schedules
- `Spare_Part` - Inventory parts
- `Inventory_Transaction` - Parts inventory movements
- `WorkOrder_Parts` - Parts used in work orders
- `Maintenance_Cost` - Cost tracking

## Setup Instructions

### 1. Run Database Migrations

First, ensure your ASP.NET Identity tables are created:

```bash
dotnet ef database update
```

This will create all Identity tables automatically.

### 2. Run Schema Script

Execute the schema script to create business tables:

```bash
# Using SQL Server Management Studio (SSMS)
# Open and execute: Database/maintenx_schema.sql

# Or using sqlcmd
sqlcmd -S your-server -d master -i Database/maintenx_schema.sql
```

### 3. Seed Roles and Admin User

Add this code to your `Program.cs` file:

```csharp
// After var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        // Seed roles and admin user
        await DbSeeder.SeedRolesAndAdminAsync(services);
        
        // Optional: Seed sample users (development only)
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedSampleUsersAsync(services);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
```

### 4. Seed Business Data (Optional)

The `maintenx_seed.sql` file contains sample data for assets, parts, and inventory. 

**Important:** Before running this file, you need to:
1. Create users through the application or seeding
2. Get their user IDs from `AspNetUsers` table
3. Update the commented sections in `maintenx_seed.sql` with actual user IDs

```sql
-- Example: Get user IDs
SELECT Id, Email FROM AspNetUsers;

-- Then update the seed file with actual IDs
```

## Default Credentials

After seeding, you can login with:

**Admin Account:**
- Email: `admin@maintenx.com`
- Password: `Admin@123`

**Sample Accounts (Development):**
- Manager: `manager@maintenx.com` / `Manager@123`
- Technician: `technician1@maintenx.com` / `Tech@123`
- Requester: `requester@maintenx.com` / `User@123`

## User Roles

| Role | Description | Permissions |
|------|-------------|-------------|
| **Admin** | System administrator | Full access to all features |
| **Manager** | Facility manager | Create/assign work orders, view reports |
| **Technician** | Maintenance technician | Complete work orders, log maintenance |
| **Requester** | Regular user | Submit maintenance requests |

## Foreign Key Relationships

All user-related foreign keys now reference `AspNetUsers.Id` (NVARCHAR(450)):

- `Work_Order.assigned_to` → `AspNetUsers.Id`
- `Work_Order.created_by` → `AspNetUsers.Id`
- `Maintenance_Log.performed_by` → `AspNetUsers.Id`
- `Maintenance_Schedule.created_by` → `AspNetUsers.Id`

## Entity Framework Models

When creating your entity models, reference users like this:

```csharp
public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public int? AssetId { get; set; }
    
    // Foreign keys to AspNetUsers
    public string? AssignedTo { get; set; }
    public string? CreatedBy { get; set; }
    
    // Navigation properties
    public virtual IdentityUser? AssignedToUser { get; set; }
    public virtual IdentityUser? CreatedByUser { get; set; }
    public virtual Asset? Asset { get; set; }
    
    // Other properties...
}
```

## Troubleshooting

### Issue: Foreign key constraint errors

**Solution:** Ensure ASP.NET Identity tables exist before creating business tables.

```bash
dotnet ef database update
# Then run maintenx_schema.sql
```

### Issue: Cannot create admin user

**Solution:** Check password requirements in `Program.cs`:

```csharp
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
});
```

### Issue: User IDs are NULL in work orders

**Solution:** Always get user IDs from `AspNetUsers` table:

```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
// or
var user = await _userManager.GetUserAsync(User);
var userId = user.Id;
```

## Migration from Old Schema

If you have existing data in `dbo.Users` and `dbo.Role` tables:

1. **Backup your database**
2. Export user data
3. Create users in ASP.NET Identity
4. Map old user IDs to new Identity IDs
5. Update foreign keys in business tables
6. Drop old `Users` and `Role` tables

## Additional Resources

- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/sql-server/)
