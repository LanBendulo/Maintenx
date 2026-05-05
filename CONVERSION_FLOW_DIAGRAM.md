# Conversion Flow Diagram

## 🔄 Complete Conversion Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                    MAINTENANCE REQUESTS PAGE                        │
│                  /admin/maintenance-requests                        │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ User finds Approved request
                              │ Clicks Actions → "Convert to Work Order"
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                      JAVASCRIPT ACTION                              │
│  maintenance-requests.js                                            │
│                                                                     │
│  1. Fetch request details from API                                 │
│  2. Store in sessionStorage:                                       │
│     {                                                               │
│       maintenanceRequestId: 1,                                     │
│       requestNumber: "MR-0001",                                    │
│       assetId: 5,                                                  │
│       assetName: "HVAC Unit - Building A",                         │
│       description: "AC not cooling...",                            │
│       priority: "High"                                             │
│     }                                                               │
│  3. Redirect to: /admin/work-orders                                │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ Browser navigates
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                       WORK ORDERS PAGE                              │
│                    /admin/work-orders                               │
│                                                                     │
│  Page loads → work-orders.js executes                              │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ DOMContentLoaded event fires
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    AUTO-OPEN MODAL LOGIC                            │
│  work-orders.js                                                     │
│                                                                     │
│  1. Check sessionStorage for 'convertFromRequest'                  │
│  2. If found → setTimeout(openModal, 500ms)                        │
│  3. If not found → Do nothing (normal page load)                   │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ After 500ms delay
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    MODAL OPENS AUTOMATICALLY                        │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │  Convert Request to Work Order                          [×]   │ │
│  │  Converting MR-0001 to a work order                           │ │
│  ├───────────────────────────────────────────────────────────────┤ │
│  │                                                               │ │
│  │  Equipment / Asset *                                          │ │
│  │  [HVAC Unit - Building A ▼] ← LOCKED (gray, disabled)        │ │
│  │                                                               │ │
│  │  Issue Description *                                          │ │
│  │  [AC not cooling properly...] ← LOCKED (gray, disabled)      │ │
│  │                                                               │ │
│  │  Priority *                                                   │ │
│  │  ○ Low  ○ Medium  ● High ← LOCKED (faded, disabled)          │ │
│  │                                                               │ │
│  │  Assign Technician * ← USER FILLS                            │ │
│  │  [Select technician... ▼]                                     │ │
│  │                                                               │ │
│  │  Start Date * ← USER FILLS                                    │ │
│  │  [2026-05-03]                                                 │ │
│  │                                                               │ │
│  │  Expected Completion * ← USER FILLS                           │ │
│  │  [2026-05-10]                                                 │ │
│  │                                                               │ │
│  │  Notes (optional) ← USER FILLS                                │ │
│  │  [Additional instructions...]                                 │ │
│  │                                                               │ │
│  ├───────────────────────────────────────────────────────────────┤ │
│  │  [Cancel]              [✓ Convert to Work Order]             │ │
│  └───────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ User fills required fields
                              │ Clicks "Convert to Work Order"
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    SUBMIT TO BACKEND                                │
│  POST /admin/work-orders/create                                    │
│                                                                     │
│  Request Body:                                                      │
│  {                                                                  │
│    maintenanceRequestId: 1,        ← From sessionStorage           │
│    assetId: 5,                     ← From sessionStorage (locked)  │
│    description: "AC not cooling...", ← From sessionStorage (locked)│
│    priority: "High",               ← From sessionStorage (locked)  │
│    assignedTo: 3,                  ← User filled                   │
│    dateCreated: "2026-05-03",      ← User filled                   │
│    dueDate: "2026-05-10",          ← User filled                   │
│    notes: "Check filters first"    ← User filled                   │
│  }                                                                  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ Backend processes
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    BACKEND VALIDATION                               │
│  DashboardController.CreateWorkOrder()                             │
│                                                                     │
│  1. Validate maintenanceRequestId exists                           │
│  2. Check request.Status == "Approved"                             │
│  3. Check request.WorkOrder == null (not already converted)        │
│  4. ENFORCE data from request:                                     │
│     - model.AssetId = request.AssetId                              │
│     - model.Description = request.Title + request.Description      │
│     - model.Priority = request.Priority                            │
│  5. Create WorkOrder with Status = "Open"                          │
│  6. Set WorkOrder.MaintenanceRequestId = request.RequestId         │
│  7. Update request.Status = "Converted"                            │
│  8. Save to database                                               │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ Success response
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    SUCCESS HANDLING                                 │
│  work-orders.js                                                     │
│                                                                     │
│  1. Close modal                                                     │
│  2. Show green toast: "Request converted successfully!"            │
│  3. Clear sessionStorage                                            │
│  4. Reload page after 1.5 seconds                                  │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ Page reloads
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    WORK ORDERS TABLE                                │
│                                                                     │
│  ┌────────┬──────────────────┬─────────────┬────────────┬────────┐ │
│  │ WO ID  │ Source           │ Equipment   │ Technician │ Status │ │
│  ├────────┼──────────────────┼─────────────┼────────────┼────────┤ │
│  │#WO-0001│Request #MR-0001  │HVAC Unit    │Juan D.     │ Open   │ │
│  │        │ ← NEW!           │             │            │        │ │
│  └────────┴──────────────────┴─────────────┴────────────┴────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ User navigates back
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                    MAINTENANCE REQUESTS TABLE                       │
│                                                                     │
│  ┌──────────┬───────────┬──────────┬──────────┬────────────────┐   │
│  │Request # │ Title     │ Asset    │ Priority │ Status         │   │
│  ├──────────┼───────────┼──────────┼──────────┼────────────────┤   │
│  │MR-0001   │Test AC    │HVAC Unit │ High     │ Converted ✓    │   │
│  │          │           │          │          │ [View WO]      │   │
│  └──────────┴───────────┴──────────┴──────────┴────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔑 Key Points

