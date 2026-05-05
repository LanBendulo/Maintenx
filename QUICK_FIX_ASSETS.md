# Quick Fix: Asset Dropdown Empty Issue

## The Problem
When you click "Convert to Work Order", the modal opens but the Equipment/Asset dropdown is empty, causing the error:
```
The model field is required. The JSON value could not be converted to System.Int32. Path: $.assetId
```

## The Solution (3 Steps)

### Step 1: Run the SQL Script to Seed Assets
1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Open the file: `Database/verify_and_seed_assets.sql`
4. Click **Execute** (or press F5)
5. Check the **Messages** tab for output like:
   ```
   Categories: 8
   Assets: 10
   ```

### Step 2: Restart the Application
1. Stop the application if it's running (Shift+F5 in Visual Studio)
2. Start it again (F5)
3. The console should show seeding messages

### Step 3: Test the Conversion
1. Login as admin
2. Go to Maintenance Requests
3. Click "Convert to Work Order" on an approved request
4. Open browser Developer Tools (F12)
5. Go to the **Console** tab
6. You should see detailed logs like:
   ```
   Loading assets from /admin/assets/list...
   Assets loaded: 10 items
   Assets dropdown populated successfully
   ```

## What to Check in Browser Console

After clicking "Convert to Work Order", look for these messages:

### ✅ Success (What you want to see):
```
=== Opening Work Order Modal ===
Convert data from sessionStorage: {"maintenanceRequestId":1,...}
Loading assets and technicians for conversion...
Loading assets from /admin/assets/list...
Assets loaded: 10 items
Assets dropdown populated successfully
Available options: [{value: "1", text: "Chiller Unit #1"}, ...]
Asset successfully set to: 1
Form pre-filled successfully
```

### ❌ Error (What indicates a problem):
```
Error loading assets: Failed to load assets
Assets endpoint error: 500 {...}
```

## If Assets Still Don't Load

### Check 1: Verify Database Connection
Open `appsettings.json` and check the connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=DB_Maintenx;..."
}
```

### Check 2: Test the Endpoint Directly
1. Run the application
2. Login as admin
3. Navigate to: `https://localhost:XXXX/admin/assets/list`
4. You should see JSON like:
   ```json
   [
     {"value":1,"text":"Chiller Unit #1 - Building A"},
     {"value":2,"text":"Air Handling Unit - 3rd Floor"}
   ]
   ```

### Check 3: Check Application Logs
Look at the Visual Studio Output window for errors during startup.

## Alternative: Manual Database Seed

If the automatic seeding doesn't work, run these SQL scripts in order:

1. `Database/maintenx_schema.sql` (creates tables)
2. `Database/maintenx_seed.sql` (seeds basic data)
3. `Database/verify_and_seed_assets.sql` (verifies and seeds assets)

## Expected Result

After fixing:
- Asset dropdown will show 10 equipment items
- When converting a request, the asset will be pre-selected and locked
- The form will submit successfully
- You'll see the new work order in the Work Orders table

## Still Having Issues?

Check the browser console (F12) and share:
1. Any error messages in red
2. The output of the console.log messages
3. The response from `/admin/assets/list` endpoint
