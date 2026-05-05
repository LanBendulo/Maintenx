# MaintenX Setup Instructions

## 🎯 Current Status

✅ **Database schema updated** - Personnel table added with proper separation  
✅ **C# models created** - Personnel, ApplicationUser, WorkOrder updated  
✅ **Controllers updated** - DashboardController now uses Personnel  
✅ **DbSeeder updated** - Creates Personnel records with user accounts  
✅ **Architecture documented** - See `Database/PERSONNEL_ARCHITECTURE.md`

## 📋 Next Steps to Get Running

### Step 1: Create Database Migration

Since we changed the database structure, you need to create a new migration:

```bash
# Add migration for Personnel table
dotnet ef migrations add AddPersonnelTable

# Update database
dotnet ef database update
```

**What this does:**
- Creates the `Personnel` table
- Updates `Work_Order` foreign keys from `NVARCHAR(450)` to `INT`
- Updates `Maintenance_Log` and `Maintenance_Schedule` foreign keys

### Step 2: Update Program.cs

Add the seeding code to your `Program.cs`:

```csharp
// After: var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    try
    {
        // Seed roles, admin user, and admin personnel
        await DbSeeder.SeedRolesAndAdminAsync(services);
        
        // Optional: Seed sample data (development only)
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedSampleUsersAsync(services);
            await DbSeeder.SeedContractorPersonnelAsync(services);
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

### Step 3: Run the Application

```bash
dotnet run
```

### Step 4: Verify Setup

1. **Check database tables:**
   ```sql
   -- Should see Personnel table
   SELECT * FROM Personnel;
   
   -- Should see admin personnel record
   SELECT p.*, u.Email 
   FROM Personnel p
   LEFT JOIN AspNetUsers u ON p.user_id = u.Id;
   ```

2. **Login as admin:**
   - Email: `admin@maintenx.com`
   - Password: `Admin@123`

3. **Navigate to Work Orders:**
   - Click "Work Orders" in sidebar
   - Click "Create Work Order"
   - Technician dropdown should load from Personnel table

## 🔧 Troubleshooting

### Issue: Migration fails with foreign key errors

**Solution:** You may need to drop and recreate the database if you have existing data:

```bash
# WARNING: This deletes all data
dotnet ef database drop
dotnet ef database update
```

Or manually update the schema:

```sql
-- 1. Drop old foreign keys
ALTER TABLE Work_Order DROP CONSTRAINT FK_WorkOrder_AssignedTo;
ALTER TABLE Work_Order DROP CONSTRAINT FK_WorkOrder_CreatedBy;

-- 2. Change column types
ALTER TABLE Work_Order ALTER COLUMN assigned_to INT NULL;
ALTER TABLE Work_Order ALTER COLUMN created_by INT NULL;

-- 3. Add new foreign keys
ALTER TABLE Work_Order ADD CONSTRAINT FK_WorkOrder_AssignedTo
    FOREIGN KEY (assigned_to) REFERENCES Personnel(personnel_id);
    
ALTER TABLE Work_Order ADD CONSTRAINT FK_WorkOrder_CreatedBy
    FOREIGN KEY (created_by) REFERENCES Personnel(personnel_id);
```

### Issue: "Current user does not have a personnel record"

**Solution:** Make sure every user has a corresponding Personnel record:

```csharp
// Check if user has personnel record
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var personnel = await context.Personnel.FirstOrDefaultAsync(p => p.UserId == userId);

if (personnel == null)
{
    // Create personnel record for existing user
    var user = await userManager.FindByIdAsync(userId);
    personnel = new Personnel
    {
        UserId = userId,
        FirstName = user.Email.Split('@')[0],
        LastName = "User",
        Role = "Staff",
        IsActive = true
    };
    context.Personnel.Add(personnel);
    await context.SaveChangesAsync();
}
```

### Issue: Technician dropdown is empty

**Solution:** Make sure you have Personnel records with role "Technician":

```sql
-- Check technicians
SELECT * FROM Personnel WHERE role = 'Technician' AND is_active = 1;

