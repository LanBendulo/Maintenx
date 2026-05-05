# Streamlined Maintenance Request → Work Order Workflow

## 🎯 Overview

The system now enforces a **single, forward-moving workflow** that eliminates duplicate data entry and ensures consistency:

```
MaintenanceRequest (Pending) 
    → Approve 
    → Convert 
    → WorkOrder (Open) 
    → In Progress 
    → Completed
```

---

## 🔄 Primary Workflow

### 1. **User Submits Maintenance Request**
- Navigate to **Maintenance Requests** page
- Click **"New Request"** (primary entry point)
- Fill in:
  - Title (short, max 100 chars)
  - Asset
  - Category (optional)
  - Location (optional)
  - Description (detailed)
  - Attachment (optional)
  - Priority (Low/Medium/High)
- System auto-generates: Request Number (MR-0001), Status (Pending), RequestedBy

### 2. **Admin Reviews Request**
- View all requests in table
- Filter by Status, Priority, Category
- Actions available based on status:
  - **Pending**: View Details, Approve, Reject
  - **Approved**: View Details, Convert to Work Order
  - **Rejected**: View Details only
  - **Converted**: View Details, View Work Order

### 3. **Admin Approves Request**
- Click **Actions** → **Approve**
- Status changes to **"Approved"**
- Request is now ready for conversion

### 4. **Admin Converts to Work Order**
- Click **Actions** → **Convert to Work Order**
- System redirects to Work Orders page
- Modal opens with **pre-filled, read-only fields**:
  - ✅ **Asset** (locked from request)
  - ✅ **Description** (locked from request)
  - ✅ **Priority** (locked from request)
- Admin fills in **required fields**:
  - Assign Technician
  - Start Date
  - Expected Completion Date
  - Notes (optional)
- Click **"Convert to Work Order"**
- System:
  - Creates Work Order with Status = "Open"
  - Links WorkOrder.MaintenanceRequestId
  - Updates Request Status = "Converted"
  - Prevents further edits to request

### 5. **Technician Executes Work Order**
- View work order details
- Update status: Open → In Progress → Completed

---

## 🆕 Manual Work Orders (Alternative Path)

For urgent or unplanned work that doesn't require approval:

1. Navigate to **Work Orders** page
2. Click **"Manual Work Order"** button
3. Fill in all fields (nothing is pre-filled or locked)
4. Work order is created with Source = "Manual"

---

## 📊 Work Orders Page Features

### **Source Column**
Every work order shows its origin:
- **"Request #MR-0001"** - Converted from maintenance request
- **"Manual"** - Created directly

### **Filters**
- Status: Open, In Progress, Completed, Cancelled
- Priority: High, Medium, Low
- Technician: Filter by assigned person
- **Source**: From Request / Manual

### **Status Workflow**
- **Open**: Newly created, awaiting technician action
- **In Progress**: Technician is working on it
- **Completed**: Work finished successfully
- **Cancelled**: Work order cancelled

---

## 🔒 Data Integrity Rules

### **Enforced by Backend:**
1. **Only Approved requests can be converted**
   - Pending → Cannot convert
   - Rejected → Cannot convert
   - Approved → Can convert
   - Converted → Already converted (blocked)

2. **One-to-One Relationship**
   - Each Maintenance Request can only be converted to ONE Work Order
   - Each Work Order can only link to ONE Maintenance Request (or none for manual)

3. **Read-Only Fields on Conversion**
   - Asset, Description, Priority MUST come from the request
   - These fields are locked in the UI
   - Backend enforces this even if UI is bypassed

4. **Status Transitions**
   - Maintenance Request: Pending → Approved → Converted (forward only)
   - Work Order: Open → In Progress → Completed (forward only)

### **Prevented Actions:**
- ❌ Cannot edit a Converted maintenance request
- ❌ Cannot convert a Pending or Rejected request
- ❌ Cannot convert the same request twice
- ❌ Cannot change Asset/Description/Priority when converting

---

## 🎨 UI Changes

### **Maintenance Requests Page:**
- **Button**: "New Request" (primary entry point)
- **Table Columns**: Request #, Title, Asset, Category, Priority, Status, Requested By, Created
- **Row Actions** (conditional):
  - Pending: Approve, Reject
  - Approved: Convert to Work Order
  - Converted: View Work Order (link)

