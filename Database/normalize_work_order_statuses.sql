-- ═══════════════════════════════════════════════════════════════
-- NORMALIZE WORK ORDER STATUSES
-- Standardizes all Work Order status values to canonical constants
-- Safe to run multiple times (idempotent)
-- ═══════════════════════════════════════════════════════════════

USE MaintenX;
GO

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT 'Starting Work Order Status Normalization...';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- Check current status distribution
PRINT 'Current Work Order Status Distribution:';
PRINT '----------------------------------------';
SELECT 
    status,
    COUNT(*) as count
FROM Work_Order
GROUP BY status
ORDER BY count DESC;
PRINT '';

-- Normalize status values
PRINT 'Normalizing status values...';

-- Normalize "Open" to "Pending" (LEGACY MIGRATION)
UPDATE Work_Order 
SET status = 'Pending'
WHERE LOWER(LTRIM(RTRIM(status))) = 'open'
  AND status != 'Pending';

PRINT '✓ Migrated Open → Pending (legacy): ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';

-- Normalize "Pending" variations (already canonical)
UPDATE Work_Order 
SET status = 'Pending'
WHERE LOWER(LTRIM(RTRIM(status))) = 'pending'
  AND status != 'Pending';

PRINT '✓ Normalized Pending variations: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';

-- Normalize "In Progress" variations
UPDATE Work_Order 
SET status = 'In Progress'
WHERE LOWER(LTRIM(RTRIM(status))) IN ('in progress', 'inprogress', 'in-progress')
  AND status != 'In Progress';

PRINT '✓ Normalized In Progress variations: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';

-- Normalize "Completed" variations
UPDATE Work_Order 
SET status = 'Completed'
WHERE LOWER(LTRIM(RTRIM(status))) IN ('completed', 'complete', 'done')
  AND status != 'Completed';

PRINT '✓ Normalized Completed variations: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';

-- Normalize "Cancelled" variations
UPDATE Work_Order 
SET status = 'Cancelled'
WHERE LOWER(LTRIM(RTRIM(status))) IN ('cancelled', 'canceled')
  AND status != 'Cancelled';

PRINT '✓ Normalized Cancelled variations: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';

PRINT '';

-- Check for any non-standard statuses
PRINT 'Checking for non-standard status values...';
DECLARE @NonStandardCount INT;

SELECT @NonStandardCount = COUNT(*)
FROM Work_Order
WHERE status NOT IN ('Pending', 'In Progress', 'Completed', 'Cancelled');

IF @NonStandardCount > 0
BEGIN
    PRINT 'WARNING: Found ' + CAST(@NonStandardCount AS NVARCHAR(10)) + ' work orders with non-standard status values:';
    PRINT '';
    
    SELECT 
        work_order_id,
        status,
        date_created
    FROM Work_Order
    WHERE status NOT IN ('Pending', 'In Progress', 'Completed', 'Cancelled')
    ORDER BY date_created DESC;
    
    PRINT '';
    PRINT 'ACTION REQUIRED: Please review these records and manually update them to valid statuses.';
    PRINT 'Valid statuses: Pending, In Progress, Completed, Cancelled';
END
ELSE
BEGIN
    PRINT '✓ All work orders have valid status values';
END

PRINT '';

-- Final status distribution
PRINT 'Final Work Order Status Distribution:';
PRINT '----------------------------------------';
SELECT 
    status,
    COUNT(*) as count
FROM Work_Order
GROUP BY status
ORDER BY 
    CASE status
        WHEN 'Pending' THEN 1
        WHEN 'In Progress' THEN 2
        WHEN 'Completed' THEN 3
        WHEN 'Cancelled' THEN 4
        ELSE 99
    END;

PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '✓ Work Order Status Normalization Completed Successfully!';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

GO
