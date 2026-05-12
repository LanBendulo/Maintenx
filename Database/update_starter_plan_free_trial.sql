-- ============================================================
-- Update Starter Plan to Free 14-Day Trial
-- Matches landing page pricing
-- ============================================================

PRINT '==================================================';
PRINT 'Updating Starter Plan to Free 14-Day Trial';
PRINT '==================================================';
PRINT '';

-- Update Starter plan pricing
UPDATE SubscriptionPlan
SET 
    monthly_price = 0.00,
    yearly_price = 0.00,
    description = 'Perfect for small teams getting started with maintenance management. Free for 14 days.',
    updated_at = GETDATE()
WHERE name = 'Starter';

-- Verify the update
IF @@ROWCOUNT > 0
BEGIN
    PRINT '✓ Starter plan updated successfully';
    PRINT '';
    PRINT 'Updated Plan Details:';
    SELECT 
        name AS [Plan Name],
        description AS [Description],
        monthly_price AS [Monthly Price],
        yearly_price AS [Yearly Price],
        max_users AS [Max Users],
        max_assets AS [Max Assets],
        max_work_orders_per_month AS [Max WOs/Month],
        is_active AS [Active]
    FROM SubscriptionPlan
    WHERE name = 'Starter';
END
ELSE
BEGIN
    PRINT '⚠ Starter plan not found or not updated';
END

PRINT '';
PRINT '==================================================';
PRINT 'Update Complete!';
PRINT '==================================================';
PRINT '';
PRINT 'NOTE: The Starter plan is now FREE for 14 days.';
PRINT 'When assigning this plan, set:';
PRINT '  - IsTrial = true';
PRINT '  - EndDate = StartDate + 14 days';
PRINT '==================================================';
