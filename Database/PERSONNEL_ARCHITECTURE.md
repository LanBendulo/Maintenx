# Personnel Architecture - Separation of Concerns

## ✅ Architecture Overview

This system implements a **clean separation** between authentication (Identity) and business domain (Personnel).

```
┌─────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION LAYER                      │
│  AspNetUsers, AspNetRoles, AspNetUserRoles (Identity)       │
│  Purpose: Login, passwords, roles, security                 │
└─────────────────────────────────────────────────────────────┘
                              ↓ (optional link)
┌─────────────────────────────────────────────────────────────┐
│                    BUSINESS DOMAIN LAYER                     │
│  Personnel (workforce data)                                  │
│  Purpose: Skills, rates, assignments, work history          │
└─────────────────────────────────────────────────────────────┘
                              ↓ (references)
┌─────────────────────────────────────────────────────────────┐
│                    OPERATIONAL LAYER                         │
│  Work_Order, Maintenance_Log, Maintenance_Schedule          │
│  Purpose: Day-to-day operations                             │
└─────────────────────────────────────────────────────────────┘
```

## 🎯 Key Principles

### 1. **Identity = Authentication Only**
- Manages: usernames, passwords, roles, claims, tokens
- Does NOT store: business data, skills, rates, assignments

### 2. **Personnel = Business Domain**
- Manages: workforce data, skills, rates, availability
- Can exist WITHOUT a user account (contractors, external workers)
- Optionally links to Identity via `user_id`

### 3. **Operational Tables Reference Personnel**
- Work orders, logs, schedules reference `Personnel.personnel_id`
- NOT `AspNetUsers.Id`

## 📊 Database Schema

### Personnel Table
```sql
CREATE TABLE dbo.Personnel (
    personnel_id    INT             IDENTITY PRIMARY KEY,
    user_id         NVARCHAR(450)   NULL,  -- Optional FK to AspNetUsers
    first_name      NVARCHAR(100)   NOT NULL,
    last_name       NVARCHAR(100)   NOT NULL,
    role            NVARCHAR(50)    NULL,  -- Technician, Supervisor, Contractor
    skill_set       NVARCHAR(255)   NULL,
    hourly_rate     DECIMAL(10,2)   NULL,
    is_active       BIT             DEFAULT 1,
    created_at      DATETIME        DEFAULT GETDATE(),
    
    CONSTRAINT FK_Personnel_User
        FOREIGN KEY (user_id)
        REFERENCES dbo.AspNetUsers(Id)
        ON DELETE SET NULL  -- Keep personnel record if user deleted
);
```

### Work_Order Table
```sql
CREATE TABLE dbo.Work_Order (
    work_order_id   INT     IDENTITY PRIMARY KEY,
    assigned_to     INT     NULL,  -- FK to Personnel.personnel_id
    created_by      INT     NULL,  -- FK to Personnel.personnel_id
    -- other fields...
    
    CONSTRAINT FK_WorkOrder_AssignedTo
        FOREIGN KEY (assigned_to)
        REFERENCES dbo.Personnel(personnel_id)
);
```

## 🔄 Data Flow Examples

### Example 1: Employee with User Account
```
1. User registers → AspNetUsers record created
2. Admin creates Personnel record → Links to user via user_id
3. Personnel can now be assigned work orders
4. User can login and see their assignments
```

### Example 2: External Contractor (No User Account)
```
1. Admin creates Personnel record → user_id = NULL
2. Personnel can be assigned work orders
3. No login capability (no user account)
4. Work tracked via personnel_id only
```

### Example 3: Retired Employee
```
1. Admin deactivates user account → AspNetUsers deleted/disabled
2. Personnel record remains (user_id = NULL due to SET NULL)
3. Historical work orders still reference personnel_id
4. Work history preserved for reporting
```

## 💻 C# Models

### Personnel Model
```csharp
public class Personnel
{
    public int PersonnelId { get; set; }
    public string? UserId { get; set; }  // Optional
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Role { get; set; }
    public string? SkillSet { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public virtual ApplicationUser? User { get; set; }
}
```

### ApplicationUser Model
```csharp
public class ApplicationUser : IdentityUser
{
    // Only authentication-related fields
    // NO business data here
    
    public virtual Personnel? Personnel { get; set; }
}
```

### WorkOrder Model
```csharp
public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public int? AssignedTo { get; set; }  // Personnel ID
    public int? CreatedBy { get; set; }   // Personnel ID
    
    // Navigation
    public virtual Personnel? AssignedToPersonnel { get; set; }
    public virtual Personnel? CreatedByPersonnel { get; set; }
}
```

## 🔧 Common Operations

### Creating a User with Personnel
```csharp
// 1. Create user account
var user = new ApplicationUser
{
    UserName = "tech@maintenx.com",
    Email = "tech@maintenx.com"
};
await userManager.CreateAsync(user, "Password123!");
await userManager.AddToRoleAsync(user, "Technician");

// 2. Create personnel record
var personnel = new Personnel
{
    UserId = user.Id,
    FirstName = "John",
    LastName = "Doe",
    Role = "Technician",
    SkillSet = "HVAC, Electrical",
    HourlyRate = 35.00m,
    IsActive = true
};
context.Personnel.Add(personnel);
await context.SaveChangesAsync();
```