### **1. sessionStorage is the Bridge**
```javascript
// Maintenance Requests page stores data
sessionStorage.setItem('convertFromRequest', JSON.stringify(data));

// Work Orders page reads data
const data = sessionStorage.getItem('convertFromRequest');
```

### **2. Auto-Open on Page Load**
```javascript
window.addEventListener('DOMContentLoaded', function() {
    const convertData = sessionStorage.getItem('convertFromRequest');
    if (convertData) {
        setTimeout(() => openModal(), 500);
    }
});
```

### **3. Fields are Locked in UI**
```javascript
assetSelect.disabled = true;
assetSelect.style.background = '#F0F4F8';
assetSelect.style.cursor = 'not-allowed';
```

### **4. Backend Enforces Data**
```csharp
if (model.MaintenanceRequestId.HasValue) {
    model.AssetId = request.AssetId;
    model.Description = request.Title + "\n\n" + request.Description;
    model.Priority = request.Priority;
}
```

---

## 🎨 Visual States

### **State 1: Before Conversion**
```
Maintenance Request MR-0001
├─ Status: Approved ✓
└─ Actions: [Approve] [Reject] [Convert to Work Order]
```

### **State 2: During Conversion**
```
Work Orders Page
└─ Modal: Convert Request to Work Order
   ├─ Asset: [LOCKED] HVAC Unit
   ├─ Description: [LOCKED] AC not cooling...
   ├─ Priority: [LOCKED] ● High
   ├─ Technician: [EDITABLE] Select...
   ├─ Start Date: [EDITABLE] ___
   └─ Due Date: [EDITABLE] ___
```

### **State 3: After Conversion**
```
Work Order #WO-0001
├─ Source: Request #MR-0001
├─ Asset: HVAC Unit (from request)
├─ Description: AC not cooling... (from request)
├─ Priority: High (from request)
├─ Technician: Juan Dela Cruz (user filled)
├─ Start Date: 2026-05-03 (user filled)
├─ Due Date: 2026-05-10 (user filled)
└─ Status: Open

Maintenance Request MR-0001
├─ Status: Converted ✓
└─ Actions: [View Details] [View Work Order]
```

---

## 🔄 Data Flow Summary

```
User Action → JavaScript → sessionStorage → Redirect → Page Load
    ↓
Auto-open Modal → Pre-fill Fields → Lock Fields → User Fills
    ↓
Submit → Backend → Validate → Enforce Data → Create WO
    ↓
Update Request → Save DB → Success → Reload → Display
```

---

## ✅ Success Indicators

| Step | What You Should See |
|------|---------------------|
| Click "Convert" | Redirect to /admin/work-orders |
| Page loads | Modal opens automatically (~500ms) |
| Modal opens | Title: "Convert Request to Work Order" |
| Fields | Asset, Description, Priority are grayed out |
| Fill fields | Technician, Dates are editable |
| Submit | Button says "Converting..." |
| Success | Green toast appears |
| Reload | Work order in table with Source = "Request #MR-XXXX" |
| Go back | Request status = "Converted" |

---

**This is the complete flow from start to finish!**
