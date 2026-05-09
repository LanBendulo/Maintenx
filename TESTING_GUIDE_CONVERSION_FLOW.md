# Testing Guide: Convert Request to Work Order Flow

## Quick Test Steps

### Test 1: Happy Path - Successful Conversion
1. Navigate to **Maintenance Requests** page
2. Find an **Approved** request
3. Click **Actions** → **Convert to Work Order**
4. **Expected Result**:
   - ✅ Redirects to `/admin/work-orders`
   - ✅ Modal opens automatically within 300ms
   - ✅ Blue banner shows: "Converting from Maintenance Request..."
   - ✅ Equipment field is filled and locked (gray background)
   - ✅ Description is filled and locked
   - ✅ Priority is selected and locked
   - ✅ Submit button shows "Convert to Work Order"
   - ✅ URL is clean (no `?convertRequestId=` visible)
5. Select a technician
6. Verify dates are set
7. Click **Convert to Work Order**
8. **Expected Result**:
   - ✅ Work order created successfully
   - ✅ Toast: "Request converted to work order successfully!"
   - ✅ Page reloads showing new work order

### Test 2: Request Not Found
1. Manually navigate to: `/admin/work-orders?convertRequestId=99999`
2. **Expected Result**:
   - ✅ Toast: "Maintenance request no longer exists."
   - ✅ URL cleaned to `/admin/work-orders`
   - ✅ Modal does NOT open

### Test 3: Already Converted Request
1. Find a request that's already been converted (Status: "Converted")
2. Try to convert it again using the URL: `/admin/work-orders?convertRequestId={id}`
3. **Expected Result**:
   - ✅ Toast: "This request has already been converted."
   - ✅ URL cleaned
   - ✅ Modal does NOT open

### Test 4: Pending Request (Not Approved)
1. Find a **Pending** request
2. Click **Actions** → **Convert to Work Order**
3. **Expected Result**:
   - ✅ Toast: "Only approved requests can be converted. Current status: Pending"
   - ✅ URL cleaned
   - ✅ Modal does NOT open

### Test 5: Rejected Request
1. Find a **Rejected** request
2. Try to convert using URL: `/admin/work-orders?convertRequestId={id}`
3. **Expected Result**:
   - ✅ Toast: "Only approved requests can be converted. Current status: Rejected"
   - ✅ URL cleaned
   - ✅ Modal does NOT open

### Test 6: Equipment Field Validation
1. Convert an approved request
2. Wait for modal to open and prefill
3. Open browser console (F12)
4. **Expected Console Output**:
   ```
   === PAGE LOADED - Checking for conversion request ===
   convertRequestId from URL: 12
   Conversion request detected. Loading request details...
   Request details response status: 200
   Request details loaded: {requestId: 12, ...}
   Request data stored in sessionStorage
   URL cleaned
   Opening conversion modal...
   === PREFILL DEBUG ===
   Attempting to set asset to ID: 5
   After setting - assetSelect.value: 5
   ✓ Asset successfully set to: 5
   Asset field locked with value: 5
   === PREFILL COMPLETE ===
   Final equipment value: 5
   Submit button enabled: true
   ```
5. Click **Convert to Work Order**
6. **Expected Console Output**:
   ```
   === SUBMIT CLICKED ===
   Equipment field state:
     - value: 5
     - locked: true
     - originalValue: 5
   === VALIDATION DEBUG ===
   Equipment value: 5
   Equipment locked: true
   === VALIDATION RESULT: PASS ===
   === PAYLOAD DEBUG ===
   AssetId: 5 Type: number IsNaN: false
   ✓ Pre-submit validation passed
   ```

### Test 7: Page Refresh After Modal Opens
1. Convert an approved request
2. Wait for modal to open
3. Press **F5** to refresh the page
4. **Expected Result**:
   - ✅ Page reloads normally
   - ✅ Modal does NOT reopen
   - ✅ No errors in console

### Test 8: Multiple Tabs
1. Open two browser tabs with the work orders page
2. In Tab 1: Convert request A
3. In Tab 2: Convert request B
4. **Expected Result**:
   - ✅ Each tab works independently
   - ✅ No cross-tab interference
   - ✅ Each shows correct request data

### Test 9: Network Error Simulation
1. Open browser DevTools → Network tab
2. Set throttling to "Offline"
3. Try to convert a request
4. **Expected Result**:
   - ✅ Toast: "An error occurred while loading the request."
   - ✅ URL cleaned
   - ✅ Modal does NOT open
   - ✅ No JavaScript errors

### Test 10: Manual Work Order (Non-Conversion)
1. Navigate to `/admin/work-orders`
2. Click **Manual Work Order** button
3. **Expected Result**:
   - ✅ Modal opens normally
   - ✅ NO blue conversion banner
   - ✅ All fields are editable (not locked)
   - ✅ Equipment dropdown is interactive
   - ✅ Submit button shows "Create Manual Work Order"

## Console Commands for Testing

### Check Current URL Parameters
```javascript
const params = new URLSearchParams(window.location.search);
console.log('convertRequestId:', params.get('convertRequestId'));
```

### Check SessionStorage
```javascript
console.log('convertFromRequest:', sessionStorage.getItem('convertFromRequest'));
```

### Manually Trigger Conversion (for testing)
```javascript
window.location.href = '/admin/work-orders?convertRequestId=12';
```

### Clear SessionStorage (if stuck)
```javascript
sessionStorage.removeItem('convertFromRequest');
```

## Common Issues & Solutions

### Issue: Modal doesn't open
**Check**:
1. Console for errors
2. Network tab for failed AJAX request
3. Request status (must be "Approved")
4. Request not already converted

### Issue: Equipment field empty
**Check**:
1. Console logs for "Asset successfully set"
2. Asset exists in database
3. Asset is Active status
4. CompanyId matches

### Issue: Validation fails
**Check**:
1. Console logs for validation debug output
2. Equipment value is set (not empty string)
3. Equipment is locked (dataset.locked === 'true')
4. All required fields filled

### Issue: URL not cleaning
**Check**:
1. Browser supports history.replaceState
2. No JavaScript errors before replaceState call
3. Console shows "URL cleaned" message

## Success Criteria

✅ All 10 tests pass
✅ No console errors
✅ Professional error messages
✅ Smooth user experience
✅ URL stays clean
✅ Equipment validation works
✅ Conversion completes successfully

## Regression Testing

After any changes, re-run:
- Test 1 (Happy Path)
- Test 6 (Equipment Validation)
- Test 10 (Manual Work Order)

These three tests cover the critical paths.
