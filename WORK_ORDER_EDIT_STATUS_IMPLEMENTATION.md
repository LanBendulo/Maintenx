# Work Order Edit and Update Status Implementation

## Overview
Implemented separate **Edit** and **Update Status** features for Work Orders with strict separation of responsibilities and full backend enforcement.

## Implementation Summary

### 1. Backend Changes (Controllers/DashboardController.cs)

#### New Endpoints

**A. Edit Work Order** - `/admin/work-orders/{id}/edit` (PUT)
- **Purpose**: Edit operational details only
- **Editable Fields** (always):
  - `PersonnelId` (assigned technician)
  - `StartDate`
  - `ExpectedCompletion`
  - `Notes`
- **Conditionally Editable** (only if `MaintenanceRequestId == null`):
  - `AssetId`
  - `Description`
  - `Priority`
- **Restrictions**:
  - Cannot edit if Status = Completed or Cancelled
  - Cannot edit archived work orders
  - For linked work orders, Asset/Description/Priority are READ-ONLY

**B. Update Status** - `/admin/work-orders/{id}/status` (PUT)
- **Purpose**: Change lifecycle status only
- **Fields**:
  - `Status` (required)
  - `ActualCompletion` (required when Status = Completed)
- **Validation**:
  - Enforces allowed status transitions:
    - Open → In Progress, Cancelled
    - In Progress → Completed, Cancelled
    - Completed → (no transitions)
    - Cancelled → (no transitions)
  - Cannot update archived work orders
  - Requires ActualCompletion date when marking as Completed

#### Request Models
```csharp
public class EditWorkOrderRequest
{
    public int? AssetId { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public int? PersonnelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedCompletion { get; set; }
    public string? Notes { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public DateTime? ActualCompletion { get; set; }
}
```

### 2. Frontend Changes

#### A. Edit Modal (Views/Dashboard/WorkOrders.cshtml)
- **Modal ID**: `woUpdateModal`
- **Removed**: Status dropdown (now handled by separate modal)
- **Removed**: Actual Completion field (now in Update Status modal)
- **Features**:
  - Warning banner for linked work orders
  - Conditional field locking (Asset, Description, Priority)
  - Visual hints showing which fields are locked
  - Only shows for Open/In Progress work orders

#### B. Update Status Modal (NEW)
- **Modal ID**: `woStatusModal`
- **Features**:
  - Lightweight modal focused only on status changes
  - Shows current status badge
  - Displays allowed transitions dynamically
  - Dropdown populated with only valid next statuses
  - Actual Completion field (shown only when selecting "Completed")
  - Validates status transitions on frontend and backend

### 3. JavaScript Changes (wwwroot/js/work-orders.js)

#### Edit Functionality
- Opens Edit modal when "Edit" action is clicked
- Loads work order data
- Conditionally locks fields based on `MaintenanceRequestId`
- Submits to `/admin/work-orders/{id}/edit` endpoint
- Uses PascalCase property names matching backend DTO

#### Update Status Functionality
- Opens Update Status modal when "Update Status" action is clicked
- Loads current status and allowed transitions
- Dynamically populates status dropdown with valid options
- Shows/hides Actual Completion field based on selected status
- Submits to `/admin/work-orders/{id}/status` endpoint
- Validates transitions before submission

### 4. Database Migration

**File**: `Database/add_actual_completion_to_work_order.sql`

**Action Required**: Run this script in SQL Server Management Studio (SSMS)

```sql
USE DB_Maintenx;
GO

ALTER TABLE dbo.Work_Order
ADD actual_completion DATE NULL;
GO
```

This adds the `actual_completion` column to track when work orders are actually completed.

## UI Behavior

### Edit Button
- **Visibility**: Only shown for Open/In Progress work orders
- **Hidden for**: Completed, Cancelled, or Archived work orders
- **Action**: Opens Edit modal with operational fields

### Update Status Button
- **Visibility**: Always shown (except for archived)
- **Action**: Opens Update Status modal with status dropdown

### Field Locking (Edit Modal)
- **Linked Work Orders** (from Maintenance Request):
  - Asset: Locked (grayed out, disabled)
  - Description: Locked (grayed out, read-only)
  - Priority: Locked (grayed out, disabled)
  - Warning banner displayed
- **Manual Work Orders**:
  - All fields editable

## Status Transition Rules

| Current Status | Allowed Transitions |
|---------------|---------------------|
| Open | In Progress, Cancelled |
| In Progress | Completed, Cancelled |
| Completed | (none) |
| Cancelled | (none) |

## Testing Checklist

### Edit Functionality
- [ ] Edit button only shows for Open/In Progress work orders
- [ ] Edit modal opens with correct data pre-filled
- [ ] Linked work orders show warning banner
- [ ] Linked work orders have Asset/Description/Priority locked
- [ ] Manual work orders allow editing all fields
- [ ] Cannot edit Completed/Cancelled work orders
- [ ] Validation: Expected Completion >= Start Date
- [ ] Successfully saves changes and reloads page

### Update Status Functionality
- [ ] Update Status button shows for all non-archived work orders
- [ ] Modal shows current status badge
- [ ] Dropdown only shows valid next statuses
- [ ] Actual Completion field appears when selecting "Completed"
- [ ] Cannot transition from Completed to any other status
- [ ] Cannot transition from Cancelled to any other status
- [ ] Invalid transitions are rejected by backend
- [ ] Successfully updates status and reloads page

### Backend Validation
- [ ] Edit endpoint rejects changes to locked fields for linked work orders
- [ ] Edit endpoint rejects edits to Completed/Cancelled work orders
- [ ] Status endpoint validates transitions
- [ ] Status endpoint requires ActualCompletion when marking Completed
- [ ] Both endpoints reject operations on archived work orders

## Files Modified

1. **Controllers/DashboardController.cs**
   - Added `EditWorkOrder` endpoint
   - Enhanced `UpdateWorkOrderStatus` endpoint
   - Added `EditWorkOrderRequest` and `UpdateStatusRequest` models
   - Removed old `UpdateWorkOrder` and `UpdateWorkOrderViewModel`

2. **Views/Dashboard/WorkOrders.cshtml**
   - Modified Edit modal (removed Status field)
   - Added Update Status modal

3. **wwwroot/js/work-orders.js**
   - Updated Edit action handler
   - Replaced prompt-based status update with modal
   - Added status transition validation
   - Updated form submission handlers

4. **Models/WorkOrder.cs**
   - Already has `ActualCompletion` field (no changes needed)

5. **Database/add_actual_completion_to_work_order.sql**
   - Migration script to add `actual_completion` column

## Next Steps

1. **Run Database Migration**:
   - Open SQL Server Management Studio (SSMS)
   - Connect to your database
   - Open `Database/add_actual_completion_to_work_order.sql`
   - Execute the script

2. **Test the Implementation**:
   - Build and run the application
   - Navigate to Work Orders page
   - Test Edit functionality on both linked and manual work orders
   - Test Update Status functionality with various transitions
   - Verify validation rules are enforced

3. **Verify**:
   - Check that Edit button only shows for Open/In Progress work orders
   - Confirm locked fields cannot be changed for linked work orders
   - Test invalid status transitions are rejected
   - Ensure ActualCompletion is required when marking as Completed

## Benefits

✅ **Clean Separation**: Edit and Status Update are now separate concerns
✅ **Backend Enforcement**: All business rules validated on server
✅ **Data Integrity**: Locked fields cannot be changed for linked work orders
✅ **User Experience**: Clear, focused modals for each action
✅ **Validation**: Status transitions properly validated
✅ **Audit Trail**: ActualCompletion date tracked for completed work orders
