# Work Order Modal System - Testing Instructions

## Pre-Testing Setup

1. **Clear Browser Cache:**
   - Press `Ctrl + Shift + Delete`
   - Select "Cached images and files"
   - Click "Clear data"

2. **Hard Refresh:**
   - Press `Ctrl + Shift + R` (or `Cmd + Shift + R` on Mac)
   - This ensures new JavaScript files are loaded

3. **Open Browser Console:**
   - Press `F12`
   - Go to "Console" tab
   - Keep it open during all tests

## Test 1: Verify Scripts Load

### Steps:
1. Navigate to `/admin/work-orders`
2. Check console for initialization messages

### Expected Console Output:
```
=== Work Order Modal JS Initializing ===
=== Modal Lifecycle Initializing ===
Modal elements cached: {overlay: true, openBtn: true, ...}
✓ Event listeners bound
✓ WorkOrderModal namespace exposed
=== Work Orders JS Initializing ===
✓ Form elements found
```

### Pass Criteria:
- ✅ All initialization messages appear
- ✅ No JavaScript errors
- ✅ Page loads normally

### If Failed:
- Check Network tab for 404 errors on JS files
- Verify script tags in page source
- Check for syntax errors in console

---

## Test 2: Manual Work Order Modal

### Steps:
1. Click "Manual Work Order" button (top right)
2. Observe modal behavior
3. Check console logs

### Expected Console Output:
```
=== Manual Work Order Button Clicked ===
=== Opening Manual Work Order Modal ===
Dispatching loadManualWorkOrderData event
=== Loading Manual Work Order Data ===
Loading assets from /admin/assets/list...
Assets loaded successfully: X items
✓ Manual work order modal opened
```

### Expected UI Behavior:
- ✅ Modal opens smoothly
- ✅ Modal title: "Create Manual Work Order"
- ✅ Modal subtitle: "Create a work order without a maintenance request"
- ✅ Conversion banner NOT visible
- ✅ Equipment dropdown populates with assets
- ✅ Technician dropdown populates with technicians
- ✅ All fields are empty and editable
- ✅ Submit button says "Create Manual Work Order"
- ✅ Submit button is enabled

### Pass Criteria:
- ✅ Modal opens
- ✅ Dropdowns populate
- ✅ No console errors
- ✅ Form is interactive

### If Failed:
- Check if assets/technicians endpoints return data
- Verify modal element IDs match
- Check for event listener attachment

---

## Test 3: Modal Close Methods

### Test 3A: Close Button
1. Open modal
2. Click X button (top right)
3. Check console

**Expected:** 
```
Close button clicked
=== Closing Modal ===
✓ Modal closed
Resetting form
✓ Form reset complete
```

### Test 3B: Cancel Button
1. Open modal
2. Click "Cancel" button (bottom left)
3. Check console

**Expected:**
```
Cancel button clicked
=== Closing Modal ===
✓ Modal closed
```

### Test 3C: Escape Key
1. Open modal
2. Press `Esc` key
3. Check console

**Expected:**
```
Escape key pressed - closing modal
=== Closing Modal ===
✓ Modal closed
```

### Test 3D: Overlay Click
1. Open modal
2. Click outside modal (on dark overlay)
3. Check console

**Expected:**
```
Overlay clicked - closing modal
=== Closing Modal ===
✓ Modal closed
```

### Pass Criteria:
- ✅ All 4 methods close the modal
- ✅ Modal animates out smoothly
- ✅ Body scroll restored
- ✅ Form resets
- ✅ No console errors

---

## Test 4: Form Validation

### Steps:
1. Open manual work order modal
2. Click "Create Manual Work Order" without filling form
3. Observe validation

### Expected Behavior:
- ✅ Equipment error: "Please select equipment."
- ✅ Description error: "Please enter an issue description."
- ✅ Technician error: "Please assign a technician."
- ✅ Date errors if dates invalid
- ✅ Submit button disabled
- ✅ Red borders on invalid fields

