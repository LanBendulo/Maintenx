# Archive (Soft Delete) Feature Implementation Summary

## Overview
Implemented a comprehensive archive system for Maintenance Requests and Work Orders that preserves data integrity while keeping active views clean.

## Database Changes

### SQL Migration Script
**File:** `Database/add_archive_fields_complete.sql`

**Fields Added to Both Tables:**
- `is_archived` (BIT, NOT NULL, DEFAULT 0)
- `archived_at` (DATETIME, NULL)
- `archived_by_user_id` (NVARCHAR(450), NULL, FK to AspNetUsers)
- Indexes on `is_archived` for performance

**Run this script in SSMS before using the feature!**

## Business Rules

### Maintenance Requests
**Can Archive When:**
- Status = "Rejected" OR
- Status = "Converted"

**Cannot Archive When:**
- Status = "Pending" (still needs approval)
- Status = "Approved" (should be converted first)

### Work Orders
**Can Archive When:**
- Status = "Completed" OR
- Status = "Cancelled"

**Cannot Archive When:**
- Status = "Open" (work not started)
- Status = "In Progress" (work ongoing)

## Features Implemented

### 1. Archive Functionality
- **Soft delete** - records are never physically deleted
- **Audit trail** - tracks who archived and when
- **Status validation** - enforces business rules
- **Automatic filtering** - archived items hidden by default

### 2. Filter Options
**Three View Modes:**
- **Active Only** (default) - shows only non-archived records
- **Archived Only** - shows only archived records
- **All Requests/Orders** - shows everything

### 3. Restore Capability
- Archived items can be restored
- Clears archive metadata on restore
- Maintains original status

### 4. Visual Indicators
- Archived rows are greyed out (opacity: 0.6)
- "ARCHIVED" badge displayed
- Different background color (#f8f9fa)

## API Endpoints

### Maintenance Requests
```
PUT /admin/maintenance-requests/{id}/archive
PUT /admin/maintenance-requests/{id}/unarchive
GET /admin/maintenance-requests?filter=active|archived|all
```

### Work Orders
```
PUT /admin/work-orders/{id}/archive
PUT /admin/work-orders/{id}/unarchive
GET /admin/work-orders?filter=active|archived|all
```

## Data Integrity Protection

### Archived Records Cannot Be:
- Edited or updated
- Converted (for requests)
- Assigned or reassigned (for work orders)
- Deleted (no delete functionality exists)

### Query Filtering
- All list queries exclude archived records by default
- Explicit filter parameter required to view archived items
- Prevents accidental display in reports/dashboards

## User Interface

### Maintenance Requests Page
- **Filter dropdown** in header (Active/Archived/All)
- **Archive action** - only visible for Rejected/Converted requests
- **Restore action** - only visible for archived requests
- **Visual styling** - greyed out rows with badge

### Work Orders Page  
- Same UI pattern as Maintenance Requests
- **Archive action** - only visible for Completed/Cancelled orders
- **Restore action** - only visible for archived orders

## Testing Checklist

### Maintenance Requests
- [ ] Run SQL migration script
- [ ] Create a request and reject it
- [ ] Archive the rejected request
- [ ] Verify it disappears from Active view
- [ ] Switch to Archived view and verify it appears
- [ ] Restore the request
- [ ] Try to archive a Pending request (should fail)
- [ ] Try to archive an Approved request (should fail)

### Work Orders
- [ ] Complete a work order
- [ ] Archive the completed order
- [ ] Verify filtering works
- [ ] Restore the order
- [ ] Try to archive an Open order (should fail)
- [ ] Try to archive an In Progress order (should fail)

## Benefits

1. **Data Preservation** - No data loss from accidental deletes
2. **Clean Interface** - Active views show only relevant items
3. **Audit Trail** - Full history of who archived what and when
4. **Compliance** - Meets data retention requirements
5. **Reversible** - Mistakes can be undone with restore
6. **Performance** - Indexed filtering keeps queries fast

## Migration Steps

1. **Backup database** (always!)
2. **Run** `Database/add_archive_fields_complete.sql` in SSMS
3. **Verify** columns were added successfully
4. **Restart** the application
5. **Test** archive functionality
6. **Train users** on new workflow

## Notes

- Archive is **not** the same as delete - data is preserved
- Only authorized users (Admin/Manager) can archive/restore
- Archived items still count in total statistics
- Use "Active Only" filter for day-to-day operations
- Use "All" filter for historical reporting