### Creating a Contractor (No User Account)
```csharp
var contractor = new Personnel
{
    UserId = null,  // No user account
    FirstName = "External",
    LastName = "Contractor",
    Role = "Contractor",
    SkillSet = "Specialized Equipment",
    HourlyRate = 50.00m,
    IsActive = true
};
context.Personnel.Add(contractor);
await context.SaveChangesAsync();
```

### Assigning Work Order
```csharp
// Get current user's personnel record
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var currentPersonnel = await context.Personnel
    .FirstOrDefaultAsync(p => p.UserId == userId);

// Create work order
var workOrder = new WorkOrder
{
    AssignedTo = technicianPersonnelId,  // Personnel ID
    CreatedBy = currentPersonnel.PersonnelId,  // Personnel ID
    // other fields...
};
```

### Querying Work Orders with Personnel
```csharp
var workOrders = await context.WorkOrders
    .Include(w => w.AssignedToPersonnel)
        .ThenInclude(p => p.User)  // Optional: include user if exists
    .Include(w => w.CreatedByPersonnel)
    .ToListAsync();

foreach (var wo in workOrders)
{
    var assignedName = wo.AssignedToPersonnel?.FullName ?? "Unassigned";
    var hasAccount = wo.AssignedToPersonnel?.UserId != null;
    var email = wo.AssignedToPersonnel?.User?.Email;
}
```

## 📋 Seeding Data

### Program.cs Setup
```csharp
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    // Seed roles and admin
    await DbSeeder.SeedRolesAndAdminAsync(services);
    
    if (app.Environment.IsDevelopment())
    {
        // Seed sample users with personnel
        await DbSeeder.SeedSampleUsersAsync(services);
        
        // Seed contractors without user accounts
        await DbSeeder.SeedContractorPersonnelAsync(services);
    }
}
```

## ✅ Benefits of This Architecture

### 1. **Flexibility**
- ✅ Employees can have user accounts
- ✅ Contractors don't need user accounts
- ✅ Retired employees' history is preserved

### 2. **Clean Separation**
- ✅ Authentication logic separate from business logic
- ✅ Identity changes don't affect business data
- ✅ Easy to understand and maintain

### 3. **Data Integrity**
- ✅ Personnel records never deleted (historical data)
- ✅ User accounts can be deleted without losing work history
- ✅ Foreign keys reference stable personnel_id

### 4. **Scalability**
- ✅ Easy to add external workers
- ✅ Support for multiple employment types
- ✅ Flexible role management

## 🚫 What NOT to Do

### ❌ Don't Store Business Data in Identity
```csharp
// BAD - Don't do this
public class ApplicationUser : IdentityUser
{
    public string? SkillSet { get; set; }  // ❌ Business data
    public decimal? HourlyRate { get; set; }  // ❌ Business data
}
```

### ❌ Don't Reference AspNetUsers in Operational Tables
```sql
-- BAD - Don't do this
CREATE TABLE Work_Order (
    assigned_to NVARCHAR(450),  -- ❌ References AspNetUsers.Id
    FOREIGN KEY (assigned_to) REFERENCES AspNetUsers(Id)
);
```

### ❌ Don't Duplicate Identity Fields in Personnel
```sql
-- BAD - Don't do this
CREATE TABLE Personnel (
    username NVARCHAR(256),  -- ❌ Duplicates Identity
    password NVARCHAR(256),  -- ❌ Duplicates Identity
    email NVARCHAR(256),     -- ❌ Duplicates Identity
);
```

## 🔍 Querying Patterns

### Get All Active Technicians
```csharp
var technicians = await context.Personnel
    .Where(p => p.IsActive && p.Role == "Technician")
    .Select(p => new {
        p.PersonnelId,
        p.FullName,
        p.SkillSet,
        p.HourlyRate,
        HasAccount = p.UserId != null,
        Email = p.User != null ? p.User.Email : null
    })
    .ToListAsync();
```

### Get Work Orders for Current User
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var personnel = await context.Personnel
    .FirstOrDefaultAsync(p => p.UserId == userId);

if (personnel != null)
{
    var myWorkOrders = await context.WorkOrders
        .Where(w => w.AssignedTo == personnel.PersonnelId)
        .Include(w => w.Asset)
        .ToListAsync();
}
```

### Get Personnel with or without User Accounts
```csharp
var allPersonnel = await context.Personnel
    .Include(p => p.User)
    .Select(p => new {
        p.PersonnelId,
        p.FullName,
        p.Role,
        AccountStatus = p.UserId != null ? "Has Account" : "No Account",
        Email = p.User != null ? p.User.Email : "N/A"
    })
    .ToListAsync();
```

## 📝 Migration Notes

If you're migrating from the old system where Work_Order referenced AspNetUsers:

1. **Create Personnel table**
2. **Migrate existing users to Personnel**
3. **Update Work_Order foreign keys**
4. **Update application code**
5. **Test thoroughly**

See `Database/maintenx_schema.sql` for the complete schema.

## 🎓 Summary

| Aspect | Identity (AspNetUsers) | Personnel |
|--------|----------------------|-----------|
| **Purpose** | Authentication | Business Domain |
| **Stores** | Credentials, roles | Skills, rates, assignments |
| **Required** | For login | For operations |
| **Can exist alone** | Yes (admin without personnel) | Yes (contractor without account) |
| **Referenced by** | Personnel (optional) | Work orders, logs, schedules |
| **Deleted when** | User account removed | Never (historical data) |

This architecture provides the **best of both worlds**: secure authentication through Identity and flexible workforce management through Personnel.
