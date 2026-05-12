# Subscription Enforcement Examples

This document provides code examples for integrating subscription enforcement into MaintenX controllers.

## Setup

### 1. Inject SubscriptionService

Add the service to your controller constructor:

```csharp
private readonly SubscriptionService _subscriptionService;

public YourController(
    ApplicationDbContext context,
    ITenantService tenantService,
    SubscriptionService subscriptionService,
    ILogger<YourController> logger)
{
    _context = context;
    _tenantService = tenantService;
    _subscriptionService = subscriptionService;
    _logger = logger;
}
```

## Enforcement Examples

### Example 1: User Creation Enforcement

```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateUser(CreateUserViewModel model)
{
    // Get current company
    var companyId = _tenantService.GetCurrentCompanyId();
    if (!companyId.HasValue)
    {
        return BadRequest(new { success = false, message = "Company context required" });
    }

    // Check subscription limit
    var (allowed, message) = await _subscriptionService.CanAddUserAsync(companyId.Value);
    if (!allowed)
    {
        return BadRequest(new { 
            success = false, 
            message = message,
            limitReached = true,
            upgradeRequired = true
        });
    }

    // Proceed with user creation
    // ... your existing user creation code ...
    
    return Ok(new { success = true, message = "User created successfully" });
}
```

### Example 2: Asset Creation Enforcement

```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateAsset(CreateAssetViewModel model)
{
    var companyId = _tenantService.GetCurrentCompanyId();
    if (!companyId.HasValue)
    {
        return BadRequest("Company context required");
    }

    // Check asset limit
    var (allowed, message) = await _subscriptionService.CanAddAssetAsync(companyId.Value);
    if (!allowed)
    {
        ModelState.AddModelError("", message ?? "Asset limit reached");
        return View(model);
    }

    // Proceed with asset creation
    var asset = new Asset
    {
        CompanyId = companyId.Value,
        Name = model.Name,
        // ... other properties ...
    };

    _context.Assets.Add(asset);
    await _context.SaveChangesAsync();

    return RedirectToAction("Index");
}
```

### Example 3: Work Order Creation Enforcement

```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateWorkOrder(CreateWorkOrderViewModel model)
{
    var companyId = _tenantService.GetCurrentCompanyId();
    if (!companyId.HasValue)
    {
        return Json(new { success = false, message = "Company context required" });
    }

    // Check work order limit (monthly)
    var (allowed, message) = await _subscriptionService.CanCreateWorkOrderAsync(companyId.Value);
    if (!allowed)
    {
        return Json(new { 
            success = false, 
            message = message,
            limitType = "work_orders",
            upgradeUrl = "/dashboard/subscription"
        });
    }

    // Proceed with work order creation
    var workOrder = new WorkOrder
    {
        CompanyId = companyId.Value,
        Title = model.Title,
        Status = "Open",
        // ... other properties ...
    };

    _context.WorkOrders.Add(workOrder);
    await _context.SaveChangesAsync();

    return Json(new { success = true, workOrderId = workOrder.WorkOrderId });
}
```

### Example 4: Bulk Operation with Limit Check

```csharp
[HttpPost("bulk-import")]
public async Task<IActionResult> BulkImportAssets(List<ImportAssetDto> assets)
{
    var companyId = _tenantService.GetCurrentCompanyId();
    if (!companyId.HasValue)
    {
        return BadRequest("Company context required");
    }

    // Get current usage
    var usage = await _subscriptionService.GetUsageStatsAsync(companyId.Value);
    
    // Check if bulk import would exceed limit
    if (usage.MaxAssets.HasValue)
    {
        var remainingSlots = usage.MaxAssets.Value - usage.AssetCount;
        if (assets.Count > remainingSlots)
        {
            return BadRequest(new {
                success = false,
                message = $"Cannot import {assets.Count} assets. Only {remainingSlots} slots remaining.",
                currentCount = usage.AssetCount,
                maxAllowed = usage.MaxAssets.Value,
                requestedImport = assets.Count
            });
        }
    }

    // Proceed with bulk import
    foreach (var assetDto in assets)
    {
        var asset = new Asset
        {
            CompanyId = companyId.Value,
            Name = assetDto.Name,
            // ... map other properties ...
        };
        _context.Assets.Add(asset);
    }

    await _context.SaveChangesAsync();

    return Ok(new { 
        success = true, 
        imported = assets.Count,
        message = $"Successfully imported {assets.Count} assets"
    });
}
```

## Frontend Integration

### Example 1: Disable Create Button When Limit Reached

