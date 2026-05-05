-- Add company_id to WorkOrderCost table
USE [db50508];
GO

PRINT 'Adding company_id to WorkOrderCost...';

-- Add the column as NULL first
ALTER TABLE dbo.WorkOrderCost
ADD company_id INT NULL;
GO

-- Populate from Work_Order
UPDATE woc
SET woc.company_id = wo.company_id
FROM dbo.WorkOrderCost woc
INNER JOIN dbo.Work_Order wo ON woc.work_order_id = wo.work_order_id;
GO

-- Make it NOT NULL
ALTER TABLE dbo.WorkOrderCost
ALTER COLUMN company_id INT NOT NULL;
GO

-- Add foreign key
ALTER TABLE dbo.WorkOrderCost
ADD CONSTRAINT FK_WorkOrderCost_Company
FOREIGN KEY (company_id) REFERENCES dbo.Company(company_id);
GO

-- Add index
CREATE NONCLUSTERED INDEX IX_WorkOrderCost_CompanyId_WorkOrderId
ON dbo.WorkOrderCost(company_id, work_order_id);
GO

-- Create WorkOrderCost for existing Work Orders without one
INSERT INTO dbo.WorkOrderCost (company_id, work_order_id, labor_cost, parts_cost, other_cost, total_cost, created_at)
SELECT 
    wo.company_id,
    wo.work_order_id,
    0.00,
    ISNULL((SELECT SUM(wop.quantity_used * ISNULL(wop.unit_cost, 0)) FROM dbo.WorkOrderPart wop WHERE wop.work_order_id = wo.work_order_id), 0.00),
    0.00,
    ISNULL((SELECT SUM(wop.quantity_used * ISNULL(wop.unit_cost, 0)) FROM dbo.WorkOrderPart wop WHERE wop.work_order_id = wo.work_order_id), 0.00),
    GETDATE()
FROM dbo.Work_Order wo
WHERE NOT EXISTS (SELECT 1 FROM dbo.WorkOrderCost woc WHERE woc.work_order_id = wo.work_order_id);
GO

PRINT '✓ company_id added to WorkOrderCost successfully!';
GO
