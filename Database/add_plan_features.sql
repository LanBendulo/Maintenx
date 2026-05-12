-- ============================================================
-- Add Features JSON to Subscription Plans
-- Enhances plans with detailed feature lists for landing page
-- ============================================================

PRINT '==================================================';
PRINT 'Adding Features to Subscription Plans';
PRINT '==================================================';
PRINT '';

-- Update Starter Plan Features
UPDATE SubscriptionPlan
SET 
    features_json = '["Work Order Management","Basic Parts Inventory","Asset Tracking","Email Support","Mobile Access"]',
    updated_at = GETDATE()
WHERE name = 'Starter';

PRINT '✓ Starter plan features updated';

-- Update Professional Plan Features
UPDATE SubscriptionPlan
SET 
    features_json = '["Work Order Management","Preventive Maintenance","Parts Inventory Management","Cost Tracking & Reports","Maintenance Requests","Asset Management","Priority Support","Mobile Access","Email Notifications"]',
    updated_at = GETDATE()
WHERE name = 'Professional';

PRINT '✓ Professional plan features updated';

-- Update Enterprise Plan Features
UPDATE SubscriptionPlan
SET 
    features_json = '["All Features Included","Custom Workflows","API Access","Advanced Reporting","Dedicated Account Manager","24/7 Priority Support","Custom Integrations","White-Label Options","SLA Guarantee","Training & Onboarding"]',
    updated_at = GETDATE()
WHERE name = 'Enterprise';

PRINT '✓ Enterprise plan features updated';

PRINT '';
PRINT '==================================================';
PRINT 'Verification';
PRINT '==================================================';

-- Verify the updates
SELECT 
    name AS [Plan Name],
    CASE 
        WHEN monthly_price = 0 THEN 'FREE'
        ELSE '$' + CAST(monthly_price AS VARCHAR(10))
    END AS [Monthly Price],
    CASE 
        WHEN LEN(features_json) > 50 
        THEN LEFT(features_json, 50) + '...'
        ELSE features_json
    END AS [Features Preview],
    LEN(features_json) AS [Features Length]
FROM SubscriptionPlan
WHERE is_active = 1
ORDER BY monthly_price;

PRINT '';
PRINT '==================================================';
PRINT 'Features Added Successfully!';
PRINT '==================================================';
PRINT '';
PRINT 'The landing page will now display these features';
PRINT 'dynamically from the database.';
PRINT '==================================================';