```javascript
// Check subscription status before showing create form
async function checkSubscriptionLimit(limitType) {
    try {
        const response = await fetch(`/api/subscription/check-limit?type=${limitType}`);
        const result = await response.json();
        
        if (!result.allowed) {
            // Disable create button
            document.getElementById('createBtn').disabled = true;
            document.getElementById('createBtn').title = result.message;
            
            // Show upgrade prompt
            showUpgradePrompt(result.message);
        }
    } catch (error) {
        console.error('Error checking limit:', error);
    }
}
```

### Example 2: Handle Limit Error on Form Submit

```javascript
async function createUser(formData) {
    try {
        const response = await fetch('/admin/users/create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        });

        const result = await response.json();

        if (!result.success) {
            if (result.limitReached) {
                // Show upgrade modal
                showUpgradeModal({
                    title: 'User Limit Reached',
                    message: result.message,
                    upgradeUrl: '/dashboard/subscription'
                });
            } else {
                // Show regular error
                alert(result.message);
            }
            return;
        }

        // Success handling
        alert('User created successfully!');
        location.reload();
    } catch (error) {
        console.error('Error:', error);
        alert('An error occurred while creating the user.');
    }
}
```

## Dashboard Integration

### Example 1: Add Subscription Banner

In your layout or dashboard view:

```razor
@* Add at the top of main content area *@
@await Html.PartialAsync("_SubscriptionBanner")

@* Your existing dashboard content *@
<div class="dashboard-content">
    <!-- ... -->
</div>
```

### Example 2: Add Usage Widget to Admin Dashboard

```razor
<div class="dashboard-grid">
    <!-- Existing widgets -->
    <div class="widget">
        <!-- Your existing widget -->
    </div>

    <!-- Add subscription usage widget -->
    @await Html.PartialAsync("_SubscriptionUsageWidget")
</div>
```

## API Endpoint for Limit Checking

Add this to your controller for frontend limit checks:

```csharp
[HttpGet("/api/subscription/check-limit")]
public async Task<IActionResult> CheckLimit([FromQuery] string type)
{
    var companyId = _tenantService.GetCurrentCompanyId();
    if (!companyId.HasValue)
    {
        return BadRequest(new { allowed = false, message = "Company context required" });
    }

    (bool allowed, string? message) = type.ToLower() switch
    {
        "user" => await _subscriptionService.CanAddUserAsync(companyId.Value),
        "asset" => await _subscriptionService.CanAddAssetAsync(companyId.Value),
        "workorder" => await _subscriptionService.CanCreateWorkOrderAsync(companyId.Value),
        _ => (false, "Invalid limit type")
    };

    return Ok(new { allowed, message });
}
```

## Best Practices

### 1. Always Check Limits Before Creation
- Check limits at the start of create actions
- Return clear error messages
- Provide upgrade paths

### 2. Handle Limits Gracefully
- Don't throw exceptions for limit violations
- Show user-friendly messages
- Offer alternatives (upgrade, delete old items)

### 3. Check Limits on Both Frontend and Backend
- Frontend: Disable buttons, show warnings
- Backend: Enforce limits, prevent bypass

### 4. Log Limit Violations
```csharp
if (!allowed)
{
    _logger.LogWarning(
        "Subscription limit reached for company {CompanyId}. Type: {LimitType}, Message: {Message}",
        companyId.Value,
        "user",
        message
    );
    return BadRequest(new { success = false, message });
}
```

### 5. Provide Usage Visibility
- Show current usage in dashboards
- Display limits clearly
- Warn before limits are reached (e.g., at 80%, 90%)

## Testing Enforcement

### Test Scenario 1: Reach User Limit
1. Create a test company with Starter plan (5 users max)
2. Create 5 users
3. Attempt to create 6th user
4. Verify error message and enforcement

### Test Scenario 2: Unlimited Plan
1. Assign Enterprise plan (unlimited)
2. Create many users/assets
3. Verify no limits enforced

### Test Scenario 3: Expired Subscription
1. Set subscription end date to past
2. Attempt to create resources
3. Verify appropriate error messages

## Troubleshooting

### Enforcement Not Working
1. Verify `SubscriptionService` is injected
2. Check subscription is active
3. Confirm limits are set in plan
4. Review logs for errors

### False Limit Errors
1. Check current usage counts
2. Verify subscription plan limits
3. Ensure tenant context is correct
4. Review subscription status

---

**Remember**: Subscription enforcement should be helpful, not frustrating. Always provide clear messages and upgrade paths.
