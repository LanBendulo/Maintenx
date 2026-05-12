# Dynamic Pricing Implementation

## Overview
The MaintenX landing page pricing section is now **database-driven** and dynamically reflects the actual Subscription Plans managed by SuperAdmin.

## What Changed

### Before
- ❌ Hardcoded pricing cards
- ❌ Static plan information
- ❌ Manual updates required
- ❌ Disconnected from database

### After
- ✅ Database-driven pricing
- ✅ Dynamic plan information
- ✅ Automatic updates
- ✅ Single source of truth

## Architecture

### 1. Data Flow
```
SubscriptionPlan (Database)
    ↓
HomeController.Index()
    ↓
PublicPricingViewModel
    ↓
Landing Page (Views/Home/Index.cshtml)
```

### 2. Components Created

#### A. ViewModel (`Models/ViewModels/PublicPricingViewModel.cs`)
**Purpose**: Safe data transfer object for public display

**Classes**:
- `PublicPricingViewModel` - Container for all plans
- `PublicPlanDto` - Individual plan data with computed properties

**Key Features**:
- No sensitive admin data exposed
- Computed properties for display logic
- Safe for public consumption

**Properties**:
```csharp
public class PublicPlanDto
{
    // Basic Info
    public string Name { get; set; }
    public string? Description { get; set; }
    
    // Pricing
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    
    // Limits
    public int? MaxUsers { get; set; }
    public int? MaxAssets { get; set; }
    public int? MaxWorkOrdersPerMonth { get; set; }
    
    // Features
    public List<string> Features { get; set; }
    
    // Computed Properties
    public bool IsFree { get; }
    public bool IsCustomPricing { get; }
    public bool HasUnlimitedUsers { get; }
    public bool HasUnlimitedAssets { get; }
    public bool HasUnlimitedWorkOrders { get; }
    
    // Display Helpers
    public string DisplayMaxUsers { get; }
    public string DisplayMaxAssets { get; }
    public string DisplayMaxWorkOrders { get; }
}
```

#### B. Controller (`Controllers/HomeController.cs`)
**Enhanced**: Now loads subscription plans from database

**Key Methods**:
- `Index()` - Loads active plans and passes to view
- `ParseFeatures()` - Safely parses features JSON

**Features**:
- Async database query
- Error handling with fallback
- Lightweight projection (no unnecessary data)
- Ordered by price (Free → Paid → Custom)

**Query**:
```csharp
var plans = await _context.SubscriptionPlans
    .Where(p => p.IsActive)
    .OrderBy(p => p.MonthlyPrice)
    .Select(p => new PublicPlanDto { ... })
    .ToListAsync();
```

#### C. View (`Views/Home/Index.cshtml`)
**Updated**: Now uses dynamic data while preserving design

**Features**:
- Loops through `Model.Plans`
- Conditional rendering based on plan type
- Fallback message if no plans exist
- Preserves all existing CSS classes and styling

**Plan Type Detection**:
- **Starter**: Free trial badge, "14 days free" display
- **Professional**: "Most Popular" badge, featured styling
- **Enterprise**: "Custom" pricing, contact sales CTA

### 3. Features JSON Support

#### Format Options
The system supports multiple JSON formats:

**Option 1: Array of Strings**
```json
["Work Order Management","Preventive Maintenance","Cost Tracking"]
```

**Option 2: Object with Keys**
```json
{
  "work_orders": true,
  "preventive_maintenance": true,
  "cost_tracking": true
}
```

#### Parsing Logic
- Tries array format first
- Falls back to object keys
- Returns empty list if parsing fails
- Logs warnings for invalid JSON
- **Never crashes** the page

### 4. Database Migration

#### SQL Script (`Database/add_plan_features.sql`)
Adds feature lists to existing plans:

**Starter Features**:
- Work Order Management
- Basic Parts Inventory
- Asset Tracking
- Email Support
- Mobile Access

**Professional Features**:
- Work Order Management
- Preventive Maintenance
- Parts Inventory Management
- Cost Tracking & Reports
- Maintenance Requests
- Asset Management
- Priority Support
- Mobile Access
- Email Notifications

**Enterprise Features**:
- All Features Included
- Custom Workflows
- API Access
- Advanced Reporting
- Dedicated Account Manager
- 24/7 Priority Support
- Custom Integrations
- White-Label Options
- SLA Guarantee
- Training & Onboarding

