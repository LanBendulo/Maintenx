-- =============================================================
--  MaintenX Seed Data  (T-SQL / SQL Server)
--  Run AFTER maintenx_schema.sql
--  Purpose: Realistic sample data for development & testing
--  
--  NOTE: User and Role data should be seeded through ASP.NET Identity
--  This file seeds only business data (Assets, Work Orders, etc.)
--  User IDs reference AspNetUsers.Id (NVARCHAR(450))
-- =============================================================

USE DB_Maintenx;
GO

-- =============================================================
-- IMPORTANT: Before running this seed file, ensure you have
-- created users through ASP.NET Identity registration.
-- Update the user IDs below with actual AspNetUsers.Id values.
-- =============================================================

-- Example user IDs (replace with actual values from AspNetUsers table):
-- DECLARE @AdminUserId NVARCHAR(450) = 'your-admin-user-id-here';
-- DECLARE @TechnicianUserId NVARCHAR(450) = 'your-technician-user-id-here';
-- DECLARE @ManagerUserId NVARCHAR(450) = 'your-manager-user-id-here';

-- =============================================================
-- SEED: Category
-- =============================================================
SET IDENTITY_INSERT dbo.Category ON;
INSERT INTO dbo.Category (category_id, category_name) VALUES
    (1, 'HVAC'),
    (2, 'Electrical'),
    (3, 'Plumbing'),
    (4, 'Heavy Equipment'),
    (5, 'IT Infrastructure');
SET IDENTITY_INSERT dbo.Category OFF;
GO

-- =============================================================
-- SEED: Asset
-- =============================================================
SET IDENTITY_INSERT dbo.Asset ON;
INSERT INTO dbo.Asset (asset_id, asset_name, category_id, location, status, purchase_date) VALUES
    (1, 'Air Handling Unit 01',    1, 'Building A - Rooftop',  'Operational',       '2022-03-15'),
    (2, 'Generator Set 01',        2, 'Building B - Basement', 'Operational',       '2021-07-20'),
    (3, 'Water Pump Unit 02',      3, 'Main Utility Room',     'Under Maintenance', '2020-11-10'),
    (4, 'Forklift FL-04',          4, 'Warehouse 2',           'Operational',       '2023-01-05'),
    (5, 'Network Switch Core-01',  5, 'Server Room',           'Operational',       '2024-02-28');
SET IDENTITY_INSERT dbo.Asset OFF;
GO

-- =============================================================
-- SEED: Spare_Part
-- =============================================================
SET IDENTITY_INSERT dbo.Spare_Part ON;
INSERT INTO dbo.Spare_Part (part_id, part_name, stock_quantity, unit_price) VALUES
    (1, 'Fan Belt V-Type',        12,  250.00),
    (2, 'HVAC Air Filter (HEPA)', 30,  850.00),
    (3, 'Mechanical Seal Kit',     8, 1200.00),
    (4, 'Fuel Filter Element',    15,  380.00),
    (5, 'Engine Oil (5L)',        20,  650.00),
    (6, 'Network SFP Module',      5, 4500.00),
    (7, 'Forklift Tire (Solid)',   4, 3200.00);
SET IDENTITY_INSERT dbo.Spare_Part OFF;
GO

-- =============================================================
-- SEED: Inventory_Transaction
-- =============================================================
SET IDENTITY_INSERT dbo.Inventory_Transaction ON;
INSERT INTO dbo.Inventory_Transaction (transaction_id, part_id, quantity, transaction_type, date) VALUES
    (1, 1, 20, 'IN',  '2025-01-15'),
    (2, 2, 50, 'IN',  '2025-01-20'),
    (3, 3, 10, 'IN',  '2025-02-01'),
    (4, 3,  2, 'OUT', '2025-03-24'),
    (5, 4, 25, 'IN',  '2025-02-10'),
    (6, 4,  1, 'OUT', '2025-04-06'),
    (7, 5,  1, 'OUT', '2025-04-06'),
    (8, 6,  8, 'IN',  '2025-03-01'),
    (9, 7,  6, 'IN',  '2025-03-05');
SET IDENTITY_INSERT dbo.Inventory_Transaction OFF;
GO

-- =============================================================
-- NOTE: Work Orders, Maintenance Logs, and Schedules require
-- valid user IDs from AspNetUsers table. 
-- Uncomment and update the sections below after creating users.
-- =============================================================

