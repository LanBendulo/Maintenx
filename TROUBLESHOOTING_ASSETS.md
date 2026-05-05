# Troubleshooting: Assets Not Loading

## Problem
The asset dropdown is empty when trying to convert a maintenance request to a work order or when editing a work order.

## Root Cause
The `/admin/assets/list` endpoint is failing, likely because:
1. The database hasn't been seeded with asset data yet
2. There's a database connection issue
3. The Assets table is empty

## Solution Steps

### Step 1: Verify Database Connection
1. Open the application in Visual Studio
2. Check the `appsettings.json` file for the connection string
3. Ensure SQL Server is running
4. Test the connection using SQL Server Management Studio (SSMS)

### Step 2: Run the Application to Seed Data
The application automatically seeds data when running in Development mode.

1. **Run the application** (F5 in Visual Studio or `dotnet run`)
2. The `Program.cs` file will automatically call:
   - `DbSeeder.SeedCategoriesAsync()` - Creates asset categories
   - `DbSeeder.SeedAssetsAsync()` - Creates sample assets
   - `DbSeeder.SeedSampleUsersAsync()` - Creates sample users
   - `DbSeeder.SeedContractorPersonnelAsync()` - Creates contractor personnel

3. Check the console output for messages like:
   ```
   Category created: HVAC Systems
   Category created: Electrical Equipment
   Asset created: Chiller Unit #1 - Building A
   Asset created: Air Handling Unit - 3rd Floor
   ```

### Step 3: Verify Data in Database
Open SQL Server Management Studio and run:

```sql
USE DB_Maintenx;

-- Check if categories exist
SELECT * FROM dbo.Category;

-- Check if assets exist
SELECT * FROM dbo.Asset;

-- Check if personnel exist
SELECT * FROM dbo.Personnel;
```

### Step 4: Manual Seeding (If Automatic Seeding Fails)
If the automatic seeding doesn't work, you can manually run the SQL seed scripts:

1. Open SSMS and connect to your SQL Server
2. Run the scripts in this order:
   - `Database/maintenx_schema.sql` (if not already run)
   - `Database/maintenx_seed.sql` (contains Categories and Assets)

### Step 5: Test the Endpoint
After seeding, test the endpoint directly:

1. Run the application
2. Login as admin (admin@maintenx.com / Admin@123)
3. Open browser developer tools (F12)
4. Navigate to: `https://localhost:XXXX/admin/assets/list`
5. You should see JSON data like:
   ```json
   [
     {"value":1,"text":"Chiller Unit #1 - Building A"},
     {"value":2,"text":"Air Handling Unit - 3rd Floor"},
     ...
   ]
   ```

### Step 6: Check Browser Console
1. Open the Work Orders page
2. Open browser developer tools (F12)
3. Go to the Console tab
4. Look for error messages related to asset loading
5. The improved error handling will now show detailed error messages

## Expected Behavior After Fix
- Asset dropdown should populate with equipment names
- When converting a maintenance request, the asset should be pre-selected and locked
- When editing a work order, assets should load in the dropdown

## Additional Notes
- The application must be in Development mode for automatic seeding
- Check `appsettings.Development.json` for the correct connection string
- Ensure the database `DB_Maintenx` exists
- The admin user must have a Personnel record to create work orders
