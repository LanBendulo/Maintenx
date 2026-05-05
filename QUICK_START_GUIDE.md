# Quick Start Guide - Edit and Update Status Features

## 🚀 Getting Started

### Step 1: Run Database Migration

Open SQL Server Management Studio (SSMS) and run:

```sql
-- File: Database/add_actual_completion_to_work_order.sql
USE DB_Maintenx;
GO

ALTER TABLE dbo.Work_Order
ADD actual_completion DATE NULL;
GO
```

### Step 2: Build and Run

```bash
dotnet build
dotnet run
```

### Step 3: Navigate to Work Orders

Go to: `https://localhost:XXXX/admin/work-orders`

---

## 📋 How to Use

### Edit a Work Order

1. Find a work order with status **Open** or **In Progress**
2. Click **Actions** → **Edit**
3. Edit modal opens with:
   - **Always Editable**: Technician, Start Date, Expected Completion, Notes
   - **Conditionally Editable** (manual work orders only): Asset, Description, Priority
   - **Locked** (linked work orders): Asset, Description, Priority (grayed out)
4. Make your changes
5. Click **Save Changes**

**Note**: Edit button is hidden for Completed/Cancelled work orders.

### Update Status

1. Find any work order (any status except archived)
2. Click **Actions** → **Update Status**
3. Status modal opens showing:
   - Current status badge
   - Dropdown with only valid next statuses
   - Actual Completion field (if selecting "Completed")
4. Select new status
5. If marking as Completed, enter Actual Completion date
6. Click **Update Status**

---

## ✅ Status Transitions

| From | To |
|------|-----|
| **Open** | In Progress, Cancelled |
| **In Progress** | Completed, Cancelled |
| **Completed** | ❌ No transitions allowed |
| **Cancelled** | ❌ No transitions allowed |

---

## � Field Locking Rules

### Linked Work Orders (from Maintenance Request)
- ❌ **Cannot Edit**: Asset, Description, Priority
- ✅ **Can Edit**: Technician, Start Date, Expected Completion, Notes
- 🔔 **Warning Banner**: Shows which request it's linked to

### Manual Work Orders
- ✅ **Can Edit**: All fields

---

## 🛡️ Validation Rules

### Edit Modal
- ✅ Expected Completion must be after Start Date
- ✅ Cannot edit Completed/Cancelled work orders
- ✅ Cannot edit archived work orders
- ✅ Cannot change locked fields for linked work orders

### Update Status Modal
- ✅ Only valid status transitions allowed
- ✅ Actual Completion required when marking as Completed
- ✅ Cannot update archived work orders
- ✅ Cannot transition from Completed or Cancelled

---

## 🧪 Testing Scenarios

### Test 1: Edit Manual Work Order
1. Create a manual work order
2. Click Edit
3. Change Asset, Description, Priority, Technician, Dates
4. Save → Should succeed

### Test 2: Edit Linked Work Order
1. Convert a maintenance request to work order
2. Click Edit
3. Try to change Asset → Should be locked (grayed out)
4. Change Technician and Dates → Should succeed

### Test 3: Update Status - Valid Transition
1. Find work order with status "Open"
2. Click Update Status
3. Select "In Progress"
4. Save → Should succeed

### Test 4: Update Status - Invalid Transition
1. Find work order with status "Completed"
2. Click Update Status
3. Dropdown should be empty (no valid transitions)

### Test 5: Complete Work Order
1. Find work order with status "In Progress"
2. Click Update Status
3. Select "Completed"
4. Actual Completion field appears
5. Enter date and save → Should succeed

---

## 🐛 Troubleshooting

### Edit button not showing
- Check work order status (must be Open or In Progress)
- Archived work orders don't show Edit button

### Cannot change Asset/Description/Priority
- Check if work order is linked to a maintenance request
- These fields are locked for linked work orders

### Status update fails
- Check if transition is valid (see Status Transitions table)
- Ensure Actual Completion date is provided when marking as Completed

### Database error
- Ensure `actual_completion` column exists in Work_Order table
- Run the migration script if not already done

---

## � Files Changed

- ✅ `Controllers/DashboardController.cs` - New endpoints
- ✅ `Views/Dashboard/WorkOrders.cshtml` - New modal
- ✅ `wwwroot/js/work-orders.js` - Modal handlers
- ✅ `Models/WorkOrder.cs` - Already has ActualCompletion field
- ⏳ `Database/add_actual_completion_to_work_order.sql` - **Run this!**

---

## 🎯 Key Benefits

✨ **Separation of Concerns**: Edit and Status Update are now separate
✨ **Data Integrity**: Locked fields prevent accidental changes
✨ **User Experience**: Clear, focused modals for each action
✨ **Validation**: All rules enforced on backend
✨ **Audit Trail**: Actual completion dates tracked

---

## 📞 Need Help?

Refer to `WORK_ORDER_EDIT_STATUS_IMPLEMENTATION.md` for detailed technical documentation.