/*
-- =============================================================
-- SEED: Work_Order
-- Replace NULL with actual AspNetUsers.Id values
-- =============================================================
SET IDENTITY_INSERT dbo.Work_Order ON;
INSERT INTO dbo.Work_Order (work_order_id, asset_id, assigned_to, created_by, status, priority, description, date_created, due_date) VALUES
    (1, 1, NULL, NULL, 'Open',        'High',   'HVAC unit making unusual noise. Inspect fan belt and filters.',        '2025-04-01', '2025-04-05'),
    (2, 2, NULL, NULL, 'In Progress', 'Medium', 'Scheduled quarterly maintenance on Generator Set 01.',                  '2025-04-03', '2025-04-10'),
    (3, 3, NULL, NULL, 'Completed',   'High',   'Water pump leaking — replace seals and check impeller.',               '2025-03-20', '2025-03-25'),
    (4, 4, NULL, NULL, 'Open',        'Low',    'Routine forklift lubrication and tire pressure check.',                '2025-04-15', '2025-04-20'),
    (5, 5, NULL, NULL, 'Completed',   'Medium', 'Network switch firmware upgrade and port diagnostics.',                '2025-03-10', '2025-03-12');
SET IDENTITY_INSERT dbo.Work_Order OFF;
GO

-- =============================================================
-- SEED: Maintenance_Log
-- Replace NULL with actual AspNetUsers.Id values
-- =============================================================
SET IDENTITY_INSERT dbo.Maintenance_Log ON;
INSERT INTO dbo.Maintenance_Log (log_id, asset_id, work_order_id, performed_by, description, date_performed) VALUES
    (1, 3, 3, NULL, 'Replaced mechanical seals. Inspected and cleaned impeller. Pump restored to full capacity.', '2025-03-24'),
    (2, 5, 5, NULL, 'Upgraded firmware to v14.2.1. All 48 ports verified operational. STP re-converged normally.', '2025-03-12'),
    (3, 2, 2, NULL, 'Checked oil levels, replaced fuel filter, tested load bank. Generator passed all load tests.', '2025-04-06');
SET IDENTITY_INSERT dbo.Maintenance_Log OFF;
GO

-- =============================================================
-- SEED: Maintenance_Schedule
-- Replace NULL with actual AspNetUsers.Id values
-- =============================================================
SET IDENTITY_INSERT dbo.Maintenance_Schedule ON;
INSERT INTO dbo.Maintenance_Schedule (schedule_id, asset_id, frequency, next_maintenance_date, created_by) VALUES
    (1, 1, 'Monthly',   '2025-05-01', NULL),
    (2, 2, 'Quarterly', '2025-07-01', NULL),
    (3, 3, 'Bi-Annual', '2025-09-20', NULL),
    (4, 4, 'Monthly',   '2025-05-15', NULL),
    (5, 5, 'Bi-Annual', '2025-09-10', NULL);
SET IDENTITY_INSERT dbo.Maintenance_Schedule OFF;
GO

-- =============================================================
-- SEED: WorkOrder_Parts
-- =============================================================
SET IDENTITY_INSERT dbo.WorkOrder_Parts ON;
INSERT INTO dbo.WorkOrder_Parts (id, work_order_id, part_id, quantity_used) VALUES
    (1, 3, 3, 2),
    (2, 2, 4, 1),
    (3, 2, 5, 1);
SET IDENTITY_INSERT dbo.WorkOrder_Parts OFF;
GO

-- =============================================================
-- SEED: Maintenance_Cost
-- =============================================================
SET IDENTITY_INSERT dbo.Maintenance_Cost ON;
INSERT INTO dbo.Maintenance_Cost (cost_id, work_order_id, labor_cost, parts_cost, total_cost, date_recorded) VALUES
    (1, 3, 1500.00, 2400.00, 3900.00, '2025-03-24'),
    (2, 2,  800.00,  650.00, 1450.00, '2025-04-06'),
    (3, 5,  500.00, 4500.00, 5000.00, '2025-03-12');
SET IDENTITY_INSERT dbo.Maintenance_Cost OFF;
GO
*/

-- =============================================================
-- Seed complete. Run SELECT * FROM dbo.[TableName] to verify.
-- =============================================================
