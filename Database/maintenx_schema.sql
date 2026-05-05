-- =============================================================
--  MaintenX Database Schema
--  Database: SQL Server (T-SQL)
--  Normalization: 3rd Normal Form (3NF)
--  Generated: 2026-04-30
--  Conventions: snake_case, INT IDENTITY(1,1) PKs
--  FK Rules: ON DELETE NO ACTION, ON UPDATE CASCADE
-- =============================================================

-- =============================================================
-- Create database if it doesn't already exist
-- =============================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DB_Maintenx')
BEGIN
    CREATE DATABASE DB_Maintenx;
END
GO

USE DB_Maintenx;
GO

-- =============================================================
-- NOTE: User and Role tables are managed by ASP.NET Core Identity
-- Identity tables: AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
-- Foreign keys in this schema reference Personnel.personnel_id
-- Personnel optionally links to AspNetUsers via user_id
-- =============================================================

-- =============================================================
-- TABLE: Personnel
-- Domain-level workforce data (technicians, contractors, etc.)
-- Can exist with or without a user account
-- =============================================================
IF OBJECT_ID('dbo.Personnel', 'U') IS NULL
CREATE TABLE dbo.Personnel (
    personnel_id    INT             NOT NULL IDENTITY(1,1),
    user_id         NVARCHAR(450)   NULL,  -- Optional FK to AspNetUsers.Id
    first_name      NVARCHAR(100)   NOT NULL,
    last_name       NVARCHAR(100)   NOT NULL,
    role            NVARCHAR(50)    NULL,  -- Job role: Technician, Supervisor, etc.
    skill_set       NVARCHAR(255)   NULL,  -- Skills: HVAC, Electrical, Plumbing, etc.
    hourly_rate     DECIMAL(10,2)   NULL,
    is_active       BIT             DEFAULT 1,
    created_at      DATETIME        DEFAULT GETDATE(),

    CONSTRAINT PK_Personnel PRIMARY KEY (personnel_id),

    CONSTRAINT FK_Personnel_User
        FOREIGN KEY (user_id)
        REFERENCES dbo.AspNetUsers(Id)
        ON DELETE SET NULL  -- If user account deleted, keep personnel record
        ON UPDATE CASCADE
);
GO

-- Index on user_id for lookups
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Personnel_user_id' AND object_id = OBJECT_ID('dbo.Personnel'))
    CREATE INDEX IX_Personnel_user_id ON dbo.Personnel (user_id);
GO

-- Index on is_active for filtering active personnel
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Personnel_is_active' AND object_id = OBJECT_ID('dbo.Personnel'))
    CREATE INDEX IX_Personnel_is_active ON dbo.Personnel (is_active);
GO

-- =============================================================
-- TABLE: Category
-- Referenced by: Asset
-- =============================================================
IF OBJECT_ID('dbo.Category', 'U') IS NULL
CREATE TABLE dbo.Category (
    category_id     INT            NOT NULL IDENTITY(1,1),
    category_name   VARCHAR(100)   NOT NULL,

    CONSTRAINT PK_Category PRIMARY KEY (category_id)
);
GO

