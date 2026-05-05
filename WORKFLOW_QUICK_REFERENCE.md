# Workflow Quick Reference Card

## 🎯 Primary Flow: Request → Approve → Convert → Execute

```
┌─────────────────────────────────────────────────────────────────┐
│                    MAINTENANCE REQUEST                          │
│  Entry Point: "New Request" button                             │
│  Status: Pending → Approved → Rejected → Converted             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
                         [APPROVE]
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                  CONVERT TO WORK ORDER                          │
│  Pre-filled (Read-Only): Asset, Description, Priority          │
│  Required: Technician, Start Date, Due Date                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                       WORK ORDER                                │
│  Status: Open → In Progress → Completed → Cancelled            │
│  Source: "Request #MR-XXXX" or "Manual"                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📋 Status Reference

### Maintenance Request Statuses:
| Status | Meaning | Available Actions |
|--------|---------|-------------------|
| **Pending** | Awaiting review | Approve, Reject |
| **Approved** | Ready for conversion | Convert to Work Order |
| **Rejected** | Not approved | View only |
| **Converted** | Linked to Work Order | View Work Order |

### Work Order Statuses:
| Status | Meaning | Next Action |
|--------|---------|-------------|
| **Open** | Newly created | Assign/Start work |
| **In Progress** | Technician working | Complete work |
| **Completed** | Work finished | Archive |
| **Cancelled** | Work cancelled | Archive |

---

## 🔑 Key Rules

### ✅ ALLOWED:
- Create Maintenance Request (any user with permission)
- Approve Pending requests (Admin/Manager)
- Convert Approved requests (Admin/Manager)
- Create Manual Work Orders (Admin/Manager)
- Update Work Order status (Technician/Admin)

### ❌ BLOCKED:
- Convert Pending requests (must approve first)
- Convert Rejected requests (rejected = no work)
- Convert same request twice (one-to-one relationship)
- Edit Converted requests (data locked)
- Change Asset/Description/Priority when converting (enforced from request)

---

## 🎨 UI Button Labels

| Page | Button | Purpose |
|------|--------|---------|
| Maintenance Requests | **"New Request"** | Primary entry point |
| Work Orders | **"Manual Work Order"** | Urgent/unplanned work |
| Request Actions (Pending) | **"Approve"** | Approve request |
| Request Actions (Pending) | **"Reject"** | Reject request |
| Request Actions (Approved) | **"Convert to Work Order"** | Start conversion |
| Request Actions (Converted) | **"View Work Order"** | See linked WO |

---

## 📊 Table Columns

### Maintenance Requests Table:
- Request # (MR-0001)
- Title
- Asset
- **Category** (NEW)
- Priority
- Status
- Requested By
- Created Date

### Work Orders Table:
- WO ID (#WO-0001)
- **Source** (NEW: "Request #MR-XXXX" or "Manual")
- Equipment / Asset
- Technician
- Priority
- Status
- Start Date
- Expected Completion

---

## 🔍 Filters

### Maintenance Requests:
- Status: All / Pending / Approved / Rejected / Converted
- Priority: All / High / Medium / Low
- Search: Request #, Title, Asset

### Work Orders:
- Status: All / Open / In Progress / Completed / Cancelled
- Priority: All / High / Medium / Low
- Technician: All / [List of technicians]
- **Source: All / From Request / Manual** (NEW)
- Search: WO ID, Equipment, Technician

---

## 🔒 Read-Only Fields (When Converting)

When converting an Approved request to a Work Order, these fields are **locked**:

| Field | Source | Why Locked |
|-------|--------|------------|
| **Asset** | From Request | Prevent changing equipment |
| **Description** | From Request | Preserve original issue |
| **Priority** | From Request | Maintain urgency level |

**Editable fields:**
- Assign Technician ✏️
- Start Date ✏️
- Expected Completion ✏️
- Notes ✏️

---

## 🚦 Workflow Decision Tree

```
User has maintenance issue
    ↓
Is it urgent/emergency?
    ├─ YES → Create Manual Work Order (skip approval)
    └─ NO → Create Maintenance Request
              ↓
         Admin reviews
              ↓
         Approve or Reject?
              ├─ REJECT → End (no work order)
              └─ APPROVE → Convert to Work Order
                              ↓
                         Assign Technician
                              ↓
                         Execute Work
                              ↓
                         Mark Completed
```

---

## 📞 Common Questions

### Q: When should I use "New Request" vs "Manual Work Order"?
**A:** Use "New Request" for planned maintenance that needs approval. Use "Manual Work Order" for urgent/emergency work that can't wait for approval.

### Q: Can I edit a request after it's converted?
**A:** No. Once converted, the request is locked to maintain data integrity.

### Q: Can I convert a request without approving it first?
**A:** No. Only Approved requests can be converted.

### Q: What happens if I try to convert the same request twice?
**A:** The system will block it. Each request can only be converted once.

### Q: Can I change the Asset when converting?
**A:** No. Asset, Description, and Priority are locked from the original request.

### Q: How do I know if a Work Order came from a request?
**A:** Check the "Source" column. It will show "Request #MR-XXXX" or "Manual".

---

## 🎯 Best Practices

### For Requesters:
1. ✅ Use clear, short titles (5-10 words)
2. ✅ Provide detailed descriptions
3. ✅ Upload photos when possible
4. ✅ Select correct priority
5. ✅ Choose appropriate category

### For Admins:
1. ✅ Review requests promptly
2. ✅ Approve only actionable requests
3. ✅ Reject duplicates or unclear requests
4. ✅ Convert approved requests within 24 hours
5. ✅ Assign appropriate technicians

### For Technicians:
1. ✅ Update status regularly
2. ✅ Add notes on progress
3. ✅ Mark completed when done
4. ✅ Upload completion photos

---

## 📈 Metrics to Track

- **Average Approval Time**: Request Created → Approved
- **Average Conversion Time**: Approved → Work Order Created
- **Average Completion Time**: Work Order Created → Completed
- **Approval Rate**: Approved / Total Requests
- **Source Distribution**: Manual vs. From Request
- **Category Distribution**: Which categories are most common

---

## ✨ Quick Tips

💡 **Tip 1**: Use filters to focus on what matters (e.g., "Pending" requests needing approval)

💡 **Tip 2**: The "Source" column helps identify planned vs. urgent work

💡 **Tip 3**: Converted requests show a "View Work Order" link for easy navigation

💡 **Tip 4**: Priority from requests carries over to work orders automatically

💡 **Tip 5**: Use the search box to quickly find specific requests or work orders

---

## 🔧 Troubleshooting

| Issue | Solution |
|-------|----------|
| Can't convert request | Check if status is "Approved" |
| Fields are locked in modal | This is correct for conversions (Asset, Description, Priority) |
| Request already converted | Each request can only be converted once |
| Can't find work order | Use "Source" filter or search by Request # |
| Status won't update | Check user permissions |

---

## 📚 Related Documentation

- **STREAMLINED_WORKFLOW_GUIDE.md** - Detailed workflow documentation
- **MAINTENANCE_REQUEST_ENHANCEMENTS.md** - Feature enhancements
- **QUICK_START_GUIDE.md** - Testing checklist

---

**Last Updated**: May 2, 2026  
**Version**: 2.0 (Streamlined Workflow)