### **Work Orders Page:**
- **Button**: "Manual Work Order" (renamed from "Create Work Order")
- **Table Columns**: WO ID, **Source**, Equipment, Technician, Priority, Status, Start Date, Completion
- **Filter**: Added "Source" filter (From Request / Manual)
- **Modal Title**: 
  - Converting: "Convert Request to Work Order"
  - Manual: "Create Manual Work Order"

---

## 📁 Files Modified

### **Backend:**
1. **Controllers/DashboardController.cs**
   - Added `GetApprovedRequests()` endpoint
   - Updated `CreateWorkOrder()` to handle MaintenanceRequestId
   - Enforces data from request when converting
   - Updates request status to "Converted"
   - Added "source" field to work order responses

2. **Models/ViewModels/CreateWorkOrderViewModel.cs**
   - Added `MaintenanceRequestId` (nullable)
   - Removed `Status` field (always starts as "Open")

### **Frontend:**
3. **Views/Dashboard/WorkOrders.cshtml**
   - Changed button text to "Manual Work Order"
   - Added "Source" column to table
   - Added "Source" filter
   - Removed "Status" field from create modal
   - Updated modal title and subtitle

4. **wwwroot/js/work-orders.js**
   - Added conversion flow handling
   - Pre-fills and locks Asset, Description, Priority when converting
   - Reads from sessionStorage for conversion data
   - Updates button text based on mode

5. **wwwroot/js/maintenance-requests.js**
   - Updated "Convert" action to redirect to Work Orders page
   - Stores conversion data in sessionStorage

---

## 🧪 Testing Checklist

### **Test 1: Full Workflow (Request → Work Order)**
1. ✅ Create a maintenance request
2. ✅ Verify status is "Pending"
3. ✅ Approve the request
4. ✅ Verify status is "Approved"
5. ✅ Click "Convert to Work Order"
6. ✅ Verify redirect to Work Orders page
7. ✅ Verify modal opens with pre-filled, locked fields
8. ✅ Assign technician and dates
9. ✅ Submit conversion
10. ✅ Verify work order created with Source = "Request #MR-XXXX"
11. ✅ Go back to Maintenance Requests
12. ✅ Verify request status is "Converted"
13. ✅ Verify "View Work Order" link appears

### **Test 2: Manual Work Order**
1. ✅ Go to Work Orders page
2. ✅ Click "Manual Work Order"
3. ✅ Verify all fields are editable
4. ✅ Fill in all fields
5. ✅ Submit
6. ✅ Verify work order created with Source = "Manual"

### **Test 3: Data Integrity**
1. ✅ Try to convert a Pending request → Should fail
2. ✅ Try to convert a Rejected request → Should fail
3. ✅ Try to convert an Approved request twice → Should fail
4. ✅ Verify converted request cannot be edited

### **Test 4: Filtering**
1. ✅ Filter work orders by "From Request"
2. ✅ Filter work orders by "Manual"
3. ✅ Verify correct results

---

## 🚀 Benefits

### **For Users:**
- ✅ Single entry point: "New Request" button
- ✅ No duplicate data entry
- ✅ Clear workflow progression
- ✅ Audit trail (who requested, who approved, who converted)

### **For Admins:**
- ✅ Review and approve before work begins
- ✅ Track request origin for every work order
- ✅ Filter by source (planned vs. urgent)
- ✅ Prevent data inconsistencies

### **For Technicians:**
- ✅ Clear work order details
- ✅ Know if work came from a request or was urgent
- ✅ Access original request details if needed

### **For System:**
- ✅ Data integrity enforced at backend
- ✅ One-to-one relationship maintained
- ✅ Forward-only status transitions
- ✅ No orphaned or duplicate records

---

## 📈 Future Enhancements (Optional)

### **Priority-Based SLA:**
- High: 4-hour response time
- Medium: 24-hour response time
- Low: 72-hour response time
- Add alerts for overdue work orders

### **Bulk Operations:**
- Approve multiple requests at once
- Assign multiple work orders to same technician

### **Mobile App:**
- Technicians can update work order status from mobile
- Upload completion photos

### **Analytics Dashboard:**
- Average time from Request → Approval
- Average time from Approval → Conversion
- Average time from Open → Completed
- Requests by Category over time

---

## ✨ Summary

Your system now enforces a **clean, single-direction workflow**:

1. **Maintenance Request** is the primary entry point
2. **Approval** gates the conversion process
3. **Conversion** creates Work Order with locked fields
4. **Manual Work Orders** available for urgent cases
5. **Source tracking** shows origin of every work order
6. **Data integrity** enforced at every step

**No more duplicate data entry. No more inconsistencies. One workflow, fully enforced.**
