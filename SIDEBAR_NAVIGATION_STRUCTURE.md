# Sidebar Navigation Structure

## Overview
The sidebar has been refactored to align with the actual maintenance workflow and module responsibilities, improving usability and system clarity.

---

## Navigation Structure

### 📊 MAIN
Core operational modules for day-to-day maintenance management.

#### Dashboard
- **Purpose**: Overview of system status, metrics, and KPIs
- **Shows**: Active work orders, pending requests, asset health, recent activity

#### Maintenance Requests
- **Purpose**: User-submitted maintenance needs
- **Workflow**: Users report issues → Admin reviews → Approve/Reject → Convert to Work Order
- **Badge**: Shows count of pending requests
- **Status Flow**: Pending → Approved → Converted (or Rejected)

#### Work Orders
- **Purpose**: Actionable maintenance tasks assigned to technicians
- **Workflow**: Created from approved requests OR manually created
- **Status Flow**: Open → In Progress → Completed (or Cancelled)
- **Features**: 
  - Edit operational details (technician, dates, notes)
  - Update status separately
  - Track actual completion dates

---

### 🏭 ASSETS & PLANNING
Asset management and proactive maintenance planning.

#### Assets
- **Purpose**: Equipment/asset inventory and tracking
- **Renamed from**: "Equipment / Assets"
- **Contains**: Asset details, location, status, purchase info, maintenance history
- **Integration**: Links to work orders and maintenance logs

#### Preventive Maintenance
- **Purpose**: Scheduled maintenance to prevent breakdowns
- **Workflow**: Schedule → Auto-create Work Orders (NOT Requests)
- **Key Rule**: Creates Work Orders directly, bypassing the request approval flow
- **Examples**: Quarterly inspections, annual servicing, routine checks

#### Maintenance Logs
- **Purpose**: Historical record of all completed maintenance
- **Key Rule**: READ-ONLY (auto-generated from completed Work Orders)
- **Lock Icon**: Visual indicator that this is view-only
- **Contains**: What was done, when, by whom, parts used, costs
- **Integration**: Automatically populated when Work Orders are marked as Completed

---

### 📦 INVENTORY & COST
Resource management and financial tracking.

#### Spare Parts
- **Purpose**: Inventory management for maintenance parts
- **Integration**: Links to Work Orders (parts used per job)
- **Tracks**: Stock levels, reorder points, part costs, suppliers
- **Workflow**: Parts consumed → Linked to Work Order → Updates inventory

#### Cost Tracking
- **Purpose**: Financial analysis of maintenance operations
- **Integration**: Pulls data from Work Orders (labor + parts)
- **Reports**: Cost per asset, cost per work order, budget vs actual
- **Breakdown**: Labor costs, parts costs, total maintenance spend

---

### 👥 MANAGEMENT
Personnel and scheduling management.

#### Personnel
- **Purpose**: Technician and staff management
- **Renamed from**: "Employee Management"
- **Contains**: Skills, certifications, hourly rates, availability
- **Integration**: Assigned to Work Orders, tracked in logs

#### Schedule / Calendar
- **Purpose**: Visual scheduling and resource planning
- **Shows**: Work order timelines, technician assignments, preventive maintenance schedules
- **Features**: Drag-and-drop scheduling, conflict detection, workload balancing

---

### ⚙️ SYSTEM
Administrative and configuration settings.

#### User Management
- **Purpose**: User accounts and access control
- **Manages**: Login credentials, roles (Admin, Technician, User), permissions

#### Settings
- **Purpose**: System configuration and preferences
- **Contains**: Email settings, notification preferences, system defaults, integrations

---

## Workflow Alignment

### Reactive Maintenance Flow
```
User Reports Issue
    ↓
Maintenance Request (Pending)
    ↓
Admin Reviews → Approve
    ↓
Convert to Work Order
    ↓
Assign Technician
    ↓
Execute Work (In Progress)
    ↓
Complete Work Order
    ↓
Auto-Generate Maintenance Log
```

### Proactive Maintenance Flow
```
Preventive Maintenance Schedule
    ↓
Auto-Create Work Order (bypasses Request)
    ↓
Assign Technician
    ↓
Execute Work (In Progress)
    ↓
Complete Work Order
    ↓
Auto-Generate Maintenance Log
```

### Inventory Integration
```
Work Order Created
    ↓
Technician Uses Parts
    ↓
Parts Linked to Work Order
    ↓
Inventory Updated
    ↓
Cost Calculated (Labor + Parts)
    ↓
Cost Tracking Updated
```

---

## Key Design Principles

### 1. Workflow-Based Organization
Modules are grouped by their role in the maintenance process, not by data type.

### 2. Clear Separation of Concerns
- **Requests** = User-initiated needs
- **Work Orders** = Actionable tasks
- **Logs** = Historical records