-- If empty, run the seeder:
-- await DbSeeder.SeedSampleUsersAsync(services);
```

## 📊 Sample Data

After running the seeders, you should have:

### Users with Personnel Records
| Email | Password | Role | Personnel |
|-------|----------|------|-----------|
| admin@maintenx.com | Admin@123 | Admin | System Administrator |
| manager@maintenx.com | Manager@123 | Manager | Maria Santos |
| technician1@maintenx.com | Tech@123 | Technician | Juan Dela Cruz |
| technician2@maintenx.com | Tech@123 | Technician | Carlo Reyes |
| requester@maintenx.com | User@123 | Requester | Ana Lim |

### Contractors (No User Accounts)
| Name | Role | Skills |
|------|------|--------|
| Roberto Garcia | Contractor | Electrical, Generator |
| Elena Fernandez | Contractor | HVAC Specialist |
| Miguel Torres | External Technician | Elevator, Safety |

## 🎯 Testing the System

### Test 1: Create Work Order with Employee
1. Login as admin
2. Go to Work Orders
3. Click "Create Work Order"
4. Select equipment
5. Select technician (Juan Dela Cruz or Carlo Reyes)
6. Fill form and submit
7. ✅ Should create successfully

### Test 2: Create Work Order with Contractor
1. Login as admin
2. Go to Work Orders
3. Click "Create Work Order"
4. Select equipment
5. Select contractor (Roberto Garcia, Elena Fernandez, or Miguel Torres)
6. Fill form and submit
7. ✅ Should create successfully (even though they have no user account)

### Test 3: View Work Order Details
1. Check that assigned technician name displays correctly
2. Check that created by name displays correctly
3. ✅ Should show full names from Personnel table

## 📁 File Structure

```
IT15 Project/
├── Controllers/
│   └── DashboardController.cs ✅ Updated
├── Data/
│   ├── ApplicationDbContext.cs ✅ Updated
│   └── DbSeeder.cs ✅ Updated
├── Database/
│   ├── maintenx_schema.sql ✅ Updated
│   ├── maintenx_seed.sql
│   ├── PERSONNEL_ARCHITECTURE.md ✅ New
│   └── WORK_ORDERS_INTEGRATION.md
├── Models/
│   ├── ApplicationUser.cs ✅ New
│   ├── Personnel.cs ✅ New
│   ├── WorkOrder.cs ✅ Updated
│   ├── Asset.cs
│   ├── Category.cs
│   └── ViewModels/
│       └── CreateWorkOrderViewModel.cs ✅ Updated
├── Views/
│   └── Dashboard/
│       └── WorkOrders.cshtml
├── wwwroot/
│   └── js/
│       └── work-orders.js
└── SETUP_INSTRUCTIONS.md ✅ This file
```

## 🚀 Production Deployment Checklist

Before deploying to production:

- [ ] Run all migrations
- [ ] Seed admin user and personnel
- [ ] Test user registration flow
- [ ] Test work order creation
- [ ] Test with contractors (no user accounts)
- [ ] Verify all foreign keys are correct
- [ ] Test user deletion (personnel should remain)
- [ ] Backup database
- [ ] Update connection strings
- [ ] Configure email settings
- [ ] Set up logging
- [ ] Enable HTTPS
- [ ] Configure authentication settings

## 📚 Additional Resources

- **Architecture Guide:** `Database/PERSONNEL_ARCHITECTURE.md`
- **Work Orders Integration:** `Database/WORK_ORDERS_INTEGRATION.md`
- **Database Schema:** `Database/maintenx_schema.sql`
- **ASP.NET Identity Docs:** https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity

## 💡 Key Takeaways

1. **Personnel is the source of truth** for workforce data
2. **AspNetUsers is only for authentication** (login, passwords, roles)
3. **Work orders reference Personnel.personnel_id**, not AspNetUsers.Id
4. **Personnel can exist without user accounts** (contractors, external workers)
5. **User accounts can be deleted** without losing personnel history

## ✅ Success Criteria

You'll know everything is working when:

✅ Admin can login  
✅ Work Orders page loads  
✅ Technician dropdown shows names from Personnel table  
✅ Can create work order assigned to employee  
✅ Can create work order assigned to contractor  
✅ Work order displays correct personnel names  
✅ Historical data preserved when user account deleted  

---

**Need Help?** Check the architecture documentation in `Database/PERSONNEL_ARCHITECTURE.md`
