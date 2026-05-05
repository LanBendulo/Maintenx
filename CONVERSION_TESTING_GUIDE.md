# Conversion Testing Guide

## 🎯 How the Conversion Flow Works

### **Step-by-Step Process:**

1. **User clicks "Convert to Work Order"** on an Approved maintenance request
2. JavaScript stores conversion data in `sessionStorage`
3. Browser redirects to `/admin/work-orders` page
4. Page loads and checks for conversion data
5. **Modal automatically opens** with pre-filled fields
6. User completes required fields and submits
7. Work order is created and appears in the table

---

## 🧪 Testing the Conversion Flow

### **Prerequisites:**
✅ Database migration completed (`add_maintenance_requests.sql`)  
✅ Application is running (`dotnet run`)  
✅ You have at least one Approved maintenance request  

### **Test 1: Convert Approved Request**

1. **Navigate to Maintenance Requests**
   ```
   URL: /admin/maintenance-requests
   ```

2. **Find an Approved Request**
   - Look for a request with Status = "Approved"
   - If none exist, create one and approve it first

3. **Click "Convert to Work Order"**
   - Click **Actions** dropdown on the approved request
   - Click **"Convert to Work Order"**

4. **Verify Redirect**
   - You should be redirected to `/admin/work-orders`
   - Modal should **automatically open** after ~500ms

5. **Verify Pre-filled Fields (Read-Only)**
   - ✅ **Equipment/Asset**: Should show the asset from request (grayed out, disabled)
   - ✅ **Issue Description**: Should show title + description from request (grayed out, disabled)
   - ✅ **Priority**: Should be pre-selected from request (radio buttons disabled, slightly faded)
   - ✅ **Modal Title**: Should say "Convert Request to Work Order"
   - ✅ **Modal Subtitle**: Should say "Converting MR-XXXX to a work order"

6. **Fill Required Fields**
   - **Assign Technician**: Select a technician from dropdown
   - **Start Date**: Select today or future date
   - **Expected Completion**: Select a date after start date
   - **Notes** (optional): Add any additional instructions

7. **Submit Conversion**
   - Click **"Convert to Work Order"** button
   - Button should change to "Converting..."
   - Wait for success toast

8. **Verify Results**
   - ✅ Green success toast: "Request converted to work order successfully!"
   - ✅ Page reloads after 1.5 seconds
   - ✅ New work order appears in table
   - ✅ **Source column** shows "Request #MR-XXXX"

9. **Go Back to Maintenance Requests**
   - Navigate back to `/admin/maintenance-requests`
   - Find the converted request
   - ✅ Status should be "Converted"
   - ✅ Actions should show "View Work Order" link

---

### **Test 2: Manual Work Order (No Conversion)**

1. **Navigate to Work Orders**
   ```
   URL: /admin/work-orders
   ```

2. **Click "Manual Work Order"**
   - Click the button in the top-right corner

3. **Verify Modal Opens Normally**
   - ✅ **Modal Title**: "Create Manual Work Order"
   - ✅ **Modal Subtitle**: "Create a work order without a maintenance request"
   - ✅ All fields are **editable** (not grayed out)

4. **Fill All Fields**
   - Equipment/Asset: Select from dropdown
   - Issue Description: Type description
   - Priority: Select Low/Medium/High
   - Assign Technician: Select technician
   - Start Date: Select date
   - Expected Completion: Select date
   - Notes (optional): Add notes

5. **Submit**
   - Click **"Create Manual Work Order"** button
   - Wait for success toast

6. **Verify Results**
   - ✅ New work order appears in table
   - ✅ **Source column** shows "Manual"

---

## 🔍 Troubleshooting

### **Issue: Modal doesn't open automatically**

**Possible Causes:**
1. JavaScript not loaded properly
2. sessionStorage not working
3. Timing issue

**Solutions:**
1. **Check Browser Console** (F12 → Console tab)
   - Look for JavaScript errors
   - Should see no errors

2. **Check sessionStorage**
   - Open Console (F12)
   - Type: `sessionStorage.getItem('convertFromRequest')`
   - Should return JSON data or null

3. **Clear Browser Cache**
   - Press `Ctrl + Shift + Delete`
   - Clear cached images and files
   - Reload page

4. **Check Network Tab**
   - F12 → Network tab
   - Verify `work-orders.js` is loaded (Status 200)

---

### **Issue: Fields are not locked**

**Possible Causes:**
1. Conversion data not in sessionStorage
2. JavaScript timing issue