#### PowerShell Script (`ApplyPlanFeatures.ps1`)
Applies the features migration to the database.

## Usage

### For SuperAdmin

#### Updating Pricing
1. Navigate to `/superadmin/subscriptions/plans`
2. Edit any plan (name, description, pricing, limits)
3. Save changes
4. **Landing page updates automatically** ✨

#### Adding Features
1. Edit a plan in SuperAdmin
2. Update the `Features (JSON)` field
3. Use array format: `["Feature 1","Feature 2","Feature 3"]`
4. Save changes
5. Features appear on landing page immediately

#### Creating New Plans
1. Create a new plan in SuperAdmin
2. Set `IsActive = true`
3. Plan appears on landing page automatically
4. Order determined by `MonthlyPrice` (ascending)

### For Developers

#### Running the Migration
```powershell
.\ApplyPlanFeatures.ps1
```

#### Testing Locally
1. Ensure database connection is configured
2. Run the application
3. Visit `http://localhost:5262` (or your port)
4. Scroll to pricing section
5. Verify plans display correctly

## Display Logic

### Plan Card Styling
- **Starter (Free)**: Standard card with "Free Trial" badge
- **Professional**: Featured card with "Most Popular" badge
- **Enterprise**: Standard card, "Contact Sales" CTA

### Pricing Display
```
If MonthlyPrice = 0 AND Name contains "Starter":
    → Display "14 days free"
    
If MonthlyPrice = 0 AND Name contains "Enterprise":
    → Display "Custom"
    
Otherwise:
    → Display "$XX/month"
```

### Limits Display
```
If MaxUsers is NULL:
    → Display "Unlimited Users"
    
If MaxUsers has value:
    → Display "Up to X Users"
    
Same logic for Assets and Work Orders
```

### Features Display
```
If FeaturesJson exists and is valid:
    → Display parsed features
    
Otherwise:
    → Display default features based on plan name
```

## Security

### What's Exposed
✅ Plan name
✅ Description
✅ Pricing
✅ Limits
✅ Features

### What's NOT Exposed
❌ Plan ID
❌ Created/Updated timestamps
❌ Internal metadata
❌ Inactive plans
❌ Admin-only fields

### Query Filter
```csharp
.Where(p => p.IsActive)  // Only active plans
```

## Performance

### Optimizations
- **Lightweight Query**: Only selects needed fields
- **No Joins**: No navigation properties loaded
- **Async Loading**: Non-blocking database call
- **Projection**: Maps directly to DTO

### Caching (Future Enhancement)
Consider adding output caching:
```csharp
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<IActionResult> Index()
```

## Error Handling

### Database Errors
- Try-catch in controller
- Logs error to ILogger
- Returns empty plans list
- Shows fallback message

### JSON Parsing Errors
- Try-catch in ParseFeatures()
- Logs warning
- Returns empty features list
- Page continues to render

### No Plans Available
- Displays friendly message
- Preserves page layout
- Shows "Sign Up for Updates" CTA

## Fallback Behavior

### If No Plans Exist
```html
<div class="alert alert-info">
    <h4>Pricing Plans Coming Soon</h4>
    <p>Please check back soon or contact us...</p>
</div>
```

### If Features Missing
- Uses default features based on plan name
- Starter → Basic features
- Professional → Standard features
- Enterprise → Premium features

### If Description Missing
- Uses generic text: "Perfect for your maintenance needs"

## Testing Checklist

### Functional Tests
- [ ] Landing page loads without errors
- [ ] All active plans display correctly
- [ ] Pricing displays correctly (free, paid, custom)
- [ ] Limits display correctly (values and unlimited)
- [ ] Features display correctly
- [ ] Badges display correctly (Free Trial, Most Popular)
- [ ] CTAs work correctly (Sign Up, Contact Sales)
- [ ] Fallback message shows when no plans exist

### SuperAdmin Integration Tests
- [ ] Create new plan → Appears on landing page
- [ ] Edit plan → Changes reflect on landing page
- [ ] Deactivate plan → Disappears from landing page
- [ ] Update features → Features update on landing page
- [ ] Change pricing → Pricing updates on landing page