### Pass Criteria:
- ✅ Validation prevents submission
- ✅ Error messages display
- ✅ Fields highlighted in red

---

## Test 5: Create Manual Work Order

### Steps:
1. Open manual work order modal
2. Fill all required fields:
   - Equipment: Select any
   - Description: "Test work order"
   - Technician: Select any
   - Priority: Select any
   - Start Date: Today
   - Expected Completion: Tomorrow
3. Click "Create Manual Work Order"
4. Observe behavior

### Expected Console Output:
```
=== SUBMIT CLICKED ===
=== FORM VALUES ===
Equipment raw value: X
Technician raw value: Y
=== PAYLOAD DEBUG ===
Work Order Data: {...}
✓ Pre-submit validation passed
Sending POST request to /admin/work-orders/create...
Response status: 200
Work order created successfully!
=== Closing Modal ===
```

### Expected UI Behavior:
- ✅ Submit button shows "Creating..."
- ✅ Submit button disabled during request
- ✅ Success toast appears: "Work order created successfully!"
- ✅ Modal closes automatically
- ✅ Page reloads after 1.5 seconds
- ✅ New work order appears in table

### Pass Criteria:
- ✅ Work order created in database
- ✅ Modal closes on success
- ✅ Toast notification shows
- ✅ Page refreshes
- ✅ No console errors

---

## Test 6: Convert Request to Work Order

### Steps:
1. Navigate to `/admin/maintenance-requests`
2. Find any "Pending" request
3. Click "Convert to Work Order" button
4. Observe redirect and modal behavior

### Expected Console Output:
```
=== Checking for Conversion Querystring ===
✓ Conversion detected - Request ID: X
✓ URL cleaned
=== Fetching Request Details ===
Request ID: X
Response status: 200
Request details loaded: {...}
=== Opening Conversion Modal ===
✓ Conversion modal opened
Dispatching prefillConversionData event
=== Prefilling Conversion Data ===
Conversion data: {...}
Assets and technicians loaded. Pre-filling form...
=== PREFILL DEBUG ===
Asset select element: [object HTMLSelectElement]
Attempting to set asset to ID: X Name: Y
✓ Asset successfully set to: X
Asset field locked with value: X
=== PREFILL COMPLETE ===
```

### Expected UI Behavior:
- ✅ Redirects to `/admin/work-orders` (querystring removed)
- ✅ Modal opens automatically
- ✅ Modal title: "Convert to Work Order"
- ✅ Modal subtitle: "Converting Request #XXXX"
- ✅ Blue conversion banner visible
- ✅ Equipment field locked and prefilled
- ✅ Description field locked and prefilled
- ✅ Priority locked and prefilled
- ✅ Technician field editable
- ✅ Date fields editable
- ✅ Submit button says "Convert to Work Order"
- ✅ Submit button enabled after prefill

### Pass Criteria:
- ✅ Modal auto-opens
- ✅ Fields prefilled correctly
- ✅ Locked fields cannot be edited
- ✅ Conversion banner visible
- ✅ No console errors

### If Failed:
- Check if `/admin/work-orders/request-details/{id}` endpoint works
- Verify querystring parameter present before redirect
- Check asset dropdown has matching asset ID
- Verify timing (setTimeout may need adjustment)

---

## Test 7: Submit Converted Work Order

### Steps:
1. Complete Test 6 (conversion modal open)
2. Fill editable fields:
   - Technician: Select any
   - Start Date: Today
   - Expected Completion: Tomorrow
3. Click "Convert to Work Order"
4. Observe behavior

### Expected Console Output:
```
=== SUBMIT CLICKED ===
Equipment field state:
  - value: X
  - locked: true
  - originalValue: X
=== FORM VALUES ===
Equipment raw value: X
=== PAYLOAD DEBUG ===
Work Order Data: {...}
MaintenanceRequestId: X
✓ Pre-submit validation passed
Sending POST request to /admin/work-orders/create...
Work order created successfully!
```

