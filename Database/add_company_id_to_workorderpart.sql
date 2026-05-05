-- Add company_id to WorkOrderPart table
USE [db50508];
GO

PRINT 'Adding company_id to WorkOrderPart...';

-- Add the column as NULL first
ALTER TABLE dbo.WorkOrderPart
ADD company_id INT NULL;
GO

-- Populate from Work_Order
UPDATE wop
SET wop.company_id = wo.company_id
FROM dbo.WorkOrderPart wop
INNER JOIN dbo.Work_Order wo ON wop.work_order_id = wo.work_order_id;
GO

-- Make it NOT NULL
ALTER TABLE dbo.WorkOrderPart
ALTER COLUMN company_id INT NOT NULL;
GO

-- Add foreign key
ALTER TABLE dbo.WorkOrderPart
ADD CONSTRAINT FK_WorkOrderPart_Company
FOREIGN KEY (company_id) REFERENCES dbo.Company(company_id);
GO

-- Add indexes
CREATE NONCLUSTERED INDEX IX_WorkOrderPart_CompanyId_WorkOrderId
ON dbo.WorkOrderPart(company_id, work_order_id);
GO

CREATE NONCLUSTERED INDEX IX_WorkOrderPart_CompanyId_PartId
ON dbo.WorkOrderPart(company_id, part_id);
GO

PRINT '✓ company_id added to WorkOrderPart successfully!';
GO