### Edge Cases
- [ ] Invalid features JSON → Page doesn't crash
- [ ] NULL description → Generic text displays
- [ ] NULL limits → "Unlimited" displays
- [ ] Zero price → Displays correctly based on plan name
- [ ] Database connection error → Fallback message displays

## Benefits

### 1. Single Source of Truth
- Pricing managed in one place (database)
- No code changes needed for pricing updates
- Consistent across admin and public pages

### 2. Real-Time Updates
- SuperAdmin edits → Immediate landing page updates
- No deployment required
- No cache clearing needed

### 3. Maintainability
- No hardcoded values
- Easy to add new plans
- Easy to modify existing plans

### 4. Professional SaaS Experience
- Dynamic pricing like real SaaS platforms
- Flexible plan management
- Enterprise-grade architecture

### 5. Preserved Design
- All existing CSS maintained
- Layout unchanged
- Visual design intact
- Responsive behavior preserved

## Future Enhancements

### 1. Plan Ordering
Add `display_order` column to control plan sequence:
```sql
ALTER TABLE SubscriptionPlan ADD display_order INT DEFAULT 0;
```

### 2. Plan Visibility
Add `show_on_landing_page` flag for more control:
```sql
ALTER TABLE SubscriptionPlan ADD show_on_landing_page BIT DEFAULT 1;
```

### 3. Featured Plan
Add `is_featured` flag instead of name-based detection:
```sql
ALTER TABLE SubscriptionPlan ADD is_featured BIT DEFAULT 0;
```

### 4. Custom CTAs
Add `cta_text` and `cta_url` fields:
```sql
ALTER TABLE SubscriptionPlan ADD cta_text NVARCHAR(50);
ALTER TABLE SubscriptionPlan ADD cta_url NVARCHAR(200);
```

### 5. Plan Icons
Add `icon_class` for custom icons:
```sql
ALTER TABLE SubscriptionPlan ADD icon_class NVARCHAR(50);
```

### 6. Testimonials
Link plans to customer testimonials:
```sql
CREATE TABLE PlanTestimonial (
    testimonial_id INT PRIMARY KEY IDENTITY,
    plan_id INT FOREIGN KEY REFERENCES SubscriptionPlan(plan_id),
    customer_name NVARCHAR(100),
    quote NVARCHAR(500),
    company NVARCHAR(100)
);
```

## Files Modified/Created

### Created
1. `Models/ViewModels/PublicPricingViewModel.cs`
2. `Database/add_plan_features.sql`
3. `ApplyPlanFeatures.ps1`
4. `DYNAMIC_PRICING_IMPLEMENTATION.md`

### Modified
1. `Controllers/HomeController.cs`
2. `Views/Home/Index.cshtml`

## Migration Steps

### Step 1: Apply Features Migration
```powershell
.\ApplyPlanFeatures.ps1
```

### Step 2: Restart Application
```powershell
# Stop the application
# Rebuild if needed
dotnet build

# Start the application
dotnet run
```

### Step 3: Verify
1. Visit landing page
2. Scroll to pricing section
3. Verify all plans display correctly
4. Test SuperAdmin plan editing

## Troubleshooting

### Issue: Plans Not Displaying
**Check**:
1. Are plans marked as `IsActive = true`?
2. Is database connection working?
3. Check browser console for errors
4. Check application logs

### Issue: Features Not Displaying
**Check**:
1. Is `features_json` valid JSON?
2. Check application logs for parsing warnings
3. Verify features migration was applied

### Issue: Styling Broken
**Check**:
1. Verify CSS files are loading
2. Check for JavaScript errors
3. Verify Bootstrap icons are loading

### Issue: Database Connection Error
**Check**:
1. Connection string in `appsettings.json`
2. Database server is running
3. Credentials are correct
4. Firewall allows connection

## Support

For issues or questions:
1. Check application logs
2. Review this documentation
3. Test in SuperAdmin first
4. Verify database state

## Conclusion

The landing page pricing section is now a **professional, database-driven component** that:
- ✅ Automatically reflects SuperAdmin changes
- ✅ Maintains visual design integrity
- ✅ Handles errors gracefully
- ✅ Provides single source of truth
- ✅ Scales with business needs

**The MaintenX landing page now functions like a real enterprise SaaS platform!** 🚀
