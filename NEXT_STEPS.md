# Next Steps - Action Required

## ⚠️ CRITICAL: Database Migration Required

Before testing, you **MUST** run the database migration script:

### **Step 1: Open SQL Server Management Studio (SSMS)**

### **Step 2: Connect to Your Database**
- Server: Your SQL Server instance
- Database: `DB_Maintenx`

### **Step 3: Execute Migration Script**
1. Open the file: `Database/add_maintenance_requests.sql`
2. Copy the entire script
3. Paste into a new query window in SSMS
4. Click **Execute** (or press F5)

### **Step 4: Verify Migration**
You should see these messages:
```
Maintenance_Request table already exists.
maintenance_request_id column already exists in Work_Order table.
category column added to Maintenance_Request table.
location column added to Maintenance_Request table.
attachment_url column added to Maintenance_Request table.
Maintenance Request feature migration completed successfully!
```

---

## 🚀 Step 2: Run the Application

```bash
dotnet run
```

Wait for:
```
Now listening on: https://localhost:5001
Application started. Press Ctrl+C to shut down.
```

---

## 🧪 Step 3: Test the Conversion Flow

### **Quick Test (2 minutes):**

1. **Navigate to Maintenance Requests**
   ```
   https://localhost:5001/admin/maintenance-requests
   ```

2. **Create a New Request**
   - Click **"New Request"**
   - Fill in:
     - Title: "Test AC Issue"
     - Asset: Select any
     - Description: "Testing conversion flow"
     - Priority: High
   - Click **"Submit Request"**

3. **Approve the Request**
   - Find your new request in the table
   - Click **Actions** → **Approve**
   - Verify status changes to "Approved"

4. **Convert to Work Order**
   - Click **Actions** → **Convert to Work Order**
   - **You should be redirected to Work Orders page**
   - **Modal should automatically open** (wait ~500ms)
   - **Verify locked fields**:
     - Equipment (grayed out)
     - Description (grayed out)
     - Priority (faded radio buttons)

5. **Complete the Conversion**
   - **Assign Technician**: Select from dropdown
   - **Start Date**: Select today
   - **Expected Completion**: Select next week
   - Click **"Convert to Work Order"**

6. **Verify Success**
   - ✅ Green toast: "Request converted to work order successfully!"
   - ✅ Page reloads
   - ✅ New work order appears in table
   - ✅ **Source column** shows "Request #MR-0001"

7. **Verify Request Updated**
   - Go back to Maintenance Requests
   - Find your request
   - ✅ Status should be "Converted"
   - ✅ Actions should show "View Work Order"

---

## 🔍 What to Look For

### **✅ Success Indicators:**
- Modal opens automatically when converting
- Asset, Description, Priority fields are **grayed out and disabled**
- Modal title says "Convert Request to Work Order"
- Submit button says "Convert to Work Order"
- Work order appears with Source = "Request #MR-XXXX"
- Original request status changes to "Converted"

### **❌ Problem Indicators:**
- Modal doesn't open → Check browser console (F12)
- Fields are not locked → Check sessionStorage
- "Cannot convert" error → Check request status is "Approved"
- Work order not appearing → Check database migration

---

## 🐛 Troubleshooting

### **If Modal Doesn't Open:**

1. **Check Browser Console** (F12 → Console)
   - Look for JavaScript errors
   - Should see no red errors

2. **Check sessionStorage**
   - Open Console (F12)
   - Type: `sessionStorage.getItem('convertFromRequest')`
   - Should return JSON data after clicking "Convert"

3. **Hard Reload**
   - Press `Ctrl + Shift + R` (Windows)
   - Or `Cmd + Shift + R` (Mac)

### **If Fields Are Not Locked:**

1. **Wait a bit longer** (modal pre-fills after 500ms)
2. **Check if conversion data exists** (see sessionStorage above)
3. **Try clearing browser cache**

### **If "Column not found" Error:**

1. **Database migration not run**
   - Go back to Step 1
   - Execute `add_maintenance_requests.sql`

### **If "Cannot convert" Error:**

1. **Check request status**
   - Must be "Approved" (not Pending, Rejected, or Converted)
2. **Check if already converted**
   - Each request can only be converted once

---

## 📊 Expected Behavior Summary

### **Conversion Flow:**
```
1. User clicks "Convert to Work Order" on Approved request
   ↓
2. Browser redirects to /admin/work-orders
   ↓
3. Modal automatically opens with pre-filled data
   ↓
4. User fills Technician + Dates
   ↓
5. User clicks "Convert to Work Order"
   ↓
6. System creates Work Order
   ↓
7. System updates Request status to "Converted"
   ↓
8. Page reloads, work order appears in table
```

### **Manual Work Order Flow:**
```
1. User clicks "Manual Work Order" button
   ↓
2. Modal opens with empty, editable fields
   ↓
3. User fills all fields
   ↓
4. User clicks "Create Manual Work Order"
   ↓
5. System creates Work Order with Source = "Manual"
   ↓
6. Page reloads, work order appears in table
```

---

## 📁 Documentation Reference

- **CONVERSION_TESTING_GUIDE.md** - Detailed testing instructions
- **STREAMLINED_WORKFLOW_GUIDE.md** - Complete workflow documentation
- **WORKFLOW_QUICK_REFERENCE.md** - Quick reference card
- **IMPLEMENTATION_SUMMARY.md** - Technical implementation details

---

## ✅ Final Checklist

Before testing:
- [ ] Database migration executed successfully
- [ ] Application is running (`dotnet run`)
- [ ] Browser is open to the application
- [ ] You have Admin/Manager role
- [ ] You have a Personnel record

During testing:
- [ ] Create a maintenance request
- [ ] Approve the request
- [ ] Click "Convert to Work Order"
- [ ] Modal opens automatically
- [ ] Fields are locked (grayed out)
- [ ] Fill Technician and Dates
- [ ] Submit conversion
- [ ] Work order appears in table
- [ ] Source shows "Request #MR-XXXX"
- [ ] Request status is "Converted"

---

## 🎯 Success Criteria

Your implementation is successful when:

1. ✅ Clicking "Convert to Work Order" redirects to Work Orders page
2. ✅ Modal opens automatically with pre-filled data
3. ✅ Asset, Description, Priority are locked (read-only)
4. ✅ User can fill Technician, Dates, Notes
5. ✅ Submitting creates work order with correct data
6. ✅ Work order shows Source = "Request #MR-XXXX"
7. ✅ Original request status changes to "Converted"
8. ✅ Manual work orders still work normally

---

## 📞 If You Need Help

1. **Check browser console** (F12) for errors
2. **Check application logs** for backend errors
3. **Verify database migration** was successful
4. **Review documentation** files listed above
5. **Test with a fresh request** (create new, approve, convert)

---

## 🚀 You're Ready!

Everything is implemented and ready to test. Just:

1. ⚠️ **Run database migration** (CRITICAL)
2. 🏃 **Run the application**
3. 🧪 **Test the conversion flow**

**Good luck!** 🎉

---

**Last Updated**: May 2, 2026  
**Version**: 2.0 (Streamlined Workflow)
