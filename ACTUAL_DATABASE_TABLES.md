# MaintenX Actual Database Tables

**Database:** db50508  
**Server:** db50508.public.databaseasp.net  
**Verified:** May 13, 2026

---

## Complete Table List (29 Tables)

### ASP.NET Core Identity Tables (8)
1. `__EFMigrationsHistory` - Entity Framework migration tracking
2. `AspNetRoleClaims` - Role-based claims
3. `AspNetRoles` - User roles (Owner, Admin, Supervisor, Technician, User, SuperAdmin)
4. `AspNetUserClaims` - User-specific claims
5. `AspNetUserLogins` - External login providers (Google OAuth)
6. `AspNetUserRoles` - User-role assignments
7. `AspNetUsers` - User accounts (extended with CompanyId, FullName, etc.)
8. `AspNetUserTokens` - Authentication tokens

---

### Core Business Tables (21)

#### **Tenant & Subscription Management (3)**
9. `Company` - Multi-tenant companies
10. `SubscriptionPlan` - Subscription plans (Starter, Professional, Enterprise)
11. `CompanySubscription` - Company subscription records

#### **Personnel & Assets (4)**
12. `Personnel` - Workforce (technicians, supervisors, contractors)
13. `Asset` - Equipment and assets
14. `AssetStatusHistory` - Asset status change audit trail
15. `Category` - Asset categories

#### **Maintenance Workflow (3)**
16. `Maintenance_Request` - Maintenance requests (entry point)
17. `Work_Order` - Work orders (core maintenance entity)
18. `MaintenanceLog` - Completed work records (immutable audit)

#### **Preventive Maintenance (1)**
19. `PreventiveSchedule` - PM schedules for automatic WO generation

#### **Parts & Inventory (3)**
20. `Part` - Spare parts inventory
21. `WorkOrderPart` - Parts used in work orders
22. `InventoryMovement` - Inventory change audit trail

#### **Cost Tracking (1)**
23. `WorkOrderCost` - Work order cost breakdown

---

### Legacy Tables (6) - May Need Cleanup

24. `Maintenance_Log` - ⚠️ Legacy version (replaced by `MaintenanceLog`)
25. `Maintenance_Schedule` - ⚠️ Legacy version (replaced by `PreventiveSchedule`)
26. `Maintenance_Cost` - ⚠️ Legacy version (replaced by `WorkOrderCost`)
27. `Spare_Part` - ⚠️ Legacy version (replaced by `Part`)
28. `WorkOrder_Parts` - ⚠️ Legacy version (replaced by `WorkOrderPart`)
29. `Inventory_Transaction` - ⚠️ Legacy version (replaced by `InventoryMovement`)

---

## Table Naming Patterns

### Modern Tables (PascalCase)
- `Company`, `SubscriptionPlan`, `CompanySubscription`
- `Personnel`, `Asset`, `AssetStatusHistory`, `Category`
- `MaintenanceLog`, `PreventiveSchedule`
- `Part`, `WorkOrderPart`, `WorkOrderCost`, `InventoryMovement`

### Legacy Tables (snake_case)
- `Maintenance_Request`, `Work_Order` (still actively used)
- `Maintenance_Log`, `Maintenance_Schedule`, `Maintenance_Cost` (deprecated)
- `Spare_Part`, `WorkOrder_Parts`, `Inventory_Transaction` (deprecated)

---

## Data Dictionary Status

✅ **DATA_DICTIONARY.md** has been updated to reflect the actual database structure  
✅ All 29 tables documented  
✅ Legacy tables identified  
✅ Foreign key relationships mapped  

---

## Recommended Actions

### 1. Verify Legacy Table Usage
```sql
-- Check if legacy tables contain data
SELECT 'Maintenance_Log' as TableName, COUNT(*) as RowCount FROM Maintenance_Log
UNION ALL
SELECT 'Maintenance_Schedule', COUNT(*) FROM Maintenance_Schedule
UNION ALL
SELECT 'Maintenance_Cost', COUNT(*) FROM Maintenance_Cost
UNION ALL
SELECT 'Spare_Part', COUNT(*) FROM Spare_Part
UNION ALL
SELECT 'WorkOrder_Parts', COUNT(*) FROM WorkOrder_Parts
UNION ALL
SELECT 'Inventory_Transaction', COUNT(*) FROM Inventory_Transaction;
```

### 2. If Legacy Tables Are Empty
```sql
-- Safe to drop if no data exists
DROP TABLE IF EXISTS Maintenance_Log;
DROP TABLE IF EXISTS Maintenance_Schedule;
DROP TABLE IF EXISTS Maintenance_Cost;
DROP TABLE IF EXISTS Spare_Part;
DROP TABLE IF EXISTS WorkOrder_Parts;
DROP TABLE IF EXISTS Inventory_Transaction;
```

### 3. If Legacy Tables Have Data
- Create migration scripts to move data to new tables
- Verify data integrity after migration
- Keep legacy tables temporarily for rollback safety
- Drop after successful migration verification

---

## Your Question: "is this my real database"

**Answer:** YES! ✅

Your actual database has **29 tables**:
- 8 ASP.NET Identity tables
- 15 active business tables
- 6 legacy tables (may need cleanup)

The data dictionary I created documents all of these tables with their actual structure, relationships, and field descriptions.
