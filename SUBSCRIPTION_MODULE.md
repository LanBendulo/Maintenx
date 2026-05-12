# MaintenX SaaS Subscription Management Module

## Overview

The MaintenX Subscription Management module provides lightweight, operational SaaS subscription governance for multi-tenant maintenance management. This module focuses on tenant lifecycle management, plan enforcement, and subscription visibility without overengineering billing automation.

## Architecture

### Core Components

1. **Models**
   - `SubscriptionPlan` - Platform-level subscription plans
   - `CompanySubscription` - Tenant subscription assignments
   - `Company` - Enhanced with subscription fields

2. **Services**
   - `SubscriptionService` - Subscription enforcement and usage tracking

3. **Controllers**
   - `SuperAdminSubscriptionsController` - Subscription management endpoints

4. **Views**
   - Subscription Plans Management
   - Company Subscriptions Management
   - Subscription Usage Widgets
   - Subscription Status Banners

## Features

### ✅ Implemented

#### 1. Subscription Plans
- Create, edit, and manage subscription plans
- Configure pricing (monthly/yearly)
- Set resource limits (users, assets, work orders)
- Activate/deactivate plans
- Track active tenant count per plan

#### 2. Company Subscriptions
- Assign subscriptions to companies
- Track subscription lifecycle (Active, Trial, Expired, Suspended)
- Extend subscription dates
- Manage payment status
- Trial subscription support

#### 3. Plan Enforcement
- User limit checking
- Asset limit checking
- Work order limit checking (monthly)
- Graceful limit warnings

#### 4. Subscription Status Tracking
- Dashboard banners for expiring/expired subscriptions
- Usage widgets showing current consumption
- Expiration warnings (30-day, 7-day for trials)

#### 5. SuperAdmin Dashboard
- Platform-wide subscription metrics
- Expiring subscription alerts
- Subscription plan distribution
- Company subscription overview

## Database Schema

### SubscriptionPlan Table
```sql
- plan_id (PK)
- name
- description
- monthly_price
- yearly_price
- max_users
- max_assets
- max_work_orders_per_month
- features_json
- is_active
- created_at
- updated_at
```

### CompanySubscription Table
```sql
- subscription_id (PK)
- company_id (FK)
- plan_id (FK)
- start_date
- end_date
- is_trial
- is_active
- payment_status
- external_payment_id
- last_payment_date
- created_at
- updated_at
```

## Usage

### For SuperAdmins

#### Managing Subscription Plans

1. Navigate to **Platform → Subscription Plans**
2. Click **Create Plan**
3. Configure:
   - Plan name and description
   - Monthly and yearly pricing
   - Resource limits (users, assets, work orders)
   - Features (JSON format)
4. Activate/deactivate plans as needed

#### Managing Company Subscriptions

1. Navigate to **Platform → Subscriptions**
2. Click **Assign Subscription**
3. Select:
   - Company
   - Subscription plan
   - Start and end dates
   - Trial status
   - Payment status
4. Monitor subscription status and expiration

#### Extending Subscriptions

1. Find the subscription in the list
2. Click the **Extend** button
3. Set new end date
4. Confirm extension

### For Admins (Tenant Level)

#### Viewing Subscription Status

- Dashboard displays subscription usage widget
- Shows current consumption vs. limits
- Displays expiration warnings

#### Understanding Limits

When limits are reached:
- **Users**: Cannot create new users
- **Assets**: Cannot add new assets
- **Work Orders**: Cannot create new work orders (monthly limit)

System displays clear error messages with upgrade prompts.

## API Endpoints

### SuperAdmin Endpoints

#### Subscription Plans
- `GET /superadmin/subscriptions/plans` - List all plans
- `POST /superadmin/subscriptions/plans/create` - Create plan
- `POST /superadmin/subscriptions/plans/{id}/update` - Update plan
- `POST /superadmin/subscriptions/plans/{id}/toggle` - Activate/deactivate

#### Company Subscriptions
- `GET /superadmin/subscriptions` - List all subscriptions
- `POST /superadmin/subscriptions/assign` - Assign subscription
- `POST /superadmin/subscriptions/{id}/extend` - Extend subscription
- `POST /superadmin/subscriptions/{id}/payment-status` - Update payment status

#### Helper APIs
- `GET /api/companies` - Get companies for dropdown
- `GET /api/subscription-plans` - Get plans for dropdown

