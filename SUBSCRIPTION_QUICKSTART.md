# MaintenX Subscription Module - Quick Start Guide

## 🚀 Get Started in 5 Minutes

### Step 1: Apply Database Migration (1 minute)

Run the PowerShell script:
```powershell
.\ApplySaaSMigration.ps1
```

**What this does**:
- Creates `SubscriptionPlan` table
- Creates `CompanySubscription` table
- Seeds 3 default plans (Starter, Professional, Enterprise)
- Creates SuperAdmin role

### Step 2: Restart Application (1 minute)

The application will automatically:
- Register `SubscriptionService`
- Load subscription views
- Enable SuperAdmin navigation

### Step 3: Access SuperAdmin Panel (1 minute)

1. Login as SuperAdmin user
2. Navigate to **Platform → Subscription Plans**
3. You should see 3 default plans

### Step 4: Assign First Subscription (2 minutes)

1. Go to **Platform → Subscriptions**
2. Click **Assign Subscription**
3. Select:
   - Company: Choose a test company
   - Plan: Select "Starter"
   - Start Date: Today
   - End Date: 1 year from today
   - Trial: Unchecked
   - Payment Status: Paid
4. Click **Assign**

### Step 5: Verify (30 seconds)

1. Login as Admin of the test company
2. Check dashboard for subscription usage widget
3. Verify limits are displayed

---

## 🎯 Default Plans

### Starter Plan
- **$999/month** or **$9,990/year**
- 5 users, 50 assets, 100 work orders/month
- Perfect for small teams

### Professional Plan
- **$2,499/month** or **$24,990/year**
- 20 users, 200 assets, 500 work orders/month
- Ideal for growing businesses

### Enterprise Plan
- **$4,999/month** or **$49,990/year**
- Unlimited users, assets, and work orders
- Complete solution for large organizations

---

## 📊 SuperAdmin Features

### Subscription Plans (`/superadmin/subscriptions/plans`)
- View all plans
- Create new plans
- Edit existing plans
- Activate/deactivate plans
- See active tenant count

### Company Subscriptions (`/superadmin/subscriptions`)
- View all subscriptions
- Assign subscriptions
- Extend expiration dates
- Update payment status
- Monitor expiring subscriptions

### Dashboard (`/superadmin/dashboard`)
- Platform metrics
- Subscription distribution chart
- Expiring subscription alerts
- Suspended company tracking

---

## 🔧 Adding Enforcement (Optional)

### Quick Example: User Limit

1. Open `Controllers/UserManagementController.cs`
2. Inject `SubscriptionService`:
```csharp
private readonly SubscriptionService _subscriptionService;

public UserManagementController(
    // ... existing parameters ...
    SubscriptionService subscriptionService)
{
    // ... existing assignments ...
    _subscriptionService = subscriptionService;
}
```

3. Add check before user creation:
```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateUser(CreateUserViewModel model)
{
    var companyId = _tenantService.GetCurrentCompanyId();
    if (companyId.HasValue)
    {
        var (allowed, message) = await _subscriptionService.CanAddUserAsync(companyId.Value);
        if (!allowed)
        {
            ModelState.AddModelError("", message ?? "User limit reached");
            return View(model);
        }
    }
    
    // ... proceed with user creation ...
}
```

**See `SUBSCRIPTION_ENFORCEMENT_EXAMPLES.md` for more examples.**

---

## 🎨 Adding UI Components

### Subscription Banner (Warnings)

Add to your layout or dashboard:
```razor
@await Html.PartialAsync("_SubscriptionBanner")
```

Shows warnings for:
- Expiring subscriptions (30 days)
- Trial ending (7 days)
- Expired subscriptions

### Usage Widget (Admin Dashboard)

Add to admin dashboard:
```razor
@await Html.PartialAsync("_SubscriptionUsageWidget")
```

Shows:
- Current usage vs. limits
- Progress bars with color coding
- Upgrade prompts

---

## 🧪 Testing

### Test Scenario 1: View Plans
1. Login as SuperAdmin
2. Go to `/superadmin/subscriptions/plans`
3. Verify 3 default plans are visible

### Test Scenario 2: Assign Subscription
1. Go to `/superadmin/subscriptions`
2. Click "Assign Subscription"
3. Fill form and submit
4. Verify subscription appears in list

### Test Scenario 3: View Usage
1. Login as Admin of subscribed company
2. Check dashboard
3. Verify usage widget shows current consumption

### Test Scenario 4: Expiration Warning
1. As SuperAdmin, assign subscription with end date in 15 days
2. Login as Admin of that company
3. Verify warning banner appears

---

## 📖 Documentation

- **SUBSCRIPTION_MODULE.md** - Complete module documentation
- **SUBSCRIPTION_ENFORCEMENT_EXAMPLES.md** - Code examples
- **IMPLEMENTATION_SUMMARY.md** - What was implemented

---

## ❓ Troubleshooting

### Plans Not Showing
**Problem**: SuperAdmin sees empty plans page  
**Solution**: Run database migration script

### Cannot Assign Subscription
**Problem**: Companies dropdown is empty  
**Solution**: Ensure companies exist in database

### Usage Widget Not Showing
**Problem**: Widget doesn't appear on dashboard  
**Solution**: Verify user is Admin role and has CompanyId

### Enforcement Not Working
**Problem**: Can create users beyond limit  
**Solution**: Add enforcement code to controllers (see examples)

---

## 🎉 You're Done!

Your MaintenX instance now has:
- ✅ Subscription plan management
- ✅ Company subscription tracking
- ✅ Usage monitoring
- ✅ Expiration warnings
- ✅ SuperAdmin governance

**Next Steps**:
1. Customize default plans (pricing, limits)
2. Add enforcement to key controllers
3. Configure payment integration (future)

---

## 🆘 Need Help?

1. Check documentation files
2. Review code examples
3. Check application logs
4. Verify database schema

---

**Quick Start Complete!** 🚀

The subscription module is ready to use. Start by assigning subscriptions to your companies and monitoring usage through the SuperAdmin dashboard.