**Solutions:**
1. **Check sessionStorage** (see above)
2. **Increase timeout** in `work-orders.js`:
   ```javascript
   setTimeout(() => {
       // Pre-fill code
   }, 1000); // Increase from 500 to 1000
   ```

---

### **Issue: "Cannot convert" error**

**Possible Causes:**
1. Request status is not "Approved"
2. Request already converted
3. Request not found

**Solutions:**
1. **Check Request Status**
   - Go to Maintenance Requests page
   - Verify status is "Approved" (not Pending, Rejected, or Converted)

2. **Check if Already Converted**
   - If status is "Converted", it's already been converted
   - Each request can only be converted once

3. **Try a Different Request**
   - Create a new maintenance request
   - Approve it
   - Try converting again

---

### **Issue: Work order not appearing in table**

**Possible Causes:**
1. Creation failed (check console)
2. Page didn't reload
3. Filter hiding the work order

**Solutions:**
1. **Check for Errors**
   - Browser console (F12)
   - Look for red error messages

2. **Manually Reload**
   - Press `F5` or `Ctrl + R`
   - Check if work order appears

3. **Check Filters**
   - Reset all filters (click "Reset" button)
   - Look for the work order

4. **Check Database**
   - Open SSMS
   - Run: `SELECT TOP 10 * FROM Work_Order ORDER BY work_order_id DESC`
   - Verify work order was created

---

## 🎨 Visual Indicators

### **When Converting (Fields Locked):**
```
Equipment/Asset:     [Grayed out dropdown ▼]  ← Disabled, gray background
Issue Description:   [Grayed out textarea]    ← Disabled, gray background
Priority:            ○ Low  ○ Medium  ○ High  ← Disabled, faded (60% opacity)
```

### **When Manual (Fields Editable):**
```
Equipment/Asset:     [White dropdown ▼]       ← Enabled, white background
Issue Description:   [White textarea]         ← Enabled, white background
Priority:            ○ Low  ● Medium  ○ High  ← Enabled, full opacity
```

---

## 📊 Expected Data Flow

### **Conversion Flow:**
```
Maintenance Request (MR-0001)
├─ Asset: HVAC Unit - Building A
├─ Description: "AC not cooling properly..."
├─ Priority: High
└─ Status: Approved

         ↓ [Convert]

Work Order (#WO-0001)
├─ Asset: HVAC Unit - Building A        ← From Request (locked)
├─ Description: "AC not cooling..."     ← From Request (locked)
├─ Priority: High                       ← From Request (locked)
├─ Technician: Juan Dela Cruz           ← User fills
├─ Start Date: 2026-05-03               ← User fills
├─ Due Date: 2026-05-10                 ← User fills
├─ Status: Open                         ← Auto-set
├─ Source: "Request #MR-0001"           ← Auto-set
└─ MaintenanceRequestId: 1              ← Auto-set

         ↓ [Update]

Maintenance Request (MR-0001)
└─ Status: Converted                    ← Auto-updated
```

---

## ✅ Success Checklist

After conversion, verify:

- [ ] Work order appears in Work Orders table
- [ ] Source column shows "Request #MR-XXXX"
- [ ] Asset matches original request
- [ ] Description matches original request
- [ ] Priority matches original request
- [ ] Technician is assigned
- [ ] Dates are set
- [ ] Status is "Open"
- [ ] Request status changed to "Converted"
- [ ] Request shows "View Work Order" link

---

## 🚀 Quick Test Script

Run this complete test in 2 minutes:

1. **Create Request** (30 seconds)
   - Go to Maintenance Requests
   - Click "New Request"
   - Fill: Title, Asset, Description, Priority
   - Submit

2. **Approve Request** (10 seconds)
   - Click Actions → Approve
   - Verify status = "Approved"

3. **Convert to Work Order** (30 seconds)
   - Click Actions → Convert to Work Order
   - Wait for modal to open
   - Verify fields are locked
   - Fill: Technician, Dates
   - Click "Convert to Work Order"

4. **Verify Results** (30 seconds)
   - Check work order in table
   - Check source = "Request #MR-XXXX"
   - Go back to Maintenance Requests
   - Verify status = "Converted"

**Total Time**: ~2 minutes

---

## 📞 Need Help?

### **Check These First:**
1. Browser console (F12) for JavaScript errors
2. Network tab (F12) for failed requests
3. Application logs for backend errors
4. Database for created records

### **Common Solutions:**
- Clear browser cache
- Hard reload (Ctrl + Shift + R)
- Check database migration was run
- Verify Personnel records exist
- Check user has Admin/Manager role

---

**Last Updated**: May 2, 2026  
**Version**: 2.0 (Streamlined Workflow)