## Service Methods

### SubscriptionService

```csharp
// Get active subscription
Task<CompanySubscription?> GetActiveSubscriptionAsync(int companyId)

// Enforcement checks
Task<(bool allowed, string? message)> CanAddUserAsync(int companyId)
Task<(bool allowed, string? message)> CanAddAssetAsync(int companyId)
Task<(bool allowed, string? message)> CanCreateWorkOrderAsync(int companyId)

// Status and usage
Task<SubscriptionStatus?> GetSubscriptionStatusAsync(int companyId)
Task<SubscriptionUsage> GetUsageStatsAsync(int companyId)
```

## Integration Points

### Adding Enforcement to Controllers

```csharp
// Example: User creation enforcement
private readonly SubscriptionService _subscriptionService;

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
    
    // Proceed with user creation
}
```

### Adding Subscription Banner to Views

```razor
@* Add to layout or dashboard *@
@await Html.PartialAsync("_SubscriptionBanner")
```

### Adding Usage Widget to Dashboard

```razor
@* Add to admin dashboard *@
@await Html.PartialAsync("_SubscriptionUsageWidget")
```

## Default Subscription Plans

The system seeds three default plans:

### Starter Plan
- **Price**: $999/month, $9,990/year
- **Limits**: 5 users, 50 assets, 100 work orders/month
- **Target**: Small teams

### Professional Plan
- **Price**: $2,499/month, $24,990/year
- **Limits**: 20 users, 200 assets, 500 work orders/month
- **Target**: Growing businesses

### Enterprise Plan
- **Price**: $4,999/month, $49,990/year
- **Limits**: Unlimited users, assets, and work orders
- **Target**: Large organizations

## Future Enhancements (Not Implemented)

### PayMongo Integration
- Automated payment processing
- Webhook handling for payment events
- Automatic subscription renewal
- Payment history tracking

### Advanced Features
- Usage-based billing
- Add-on modules
- Custom plan creation per tenant
- Automated suspension on payment failure
- Self-service plan upgrades
- Billing portal for admins

## Security Considerations

### Authorization
- Only SuperAdmin can manage plans and subscriptions
- Tenant admins can only view their own subscription
- Regular users cannot access subscription data

### Data Isolation
- All subscription checks respect tenant boundaries
- CompanyId validation on all operations
- No cross-tenant subscription visibility

## Troubleshooting

### Subscription Not Showing
1. Verify subscription is active (`is_active = 1`)
2. Check end date is in the future
3. Confirm company has active subscription record

### Limits Not Enforcing
1. Ensure `SubscriptionService` is registered in `Program.cs`
2. Verify enforcement code is added to controllers
3. Check subscription plan has limits configured

### SuperAdmin Cannot Access
1. Verify user has SuperAdmin role
2. Confirm `CompanyId` is NULL for SuperAdmin users
3. Check authorization attributes on controllers

## Migration

### Applying the SaaS Migration

```powershell
# Run the PowerShell script
.\ApplySaaSMigration.ps1
```

Or manually execute:
```sql
-- Run the SQL script
Database/add_saas_architecture.sql
```

### Verification

```sql
-- Check tables exist
SELECT COUNT(*) FROM SubscriptionPlan;
SELECT COUNT(*) FROM CompanySubscription;

-- Check SuperAdmin role
SELECT * FROM AspNetRoles WHERE Name = 'SuperAdmin';

-- Check default plans
SELECT * FROM SubscriptionPlan WHERE is_active = 1;
```

## Best Practices

### Plan Design
- Keep plans simple and clear
- Use NULL for unlimited limits
- Price competitively based on value
- Review plan distribution regularly

### Subscription Management
- Set realistic trial periods (14-30 days)
- Send expiration warnings early (30 days)
- Document payment status changes
- Track external payment IDs

### Enforcement
- Show clear error messages
- Provide upgrade paths
- Don't block critical operations
- Log limit violations for analysis

## Support

For issues or questions:
1. Check this documentation
2. Review controller and service code
3. Verify database schema
4. Check application logs

## Changelog

### Version 1.0 (Current)
- Initial subscription management module
- Subscription plans CRUD
- Company subscription assignment
- Basic plan enforcement
- Usage tracking and widgets
- SuperAdmin dashboard integration
- Subscription status banners

---

**MaintenX SaaS Subscription Management** - Lightweight, operational, and maintainable.
