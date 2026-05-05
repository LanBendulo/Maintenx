# Sidebar Navigation - Quick Reference

## 📋 Navigation Structure

```
┌─────────────────────────────────────┐
│  MaintenX                           │
├─────────────────────────────────────┤
│                                     │
│  📊 MAIN                            │
│  ├─ Dashboard                       │
│  ├─ Maintenance Requests [badge]   │
│  └─ Work Orders                     │
│                                     │
│  🏭 ASSETS & PLANNING               │
│  ├─ Assets                          │
│  ├─ Preventive Maintenance          │
│  └─ Maintenance Logs 🔒             │
│                                     │
│  📦 INVENTORY & COST                │
│  ├─ Spare Parts                     │
│  └─ Cost Tracking                   │
│                                     │
│  👥 MANAGEMENT                      │
│  ├─ Personnel                       │
│  └─ Schedule / Calendar             │
│                                     │
│  ⚙️ SYSTEM                          │
│  ├─ User Management                 │
│  └─ Settings                        │
│                                     │
└─────────────────────────────────────┘
```

---

## 🔄 Workflow Paths

### Path 1: Reactive Maintenance (User Reports Issue)
```
Maintenance Requests → Work Orders → Maintenance Logs
```

### Path 2: Proactive Maintenance (Scheduled)
```
Preventive Maintenance → Work Orders → Maintenance Logs
```

### Path 3: Resource Management
```
Assets → Work Orders → Spare Parts → Cost Tracking
```

---

## 🎯 Quick Access by Role

### 👨‍💼 Admin
**Daily Use:**
- Dashboard
- Maintenance Requests
- Work Orders

**Weekly Use:**
- Assets
- Personnel
- Cost Tracking

**Monthly Use:**
- Preventive Maintenance
- Maintenance Logs
- Settings

---

### 🔧 Technician
**Daily Use:**
- Work Orders
- Assets
- Spare Parts

**Weekly Use:**
- Maintenance Logs
- Schedule / Calendar

**Rarely:**
- Dashboard (view only)

---

### 👤 Regular User
**Daily Use:**
- Maintenance Requests (submit)

**View Only:**
- Work Orders (track status)

---

## 🔑 Key Indicators

| Symbol | Meaning |
|--------|---------|
| 🔒 | Read-only module |
| [badge] | Shows count (e.g., pending requests) |
| ⚠️ | Requires approval |
| ✅ | Auto-generated |

---

## 📝 Module Cheat Sheet

| Module | Purpose | Can Create? | Can Edit? | Auto-Generated? |
|--------|---------|-------------|-----------|-----------------|
| Dashboard | Overview | ❌ | ❌ | ✅ |
| Maintenance Requests | Report issues | ✅ | ✅ | ❌ |
| Work Orders | Execute tasks | ✅ | ✅ | ⚠️ (from PM) |
| Assets | Track equipment | ✅ | ✅ | ❌ |
| Preventive Maintenance | Schedule work | ✅ | ✅ | ❌ |
| Maintenance Logs | History | ❌ | ❌ | ✅ |
| Spare Parts | Inventory | ✅ | ✅ | ❌ |
| Cost Tracking | Financials | ❌ | ❌ | ✅ |
| Personnel | Staff | ✅ | ✅ | ❌ |
| Schedule | Calendar | ✅ | ✅ | ❌ |
| User Management | Accounts | ✅ | ✅ | ❌ |
| Settings | Config | ❌ | ✅ | ❌ |

---

## 🚀 Common Tasks

### Submit a Maintenance Request
1. Click **Maintenance Requests**
2. Click "Create Request"
3. Fill in details
4. Submit

### Create a Work Order
1. Click **Work Orders**
2. Click "Manual Work Order" OR
3. Go to **Maintenance Requests** → Approve → Convert

### Schedule Preventive Maintenance
1. Click **Preventive Maintenance**
2. Create schedule
3. System auto-creates Work Orders

### Check Maintenance History
1. Click **Maintenance Logs** 🔒
2. Filter by asset, date, or technician
3. View details (read-only)

### Track Costs
1. Click **Cost Tracking**
2. View reports
3. Filter by date, asset, or work order

### Manage Inventory
1. Click **Spare Parts**
2. Add/update parts
3. Track usage via Work Orders

---

## 💡 Pro Tips

### Tip 1: Use the Badge
The badge on **Maintenance Requests** shows pending count. Click to see what needs attention.

### Tip 2: Maintenance Logs are Auto-Generated
Don't try to edit Maintenance Logs—they're created automatically when Work Orders are completed.

### Tip 3: Preventive Maintenance Bypasses Requests
Scheduled maintenance creates Work Orders directly, no approval needed.

### Tip 4: Cost Tracking is Calculated
Costs are auto-calculated from Work Orders (labor + parts), ensuring accuracy.

### Tip 5: Link Everything to Assets
Always link Work Orders, Parts, and Costs to Assets for better tracking.

---

## 🔍 Finding What You Need

| I want to... | Go to... |
|--------------|----------|
| Report a problem | Maintenance Requests |
| Assign work to a technician | Work Orders |
| See what was done last month | Maintenance Logs |
| Check equipment details | Assets |
| Schedule routine maintenance | Preventive Maintenance |
| See how much we spent | Cost Tracking |
| Check parts inventory | Spare Parts |
| Manage technicians | Personnel |
| View work schedule | Schedule / Calendar |
| Add a new user | User Management |
| Change system settings | Settings |

---

## 📞 Need Help?

Refer to `SIDEBAR_NAVIGATION_STRUCTURE.md` for detailed documentation.