### 3. Read-Only Enforcement
Maintenance Logs cannot be manually edited—they're generated from completed Work Orders to ensure data integrity.

### 4. Direct Work Order Creation
Preventive Maintenance creates Work Orders directly, not Requests, because scheduled maintenance doesn't need approval.

### 5. Integrated Cost Tracking
Costs are calculated from Work Orders (labor + parts), not entered separately, ensuring accuracy.

---

## Visual Indicators

### Badge (Maintenance Requests)
- Shows count of pending requests
- Updates in real-time
- Helps admins prioritize work

### Lock Icon (Maintenance Logs)
- Indicates read-only module
- Prevents accidental edit attempts
- Reinforces data integrity

---

## Module Responsibilities

| Module | Create | Read | Update | Delete | Generate |
|--------|--------|------|--------|--------|----------|
| Maintenance Requests | ✅ | ✅ | ✅ | ❌ | - |
| Work Orders | ✅ | ✅ | ✅ | ❌ | - |
| Assets | ✅ | ✅ | ✅ | ❌ | - |
| Preventive Maintenance | ✅ | ✅ | ✅ | ❌ | Work Orders |
| Maintenance Logs | ❌ | ✅ | ❌ | ❌ | Auto from WO |
| Spare Parts | ✅ | ✅ | ✅ | ❌ | - |
| Cost Tracking | ❌ | ✅ | ❌ | ❌ | Auto from WO |
| Personnel | ✅ | ✅ | ✅ | ❌ | - |
| Schedule | ✅ | ✅ | ✅ | ❌ | - |

**Legend:**
- ✅ = Allowed
- ❌ = Not allowed (enforced by system)
- Auto = Automatically generated

---

## Benefits of New Structure

### ✅ Improved Usability
- Logical grouping makes navigation intuitive
- Users can find modules based on their task, not data type

### ✅ Workflow Clarity
- Structure reflects actual operational flow
- New users understand the process by looking at the sidebar

### ✅ Data Integrity
- Read-only modules prevent accidental data corruption
- Auto-generation ensures consistency

### ✅ Better Integration
- Related modules are grouped together
- Clear relationships between modules (e.g., Work Orders → Logs)

### ✅ Scalability
- Easy to add new modules to appropriate sections
- Clear separation makes future development easier

---

## Implementation Notes

### Files Modified
- `Views/Shared/_AdminLayout.cshtml` - Sidebar navigation structure

### Changes Made
1. Reorganized navigation into 4 logical sections
2. Renamed "Equipment / Assets" → "Assets"
3. Renamed "Employee Management" → "Personnel"
4. Added "Preventive Maintenance" module
5. Added "Maintenance Logs" module (with lock icon)
6. Added "Spare Parts" module
7. Added "Cost Tracking" module
8. Reordered modules to follow workflow

### Visual Enhancements
- Lock icon on Maintenance Logs to indicate read-only
- Section labels clearly separate module groups
- Consistent iconography for related modules

---

## Future Enhancements

### Planned Features
1. **Preventive Maintenance Module**: Auto-schedule work orders based on asset maintenance intervals
2. **Maintenance Logs Module**: Read-only view of completed work with filtering and export
3. **Spare Parts Module**: Inventory management with low-stock alerts
4. **Cost Tracking Module**: Financial reports and budget analysis
5. **Assets Module**: Full asset lifecycle management

### Integration Points
- Preventive Maintenance → Work Orders (auto-create)
- Work Orders → Maintenance Logs (auto-generate on completion)
- Work Orders → Spare Parts (track parts usage)
- Work Orders → Cost Tracking (calculate costs)
- Assets → All modules (central reference point)

---

## Testing Checklist

- [ ] All navigation links are accessible
- [ ] Active state highlights correctly
- [ ] Section labels are visible and clear
- [ ] Lock icon appears on Maintenance Logs
- [ ] Badge shows on Maintenance Requests
- [ ] Responsive design works on mobile
- [ ] Icons are consistent and meaningful
- [ ] Hover states work correctly

---

## User Training Notes

### For Admins
- **Main section**: Daily operations (Requests → Work Orders)
- **Assets & Planning**: Long-term planning and history
- **Inventory & Cost**: Resource and financial management
- **Management**: People and scheduling
- **System**: Configuration and access control

### For Technicians
- Focus on: Work Orders, Assets, Maintenance Logs, Spare Parts
- Less relevant: Maintenance Requests, User Management, Settings

### For Regular Users
- Focus on: Maintenance Requests (submit issues)
- View-only: Work Orders (track their requests)

---

## Conclusion

The refactored sidebar navigation aligns with real-world maintenance workflows, improving system usability and clarity. The structure reflects how maintenance operations actually work, making the system more intuitive for all users.