-- =============================================================
-- TABLE: Asset
-- References: Category
-- Referenced by: Work_Order, Maintenance_Log, Maintenance_Schedule
-- =============================================================
IF OBJECT_ID('dbo.Asset', 'U') IS NULL
CREATE TABLE dbo.Asset (
    asset_id        INT            NOT NULL IDENTITY(1,1),
    asset_name      VARCHAR(100)   NOT NULL,
    category_id     INT            NULL,
    location        VARCHAR(150)   NULL,
    status          VARCHAR(30)    NULL,
    purchase_date   DATE           NULL,

    CONSTRAINT PK_Asset PRIMARY KEY (asset_id),

    CONSTRAINT FK_Asset_Category
        FOREIGN KEY (category_id)
        REFERENCES dbo.Category (category_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_Asset_category_id' AND object_id = OBJECT_ID('dbo.Asset'))
    CREATE INDEX IX_Asset_category_id ON dbo.Asset (category_id);
GO

-- =============================================================
-- TABLE: Work_Order
-- References: Asset, Personnel (assigned_to), Personnel (created_by)
-- Referenced by: Maintenance_Log, WorkOrder_Parts, Maintenance_Cost
-- =============================================================
IF OBJECT_ID('dbo.Work_Order', 'U') IS NULL
CREATE TABLE dbo.Work_Order (
    work_order_id   INT            NOT NULL IDENTITY(1,1),
    asset_id        INT            NULL,
    assigned_to     INT            NULL,  -- FK to Personnel.personnel_id
    created_by      INT            NULL,  -- FK to Personnel.personnel_id
    status          VARCHAR(30)    NULL,
    priority        VARCHAR(20)    NULL,
    description     VARCHAR(MAX)   NULL,
    date_created    DATE           NULL,
    due_date        DATE           NULL,

    CONSTRAINT PK_Work_Order PRIMARY KEY (work_order_id),

    CONSTRAINT FK_WorkOrder_Asset
        FOREIGN KEY (asset_id)
        REFERENCES dbo.Asset (asset_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE,

    CONSTRAINT FK_WorkOrder_AssignedTo
        FOREIGN KEY (assigned_to)
        REFERENCES dbo.Personnel (personnel_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_WorkOrder_CreatedBy
        FOREIGN KEY (created_by)
        REFERENCES dbo.Personnel (personnel_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WorkOrder_asset_id' AND object_id = OBJECT_ID('dbo.Work_Order'))
    CREATE INDEX IX_WorkOrder_asset_id ON dbo.Work_Order (asset_id);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WorkOrder_assigned_to' AND object_id = OBJECT_ID('dbo.Work_Order'))
    CREATE INDEX IX_WorkOrder_assigned_to ON dbo.Work_Order (assigned_to);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WorkOrder_created_by' AND object_id = OBJECT_ID('dbo.Work_Order'))
    CREATE INDEX IX_WorkOrder_created_by ON dbo.Work_Order (created_by);
GO

-- =============================================================
-- TABLE: Maintenance_Log
-- References: Asset, Work_Order, Personnel (performed_by)
-- =============================================================
IF OBJECT_ID('dbo.Maintenance_Log', 'U') IS NULL
CREATE TABLE dbo.Maintenance_Log (
    log_id          INT            NOT NULL IDENTITY(1,1),
    asset_id        INT            NULL,
    work_order_id   INT            NULL,
    performed_by    INT            NULL,  -- FK to Personnel.personnel_id
    description     VARCHAR(MAX)   NULL,
    date_performed  DATE           NULL,

    CONSTRAINT PK_Maintenance_Log PRIMARY KEY (log_id),

    CONSTRAINT FK_MainLog_Asset
        FOREIGN KEY (asset_id)
        REFERENCES dbo.Asset (asset_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_MainLog_WorkOrder
        FOREIGN KEY (work_order_id)
        REFERENCES dbo.Work_Order (work_order_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_MainLog_PerformedBy
        FOREIGN KEY (performed_by)
        REFERENCES dbo.Personnel (personnel_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainLog_asset_id' AND object_id = OBJECT_ID('dbo.Maintenance_Log'))
    CREATE INDEX IX_MainLog_asset_id ON dbo.Maintenance_Log (asset_id);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainLog_work_order_id' AND object_id = OBJECT_ID('dbo.Maintenance_Log'))
    CREATE INDEX IX_MainLog_work_order_id ON dbo.Maintenance_Log (work_order_id);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainLog_performed_by' AND object_id = OBJECT_ID('dbo.Maintenance_Log'))
    CREATE INDEX IX_MainLog_performed_by ON dbo.Maintenance_Log (performed_by);
GO

-- =============================================================
-- TABLE: Maintenance_Schedule
-- References: Asset, Personnel (created_by)
-- =============================================================
IF OBJECT_ID('dbo.Maintenance_Schedule', 'U') IS NULL
CREATE TABLE dbo.Maintenance_Schedule (
    schedule_id             INT            NOT NULL IDENTITY(1,1),
    asset_id                INT            NULL,
    frequency               VARCHAR(50)    NULL,
    next_maintenance_date   DATE           NULL,
    created_by              INT            NULL,  -- FK to Personnel.personnel_id

    CONSTRAINT PK_Maintenance_Schedule PRIMARY KEY (schedule_id),

    CONSTRAINT FK_MainSched_Asset
        FOREIGN KEY (asset_id)
        REFERENCES dbo.Asset (asset_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_MainSched_CreatedBy
        FOREIGN KEY (created_by)
        REFERENCES dbo.Personnel (personnel_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainSched_asset_id' AND object_id = OBJECT_ID('dbo.Maintenance_Schedule'))
    CREATE INDEX IX_MainSched_asset_id ON dbo.Maintenance_Schedule (asset_id);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainSched_created_by' AND object_id = OBJECT_ID('dbo.Maintenance_Schedule'))
    CREATE INDEX IX_MainSched_created_by ON dbo.Maintenance_Schedule (created_by);
GO

-- =============================================================
-- TABLE: Spare_Part
-- Referenced by: Inventory_Transaction, WorkOrder_Parts
-- =============================================================
IF OBJECT_ID('dbo.Spare_Part', 'U') IS NULL
CREATE TABLE dbo.Spare_Part (
    part_id         INT            NOT NULL IDENTITY(1,1),
    part_name       VARCHAR(100)   NULL,
    stock_quantity  INT            NULL,
    unit_price      DECIMAL(10,2)  NULL,

    CONSTRAINT PK_Spare_Part PRIMARY KEY (part_id)
);
GO

-- =============================================================
-- TABLE: Inventory_Transaction
-- References: Spare_Part
-- =============================================================
IF OBJECT_ID('dbo.Inventory_Transaction', 'U') IS NULL
CREATE TABLE dbo.Inventory_Transaction (
    transaction_id      INT          NOT NULL IDENTITY(1,1),
    part_id             INT          NULL,
    quantity            INT          NULL,
    transaction_type    VARCHAR(10)  NULL,
    date                DATE         NULL,

    CONSTRAINT PK_Inventory_Transaction PRIMARY KEY (transaction_id),

    CONSTRAINT FK_InvTrans_Part
        FOREIGN KEY (part_id)
        REFERENCES dbo.Spare_Part (part_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_InvTrans_part_id' AND object_id = OBJECT_ID('dbo.Inventory_Transaction'))
    CREATE INDEX IX_InvTrans_part_id ON dbo.Inventory_Transaction (part_id);
GO

-- =============================================================
-- TABLE: WorkOrder_Parts  (junction / bridge table)
-- References: Work_Order, Spare_Part
-- =============================================================
IF OBJECT_ID('dbo.WorkOrder_Parts', 'U') IS NULL
CREATE TABLE dbo.WorkOrder_Parts (
    id              INT  NOT NULL IDENTITY(1,1),
    work_order_id   INT  NULL,
    part_id         INT  NULL,
    quantity_used   INT  NULL,

    CONSTRAINT PK_WorkOrder_Parts PRIMARY KEY (id),

    CONSTRAINT FK_WOP_WorkOrder
        FOREIGN KEY (work_order_id)
        REFERENCES dbo.Work_Order (work_order_id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,

    CONSTRAINT FK_WOP_Part
        FOREIGN KEY (part_id)
        REFERENCES dbo.Spare_Part (part_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WOP_work_order_id' AND object_id = OBJECT_ID('dbo.WorkOrder_Parts'))
    CREATE INDEX IX_WOP_work_order_id ON dbo.WorkOrder_Parts (work_order_id);
GO
IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_WOP_part_id' AND object_id = OBJECT_ID('dbo.WorkOrder_Parts'))
    CREATE INDEX IX_WOP_part_id ON dbo.WorkOrder_Parts (part_id);
GO

-- =============================================================
-- TABLE: Maintenance_Cost
-- References: Work_Order
-- =============================================================
IF OBJECT_ID('dbo.Maintenance_Cost', 'U') IS NULL
CREATE TABLE dbo.Maintenance_Cost (
    cost_id         INT            NOT NULL IDENTITY(1,1),
    work_order_id   INT            NULL,
    labor_cost      DECIMAL(10,2)  NULL,
    parts_cost      DECIMAL(10,2)  NULL,
    total_cost      DECIMAL(10,2)  NULL,
    date_recorded   DATE           NULL,

    CONSTRAINT PK_Maintenance_Cost PRIMARY KEY (cost_id),

    CONSTRAINT FK_MainCost_WorkOrder
        FOREIGN KEY (work_order_id)
        REFERENCES dbo.Work_Order (work_order_id)
        ON DELETE NO ACTION
        ON UPDATE CASCADE
);
GO

IF NOT EXISTS (SELECT name FROM sys.indexes WHERE name = N'IX_MainCost_work_order_id' AND object_id = OBJECT_ID('dbo.Maintenance_Cost'))
    CREATE INDEX IX_MainCost_work_order_id ON dbo.Maintenance_Cost (work_order_id);
GO

-- =============================================================
-- Schema creation complete.
-- Run maintenx_seed.sql to populate with sample data.
-- =============================================================