### Expected UI Behavior:
- ✅ Submit button shows "Converting..."
- ✅ Success toast: "Work order created successfully!"
- ✅ Modal closes
- ✅ Page reloads
- ✅ New work order in table with "Request #XXXX" source

### Pass Criteria:
- ✅ Work order created with MaintenanceRequestId
- ✅ Source shows "Request #XXXX"
- ✅ Original request status updated
- ✅ No console errors

---

## Test 8: Existing Features Still Work

### Test 8A: View Details
1. Click "Actions" on any work order
2. Click "View Details"
3. Verify details modal opens

**Pass:** ✅ Details modal works

### Test 8B: Edit Work Order
1. Click "Actions" on "Open" or "In Progress" work order
2. Click "Edit"
3. Verify edit modal opens with prefilled data

**Pass:** ✅ Edit modal works

### Test 8C: Update Status
1. Click "Actions" on any work order
2. Click "Update Status"
3. Verify status modal opens

**Pass:** ✅ Status modal works

### Test 8D: Cost Tracking
1. Open details modal
2. Update labor/other costs
3. Click "Save Cost"
4. Verify cost updates

**Pass:** ✅ Cost tracking works

### Test 8E: Parts Usage
1. Open details modal
2. Click "Add Part"
3. Select part and quantity
4. Click "Add"
5. Verify part added to table

**Pass:** ✅ Parts system works

### Test 8F: Filters
1. Use search box
2. Use status filter
3. Use priority filter
4. Click "Reset"

**Pass:** ✅ Filters work

---

## Test 9: Reopen Modal After Close

### Steps:
1. Open manual work order modal
2. Close it (any method)
3. Open it again
4. Verify form is clean

### Expected Behavior:
- ✅ Modal opens again
- ✅ Form is empty
- ✅ No validation errors
- ✅ Dropdowns populated
- ✅ Submit button enabled

### Pass Criteria:
- ✅ Modal can be reopened multiple times
- ✅ No stale data
- ✅ No duplicate event listeners

---

## Test 10: Error Handling

### Test 10A: Backend Error
1. Open modal
2. Fill form with invalid data (if possible)
3. Submit
4. Verify error handling

**Expected:** Error toast shows, modal stays open

### Test 10B: Network Error
1. Open DevTools → Network tab
2. Set throttling to "Offline"
3. Try to open modal
4. Verify graceful failure

**Expected:** Error message, no crash

### Test 10C: Missing Asset
1. Convert request with deleted asset
2. Verify error handling

**Expected:** Error message or manual asset addition

---

## Final Checklist

### Functionality:
- [ ] Manual work order modal opens
- [ ] Conversion modal auto-opens
- [ ] All close methods work
- [ ] Form validation works
- [ ] Manual work order creates successfully
- [ ] Conversion creates successfully
- [ ] Modal can reopen after close
- [ ] Existing features still work

### Console:
- [ ] No JavaScript errors
- [ ] All initialization logs present
- [ ] Event logs show correct flow
- [ ] AJAX requests succeed

### UI/UX:
- [ ] Modal animations smooth
- [ ] Buttons responsive
- [ ] Dropdowns populate
- [ ] Validation messages clear
- [ ] Toast notifications work
- [ ] Locked fields visually distinct

### Data Integrity:
- [ ] Work orders created in database
- [ ] MaintenanceRequestId set for conversions
- [ ] Source field correct
- [ ] All fields saved correctly

---

## Reporting Issues

If any test fails, report:

1. **Test Number:** (e.g., Test 6)
2. **Browser:** (Chrome, Firefox, Edge, etc.)
3. **Console Errors:** (copy full error)
4. **Network Tab:** (any failed requests)
5. **Expected vs Actual:** (what should happen vs what happened)
6. **Screenshots:** (if UI issue)

---

## Success Criteria

All tests must pass for the modal system to be considered stable and production-ready.

**Status:** 🟡 Awaiting Testing
**Last Updated:** [Current Date]
